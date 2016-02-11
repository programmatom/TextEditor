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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace TextEditor
{
    // Wrappers around GDI routines for use with 'using' pattern

    public static class GDI
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int SetBkColor(
            [In] IntPtr hdc,
            [In, MarshalAs(UnmanagedType.U4)] int crColor);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int SetTextColor(
            [In] IntPtr hdc,
            [In, MarshalAs(UnmanagedType.U4)] int crColor);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr SelectObject(
            [In] IntPtr hdc,
            [In] IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool DeleteObject(
            [In] IntPtr hgdiobj);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int SetBkMode(
            [In] IntPtr hdc,
            [In] int mode);
        public const int TRANSPARENT = 1;
        public const int OPAQUE = 2;

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool BitBlt(
            IntPtr hdc,
            int x,
            int y,
            int cx,
            int cy,
            IntPtr hdcSrc,
            int x1,
            int y1,
            int rop);
        public const int SRCCOPY = 0x00CC0020; // dest = source
        public const int SRCPAINT = 0x00EE0086; // dest = source OR dest
        public const int SRCAND = 0x008800C6; // dest = source AND dest
        public const int SRCINVERT = 0x00660046; // dest = source XOR dest
        public const int SRCERASE = 0x00440328; // dest = source AND (NOT dest)
        public const int NOTSRCCOPY = 0x00330008; // dest = (NOT source)
        public const int NOTSRCERASE = 0x001100A6; // dest = (NOT src) AND (NOT dest)
        public const int MERGECOPY = 0x00C000CA; // dest = (source AND pattern)
        public const int MERGEPAINT = 0x00BB0226; // dest = (NOT source) OR dest
        public const int PATCOPY = 0x00F00021; // dest = pattern
        public const int PATPAINT = 0x00FB0A09; // dest = DPSnoo
        public const int PATINVERT = 0x005A0049; // dest = pattern XOR dest
        public const int DSTINVERT = 0x00550009; // dest = (NOT dest)
        public const int BLACKNESS = 0x00000042; // dest = BLACK
        public const int WHITENESS = 0x00FF0062; // dest = WHITE

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(
            IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool DeleteDC(
            IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int SelectClipRgn(
            IntPtr hdc,
            IntPtr hrgn);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd145037%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 92)]
        public struct LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string lfFaceName;
        }
        public const int LF_FACESIZE = 32;
        // lfCharSet
        public const byte DEFAULT_CHARSET = 1;
        // lfPitchAndFamily
        public const byte DEFAULT_PITCH = 0;
        public const byte FIXED_PITCH = 1;
        public const byte VARIABLE_PITCH = 2;
        public const byte MONO_FONT = 8;
        /* Font Families */
        public const byte FF_DONTCARE = (0 << 4); // Don't care or don't know.
        public const byte FF_ROMAN = (1 << 4); // Variable stroke width, serifed. Times Roman, Century Schoolbook, etc.
        public const byte FF_SWISS = (2 << 4); // Variable stroke width, sans-serifed. Helvetica, Swiss, etc.
        public const byte FF_MODERN = (3 << 4); // Constant stroke width, serifed or sans-serifed. Pica, Elite, Courier, etc.
        public const byte FF_SCRIPT = (4 << 4); // Cursive, etc.
        public const byte FF_DECORATIVE = (5 << 4); // Old English, etc.
        /* Font Weights */
        public const int FW_DONTCARE = 0;
        public const int FW_THIN = 100;
        public const int FW_EXTRALIGHT = 200;
        public const int FW_LIGHT = 300;
        public const int FW_NORMAL = 400;
        public const int FW_MEDIUM = 500;
        public const int FW_SEMIBOLD = 600;
        public const int FW_BOLD = 700;
        public const int FW_EXTRABOLD = 800;
        public const int FW_HEAVY = 900;
        public const int FW_ULTRALIGHT = FW_EXTRALIGHT;
        public const int FW_REGULAR = FW_NORMAL;
        public const int FW_DEMIBOLD = FW_SEMIBOLD;
        public const int FW_ULTRABOLD = FW_EXTRABOLD;
        public const int FW_BLACK = FW_HEAVY;
        // Out precision
        public const int OUT_DEFAULT_PRECIS = 0;
        public const int OUT_STRING_PRECIS = 1;
        public const int OUT_CHARACTER_PRECIS = 2;
        public const int OUT_STROKE_PRECIS = 3;
        public const int OUT_TT_PRECIS = 4;
        public const int OUT_DEVICE_PRECIS = 5;
        public const int OUT_RASTER_PRECIS = 6;
        public const int OUT_TT_ONLY_PRECIS = 7;
        public const int OUT_OUTLINE_PRECIS = 8;
        public const int OUT_SCREEN_OUTLINE_PRECIS = 9;
        public const int OUT_PS_ONLY_PRECIS = 10;
        // Clip precision
        public const int CLIP_DEFAULT_PRECIS = 0;
        public const int CLIP_CHARACTER_PRECIS = 1;
        public const int CLIP_STROKE_PRECIS = 2;
        public const int CLIP_MASK = 0xf;
        public const int CLIP_LH_ANGLES = (1 << 4);
        public const int CLIP_TT_ALWAYS = (2 << 4);
        public const int CLIP_DFA_DISABLE = (4 << 4);
        public const int CLIP_EMBEDDED = (8 << 4);
        // Quality
        public const int DEFAULT_QUALITY = 0;
        public const int DRAFT_QUALITY = 1;
        public const int PROOF_QUALITY = 2;
        public const int NONANTIALIASED_QUALITY = 3;
        public const int ANTIALIASED_QUALITY = 4;
        public const int CLEARTYPE_QUALITY = 5;
        public const int CLEARTYPE_NATURAL_QUALITY = 6;

        public static void GetLogFont(Font font, Graphics graphics, out LOGFONT logfont)
        {
            object o = new LOGFONT();
            if (graphics != null)
            {
                font.ToLogFont(o, graphics);
            }
            else
            {
                font.ToLogFont(o);
            }
            logfont = (LOGFONT)o;
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr/*HANDLE*/ AddFontMemResourceEx(
            [In] byte[] pbFont,
            [In] int cbFont,
            [In] IntPtr pdv,
            [Out] out int pcFonts);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd162627%28v=vs.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 348)]
        public struct ENUMLOGFONTEX
        {
            public LOGFONT elfLogFont;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FULLFACESIZE)]
            public string elfFullName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string elfStyle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string elfScript;
        }
        public const int LF_FULLFACESIZE = 64;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 76)]
        public struct NEWTEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
            public int ntmFlags;
            public uint ntmSizeEM;
            public uint ntmCellHeight;
            public uint ntmAvgWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 24)]
        public struct FONTSIGNATURE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] fsUsb;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public int[] fsCsb;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 100)]
        public struct NEWTEXTMETRICEX
        {
            public NEWTEXTMETRIC ntmTm;
            public FONTSIGNATURE ntmFontSig;
        }

        public delegate int FONTENUMPROC(
            [In, MarshalAs(UnmanagedType.Struct)] ref ENUMLOGFONTEX lpelfe,
            [In, MarshalAs(UnmanagedType.Struct)] ref NEWTEXTMETRIC lpntme,
            int FontType,
            int lParam);

        [DllImport("gdi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int EnumFontFamiliesEx(
            IntPtr hdc,
            [In, MarshalAs(UnmanagedType.Struct)] ref LOGFONT lpLogfont,
            FONTENUMPROC lpEnumFontFamExProc,
            int lParam,
            int dwFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int GetDeviceCaps(
            [In] IntPtr hdc, // HDC
            [In] int index);
        public const int LOGPIXELSX = 88; // Logical pixels/inch in X
        public const int LOGPIXELSY = 90; // Logical pixels/inch in Y

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(
            IntPtr hwnd);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateBitmap(
            int nWidth,
            int nHeight,
            uint cPlanes,
            uint cBitsPerPel,
            IntPtr lpvBits);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleBitmap(
            IntPtr hdc,
            int nWidth,
            int nHeight);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int FillRect(
            IntPtr hDC,
            [In, MarshalAs(UnmanagedType.Struct)] ref Rectangle lprc,
            IntPtr hbr); // HBRUSH

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateSolidBrush(
            int crColor);// COLORREF

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr/*HDC*/ CreateEnhMetaFile(
            IntPtr hdcRef, // HDC
            string lpFilename,
            IntPtr lpRect,
            string lpDescription);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr/*HENHMETAFILE*/ CloseEnhMetaFile(
            IntPtr hdc); // HDC

        public const int EMR_EXTCREATEFONTINDIRECTW = 82;

        [StructLayout(LayoutKind.Sequential)]
        public struct EMR
        {
            public int iType; // Enhanced metafile record type
            public int nSize; // Length of the record in bytes. This must be a multiple of 4.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PANOSE
        {
            public byte bFamilyType;
            public byte bSerifStyle;
            public byte bWeight;
            public byte bProportion;
            public byte bContrast;
            public byte bStrokeVariation;
            public byte bArmStyle;
            public byte bLetterform;
            public byte bMidline;
            public byte bXHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct EXTLOGFONT
        {
            public LOGFONT elfLogFont;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FULLFACESIZE)]
            public string elfFullName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string elfStyle;
            public int elfVersion; // 0 for the first release of NT
            public int elfStyleSize;
            public int elfMatch;
            public int elfReserved;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I1, SizeConst = ELF_VENDOR_SIZE)]
            public byte[] elfVendorId;
            public int elfCulture; // 0 for Latin
            public PANOSE elfPanose;
        }
        public const int ELF_VENDOR_SIZE = 4;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct EMREXTCREATEFONTINDIRECT
        {
            public EMR emr;
            public int ihFont; // Font handle index
            public EXTLOGFONT elfw;
        }

        public delegate int ENHMFENUMPROC(
            IntPtr hdc, // HDC
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] IntPtr[] lpht,// capacity is nHandles
            IntPtr lpmr, // ENHMETARECORD
            int nHandles,
            [In, Optional] int data); // LPARAM

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool EnumEnhMetaFile(
            [In, Optional] IntPtr hdc,  // HDC
            IntPtr hmf, // HENHMETAFILE
            ENHMFENUMPROC proc,
            [In, Optional] IntPtr param, // LPVOID
            [In, Optional] IntPtr lpRect); // RECT

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool DeleteEnhMetaFile(
            [In, Optional] IntPtr /*HENHMETAFILE*/ hmf);
    }

    public class GraphicsHDC : IDisposable
    {
        private readonly IDeviceContext graphics;
        private readonly IntPtr h;

        public GraphicsHDC(IDeviceContext graphics)
        {
            this.graphics = graphics;
            this.h = graphics.GetHdc();
        }

        public void Dispose()
        {
            graphics.ReleaseHdc();

            GC.SuppressFinalize(this);
        }

        ~GraphicsHDC()
        {
#if DEBUG
            Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        public static implicit operator IntPtr(GraphicsHDC o)
        {
            return o.h;
        }
    }


    // GDI Objects

    public abstract class GDIObject : IDisposable // Any object destroyed va DeleteObject()
    {
        protected readonly IntPtr h;

        protected GDIObject(IntPtr h)
        {
            this.h = h;
        }

        public void Dispose()
        {
            if (h != IntPtr.Zero)
            {
                GDI.DeleteObject(h);
            }

            GC.SuppressFinalize(this);
        }

        ~GDIObject()
        {
#if DEBUG
            Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        public static implicit operator IntPtr(GDIObject o)
        {
            return o.h;
        }
    }

    public class GDIFont : GDIObject
    {
        public GDIFont(Font font) // from System.Drawing.Font
            : base(font.ToHfont())
        {
        }
    }

    public class GDIRegion : GDIObject
    {
        public GDIRegion(IntPtr h) // takes ownership; use, e.g.: new GDIRegion(graphics.Clip.GetHrgn(graphics))
            : base(h)
        {
        }
    }

    public class GDIBitmap : GDIObject
    {
        private readonly int width;
        private readonly int height;

        public GDIBitmap(int width, int height, IntPtr hdc) // CreateCompatibleBitmap wrapper
            : base(CreateBitmapInternal(width, height, hdc))
        {
            this.width = width;
            this.height = height;
        }

        public GDIBitmap(int width, int height, PixelFormat format) // CreateBitmap wrapper
            : base(CreateBitmapInternal(width, height, format))
        {
            this.width = width;
            this.height = height;
        }

        public int Width { get { return width; } }
        public int Height { get { return height; } }

        private static IntPtr CreateBitmapInternal(int width, int height, IntPtr hdc)
        {
            IntPtr h = GDI.CreateCompatibleBitmap(hdc, width, height);
            if (h == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return h;
        }

        private static IntPtr CreateBitmapInternal(int width, int height, PixelFormat format)
        {
            uint cPlanes, cBitsPerPixel;
            switch (format)
            {
                default:
                    throw new ArgumentException();
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                    cPlanes = 1;
                    cBitsPerPixel = 32;
                    break;
            }
            IntPtr h = GDI.CreateBitmap(width, height, cPlanes, cBitsPerPixel, IntPtr.Zero);
            if (h == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return h;
        }
    }

    public struct COLORREF
    {
        public int v;

        public COLORREF(int v)
        {
            this.v = v;
        }

        public COLORREF(Color c)
        {
            v = c.R | (c.G << 8) | (c.B << 16);
        }

        public static implicit operator int(COLORREF c)
        {
            return c.v;
        }
    }

    public class GDIBrush : GDIObject
    {
        public GDIBrush(Color color)
            : base(CreateSolidBrushInternal(color))
        {
        }

        private static IntPtr CreateSolidBrushInternal(Color color)
        {
            IntPtr h = GDI.CreateSolidBrush(new COLORREF(color));
            if (h == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return h;
        }
    }


    // GDI Device Contexts

    public abstract class GDIDeviceContext : IDisposable // Any object destroyed va DeleteDC()
    {
        protected readonly IntPtr h;

        protected GDIDeviceContext(IntPtr h)
        {
            this.h = h;
        }

        public void Dispose()
        {
            if (h != IntPtr.Zero)
            {
                GDI.DeleteDC(h);
            }

            GC.SuppressFinalize(this);
        }

        ~GDIDeviceContext()
        {
#if DEBUG
            Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        public static implicit operator IntPtr(GDIDeviceContext o)
        {
            return o.h;
        }
    }

    public class GDIDC : GDIDeviceContext
    {
        public static GDIDC CreateCompatibleDC(IntPtr hdc)
        {
            IntPtr h = GDI.CreateCompatibleDC(hdc);
            if (h == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return new GDIDC(h);
        }

        public static GDIDC Create(GDIBitmap bitmap)
        {
            IntPtr hdc = GDI.CreateCompatibleDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            GDIDC gdidc = new GDIDC(hdc);
            GDI.SelectObject(hdc, bitmap);
            return gdidc;
        }

        public GDIDC(IntPtr hdc)
            : base(hdc)
        {
        }
    }

    // package for GDI offscreen bitmap that plays nice with ExtTextOut, Uniscribe, and DirectWrite
    public class GDIOffscreenBitmap : IDisposable
    {
        private readonly GDIBitmap hBitmap;
        private readonly GDIDC hDC;
        private readonly Graphics graphics;

        public GDIOffscreenBitmap(Graphics graphicsCompatibleWith, int width, int height)
        {
            if ((width <= 0) || (height <= 0))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            // Create GDI objects for offscreen. Must be created through GDI because Uniscribe/DirectWrite do not
            // like objects created by GDI+ and will draw with very poor quality on them.
            using (GraphicsHDC hDC = new GraphicsHDC(graphicsCompatibleWith))
            {
                this.hDC = GDIDC.CreateCompatibleDC(hDC);
                hBitmap = new GDIBitmap(width, height, PixelFormat.Format32bppArgb);
                GDI.SelectObject(this.hDC, hBitmap);
            }
            graphics = Graphics.FromHdc(hDC);
        }

        public void Dispose()
        {
            if (graphics != null)
            {
                graphics.Dispose();
            }
            if (hDC != null)
            {
                hDC.Dispose();
            }
            if (hBitmap != null)
            {
                hBitmap.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        ~GDIOffscreenBitmap()
        {
#if DEBUG
            Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            Dispose();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        // gets the GDI+ wrapper
        public Graphics Graphics { get { return graphics; } }

        // gets the device context handle for use with BitBlt()
        public IntPtr HDC { get { return hDC; } }

        // gets the bitmap handle for use with Bitmap.FromHBitmap()
        public IntPtr HBitmap { get { return hBitmap; } }

        public int Width { get { return hBitmap.Width; } }
        public int Height { get { return hBitmap.Height; } }
    }
}
