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

#pragma once

using namespace System;
using namespace System::Drawing;
using namespace System::Globalization;
using namespace System::Runtime::InteropServices;
using namespace System::Windows::Forms;

namespace TextEditor
{
	//

	public ref class TextServiceDirectWriteInterop
	{
	public:
		IDWriteFactory* factory;

		IDWriteRenderingParams* renderingParams;

		IDWriteTextFormat* textFormat;

		int visibleWidth;
		int lineHeight;
		float baseline;
		IDWriteBitmapRenderTarget* renderTarget; // contains offscreen strip
		float rdpiX, rdpiY;

		static IDWriteFontCollectionLoader* customFontCollectionLoader;

	public:
		static IDWriteFontFileLoader* customFontFileLoader;

	public:

		TextServiceDirectWriteInterop();

		~TextServiceDirectWriteInterop();

		!TextServiceDirectWriteInterop();

		HRESULT Reset(
			Font^ font,
			int visibleWidth);

		void _Dispose();

		void ClearCaches();
	};


	// 

	public ref class TextServiceLineDirectWriteInterop
	{
	private:
		TextServiceDirectWriteInterop^ service;
		IDWriteTextLayout* textLayout;

		int totalChars;
		COLORREF foreColor;

	public:

		TextServiceLineDirectWriteInterop();

		~TextServiceLineDirectWriteInterop();

		!TextServiceLineDirectWriteInterop();

		HRESULT Init(
			TextServiceDirectWriteInterop^ service,
			String^ line);

		void _Dispose();

		HRESULT DrawText(
			Graphics^ graphics_,
			Point position,
			Color foreColor,
			Color backColor);

		HRESULT DrawTextWithRenderTarget(
			Graphics^ graphics,
			System::Drawing::Rectangle bounds,
			Color foreColor,
			Color backColor);

		HRESULT BuildRegion(
			Graphics^ graphics,
			Point position,
			int startPos,
			int endPosPlusOne,
			[Out] Region^ %regionOut);

		HRESULT GetExtent(
			Graphics^ graphics,
			[Out] Size %sizeOut);

		HRESULT CharPosToX(
			Graphics^ graphics,
			int offset,
			bool trailing,
			[Out] int %x);

		HRESULT XToCharPos(
			Graphics^ graphics,
			int x,
			[Out] int %offset,
			[Out] bool %trailing);
	};


	//

	public class TextServiceLineDirectWriteInterop2 : /*public IUnknown, public IDWritePixelSnapping, */public IDWriteTextRenderer
	{
	private:
		long refCount;
		float rdpiY;
		IDWriteBitmapRenderTarget* renderTarget;
		IDWriteRenderingParams* renderingParams;
		COLORREF foreColor;

	public:

		TextServiceLineDirectWriteInterop2();

		HRESULT Init(
			float rdpiY,
			IDWriteBitmapRenderTarget* renderTarget,
			IDWriteRenderingParams* renderingParams,
			COLORREF foreColor);


		unsigned long STDMETHODCALLTYPE AddRef();

		unsigned long STDMETHODCALLTYPE Release();

		STDMETHOD(QueryInterface)(
			IID const& riid,
			void** ppvObject);


		STDMETHOD(IsPixelSnappingDisabled)(
			__maybenull void* clientDrawingContext,
			__out BOOL* isDisabled);

		STDMETHOD(GetCurrentTransform)(
			__maybenull void* clientDrawingContext,
			__out DWRITE_MATRIX* transform);

		STDMETHOD(GetPixelsPerDip)(
			__maybenull void* clientDrawingContext,
			__out FLOAT* pixelsPerDip);


		STDMETHOD(DrawGlyphRun)(
			__maybenull void* clientDrawingContext,
			FLOAT baselineOriginX,
			FLOAT baselineOriginY,
			DWRITE_MEASURING_MODE measuringMode,
			__in DWRITE_GLYPH_RUN const* glyphRun,
			__in DWRITE_GLYPH_RUN_DESCRIPTION const* glyphRunDescription,
			__maybenull IUnknown* clientDrawingEffect);

