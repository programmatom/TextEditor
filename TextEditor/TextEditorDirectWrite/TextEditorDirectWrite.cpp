/*
 *  Copyright © 1992-2002, 2015 Thomas R. Lawrence
 * 
 *  GNU General Public License
 * 
 *  This file is part of "Text Editor"
 * 
 *  "Text Editor" is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
*/

#include "stdafx.h"

#include "TextEditorDirectWrite.h"

namespace TextEditor
{
	//

	//template <class T> void SafeRelease(T **ppT)
	//{
	//	if (*ppT)
	//	{
	//		(*ppT)->Release();
	//		*ppT = NULL;
	//	}
	//}

	template <class T> void SafeRelease(interior_ptr<T*> ppT)
	{
		pin_ptr<T*> ppT_pin = ppT;
		if (*ppT_pin)
		{
			(*ppT_pin)->Release();
			*ppT_pin = NULL;
		}
	}


	//

	TextServiceDirectWriteInterop::TextServiceDirectWriteInterop()
	{
	}

	TextServiceDirectWriteInterop::~TextServiceDirectWriteInterop()
	{
		_Dispose();
	}

	TextServiceDirectWriteInterop::!TextServiceDirectWriteInterop()
	{
		_Dispose();
	}
	
	HRESULT TextServiceDirectWriteInterop::Reset(
		Font^ font,
		int visibleWidth)
	{
		ClearCaches();

		int hr;

		if (this->factory == NULL)
		{
			IDWriteFactory* factory;
			hr = DWriteCreateFactory(
				DWRITE_FACTORY_TYPE_ISOLATED,
				__uuidof(IDWriteFactory),
				reinterpret_cast<IUnknown**>(&factory));
			if (FAILED(hr))
			{
				return hr;
			}
			this->factory = factory;
		}


		IDWriteFontCollection* fontCollection = NULL;
		IDWriteFontCollection* fontCollection2 = NULL;
		IDWriteGdiInterop* gdiInterop = NULL;
		IDWriteFont* fontD = NULL;
		IDWriteFontFamily* family = NULL;
		IDWriteLocalizedStrings* familyNames = NULL;
		IDWriteRenderingParams* renderingParams = NULL;
		IDWriteBitmapRenderTarget* renderTarget = NULL;
		IDWriteTextFormat* textFormat = NULL;

		if (TextServiceFontEnumerator::fonts.GetSize() != 0)
		{
			// For some reason I don't understand, DWrite is losing track of the registered loader sometimes, but not
			// always, so for now, always try to register and ignore the "already registered" error.
			if (customFontCollectionLoader == NULL)
			{
				customFontCollectionLoader = new TextServiceFontCollectionLoader();
			}
			hr = factory->RegisterFontCollectionLoader(
				customFontCollectionLoader);
			if (FAILED(hr) && (hr != DWRITE_E_ALREADYREGISTERED))
			{
				goto Error;
			}
			if (customFontFileLoader == NULL)
			{
				customFontFileLoader = new TextServiceFontLoader();
			}
			hr = factory->RegisterFontFileLoader(
				customFontFileLoader);
			if (FAILED(hr) && (hr != DWRITE_E_ALREADYREGISTERED))
			{
				goto Error;
			}
		}

		hr = factory->GetSystemFontCollection(
			&fontCollection,
			false/*checkForUpdates*/);
		if (FAILED(hr))
		{
			goto Error;
		}

		hr = factory->GetGdiInterop(&gdiInterop);
		if (FAILED(hr))
		{
			goto Error;
		}

		hr = factory->CreateRenderingParams(
			&renderingParams); // "default settings for the primary monitor"
		if (FAILED(hr))
		{
			goto Error;
		}


		//lineHeight = font.Height;
		this->visibleWidth = visibleWidth;
		this->lineHeight = font->Height;
		if (visibleWidth < 1)
		{
			// if caller uses ClientSize.Width and window gets minimized, it becomes 0 which Bitmap constructor rejects
			visibleWidth = 1;
		}
		_ASSERT(visibleWidth > 0);
		_ASSERT(lineHeight > 0);
		Image^ offscreenStrip = gcnew Bitmap(visibleWidth, lineHeight, System::Drawing::Imaging::PixelFormat::Format32bppArgb);
		try
		{
			Graphics^ offscreenGraphics = Graphics::FromImage(offscreenStrip);
			try
			{
				IntPtr hdc = offscreenGraphics->GetHdc();
				try
				{
					// GDI interop: https://msdn.microsoft.com/en-us/library/windows/desktop/dd742734(v=vs.85).aspx
					// Render to a GDI surface: https://msdn.microsoft.com/en-us/library/windows/desktop/ff485856(v=vs.85).aspx
					hr = gdiInterop->CreateBitmapRenderTarget(
						(HDC)hdc.ToInt64(),
						visibleWidth,
						lineHeight,
						&renderTarget);
					if (FAILED(hr))
					{
						goto Error;
					}
				}
				finally
				{
					offscreenGraphics->ReleaseHdc();
				}
			}
			finally
			{
				delete offscreenGraphics; // Dispose()
			}
		}
		finally
		{
			delete offscreenStrip; // Dispose()
		}

#if 0
		// TODO: this will do the wrong thing since line heights are determined from font size in the winforms
		// code and then imposed upon this code - on non-standard devices text would overflow line or be too small.
#endif
		HDC hdcScreen = GetDC(NULL);
		int dpiX = GetDeviceCaps(hdcScreen, LOGPIXELSX);
		int dpiY = GetDeviceCaps(hdcScreen, LOGPIXELSY);
		rdpiX = dpiX / (float)96; // TODO: figure out correct thing to put here
		rdpiY = dpiY / (float)96; // TODO: figure out correct thing to put here
		hr = renderTarget->SetPixelsPerDip(rdpiY);
		if (FAILED(hr))
		{
			goto Error;
		}

		LOGFONTW logFontStack;
		{
			array<byte>^ pLogFont = gcnew array<byte>(sizeof(LOGFONTW));
			font->ToLogFont((Object^)pLogFont);
			pin_ptr<byte> logFontPinned = &(pLogFont[0]);
			for (int i = 0; i < sizeof(LOGFONTW); i++)
			{
				((byte*)&logFontStack)[i] = logFontPinned[i];
			}
		}

		// DirectWrite point size scaling
		const float Scaling = (float)96 / 72;
		float fontSize = Math::Abs(font->Size * Scaling);

		// first, try the custom font collection
		IDWriteFontCollection* fontCollectionActual = NULL; // weak ref; NULL means system collection
		if (TextServiceFontEnumerator::fonts.GetSize() != 0)
		{
			DWORD key = 0; // const key: we only ever have one custom font collection
			hr = factory->CreateCustomFontCollection(
				customFontCollectionLoader,
				&key,
				sizeof(DWORD),
				&fontCollection2);
			if (FAILED(hr))
			{
				goto Error;
			}
			UINT32 index;
			BOOL exists;
			hr = fontCollection2->FindFamilyName(
				logFontStack.lfFaceName,
				&index,
				&exists);
			if (FAILED(hr))
			{
				goto Error;
			}
			if (exists)
			{
				hr = fontCollection2->GetFontFamily(
					index,
					&family);
				if (FAILED(hr))
				{
					goto Error;
				}
				hr = family->GetFirstMatchingFont(
					DWRITE_FONT_WEIGHT_NORMAL,
					DWRITE_FONT_STRETCH_NORMAL,
					DWRITE_FONT_STYLE_NORMAL,
					&fontD);
				if (FAILED(hr))
				{
					goto Error;
				}
				fontCollectionActual = fontCollection2;
			}
		}
		// if font wasn't found in custom collection, try asking GDI
		if (fontCollectionActual == NULL)
		{
			_ASSERT(fontD == NULL);
			hr = gdiInterop->CreateFontFromLOGFONT(
				&logFontStack,
				&fontD);
			if (FAILED(hr))
			{
				goto Error;
			}
		}

		DWRITE_FONT_METRICS fontMetrics;
		fontD->GetMetrics(&fontMetrics);
		baseline = fontMetrics.ascent * fontSize / fontMetrics.designUnitsPerEm;

		hr = fontD->GetFontFamily(&family);
		if (FAILED(hr))
		{
			goto Error;
		}
		hr = family->GetFamilyNames(&familyNames);
		if (FAILED(hr))
		{
			goto Error;
		}
		// Get the family name at index zero. If we were going to display the name
		// we'd want to try to find one that matched the use locale, but for purposes
		// of creating a text format object any language will do.
		wchar_t familyName[256];
		hr = familyNames->GetString(0, familyName, 256);
		if (FAILED(hr))
		{
			goto Error;
		}

		wchar_t locale[256];
		{
			pin_ptr<const wchar_t> name = PtrToStringChars(CultureInfo::CurrentCulture->Name);
			wcsncpy_s(locale, 256, name, CultureInfo::CurrentCulture->Name->Length);
		}

		SafeRelease(&textFormat);
		hr = factory->CreateTextFormat(
			familyName,
			fontCollectionActual, // NULL: system font collection
			fontD->GetWeight(),
			fontD->GetStyle(),
			fontD->GetStretch(),
			fontSize,
			locale,
			&textFormat);
		if (FAILED(hr))
		{
			goto Error;
		}
		hr = textFormat->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);
		if (FAILED(hr))
		{
			goto Error;
		}
		hr = textFormat->SetLineSpacing(DWRITE_LINE_SPACING_METHOD_UNIFORM, fontSize, baseline);
		if (FAILED(hr))
		{
			goto Error;
		}
#if 0
		DWRITE_TRIMMING trimming = new DWRITE_TRIMMING();
		trimming.granularity = DWRITE_TRIMMING_GRANULARITY.DWRITE_TRIMMING_GRANULARITY_NONE;
		hr = textFormat.SetTrimming(trimming, null);
		if (FAILED(hr))
		{
			goto Error;
		}
		// TODO: hr = textFormat.SetReadingDirection(DWRITE_READING_DIRECTION.);
		hr = textFormat.SetTextAlignment(DWRITE_TEXT_ALIGNMENT.DWRITE_TEXT_ALIGNMENT_LEADING);
		if (FAILED(hr))
		{
			goto Error;
		}
#endif