		STDMETHOD(DrawUnderline)(
			__maybenull void* clientDrawingContext,
			FLOAT baselineOriginX,
			FLOAT baselineOriginY,
			__in DWRITE_UNDERLINE const* underline,
			__maybenull IUnknown* clientDrawingEffect);

		STDMETHOD(DrawStrikethrough)(
			__maybenull void* clientDrawingContext,
			FLOAT baselineOriginX,
			FLOAT baselineOriginY,
			__in DWRITE_STRIKETHROUGH const* strikethrough,
			__maybenull IUnknown* clientDrawingEffect);

		STDMETHOD(DrawInlineObject)(
			__maybenull void* clientDrawingContext,
			FLOAT originX,
			FLOAT originY,
			IDWriteInlineObject* inlineObject,
			BOOL isSideways,
			BOOL isRightToLeft,
			__maybenull IUnknown* clientDrawingEffect);
	};


	public class TextServiceFontCollectionLoader : public IDWriteFontCollectionLoader
	{
	private:
		long refCount;

	public:

		TextServiceFontCollectionLoader();

		unsigned long STDMETHODCALLTYPE AddRef();

		unsigned long STDMETHODCALLTYPE Release();

		STDMETHOD(QueryInterface)(
			IID const& riid,
			void** ppvObject);


		STDMETHOD(CreateEnumeratorFromKey)(
			IDWriteFactory* factory,
			const void* collectionKey,
			UINT32 collectionKeySize,
			IDWriteFontFileEnumerator** fontFileEnumerator);
	};

	public ref class TextServiceFontEnumeratorHelper
	{
	public:
		static void AddFont(INT64 data, int length);
	};

	public class TextServiceFontEnumerator : public IDWriteFontFileEnumerator
	{
	private:
		long refCount;
		long index;
		IDWriteFactory* factory;

	public:
		static CSimpleArray<BYTE*> fonts;
		static CSimpleArray<long> lengths;

		static void AddFont(BYTE* data, int length);

	public:

		TextServiceFontEnumerator(
			IDWriteFactory* factory);

		~TextServiceFontEnumerator();

		unsigned long STDMETHODCALLTYPE AddRef();

		unsigned long STDMETHODCALLTYPE Release();

		STDMETHOD(QueryInterface)(
			IID const& riid,
			void** ppvObject);


		STDMETHOD(GetCurrentFontFile)(
			IDWriteFontFile** fontFile);

		STDMETHOD(MoveNext)(
			BOOL* hasCurrentFile);
	};

	public class TextServiceFontLoader : public IDWriteFontFileLoader
	{
	private:
		long refCount;

	public:

		TextServiceFontLoader();

		unsigned long STDMETHODCALLTYPE AddRef();

		unsigned long STDMETHODCALLTYPE Release();

		STDMETHOD(QueryInterface)(
			IID const& riid,
			void** ppvObject);


		STDMETHOD(CreateStreamFromKey)(
			const void* fontFileReferenceKey,
			UINT32 fontFileReferenceKeySize,
			IDWriteFontFileStream** fontFileStream);
	};

	public class TextServiceFontFileStream : public IDWriteFontFileStream
	{
	private:
		long refCount;
		BYTE* data;
		long length;

	public:

		TextServiceFontFileStream(
			BYTE* data,
			long length);

		unsigned long STDMETHODCALLTYPE AddRef();

		unsigned long STDMETHODCALLTYPE Release();

		STDMETHOD(QueryInterface)(
			IID const& riid,
			void** ppvObject);


		STDMETHOD(GetFileSize)(
			UINT64* fileSize);

		STDMETHOD(GetLastWriteTime)(
			UINT64* lastWriteTime);

		STDMETHOD(ReadFileFragment)(
			const void** fragmentStart,
			UINT64 fileOffset,
			UINT64 fragmentSize,
			void** fragmentContext);

		STDMETHOD_(void, ReleaseFileFragment)(
			void* fragmentContext);
	};
}