		// Success

		this->renderingParams = renderingParams;
		renderingParams = NULL;

		this->renderTarget = renderTarget;
		renderTarget = NULL;

		this->textFormat = textFormat;
		textFormat = NULL;


	Error:

		SafeRelease(&textFormat);
		SafeRelease(&renderTarget);
		SafeRelease(&renderingParams);
		SafeRelease(&familyNames);
		SafeRelease(&family);
		SafeRelease(&fontD);
		SafeRelease(&fontCollection);
		SafeRelease(&fontCollection2);
		SafeRelease(&gdiInterop);

		return hr;
	}

	void TextServiceDirectWriteInterop::_Dispose()
	{
		ClearCaches();

		SafeRelease(&factory);
	}

	void TextServiceDirectWriteInterop::ClearCaches()
	{
		SafeRelease(&textFormat);
		SafeRelease(&renderTarget);
		SafeRelease(&renderingParams);
	}


	//

	TextServiceLineDirectWriteInterop::TextServiceLineDirectWriteInterop()
	{
	}

	TextServiceLineDirectWriteInterop::~TextServiceLineDirectWriteInterop()
	{
		_Dispose();
	}

	TextServiceLineDirectWriteInterop::!TextServiceLineDirectWriteInterop()
	{
		_Dispose();
	}

	HRESULT TextServiceLineDirectWriteInterop::Init(
		TextServiceDirectWriteInterop^ service,
		String^ line)
	{
		int hr;

		if (line == nullptr)
		{
			line = String::Empty;
		}

		this->service = service;
		totalChars = line->Length;

		IDWriteTextLayout* textLayout = NULL;

		wchar_t* pwzLine = new wchar_t[line->Length + 1];
		pin_ptr<const wchar_t> wzLine = PtrToStringChars(line);
		wcsncpy_s(pwzLine, line->Length + 1, wzLine, line->Length);
#if 1 // choose
		hr = service->factory->CreateTextLayout(
			pwzLine,
			line->Length,
			service->textFormat,
			(float)50, // layout width - shouldn't matter with DWRITE_WORD_WRAPPING_NO_WRAP specified
			(float)service->lineHeight,
			&textLayout);
#else
		hr = service->factory->CreateGdiCompatibleTextLayout(
			pwzLine,
			line->Length,
			service->textFormat,
			(float)50, // layout width - shouldn't matter with DWRITE_WORD_WRAPPING_NO_WRAP specified
			(float)service->lineHeight,
			service->rdpiY/*pixelsPerDip*/,
			NULL, // current DWRITE_MATRIX transform
			true/*useGdiNatural*/,
			&textLayout);
#endif
		if (FAILED(hr))
		{
			goto Error;
		}

		this->textLayout = textLayout;
		textLayout = NULL;

	Error:

		SafeRelease(&textLayout);

		delete pwzLine;

		return hr;
	}

	void TextServiceLineDirectWriteInterop::_Dispose()
	{
		SafeRelease(&textLayout);
	}

	HRESULT TextServiceLineDirectWriteInterop::DrawText(
		Graphics^ graphics_,
		Point position,
		Color foreColor,
		Color backColor)
	{
		int hr;

		if (totalChars == 0)
		{
			return S_OK;
		}

		this->foreColor = (unsigned int)(
			(unsigned int)foreColor.R
			| ((unsigned int)foreColor.G << 8)
			| ((unsigned int)foreColor.B << 16));

		HRGN hrgnClip = (HRGN)graphics_->Clip->GetHrgn(graphics_).ToInt64();
		try
		{
			HDC hdc = (HDC)graphics_->GetHdc().ToInt64();
			try
			{
				// Graphics/GDI+ doesn't pass clip region through so we have to reset it explicitly
				SelectClipRgn(hdc, hrgnClip);

				HDC hdcMem = service->renderTarget->GetMemoryDC();

				Graphics^ graphics2 = Graphics::FromHdc((IntPtr)hdcMem);
				try
				{
					Brush^ backBrush = gcnew SolidBrush(backColor);
					try
					{
						graphics2->FillRectangle(
							backBrush,
							0,
							0,
							service->visibleWidth,
							service->lineHeight);
					}
					finally
					{
						delete backBrush;
					}
				}
				finally
				{
					delete graphics2;
				}

				TextServiceLineDirectWriteInterop2* wrapper = new TextServiceLineDirectWriteInterop2();
				hr = wrapper->Init(
					service->rdpiY,
					service->renderTarget,
					service->renderingParams,
					this->foreColor);
				if (FAILED(hr))
				{
					goto Error;
				}
				hr = textLayout->Draw(
					(void*)0, // client context
					wrapper, // IDWriteTextRenderer
					(float)position.X,
					(float)position.Y /*+ baseline*/);
				delete wrapper;
				if (FAILED(hr))
				{
					goto Error;
				}

				BitBlt(
					hdc,
					0,
					0,
					service->visibleWidth,
					service->lineHeight,
					hdcMem,
					0,
					0,
					SRCCOPY);
			}
			finally
			{
				graphics_->ReleaseHdc();
			}
		}
		finally
		{
			DeleteObject(hrgnClip);
		}

	Error:
		return hr;
	}

	HRESULT TextServiceLineDirectWriteInterop::DrawTextWithRenderTarget(
		Graphics^ graphics_,
		System::Drawing::Rectangle bounds,
		Color foreColor,
		Color backColor)
	{
		int hr;

		if (totalChars == 0)
		{
			return S_OK;
		}

		this->foreColor = (unsigned int)(
			(unsigned int)foreColor.R
			| ((unsigned int)foreColor.G << 8)
			| ((unsigned int)foreColor.B << 16));

		HRGN hrgnClip = (HRGN)graphics_->Clip->GetHrgn(graphics_).ToInt64();
		try
		{
			HDC hdc = (HDC)graphics_->GetHdc().ToInt64();
			try
			{
				// Graphics/GDI+ doesn't pass clip region through so we have to reset it explicitly
				SelectClipRgn(hdc, hrgnClip);

				HDC hdcMem = service->renderTarget->GetMemoryDC();

				if (backColor == Color::Transparent)
				{
					BitBlt(
						hdcMem,
						0,
						0,
						bounds.Width,
						bounds.Height,
						hdc,
						bounds.Left,
						bounds.Top,
						SRCCOPY);
				}
				else
				{
					Graphics^ graphics2 = Graphics::FromHdc((IntPtr)hdcMem);
					try
					{
						Brush^ backBrush = gcnew SolidBrush(backColor);
						try
						{
							graphics2->FillRectangle(
								backBrush,
								0,
								0,
								service->visibleWidth,
								service->lineHeight);
						}
						finally
						{
							delete backBrush;
						}
					}
					finally
					{
						delete graphics2;
					}
				}

				TextServiceLineDirectWriteInterop2* wrapper = new TextServiceLineDirectWriteInterop2();
				hr = wrapper->Init(
					service->rdpiY,
					service->renderTarget,
					service->renderingParams,
					this->foreColor);
				if (FAILED(hr))
				{
					goto Error;
				}
				hr = textLayout->Draw(
					(void*)0, // client context
					wrapper, // IDWriteTextRenderer
					0,
					0);
				delete wrapper;
				if (FAILED(hr))
				{
					goto Error;
				}

				BitBlt(
					hdc,
					bounds.Left,
					bounds.Top,
					bounds.Width,
					bounds.Height,
					hdcMem,
					0,
					0,
					SRCCOPY);
			}
			finally
			{
				graphics_->ReleaseHdc();
			}
		}
		finally
		{
			DeleteObject(hrgnClip);
		}

	Error:
		return hr;
	}

	HRESULT TextServiceLineDirectWriteInterop::BuildRegion(
		Graphics^ graphics,
		Point position,
		int startPos,
		int endPosPlusOne,
		[Out] Region^ %regionOut)
	{
		int hr = S_OK;

		regionOut = nullptr;

		unsigned int cMaxMetrics = 8;
		DWRITE_HIT_TEST_METRICS* metrics = NULL;
		unsigned int cMetrics;
		while (true)
		{
			if (metrics != NULL)
			{
				delete metrics;
			}
			metrics = new DWRITE_HIT_TEST_METRICS[cMaxMetrics];
			int hr = textLayout->HitTestTextRange(
				startPos,
				(endPosPlusOne - startPos),
				(float)position.X,
				(float)position.Y /*+ service.baseline*/,
				metrics,
				cMaxMetrics,
				&cMetrics);
			if (hr == E_NOT_SUFFICIENT_BUFFER)
			{
				cMaxMetrics *= 2;
				continue;
			}
			if (FAILED(hr))
			{
				goto Error;
			}
			break;
		}

		Region^ region = gcnew Region(System::Drawing::Rectangle());
		for (unsigned int i = 0; i < cMetrics; i++)
		{
			RectangleF rectF(
				metrics[i].left,
				metrics[i].top,
				metrics[i].width,
				metrics[i].height);
			//rectF.Y -= service.baseline;
			int X = (int)Math::Round(rectF.Left * this->service->rdpiY); // rounding must match CharPosToX()
			//int Y = (int)Math::Floor(rectF.Top);
			int Y = 0;
			int Width = (int)Math::Round((rectF.Width + rectF.Left) * this->service->rdpiY - X); // rounding must match CharPosToX()
			//int Height = (int)Math::Ceiling(rectF.Height + rectF.Top - Y);
			int Height = service->lineHeight;
			region->Union(System::Drawing::Rectangle(X, Y, Width, Height));
		}

		regionOut = region;

	Error:

		if (metrics != NULL)
		{
			delete metrics;
		}

		return hr;
	}

	HRESULT TextServiceLineDirectWriteInterop::GetExtent(
		Graphics^ graphics,
		[Out] Size %sizeOut)
	{
		int hr = S_OK;

		DWRITE_TEXT_METRICS metrics;
		hr = textLayout->GetMetrics(&metrics);
		if (FAILED(hr))
		{
			goto Error;
		}
		_ASSERT(metrics.lineCount <= 1);

		int width = (int)Math::Round(metrics.widthIncludingTrailingWhitespace * this->service->rdpiY); // rounding must match CharPosToX(), BuildRegion()

		// Note: height can increase when non-western scripts are introduced (e.g. Arabic and or Devanagari)
		// and font substitution decides it needs more space to comfortably display the text,
		// unless DWRITE_LINE_SPACING_METHOD_UNIFORM is specified. In future, could adapt line display using
		// actual metrics. For now, since this is a code-oriented edit control, compromising the spacing match
		// is ok.
		int height = service->lineHeight;
		//int height = (int)Math::Ceiling(metrics.height);

		sizeOut = Size(width, height);

	Error:
		return hr;
	}

	HRESULT TextServiceLineDirectWriteInterop::CharPosToX(
		Graphics^ graphics,
		int offset,
		bool trailing,
		[Out] int %x)
	{
		int hr = S_OK;

		float x1, y1;
		DWRITE_HIT_TEST_METRICS metrics;
		hr = textLayout->HitTestTextPosition(
			offset,
			trailing,
			&x1,
			&y1,
			&metrics);
		if (FAILED(hr))
		{
			goto Error;
		}

		x = (int)Math::Round(x1 * this->service->rdpiY); // rounding must match BuildRegion()

	Error:
		return hr;
	}

	HRESULT TextServiceLineDirectWriteInterop::XToCharPos(
		Graphics^ graphics,
		int x,
		[Out] int %offset,
		[Out] bool %trailing)
	{
		int hr = S_OK;

		BOOL inside, trailing1;
		DWRITE_HIT_TEST_METRICS metric;
		hr = textLayout->HitTestPoint(
			(float)(x / this->service->rdpiY),
			(float)0,
			&trailing1,
			&inside,
			&metric);
		if (FAILED(hr))
		{
			goto Error;
		}

		trailing = trailing1 != 0;
		offset = (int)metric.textPosition;
		if (trailing)
		{
			offset += metric.length;
		}

	Error:
		return hr;
	}


	//

	TextServiceLineDirectWriteInterop2::TextServiceLineDirectWriteInterop2()
	{
	}

	HRESULT TextServiceLineDirectWriteInterop2::Init(
		float rdpiY,
		IDWriteBitmapRenderTarget* renderTarget,
		IDWriteRenderingParams* renderingParams,
		COLORREF foreColor)
	{
		this->refCount = 1;
		this->rdpiY = rdpiY;
		this->renderTarget = renderTarget;
		this->renderingParams = renderingParams;
		this->foreColor = foreColor;
		return S_OK;
	}

	unsigned long STDMETHODCALLTYPE TextServiceLineDirectWriteInterop2::AddRef()
	{
		return InterlockedIncrement(&refCount);
	}

	unsigned long STDMETHODCALLTYPE TextServiceLineDirectWriteInterop2::Release()
	{
		if (InterlockedDecrement(&refCount) == 0)
		{
			delete this;
			return 0;
		}
		return refCount;
	}

	HRESULT TextServiceLineDirectWriteInterop2::QueryInterface(
		IID const& riid,
		void** ppvObject)
	{
		if (__uuidof(IUnknown) == riid)
		{
			*ppvObject = static_cast<IUnknown*>(this);
		}
		else if (__uuidof(IDWritePixelSnapping) == riid)
		{
			*ppvObject = static_cast<IDWritePixelSnapping*>(this);
		}
		else if (__uuidof(IDWriteTextRenderer) == riid)
		{
			*ppvObject = static_cast<IDWriteTextRenderer*>(this);
		}
		else
		{
			*ppvObject = NULL;
			return E_NOINTERFACE;
		}
		InterlockedIncrement(&refCount);
		return S_OK;
	}

	HRESULT TextServiceLineDirectWriteInterop2::IsPixelSnappingDisabled(
		__maybenull void* clientDrawingContext,
		__out BOOL* isDisabled)
	{
		*isDisabled = false;
		return S_OK;
	}

	HRESULT TextServiceLineDirectWriteInterop2::GetCurrentTransform(
		__maybenull void* clientDrawingContext,
		__out DWRITE_MATRIX* transform)
	{
		transform->m11 = 1;
		transform->m12 = 0;
		transform->m21 = 0;
		transform->m22 = 1;
		transform->dx = 0;
		transform->dy = 0;
		return S_OK;
	}

	HRESULT TextServiceLineDirectWriteInterop2::GetPixelsPerDip(
		__maybenull void* clientDrawingContext,
		__out FLOAT* pixelsPerDip)
	{
		*pixelsPerDip = rdpiY;
		return S_OK;
	}

	HRESULT TextServiceLineDirectWriteInterop2::DrawGlyphRun(
		__maybenull void* clientDrawingContext,
		FLOAT baselineOriginX,
		FLOAT baselineOriginY,
		DWRITE_MEASURING_MODE measuringMode,
		__in DWRITE_GLYPH_RUN const* glyphRun,
		__in DWRITE_GLYPH_RUN_DESCRIPTION const* glyphRunDescription,
		__maybenull IUnknown* clientDrawingEffect)
	{
		RECT bb;

		int hr = renderTarget->DrawGlyphRun(
			baselineOriginX,
			baselineOriginY,
			measuringMode,
			glyphRun,
			renderingParams,
			foreColor,
			&bb);

		return hr;
	}

	HRESULT TextServiceLineDirectWriteInterop2::DrawUnderline(
		__maybenull void* clientDrawingContext,
		FLOAT baselineOriginX,
		FLOAT baselineOriginY,
		__in DWRITE_UNDERLINE const* underline,
		__maybenull IUnknown* clientDrawingEffect)
	{
		return E_NOTIMPL;
	}

	HRESULT TextServiceLineDirectWriteInterop2::DrawStrikethrough(
		__maybenull void* clientDrawingContext,
		FLOAT baselineOriginX,
		FLOAT baselineOriginY,
		__in DWRITE_STRIKETHROUGH const* strikethrough,
		__maybenull IUnknown* clientDrawingEffect)
	{
		return E_NOTIMPL;
	}

	HRESULT TextServiceLineDirectWriteInterop2::DrawInlineObject(
		__maybenull void* clientDrawingContext,
		FLOAT originX,
		FLOAT originY,
		IDWriteInlineObject* inlineObject,
		BOOL isSideways,
		BOOL isRightToLeft,
		__maybenull IUnknown* clientDrawingEffect)
	{
		return E_NOTIMPL;
	}


	//

	// https://msdn.microsoft.com/en-us/library/windows/desktop/ee264312%28v=vs.85%29.aspx

	//

	TextServiceFontCollectionLoader::TextServiceFontCollectionLoader()
	{
		this->refCount = 1;
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontCollectionLoader::AddRef()
	{
		return InterlockedIncrement(&refCount);
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontCollectionLoader::Release()
	{
		if (InterlockedDecrement(&refCount) == 0)
		{
			delete this;
			return 0;
		}
		return refCount;
	}

	HRESULT TextServiceFontCollectionLoader::QueryInterface(
		IID const& riid,
		void** ppvObject)
	{
		if (__uuidof(IUnknown) == riid)
		{
			*ppvObject = static_cast<IUnknown*>(this);
		}
		else if (__uuidof(IDWriteFontCollectionLoader) == riid)
		{
			*ppvObject = static_cast<IDWriteFontCollectionLoader*>(this);
		}
		else
		{
			*ppvObject = NULL;
			return E_NOINTERFACE;
		}
		InterlockedIncrement(&refCount);
		return S_OK;
	}

	HRESULT TextServiceFontCollectionLoader::CreateEnumeratorFromKey(
		IDWriteFactory* factory,
		const void* collectionKey,
		UINT32 collectionKeySize,
		IDWriteFontFileEnumerator** fontFileEnumerator)
	{
		*fontFileEnumerator = new TextServiceFontEnumerator(factory);
		return S_OK;
	}


	//


	void TextServiceFontEnumeratorHelper::AddFont(INT64 data, int length)
	{
		TextServiceFontEnumerator::AddFont((BYTE*)data, length);
	}


	//

	TextServiceFontEnumerator::TextServiceFontEnumerator(
		IDWriteFactory* factory)
	{
		this->refCount = 1;
		this->index = -1;
		this->factory = factory;
		factory->AddRef();
	}

	TextServiceFontEnumerator::~TextServiceFontEnumerator()
	{
		SafeRelease(&factory);
	}

	CSimpleArray<BYTE*> TextServiceFontEnumerator::fonts;
	CSimpleArray<long> TextServiceFontEnumerator::lengths;

	void TextServiceFontEnumerator::AddFont(BYTE* data, int length)
	{
		BYTE* copy = new BYTE[length];
		memcpy(copy, data, length);
		fonts.Add(copy);
		lengths.Add(length);
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontEnumerator::AddRef()
	{
		return InterlockedIncrement(&refCount);
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontEnumerator::Release()
	{
		if (InterlockedDecrement(&refCount) == 0)
		{
			delete this;
			return 0;
		}
		return refCount;
	}

	HRESULT TextServiceFontEnumerator::QueryInterface(
		IID const& riid,
		void** ppvObject)
	{
		if (__uuidof(IUnknown) == riid)
		{
			*ppvObject = static_cast<IUnknown*>(this);
		}
		else if (__uuidof(IDWriteFontFileEnumerator) == riid)
		{
			*ppvObject = static_cast<IDWriteFontFileEnumerator*>(this);
		}
		else
		{
			*ppvObject = NULL;
			return E_NOINTERFACE;
		}
		InterlockedIncrement(&refCount);
		return S_OK;
	}

	HRESULT TextServiceFontEnumerator::GetCurrentFontFile(
		IDWriteFontFile** fontFile)
	{
		HRESULT hr;

		DWORD key = index;
		hr = factory->CreateCustomFontFileReference(
			&key,
			sizeof(DWORD),
			TextServiceDirectWriteInterop::customFontFileLoader,
			fontFile);
		if (FAILED(hr))
		{
			goto Error;
		}

	Error:
		return hr;
	}

	HRESULT TextServiceFontEnumerator::MoveNext(
		BOOL* hasCurrentFile)
	{
		*hasCurrentFile = false;
		if (this->index + 1 < fonts.GetSize())
		{
			*hasCurrentFile = true;
		}
		this->index++;
		return S_OK;
	}


	//

	TextServiceFontLoader::TextServiceFontLoader()
	{
		this->refCount = 1;
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontLoader::AddRef()
	{
		return InterlockedIncrement(&refCount);
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontLoader::Release()
	{
		if (InterlockedDecrement(&refCount) == 0)
		{
			delete this;
			return 0;
		}
		return refCount;
	}

	HRESULT TextServiceFontLoader::QueryInterface(
		IID const& riid,
		void** ppvObject)
	{
		if (__uuidof(IUnknown) == riid)
		{
			*ppvObject = static_cast<IUnknown*>(this);
		}
		else if (__uuidof(IDWriteFontFileLoader) == riid)
		{
			*ppvObject = static_cast<IDWriteFontFileLoader*>(this);
		}
		else
		{
			*ppvObject = NULL;
			return E_NOINTERFACE;
		}
		InterlockedIncrement(&refCount);
		return S_OK;
	}

	HRESULT TextServiceFontLoader::CreateStreamFromKey(
		const void* fontFileReferenceKey,
		UINT32 fontFileReferenceKeySize,
		IDWriteFontFileStream** fontFileStream)
	{
		_ASSERT(fontFileReferenceKeySize == sizeof(DWORD));
		int index = *(DWORD*)fontFileReferenceKey;

		*fontFileStream = new TextServiceFontFileStream(
			TextServiceFontEnumerator::fonts[index],
			TextServiceFontEnumerator::lengths[index]);

		return S_OK;
	}


	//

	TextServiceFontFileStream::TextServiceFontFileStream(
		BYTE* data,
		long length)
	{
		this->refCount = 1;
		this->data = data;
		this->length = length;
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontFileStream::AddRef()
	{
		return InterlockedIncrement(&refCount);
	}

	unsigned long STDMETHODCALLTYPE TextServiceFontFileStream::Release()
	{
		if (InterlockedDecrement(&refCount) == 0)
		{
			delete this;
			return 0;
		}
		return refCount;
	}

	HRESULT TextServiceFontFileStream::QueryInterface(
		IID const& riid,
		void** ppvObject)
	{
		if (__uuidof(IUnknown) == riid)
		{
			*ppvObject = static_cast<IUnknown*>(this);
		}
		else if (__uuidof(IDWriteFontFileStream) == riid)
		{
			*ppvObject = static_cast<IDWriteFontFileStream*>(this);
		}
		else
		{
			*ppvObject = NULL;
			return E_NOINTERFACE;
		}
		InterlockedIncrement(&refCount);
		return S_OK;
	}

	HRESULT TextServiceFontFileStream::GetFileSize(
		UINT64* fileSize)
	{
		*fileSize = length;
		return S_OK;
	}

	HRESULT TextServiceFontFileStream::GetLastWriteTime(
		UINT64* lastWriteTime)
	{
		*lastWriteTime = 0;
		return S_OK;
	}

	HRESULT TextServiceFontFileStream::ReadFileFragment(
		const void** fragmentStart,
		UINT64 fileOffset,
		UINT64 fragmentSize,
		void** fragmentContext)
	{
		BYTE* target = new BYTE[fragmentSize];
		*fragmentContext = target;
		*fragmentStart = target;

		memcpy(target, (BYTE*)data + fileOffset, fragmentSize);

		return S_OK;
	}

	void TextServiceFontFileStream::ReleaseFileFragment(
		void* fragmentContext)
	{
		delete (BYTE*)fragmentContext;
	}
}
