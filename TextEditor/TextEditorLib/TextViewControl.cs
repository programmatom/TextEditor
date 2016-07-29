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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TextEditor
{
    // For simple databinding, see:
    // https://msdn.microsoft.com/en-us/library/ms171926.aspx
    // Only requirements are the DefaultBindingPropertyAttribute and firing the related
    // change event.

    [DefaultBindingProperty("Text"), DefaultEvent("TextChanged")]
    public partial class TextViewControl : ScrollableControl
    {
        private ITextStorageFactory textStorageFactory;
        private ITextStorage textStorage;

        private int currentWidth;

        private int selectStartLine;
        private int selectStartChar;
        private int selectEndLine;
        private int selectEndCharPlusOne;
        private bool selectStartIsActive;

        private bool cursorEnabledFlag = true;
        private bool cursorDrawnFlag = true;
        private int stickyX;
        private bool cursorAdvancing; // for Uniscript, see https://msdn.microsoft.com/en-us/library/windows/desktop/dd317793%28v=vs.85%29.aspx
        private bool simpleNavigation; // uses Char.IsLetterOrDigit and Char.IsWhiteSpace even for Uniscribe - good for source code editors

        private int clickPhase;
        private int lastClickX;
        private int lastClickY;
        private DateTime lastClickTime;

        private char? deferredHighSurrogate; // for handling non-zero plane unicode character entry

        private int spacesPerTab = 8;

        private bool selectAllOnEnable;
        private bool hideSelectionOnFocusLost;

        private string lineFeed = Environment.NewLine;

        private ITextEditorChangeTracking changeListener;

        private int fontHeight;

#if WINDOWS
        private ITextService textService = new TextServiceUniscribe(); // if changing, update TextService default value attribute as well
#else
        private ITextService textService = new TextServiceSimple(); // if changing, update TextService default value attribute as well
#endif

        private Brush normalForeBrush;
        private Brush normalBackBrush;
        private Pen normalForePen;
        private Brush selectedForeBrush;
        private Brush selectedBackBrush;
        private Brush selectedForeBrushInactive;
        private Brush selectedBackBrushInactive;
        private Bitmap offscreenStrip;

        private Color selectedBackColor = SystemColors.Highlight;
        private Color selectedForeColor = SystemColors.HighlightText;
        private Color selectedBackColorInactive = SystemColors.GradientInactiveCaption;
        private Color selectedForeColorInactive = SystemColors.ControlText;

        public TextViewControl()
        {
            InitializeComponent();

            timerCursorBlink.Interval = (int)GetCaretBlinkTime();
            timerCursorBlink.Tick += new EventHandler(timerCursorBlink_Tick);
            //timerCursorBlink.Start();

            this.Disposed += new EventHandler(TextViewControl_Disposed);

            OnFontChanged(EventArgs.Empty); // ensure recalculations
        }

#if WINDOWS
        [DllImport("user32.dll")]
        private extern static uint GetCaretBlinkTime(); // msec
#else
        private static uint GetCaretBlinkTime() { return 500; } // msec
#endif

        public TextViewControl(ITextStorageFactory textStorageFactory)
            : this()
        {
            this.textStorageFactory = textStorageFactory;
            this.textStorage = textStorageFactory.New();

            OnFontChanged(EventArgs.Empty); // ensure recalculations
        }

        ~TextViewControl()
        {
#if DEBUG
            Debug.Assert(false, this.GetType().Name + " finalizer invoked - have you forgotten to .Dispose()? " + allocatedFrom.ToString());
#endif
            DisposeThis();
        }
#if DEBUG
        private readonly StackTrace allocatedFrom = new StackTrace(true);
#endif

        private void DisposeThis()
        {
            textService.Dispose();

            DisposeGraphicsObjects();

            GC.SuppressFinalize(this);
        }

        private void TextViewControl_Disposed(object sender, EventArgs e)
        {
            DisposeThis();
        }

        [Browsable(true), Category("Storage")]
        public ITextStorageFactory TextStorageFactory
        {
            get
            {
                return textStorageFactory;
            }
            set
            {
                // a set-once property
                if (textStorageFactory != null)
                {
                    throw new InvalidOperationException();
                }
                if (value == null)
                {
                    return;
                }

                textStorageFactory = value;
                textStorage = textStorageFactory.New();

                OnFontChanged(EventArgs.Empty); // ensure recalculations
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DisposeGraphicsObjects(); // force recreate offscreen strip
            textService.Reset(Font, ClientWidth);

            RecomputeCanvasSizeIncremental();

            //Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Redraw();
            base.OnPaint(pe);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // we repaint entire surface; suppress default erase-to-background behavior to reduce flicker
            if (!DesignMode)
            {
                return;
            }

            base.OnPaintBackground(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            timerCursorBlink.Start();

            RedrawSelection();

            if (selectAllOnEnable)
            {
                SelectAll();
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            timerCursorBlink.Stop();

            RedrawSelection();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            // clear cache because, during dpi-change, values will be incorrect and then base.OnFontChanged calls OnSizeChanged
            // which tries to use them, leading to asserts.
            ResetCanvasSizeCaches();

            base.OnFontChanged(e);

            DisposeGraphicsObjects(); // reset height of offscreen strip
            textService.Reset(Font, ClientWidth);

            fontHeight = FontHeight;

            if (textService.Service != TextService.DirectWrite)
            {
                fontHeight = FontHeightHack.GetActualFontHeight(Font, fontHeight);
            }

            VerticalScroll.SmallChange = fontHeight;
            ResetCanvasSize();
            SetStickyX();
            Invalidate();
        }

        private void timerCursorBlink_Tick(object sender, EventArgs e)
        {
            if (cursorEnabledFlag)
            {
                cursorDrawnFlag = !cursorDrawnFlag;
                RedrawLine(selectStartIsActive ? selectStartLine : selectEndLine);
            }
        }

        private void EnsureGraphicsObjects()
        {
            if (normalForeBrush == null)
            {
                normalForeBrush = new SolidBrush(ForeColor);
            }
            if (normalBackBrush == null)
            {
                normalBackBrush = new SolidBrush(BackColor);
            }
            if (normalForePen == null)
            {
                normalForePen = new Pen(ForeColor);
            }
            if (selectedForeBrush == null)
            {
                selectedForeBrush = new SolidBrush(selectedForeColor);
            }
            if (selectedBackBrush == null)
            {
                selectedBackBrush = new SolidBrush(selectedBackColor);
            }
            if (selectedForeBrushInactive == null)
            {
                selectedForeBrushInactive = new SolidBrush(selectedForeColorInactive);
            }
            if (selectedBackBrushInactive == null)
            {
                selectedBackBrushInactive = new SolidBrush(selectedBackColorInactive);
            }
            if (offscreenStrip == null)
            {
                offscreenStrip = new Bitmap(Math.Max(ClientWidth, 1), fontHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            }
        }

        private void DisposeGraphicsObjects()
        {
            if (normalForeBrush != null)
            {
                normalForeBrush.Dispose();
                normalForeBrush = null;
            }
            if (normalBackBrush != null)
            {
                normalBackBrush.Dispose();
                normalBackBrush = null;
            }
            if (normalForePen != null)
            {
                normalForePen.Dispose();
                normalForePen = null;
            }
            if (selectedForeBrush != null)
            {
                selectedForeBrush.Dispose();
                selectedForeBrush = null;
            }
            if (selectedBackBrush != null)
            {
                selectedBackBrush.Dispose();
                selectedBackBrush = null;
            }
            if (offscreenStrip != null)
            {
                offscreenStrip.Dispose();
                offscreenStrip = null;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Form ParentForm { get { return FindFormInternal(); } }

        private Form FindFormInternal() // taken from .NET source
        {
            Control current = this;
            while ((current != null) && !(current is Form))
            {
                current = current.Parent;
            }
            return (Form)current;
        }

        // override to re-expose AutoSize
        [Browsable(true), Category("Layout"), DefaultValue(false), EditorBrowsable(EditorBrowsableState.Always), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override bool AutoSize
        {
            get
            {
                return base.AutoSize;
            }
            set
            {
                base.AutoSize = value;
            }
        }


        // Internal layout/draw methods

        protected int ClientWidth { get { return ClientSize.Width; } }

        protected int ClientHeight { get { return ClientSize.Height; } }

        // recompute canvas size from scratch - for use with global changes such as tab width or font change
        private void ResetCanvasSize()
        {
            ResetCanvasSizeCaches();
            RecomputeCanvasSizeIncremental();
        }

        // recompute canvas size incrementally
        private void RecomputeCanvasSizeIncremental()
        {
            if (textStorageFactory == null)
            {
                return;
            }

            if (AutoSize)
            {
                RecomputeCanvasSizeExactForAutoSize();
            }
            else
            {
                // Exact recalc is too slow for large files. Instead, only the lines near the viewport are used to compute
                // canvas size. This means the horizontal scroll bar may indicate less than the longest line in the file. As
                // the user scrolls longer lines into view the scroll bar will adjust to account for discovery of longer lines.
                RecomputeCanvasSizePartial(
                    -AutoScrollPosition.Y / fontHeight,
                    (-AutoScrollPosition.Y + ClientHeight + (fontHeight - 1)) / fontHeight,
                    true/*includeClientWidthAndOverflow*/);

            }
        }

        // Perform an exact canvas size recalc of just text length, without horizontal margin and min client size. Used for
        // AutoSize=true inline edit scenario where control should grow/shrink to exactly the bounds of the text.
        private void RecomputeCanvasSizeExactForAutoSize()
        {
            Debug.Assert(AutoSize); // only to be used for AutoSize=true mode

            currentWidth = 0; // allow shrink
            RecomputeCanvasSizePartial(0, textStorage.Count, false/*includeClientWidthAndOverflow*/);

            Size size = new Size(currentWidth, fontHeight * textStorage.Count);
            if (this.Size != size)
            {
                this.Size = size; // constraint to MinimumSize taken care of by base class ScrollableControl
            }
        }

        private readonly LineWidthCache lineWidthCache = new LineWidthCache();
        private void ResetCanvasSizeCaches()
        {
            currentWidth = 0;
            lineWidthCache.Clear();
        }

        // do not call this method directly, use RecomputeCanvasSizePartial() instead
        private void RecomputeCanvasSizePartial(int startLine, int endLine, bool includeClientWidthAndOverflow)
        {
            if (textStorageFactory == null)
            {
                return;
            }

            using (Graphics graphics = CreateGraphics())
            {
                // add some margin for two reasons: First, Graphics.DrawString seems to omit the last character if the bounding
                // rect is too tight, even if .NoClip and .NoWrap are set in the formatting style. Second, it gives some
                // extra space for the IP on the edge and feels more comfortable to use.
                int horizontalOverflow = includeClientWidthAndOverflow ? fontHeight : 1;

                int currentWidth1 = 0;
                int widestLine = -1;
                for (int i = Math.Max(startLine, 0); i <= Math.Min(endLine, this.Count - 1); i++)
                {
                    int width;
                    if (!lineWidthCache.TryGet(i, out width))
                    {
                        bool tabsFound;
                        IDecodedTextLine decodedLine = GetSpaceFromTabLineMustDispose(i, out tabsFound);
                        using (ITextInfo info = textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                        {
                            width = info.GetExtent(graphics).Width;
                        }
                        lineWidthCache.Set(i, width);
                    }
                    else
                    {
#if DEBUG
                        bool tabsFound;
                        int debugWidth;
                        IDecodedTextLine decodedLine = GetSpaceFromTabLineMustDispose(i, out tabsFound);
                        using (ITextInfo info = textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                        {
                            debugWidth = info.GetExtent(graphics).Width;
                        }
                        if (width != debugWidth)
                        {
                            Debugger.Log(0, "TextViewControl.LineWidthCache", String.Format("LineWidthCache bad value - actual: {0} cached: {1}" + Environment.NewLine, debugWidth, width));
                            Debug.Assert(false);
                        }
#endif
                    }
                    width += horizontalOverflow;
                    if (currentWidth1 < width)
                    {
                        currentWidth1 = width;
                        widestLine = i;
                    }
                }
                int oldCurrentWidth = currentWidth;
                currentWidth = currentWidth1;
                if (includeClientWidthAndOverflow)
                {
                    currentWidth = Math.Max(currentWidth, ClientWidth);
                }
                Size autoScrollMinSize = new Size(currentWidth, textStorage.Count * fontHeight);
                if ((autoScrollMinSize.Width != AutoScrollMinSize.Width) || (autoScrollMinSize.Height != AutoScrollMinSize.Height))
                {
                    Debugger.Log(
                        0,
                        "TextViewControl",
                        String.Format(
                            "{0}: {7}[{1}..{2}|{8}]:{3} -> {4} -> ({5},{6})" + Environment.NewLine,
                            DateTime.Now,
                            startLine,
                            endLine,
                            currentWidth1,
                            currentWidth,
                            autoScrollMinSize.Width,
                            autoScrollMinSize.Height,
                            oldCurrentWidth == 0 ? "RESET " : String.Empty,
                            widestLine));
                    AutoScrollMinSize = autoScrollMinSize;
                }
                if ((offscreenStrip != null) && (ClientWidth > offscreenStrip.Width))
                {
                    DisposeGraphicsObjects();
                }
            }
        }

        private void Redraw()
        {
            if (textStorageFactory == null)
            {
                return;
            }

            int startLine = -AutoScrollPosition.Y / fontHeight;
            int endLine = (-AutoScrollPosition.Y + ClientHeight + (fontHeight - 1)) / fontHeight;
            RedrawRange(startLine, endLine);
        }

        private void RedrawSelection()
        {
            RedrawRange(selectStartLine, selectEndLine);
        }

        private void RedrawRange(int startLine, int endLine)
        {
            EnsureGraphicsObjects();
            using (Graphics graphics = CreateGraphics())
            {
                for (int i = startLine; i <= endLine; i++)
                {
                    RedrawLinePrimitive(graphics, i);
                }
            }
        }

        private void RedrawLine(int line)
        {
            if (textStorageFactory == null)
            {
                return;
            }

            EnsureGraphicsObjects();
            using (Graphics graphics = CreateGraphics())
            {
                RedrawLinePrimitive(graphics, line);
            }
        }

        private void RedrawLinePrimitive(Graphics graphics, int index)
        {
            if (DesignMode)
            {
                return;
            }

            Rectangle rect = new Rectangle(
                AutoScrollPosition.X,
                index * fontHeight + AutoScrollPosition.Y,
                Math.Max(currentWidth, ClientWidth),
                fontHeight);

            if (!graphics.IsVisible(rect))
            {
                return;
            }

            using (Graphics graphics2 = Graphics.FromImage(offscreenStrip))
            {
                Rectangle rect2 = new Rectangle(rect.X, 0, rect.Width, rect.Height);
                Point anchor = rect2.Location;

                graphics2.FillRectangle(normalBackBrush, rect2);

                if ((index < 0) || (index >= textStorage.Count) || (textStorageFactory == null))
                {
                    goto PutOnscreen;
                }

                bool tabsFound;
                IDecodedTextLine line = GetSpaceFromTabLineMustDispose(index, out tabsFound);
                int rtlXAdjust = 0;
                if (RightToLeft == RightToLeft.Yes)
                {
                    using (ITextInfo info = textService.AnalyzeText(
                        graphics2,
                        Font,
                        fontHeight,
                        line.Value))
                    {
                        rtlXAdjust = currentWidth - info.GetExtent(graphics2).Width;
                    }
                }

                if ((index < selectStartLine) || (index > selectEndLine) || !cursorEnabledFlag
                    || (hideSelectionOnFocusLost && !Focused))
                {
                    /* normal draw -- no part of the line is selected */
                    using (ITextInfo info = textService.AnalyzeText(
                        graphics2,
                        Font,
                        fontHeight,
                        line.Value))
                    {
                        info.DrawText(
                            graphics2,
                            offscreenStrip,
                            new Point(anchor.X + rtlXAdjust, anchor.Y),
                            ForeColor,
                            BackColor);
                    }
                }
                else
                {
                    if ((selectStartLine == selectEndLine) && (selectStartChar == selectEndCharPlusOne))
                    {
                        /* it's just an insertion point */
                        using (ITextInfo info = textService.AnalyzeText(
                            graphics2,
                            Font,
                            fontHeight,
                            line.Value))
                        {
                            info.DrawText(
                                graphics2,
                                offscreenStrip,
                                new Point(anchor.X + rtlXAdjust, anchor.Y),
                                ForeColor,
                                BackColor);
                        }

                        if (cursorDrawnFlag && Focused)
                        {
                            int screenX = ScreenXFromCharIndex(graphics2, index, selectStartChar, true/*forInsertionPoint*/);
                            graphics2.DrawLine(
                                normalForePen,
                                new Point(screenX + anchor.X + (RightToLeft == RightToLeft.Yes ? -1 : 0), 0),
                                new Point(screenX + anchor.X + (RightToLeft == RightToLeft.Yes ? -1 : 0), fontHeight - 1));
                        }
                    }
                    else
                    {
                        /* real live selection */

                        /* since SelectStart/End deals in chars, but we deal in columns */
                        /* (i.e. tabs expanded), we have to do a conversion: */
                        int selectStartColumn = 0;
                        Debug.Assert(selectStartLine <= index);
                        if (selectStartLine == index)
                        {
                            selectStartColumn = GetColumnFromCharIndex(selectStartLine, selectStartChar);
                        }
                        int selectEndColumnPlusOne = line.Length;
                        Debug.Assert(selectEndLine >= index);
                        if (selectEndLine == index)
                        {
                            selectEndColumnPlusOne = GetColumnFromCharIndex(selectEndLine, selectEndCharPlusOne);
                        }

                        using (ITextInfo info = textService.AnalyzeText(
                            graphics2,
                            Font,
                            fontHeight,
                            line.Value))
                        {
                            // draw full line of normal text
                            graphics2.FillRectangle(
                                normalBackBrush,
                                rect2);
                            info.DrawText(
                                graphics2,
                                offscreenStrip,
                                new Point(anchor.X + rtlXAdjust, anchor.Y),
                                ForeColor,
                                BackColor);

                            // draw highlighted region
                            using (Region highlight = info.BuildRegion(
                                graphics2,
                                new Point(anchor.X + rtlXAdjust, anchor.Y),
                                selectStartColumn,
                                selectEndLine == index ? selectEndColumnPlusOne : line.Length))
                            {
                                if (((RightToLeft != RightToLeft.Yes) && (selectEndLine != index))
                                    || ((RightToLeft == RightToLeft.Yes) && (selectStartLine != index)))
                                {
                                    Size extent = info.GetExtent(graphics);
                                    Rectangle rect3 = new Rectangle(
                                        RightToLeft != RightToLeft.Yes
                                            ? extent.Width + anchor.X + rtlXAdjust
                                            : anchor.X,
                                        0,
                                        RightToLeft != RightToLeft.Yes
                                            ? Math.Max(AutoScrollMinSize.Width, ClientWidth)
                                            : rtlXAdjust,
                                        fontHeight);
                                    highlight.Union(rect3);
                                }
                                graphics2.SetClip(
                                    highlight,
                                    CombineMode.Replace);
                                graphics2.FillRectangle(
                                    Focused ? selectedBackBrush : selectedBackBrushInactive,
                                    rect2);
                                info.DrawText(
                                    graphics2,
                                    offscreenStrip,
                                    new Point(anchor.X + rtlXAdjust, anchor.Y),
                                    Focused ? selectedForeColor : selectedForeColorInactive,
                                    Focused ? selectedBackColor : selectedBackColorInactive);
                                graphics2.SetClip(
                                    highlight,
                                    CombineMode.Replace);
                            }
                        }

                        // show active end
                        graphics2.SetClip(rect2);
                        if (cursorDrawnFlag && Focused)
                        {
                            int activeLine, activeChar;
                            if (selectStartIsActive)
                            {
                                activeLine = selectStartLine;
                                activeChar = selectStartChar;
                            }
                            else
                            {
                                activeLine = selectEndLine;
                                activeChar = selectEndCharPlusOne;
                            }
                            if (activeLine == index)
                            {
                                int screenX = ScreenXFromCharIndex(graphics2, activeLine, activeChar, true/*forInsertionPoint*/);
                                graphics2.DrawLine(
                                    normalForePen,
                                    new Point(screenX + anchor.X + (RightToLeft == RightToLeft.Yes ? -1 : 0), 0),
                                    new Point(screenX + anchor.X + (RightToLeft == RightToLeft.Yes ? -1 : 0), fontHeight - 1));
                            }
                        }
                    }
                }
            }

        PutOnscreen:
            graphics.DrawImage(offscreenStrip, new Rectangle(0, rect.Y, ClientWidth, fontHeight));
        }

        public static void GetSpaceFromTabLineLength(string line, int spacesPerTab, out int length, out bool tabsFound)
        {
            if (spacesPerTab < 0)
            {
                throw new ArgumentException();
            }

            length = 0;
            tabsFound = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\t')
                {
                    length += spacesPerTab - (length % spacesPerTab);
                    tabsFound = true;
                }
                else
                {
                    length += 1;
                }
            }
        }

        public static void GetSpaceFromTabLine(string line, int spacesPerTab, char[] chars, int length)
        {
            int targetIndex = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\t')
                {
                    int index = spacesPerTab - (targetIndex % spacesPerTab);
                    while (index > 0)
                    {
                        chars[targetIndex] = ' ';
                        targetIndex++;
                        index--;
                    }
                }
                else
                {
                    chars[targetIndex] = line[i];
                    targetIndex++;
                }
            }
            Debug.Assert(length == targetIndex);
        }

        public static string GetSpaceFromTabLine(string line, int spacesPerTab)
        {
            if (spacesPerTab < 0)
            {
                throw new ArgumentException();
            }

            int length;
            bool tabsFound;
            GetSpaceFromTabLineLength(line, spacesPerTab, out length, out tabsFound);

            if ((line.Length != length) || tabsFound)
            {
                char[] chars = new char[length];
                GetSpaceFromTabLine(line, spacesPerTab, chars, length);
                return new String(chars);
            }
            else
            {
                return line;
            }
        }

        protected IDecodedTextLine GetSpaceFromTabLineMustDispose(int index, out bool tabsFound)
        {
            IDecodedTextLine decodedLine = textStorage[index].Decode_MustDispose();
            int length;
            GetSpaceFromTabLineLength(decodedLine.Value, spacesPerTab, out length, out tabsFound);

            using (Pin<char[]> pinChars = new Pin<char[]>(new char[length]))
            {
                GetSpaceFromTabLine(decodedLine.Value, spacesPerTab, pinChars.Ref, length);
                return textStorageFactory.NewDecoded_MustDispose(pinChars.Ref, 0, length);
            }
        }

        /* find out the pixel index of the left edge of the specified character */
        public int ScreenXFromCharIndex(Graphics graphics, int lineIndex, int charIndex, bool forInsertionPoint)
        {
            bool tabsFound;
            IDecodedTextLine decodedSpacedLine = GetSpaceFromTabLineMustDispose(lineIndex, out tabsFound);
            using (ITextInfo info = textService.AnalyzeText(graphics, Font, fontHeight, decodedSpacedLine.Value))
            {
                int columnIndex = GetColumnFromCharIndex(lineIndex, charIndex);
                int indent;
                // for cursorAdvancing, see https://msdn.microsoft.com/en-us/library/windows/desktop/dd317793%28v=vs.85%29.aspx
                bool cursorAdvancing = false;
                int adjust = 0;
                if (forInsertionPoint && (columnIndex > 0))
                {
                    cursorAdvancing = this.cursorAdvancing;
                    adjust = cursorAdvancing ? -1 : 0;
                }
                info.CharPosToX(graphics, columnIndex + adjust, cursorAdvancing/*trailing*/, out indent);
                if (RightToLeft == RightToLeft.Yes)
                {
                    indent += currentWidth - info.GetExtent(graphics).Width;
                }
                return indent;
            }
        }

        public int ScreenXFromCharIndex(Graphics graphics, int lineIndex, int charIndex)
        {
            return ScreenXFromCharIndex(graphics, lineIndex, charIndex, false);
        }

        /* convert a pixel position into the nearest character */
        public int CharIndexFromScreenX(Graphics graphics, int lineIndex, int screenX)
        {
            int columnIndex = 0;

            if ((lineIndex < 0) || (lineIndex >= textStorage.Count))
            {
                return 0;
            }
            bool tabsFound;
            IDecodedTextLine decodedSpacedLine = GetSpaceFromTabLineMustDispose(lineIndex, out tabsFound);
            using (ITextInfo info = textService.AnalyzeText(graphics, Font, fontHeight, decodedSpacedLine.Value))
            {
                if (RightToLeft == RightToLeft.Yes)
                {
                    screenX -= currentWidth - info.GetExtent(graphics).Width;
                }
                bool trailing;
                info.XToCharPos(graphics, screenX, out columnIndex, out trailing);
            }
            /* now we have the column index, with tabs expanded; we have to figure */
            /* out what the character index is, with tabs left intact */
            IDecodedTextLine decodedLine = textStorage[lineIndex].Decode_MustDispose();
            int index = 0;
            int column = 0;
            while (column < columnIndex)
            {
                Debug.Assert(index < decodedLine.Length);
                int width = 1;
                if (decodedLine.Value[index] == '\t')
                {
                    width = spacesPerTab - (column % spacesPerTab);
                    if (!(column + width / 2 < columnIndex))
                    {
                        break;
                    }
                }
                index++;
                column += width;
            }
            return index;
        }

        /* given a character index, calculate where the corresponding position is */
        /* when tabs have been converted into spaces */
        private int GetColumnFromCharIndex(IDecodedTextLine decodedLine, int charIndex)
        {
            if (charIndex > decodedLine.Length)
            {
                charIndex = decodedLine.Length;
            }
            Debug.Assert((charIndex >= 0) || (charIndex < decodedLine.Length));
            int column = 0;
            for (int i = 0; i < charIndex; i++)
            {
                if (decodedLine[i] == '\t')
                {
                    column += spacesPerTab - (column % spacesPerTab);
                }
                else
                {
                    column += 1;
                }
            }
            return column;
        }

        public int GetColumnFromCharIndex(int lineIndex, int charIndex)
        {
            IDecodedTextLine decodedLine = textStorage[lineIndex].Decode_MustDispose();
            return GetColumnFromCharIndex(decodedLine, charIndex);
        }

        private int GetCharIndexFromColumn(IDecodedTextLine decodedLine, int column)
        {
            int columnIndex = 0;
            for (int i = 0; i < decodedLine.Length; i++)
            {
                if (decodedLine[i] == '\t')
                {
                    columnIndex += spacesPerTab - (columnIndex % spacesPerTab);
                }
                else
                {
                    columnIndex += 1;
                }
                if (columnIndex >= column)
                {
                    return i;
                }
            }
            return decodedLine.Length;
        }

        public int GetCharIndexFromColumn(int lineIndex, int column)
        {
            IDecodedTextLine decodedLine = textStorage[lineIndex].Decode_MustDispose();
            return GetCharIndexFromColumn(decodedLine, column);
        }

        public void SetStickyX()
        {
            if (textStorage != null)
            {
                using (Graphics graphics = CreateGraphics())
                {
                    stickyX = ScreenXFromCharIndex(
                        graphics,
                        selectStartIsActive ? selectStartLine : selectEndLine,
                        selectStartIsActive ? selectStartChar : selectEndCharPlusOne,
                        true/*forInsertionPoint*/);
                }
            }
        }


        // misc

        public virtual void Reload(
            ITextStorageFactory factory,
            ITextStorage storage)
        {
            this.textStorageFactory = factory;

            this.textStorage = factory.Take(storage);
            stickyX = 0;
            SetInsertionPoint(0, 0);
            ResetCanvasSize();
        }


        // public configuration properties

        [Category("Appearance"), DefaultValue(typeof(Color), "Highlight")]
        public Color SelectedBackColor { get { return selectedBackColor; } set { selectedBackColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "HighlightText")]
        public Color SelectedForeColor { get { return selectedForeColor; } set { selectedForeColor = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "GradientInactiveCaption")]
        public Color SelectedBackColorInactive { get { return selectedBackColorInactive; } set { selectedBackColorInactive = value; } }

        [Category("Appearance"), DefaultValue(typeof(Color), "ControlText")]
        public Color SelectedForeColorInactive { get { return selectedForeColorInactive; } set { selectedForeColorInactive = value; } }

        [Category("Behavior"), DefaultValue(false)]
        public bool SimpleNavigation { get { return simpleNavigation; } set { simpleNavigation = value; } }

#if WINDOWS
        [Category("Appearance"), DefaultValue(TextService.Uniscribe)]
#else
        [Category("Appearance"), DefaultValue(TextService.Simple)]
#endif
        public TextService TextService
        {
            get
            {
                return textService.Service;
            }
            set
            {
                if (value == textService.Service)
                {
                    return;
                }

                ITextService newService;
                switch (value)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case TextService.Simple:
                        newService = new TextServiceSimple();
                        break;
#if WINDOWS
                    case TextService.Uniscribe:
                        newService = new TextServiceUniscribe();
                        break;
                    case TextService.DirectWrite:
                        if (!String.Equals(System.Diagnostics.Process.GetCurrentProcess().ProcessName, "devenv"))
                        {
                            // attempt to load unprotected
                            newService = new TextServiceDirectWrite();
                        }
                        else
                        {
                            // In design mode, VS dll isolation is causing grief with indirect dependencies.
                            // Try to create (try here - constructor can't because method fails before invocation due to
                            // missing dependency) and fall back to Uniscribe if not available.
                            try
                            {
                                newService = new TextServiceDirectWrite();
                            }
                            catch (Exception exception)
                            {
                                MessageBox.Show(String.Format("{0}: Failed to create DirectWrite wrapper - falling back to Uniscribe ({1})", this.GetType().Name, exception.Message));
                                newService = new TextServiceUniscribe();
                            }
                        }
                        break;
#endif
                }
                textService.Dispose();
                textService = newService;
                textService.Reset(Font, ClientWidth);
                ResetCanvasSize();
            }
        }

        [Browsable(true), Category("Behavior"), DefaultValue(true)]
        public bool SelectionEnabled
        {
            get
            {
                return cursorEnabledFlag;
            }
            set
            {
                bool changed = cursorEnabledFlag != value;
                cursorEnabledFlag = value;
                if (changed)
                {
                    RedrawSelection();
                }
            }
        }

        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool HideSelection { get { return hideSelectionOnFocusLost; } set { hideSelectionOnFocusLost = value; } }

        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool SelectAllOnGotFocus { get { return selectAllOnEnable; } set { selectAllOnEnable = value; } }

        [Browsable(true), Category("Appearance"), DefaultValue(8)]
        public int TabSize
        {
            get
            {
                return spacesPerTab;
            }
            set
            {
                if ((value < 1) || (value > 255))
                {
                    throw new ArgumentException();
                }
                bool changed = spacesPerTab != value;
                spacesPerTab = value;
                if (changed)
                {
                    ResetCanvasSize();
                    SetStickyX();
                    Redraw();
                }
            }
        }


        // public selection access methods

        [Browsable(false)]
        public bool SelectionNonEmpty
        {
            get
            {
                return (selectStartLine != selectEndLine) || (selectStartChar != selectEndCharPlusOne);
            }
        }

        [Browsable(false)]
        public int SelectionStartLine
        {
            get
            {
                return selectStartLine;
            }
        }

        [Browsable(false)]
        public int SelectionEndLine
        {
            get
            {
                return selectEndLine;
            }
        }

        [Browsable(false)]
        public int SelectionStartChar
        {
            get
            {
                return selectStartChar;
            }
        }

        /* get the index of the character after the end of the selection.  if the starting */
        /* line == ending line, and starting char == last char + 1, then there is no */
        /* selection, but instead an insertion point */
        [Browsable(false)]
        public int SelectionEndCharPlusOne
        {
            get
            {
                return selectEndCharPlusOne;
            }
        }

        [Browsable(false)]
        public bool SelectionStartIsActive
        {
            get
            {
                return selectStartIsActive;
            }
        }

        [Browsable(false)]
        public int SelectionActiveLine
        {
            get
            {
                return selectStartIsActive ? selectStartLine : selectEndLine;
            }
        }

        [Browsable(false)]
        public int SelectionActiveChar
        {
            get
            {
                return selectStartIsActive ? selectStartChar : selectEndCharPlusOne;
            }
        }

        [Browsable(false)]
        public SelPoint SelectionStart
        {
            get
            {
                return new SelPoint(selectStartLine, selectStartChar);
            }
        }

        [Browsable(false)]
        public SelPoint SelectionEnd
        {
            get
            {
                return new SelPoint(selectEndLine, selectEndCharPlusOne);
            }
        }

        [Browsable(false)]
        public SelPoint SelectionActive
        {
            get
            {
                return new SelPoint(SelectionActiveLine, SelectionActiveChar);
            }
        }

        [Browsable(false)]
        public SelRange Selection
        {
            get
            {
                return new SelRange(selectStartLine, selectStartChar, selectEndLine, selectEndCharPlusOne);
            }
        }

        [Browsable(false)]
        public SelPoint End
        {
            get
            {
                return new SelPoint(textStorage.Count - 1, textStorage[textStorage.Count - 1].Length);
            }
        }

        [Browsable(false)]
        public SelRange All
        {
            get
            {
                return new SelRange(new SelPoint(0, 0), End);
            }
        }

        public event EventHandler SelectionChanged;

        protected virtual void OnSelectionChanged()
        {
            if (SelectionChanged != null)
            {
                SelectionChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public void GetSelectionExtent(
            out int selectStartLine,
            out int selectStartChar,
            out int selectEndLine,
            out int selectEndCharPlusOne,
            out bool selectStartIsActive)
        {
            selectStartLine = this.selectStartLine;
            selectStartChar = this.selectStartChar;
            selectEndLine = this.selectEndLine;
            selectEndCharPlusOne = this.selectEndCharPlusOne;
            selectStartIsActive = this.selectStartIsActive;
        }

        private void AdjustForSurrogatePair(
            int line,
            ref int column,
            bool right)
        {
            ITextLine text = textStorage[line];
            if (column < text.Length)
            {
                IDecodedTextLine decodedText = text.Decode_MustDispose();
                if (Char.IsLowSurrogate(decodedText[column]))
                {
                    column = Math.Max(0, column + (right ? 1 : -1));
                }
            }
        }

        private void SetSelectionInternal(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne)
        {
            SelPoint oldSelectionActive = SelectionActive;
            if ((startLine < 0) || (startChar < 0) || (endLine < 0) || (endCharPlusOne < 0))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startLine > endLine) || ((startLine == endLine) && (startChar > endCharPlusOne)))
            {
                // Start line after end line
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if (startLine > textStorage.Count - 1)
            {
                startLine = textStorage.Count - 1;
                startChar = textStorage[startLine].Length;
            }
            if (endLine > textStorage.Count - 1)
            {
                endLine = textStorage.Count - 1;
                endCharPlusOne = textStorage[endLine].Length;
            }
            if (startChar > textStorage[startLine].Length)
            {
                startChar = textStorage[startLine].Length;
            }
            if (endCharPlusOne > textStorage[endLine].Length)
            {
                endCharPlusOne = textStorage[endLine].Length;
            }
            if ((startLine == endLine) && (startChar == endCharPlusOne))
            {
                /* make an insertion point always show up right away in it's new location */
                cursorDrawnFlag = true;
                AdjustForSurrogatePair(startLine, ref startChar, false/*right*/);
                endCharPlusOne = startChar;
            }
            else
            {
                AdjustForSurrogatePair(startLine, ref startChar, false/*right*/);
                AdjustForSurrogatePair(endLine, ref endCharPlusOne, true/*right*/);
            }
            int oldSelectStartLine = selectStartLine;
            int oldSelectEndLine = selectEndLine;
            selectStartLine = startLine;
            selectEndLine = endLine;
            selectStartChar = startChar;
            selectEndCharPlusOne = endCharPlusOne;
            cursorAdvancing = SelectionActive >= oldSelectionActive;
            if ((startLine > oldSelectEndLine) || (endLine < oldSelectStartLine))
            {
                /* old and new selection ranges are disjoint */
                RedrawRange(oldSelectStartLine, oldSelectEndLine);
                RedrawRange(startLine, endLine);
            }
            else if ((startLine >= oldSelectStartLine) && (startLine <= oldSelectEndLine))
            {
                int begin;
                int end;

                /* |  old selection   |.......|  */
                /*    |   new selection   | */
                /* we want to draw the line that overlaps due to partial selection */
                RedrawRange(oldSelectStartLine, startLine);
                if (oldSelectEndLine < endLine)
                {
                    begin = oldSelectEndLine;
                    end = endLine;
                }
                else
                {
                    begin = endLine;
                    end = oldSelectEndLine;
                }
                RedrawRange(begin, end);
            }
            else if ((oldSelectStartLine >= startLine) && (oldSelectStartLine <= endLine))
            {
                int begin;
                int end;

                /*    |   old selection  | */
                /* |  new selection   |.....| */
                RedrawRange(startLine, oldSelectStartLine);
                if (oldSelectEndLine < endLine)
                {
                    begin = oldSelectEndLine;
                    end = endLine;
                }
                else
                {
                    begin = endLine;
                    end = oldSelectEndLine;
                }
                RedrawRange(begin, end);
            }
            else
            {
                int min;
                int max;

                /* we don't know what the hell is going on */
                if (oldSelectStartLine < startLine)
                {
                    min = oldSelectStartLine;
                }
                else
                {
                    min = startLine;
                }
                if (oldSelectEndLine > endLine)
                {
                    max = oldSelectEndLine;
                }
                else
                {
                    max = endLine;
                }
                RedrawRange(min, max);
            }
        }

        public void SetSelection(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne)
        {
            SetSelectionInternal(
                startLine,
                startChar,
                endLine,
                endCharPlusOne);
            SetStickyX();

            OnSelectionChanged();
        }

        public void SetSelection(SelPoint start, SelPoint end)
        {
            SetSelection(start.Line, start.Column, end.Line, end.Column);
        }

        public void SetSelection(SelRange range)
        {
            SetSelection(range.Start.Line, range.Start.Column, range.End.Line, range.End.Column);
        }

        public void SetSelection(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne,
            bool selectStartIsActive)
        {
            if ((startLine > endLine) || ((startLine == endLine) && (startChar > endCharPlusOne)))
            {
                int temp;

                temp = startLine;
                startLine = endLine;
                endLine = temp;
                temp = startChar;

                startChar = endCharPlusOne;
                endCharPlusOne = temp;
                selectStartIsActive = !selectStartIsActive;
            }
            SetSelectionInternal(
                startLine,
                startChar,
                endLine,
                endCharPlusOne);
            this.selectStartIsActive = selectStartIsActive;
            SetStickyX();

            OnSelectionChanged();
        }

        public void SetSelection(SelPoint start, SelPoint end, bool selectStartIsActive)
        {
            SetSelection(start.Line, start.Column, end.Line, end.Column, selectStartIsActive);
        }

        public void SetSelection(SelRange region, bool selectStartIsActive)
        {
            SetSelection(region.Start.Line, region.Start.Column, region.End.Line, region.End.Column, selectStartIsActive);
        }

        public void SetInsertionPoint(int lineIndex, int charIndex)
        {
            SetSelection(lineIndex, charIndex, lineIndex, charIndex);
        }

        public void SetInsertionPoint(SelPoint where)
        {
            SetSelection(where.Line, where.Column, where.Line, where.Column);
        }

        public void SetSelectionLine(int lineIndex)
        {
            lineIndex = Math.Min(Math.Max(lineIndex, 0), textStorage.Count - 1);
            SetSelection(lineIndex, 0, lineIndex, GetLine(lineIndex).Length);
        }

        public void MoveToStart()
        {
            SetInsertionPoint(0, 0);
        }

        public void MoveToEnd()
        {
            SetInsertionPoint(textStorage.Count - 1, textStorage[textStorage.Count - 1].Length);
        }

        public void SelectAll()
        {
            SetSelection(0, 0, textStorage.Count - 1, textStorage[textStorage.Count - 1].Length);
        }

        public void SwapEnd()
        {
            selectStartIsActive = !selectStartIsActive;
            RedrawLine(selectStartLine);
            if (selectStartLine != selectEndLine)
            {
                RedrawLine(selectEndLine);
            }
            SetStickyX();

            OnSelectionChanged();
        }

        public void SetActiveEnd(bool start)
        {
            if (start != selectStartIsActive)
            {
                SwapEnd();
            }
        }

        private void ScrollTo(int startLine, int startChar, int endLine, int endCharPlusOne)
        {
            using (Graphics graphics = CreateGraphics())
            {
                /* figure out how much space to leave at bottom and top edges */
                int check = Math.Min(4 * fontHeight, Math.Max(ClientHeight / 2 - 4 * fontHeight, 0));

                /* vertical adjustment */
                if (((startLine * fontHeight >= -AutoScrollPosition.Y + check)
                        && (startLine < -AutoScrollPosition.Y + (textStorage.Count * fontHeight - check)))
                    || ((startLine * fontHeight < check)
                        && (startLine * fontHeight >= -AutoScrollPosition.Y))
                    || (ClientHeight < fontHeight))
                {
                    /* beginning of selection is in the box, so try to center the end */
                    if ((endLine * fontHeight < -AutoScrollPosition.Y + check)
                        || (ClientHeight < fontHeight))
                    {
                        /* selection is too far up */
                        AutoScrollPosition = new Point(
                            -AutoScrollPosition.X,
                            endLine * fontHeight - check);
                    }
                    else if (endLine * fontHeight >= -AutoScrollPosition.Y + (ClientHeight - fontHeight - check))
                    {
                        /* selection is too far down */
                        AutoScrollPosition = new Point(
                            -AutoScrollPosition.X,
                            endLine * fontHeight - (ClientHeight - fontHeight - check));
                    }
                }
                else
                {
                    /* center the beginning in the box */
                    if (startLine * fontHeight < -AutoScrollPosition.Y + check)
                    {
                        /* selection is to far up */
                        AutoScrollPosition = new Point(
                            -AutoScrollPosition.X,
                            startLine * fontHeight - check);
                    }
                    else if (startLine * fontHeight >= -AutoScrollPosition.Y + (ClientHeight - check))
                    {
                        /* selection is too far down */
                        AutoScrollPosition = new Point(
                            -AutoScrollPosition.X,
                            startLine * fontHeight - (ClientHeight - check));
                    }
                }

                RecomputeCanvasSizeIncremental();

                /* figure out how much space on left and right edges to leave */
                check = Math.Min(32, Math.Max(ClientWidth / 2 - 32, 0));

                /* horizontal adjustment */
                if ((startLine == endLine) && (startChar == endCharPlusOne))
                {
                    /* only adjust left-to-right if it's an insertion point */
                    if (ScreenXFromCharIndex(graphics, startLine, startChar)
                        < -AutoScrollPosition.X + check)
                    {
                        AutoScrollPosition = new Point(
                            (int)ScreenXFromCharIndex(graphics, startLine, startChar)
                                - (2 * check),
                            -AutoScrollPosition.Y);
                    }
                    if (ScreenXFromCharIndex(graphics, startLine, startChar)
                        > -AutoScrollPosition.X + ClientWidth - check)
                    {
                        AutoScrollPosition = new Point(
                            (int)ScreenXFromCharIndex(graphics, startLine, startChar)
                                - ClientWidth + (2 * check),
                            -AutoScrollPosition.Y);
                    }
                }
            }

            Update();
        }

        public void ScrollToSelection()
        {
            ScrollTo(selectStartLine, selectStartChar, selectEndLine, selectEndCharPlusOne);
        }

        public void ScrollToSelectionStartEdge()
        {
            ScrollTo(selectStartLine, selectStartChar, selectStartLine, selectStartChar);
        }

        public void ScrollToSelectionEndEdge()
        {
            ScrollTo(selectEndLine, selectEndCharPlusOne, selectEndLine, selectEndCharPlusOne);
        }

        public void ScrollToSelectionActiveEnd()
        {
            if (SelectionStartIsActive)
            {
                ScrollToSelectionStartEdge();
            }
            else
            {
                ScrollToSelectionEndEdge();
            }
        }


        // internal methods for handling mouse and keyboard

        /* find the union of two selection ranges */
        private static void UnionSelection(
            SelPoint a,
            SelPoint b,
            out SelPoint startOut,
            out SelPoint endOut,
            out bool startIsActiveOut)
        {
            bool startIsActive = false;

            if (a.CompareTo(b) > 0)
            {
                SelPoint temp;

                temp = a;
                a = b;
                b = temp;

                startIsActive = true;
            }

            startOut = a;
            endOut = b;
            startIsActiveOut = startIsActive;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (textStorageFactory == null)
            {
                return;
            }
            if (!Focused)
            {
                Focus();
            }

            timerCursorBlink.Stop();

            if (DateTime.UtcNow - lastClickTime > new TimeSpan(0, 0, 0, 0, SystemInformation.DoubleClickTime/*msec*/))
            {
                lastClickX = e.X;
                lastClickY = e.Y;
                clickPhase = 1;
                lastClickTime = DateTime.UtcNow;
            }
            else
            {
                if (((e.X - lastClickX <= 3) && (e.X - lastClickX >= -3))
                    && ((e.Y - lastClickY <= 3) && (e.Y - lastClickY >= -3)))
                {
                    clickPhase = Math.Min(clickPhase + 1, 3);
                }
                else
                {
                    clickPhase = 1;
                }
            }

            int localClickPhase = clickPhase;

            using (Graphics graphics = CreateGraphics())
            {
                SelPoint pivotPoint = new SelPoint(); /* where the mouse first hits, and if shift is down, then it's the previous range */
                SelPoint currentMousePoint = new SelPoint();
                int whereX;
                int whereY;

                whereX = e.X - AutoScrollPosition.X;
                whereY = e.Y - AutoScrollPosition.Y;

                currentMousePoint.Line = whereY / fontHeight;
                currentMousePoint.Column = CharIndexFromScreenX(graphics, currentMousePoint.Line, whereX);
                if ((ModifierKeys & Keys.Shift) != 0)
                {
                    pivotPoint.Line = selectStartIsActive
                        ? selectEndLine
                        : selectStartLine;
                    pivotPoint.Column = selectStartIsActive
                        ? selectEndCharPlusOne
                        : selectStartChar;
                }
                else
                {
                    if (SelectionNonEmpty)
                    {
                        SelPoint start = new SelPoint(), end = new SelPoint();

                        start.Line = selectStartLine;
                        start.Column = selectStartChar;
                        end.Line = selectEndLine;
                        end.Column = selectEndCharPlusOne;
                        SetInsertionPoint(
                            start.Line,
                            start.Column);
                        RedrawRange(
                            start.Line,
                            end.Line);
                    }
                    pivotPoint = currentMousePoint;
                    SetInsertionPoint(
                        pivotPoint.Line,
                        pivotPoint.Column);
                }

                /* enter the selection tracking loop */
                // Old Macintosh program used local modal event pump.
                MouseEventHandler mouseMove = new MouseEventHandler(delegate (object sender, MouseEventArgs ee)
                {
                    TextViewControl_MouseMoveCapture_SelectionLocalLoopBody(
                        sender,
                        ee,
                        pivotPoint,
                        localClickPhase);
                });
                MouseMoveCapture += mouseMove;
                // must invoke at least once even if mouse doesn't move
                mouseMove(null, new MouseEventArgs(e.Button, localClickPhase, e.X, e.Y, 0));
            }
        }

        private void TextViewControl_MouseMoveCapture_SelectionLocalLoopBody(
            object sender,
            MouseEventArgs e,
            SelPoint pivotPoint,
            int clickPhase)
        {
            using (Graphics graphics = CreateGraphics())
            {
                bool newActiveEnd;
                SelPoint extendedPivotPoint = new SelPoint();

                int whereX = e.X;
                int whereY = e.Y;
                if (whereY < 0)
                {
                    VerticalScroll.Value = Math.Max(VerticalScroll.Value - fontHeight, VerticalScroll.Minimum);
                }
                else if (whereY > ClientHeight)
                {
                    VerticalScroll.Value = Math.Min(VerticalScroll.Value + fontHeight, VerticalScroll.Maximum);
                }
                else if (whereX < 0)
                {
                    HorizontalScroll.Value = Math.Max(HorizontalScroll.Value - 24, HorizontalScroll.Minimum);
                }
                else if (whereX > ClientWidth)
                {
                    HorizontalScroll.Value = Math.Min(HorizontalScroll.Value + 24, HorizontalScroll.Maximum);
                }

                whereX = e.X - AutoScrollPosition.X;
                whereY = e.Y - AutoScrollPosition.Y;

                if (whereX < 0)
                {
                    whereX = 0;
                }
                if (whereX > currentWidth - 1)
                {
                    whereX = currentWidth - 1;
                }
                if (whereY < 0)
                {
                    whereY = 0;
                }
                if (whereY > textStorage.Count * fontHeight - 1)
                {
                    whereY = textStorage.Count * fontHeight - 1;
                }
                SelPoint currentMousePoint = new SelPoint();
                currentMousePoint.Line = whereY / fontHeight;
                currentMousePoint.Column = CharIndexFromScreenX(graphics,
                    currentMousePoint.Line, whereX);
                /* calculate what the extent of the current mouse selection should be */
                extendedPivotPoint = pivotPoint;
                if (pivotPoint.CompareTo(currentMousePoint) >= 0)
                {
                    SelPoint unused = new SelPoint();
                    ExtendSelection(
                        graphics,
                        clickPhase,
                        ref unused,
                        false,
                        ref extendedPivotPoint,
                        true);
                    ExtendSelection(
                        graphics,
                        clickPhase,
                        ref currentMousePoint,
                        true,
                        ref unused,
                        false);
                }
                else
                {
                    SelPoint unused = new SelPoint();
                    ExtendSelection(
                        graphics,
                        clickPhase,
                        ref extendedPivotPoint,
                        true,
                        ref unused,
                        false);
                    ExtendSelection(
                        graphics,
                        clickPhase,
                        ref unused,
                        false,
                        ref currentMousePoint,
                        true);
                }
                /* calculating the total selection */
                SelPoint tempFirst, tempLast;
                UnionSelection(
                    extendedPivotPoint,
                    currentMousePoint,
                    out tempFirst,
                    out tempLast,
                    out newActiveEnd);
                SetSelection(
                    tempFirst.Line,
                    tempFirst.Column,
                    tempLast.Line,
                    tempLast.Column,
                    newActiveEnd);
                if (!SelectionNonEmpty)
                {
                    cursorDrawnFlag = true;
                }
            }

            Update();
        }

        /* extend the selection using the current mouse-click state (single, double, triple) */
        private void ExtendSelection(
            Graphics graphics,
            int clickPhase,
            ref SelPoint start,
            bool startValid,
            ref SelPoint end,
            bool endValid)
        {
            if (clickPhase == 2)
            {
                if (startValid && (start.Line >= 0) && (start.Line < textStorage.Count))
                {
                    IDecodedTextLine decodedLine = textStorage[start.Line].Decode_MustDispose();
                    using (ITextInfo info = !simpleNavigation
                        ? textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value)
                        : new TextServiceSimple().AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                    {
                        int previous;
                        info.PreviousWordBoundary(start.Column, out previous);
                        start.Column = previous;
                    }
                }
                if (endValid && (end.Line >= 0) && (end.Line < textStorage.Count))
                {
                    IDecodedTextLine decodedLine = textStorage[end.Line].Decode_MustDispose();
                    using (ITextInfo info = !simpleNavigation
                        ? textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value)
                        : new TextServiceSimple().AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                    {
                        int next;
                        info.NextWordBoundary(end.Column, out next);
                        end.Column = next;
                    }
                }
            }
            else if (clickPhase == 3)
            {
                if (startValid)
                {
                    start.Column = 0;
                }
                if (endValid && (end.Column != 0))
                {
                    end.Line += 1;
                    end.Column = 0;
                }
            }
        }

        private event MouseEventHandler MouseMoveCapture;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            using (Graphics graphics = CreateGraphics())
            {
                if (MouseMoveCapture != null)
                {
                    MouseMoveCapture.Invoke(this, e);
                    return;
                }

                // uncaptured mouse move behavior:
                this.Cursor = Cursors.IBeam;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            timerCursorBlink.Start();

            // one last call to MoveMouseCapture()
            if (MouseMoveCapture != null)
            {
                MouseMoveCapture.Invoke(this, e);
            }

            MouseMoveCapture = null;
            if (cursorEnabledFlag && !SelectionNonEmpty)
            {
                cursorDrawnFlag = true;
                RedrawLine(selectStartLine);
                //timerCursorBlink.Stop();
                //timerCursorBlink.Start();
            }
        }

        private void MoveExtend(int line, int column, bool extend)
        {
            if (!extend)
            {
                SetInsertionPoint(line, column);
            }
            else
            {
                SetSelection(
                    selectStartIsActive ? line : selectStartLine,
                    selectStartIsActive ? column : selectStartChar,
                    selectStartIsActive ? selectEndLine : line,
                    selectStartIsActive ? selectEndCharPlusOne : column,
                    selectStartIsActive);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys code = keyData & (Keys.KeyCode | Keys.Control);
            switch (code)
            {
                case Keys.Space | Keys.Control:

                case Keys.Home:
                case Keys.Left:
                case Keys.Left | Keys.Control:

                case Keys.End:
                case Keys.Right:
                case Keys.Right | Keys.Control:

                case Keys.PageUp:
                case Keys.PageDown:

                case Keys.Home | Keys.Control:
                case Keys.Up:
                case Keys.Up | Keys.Control:

                case Keys.End | Keys.Control:
                case Keys.Down:
                case Keys.Down | Keys.Control:

                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            Debug.Assert(MouseMoveCapture == null); // TODO: we may have to block this if it actually happens

            base.OnKeyDown(e);
            if (e.Handled)
            {
                return;
            }
            if (textStorageFactory == null)
            {
                return;
            }

            using (Graphics graphics = CreateGraphics())
            {
                bool extend = ((e.KeyData & Keys.Shift) != 0);

                Keys code = e.KeyData & (Keys.KeyCode | Keys.Control);
                switch (code)
                {
                    case Keys.Home:
                    case Keys.Left:
                    case Keys.Left | Keys.Control:
                        if (SelectionNonEmpty && !extend)
                        {
                            if (((e.KeyData & Keys.Control) != 0) && !selectStartIsActive)
                            {
                                // set active end to start
                                SetActiveEnd(true/*start*/);
                            }
                            else
                            {
                                // collapse to IP
                                SetInsertionPoint(selectStartLine, selectStartChar);
                            }
                        }
                        else
                        {
                            int lineIndex = selectStartIsActive ? selectStartLine : selectEndLine;
                            int index = selectStartIsActive ? selectStartChar : selectEndCharPlusOne;
                            IDecodedTextLine decodedLine = textStorage[lineIndex].Decode_MustDispose();
                            if (!SelectionNonEmpty || extend)
                            {
                                if (index > 0)
                                {
                                    if (code == Keys.Home)
                                    {
                                        index = 0;
                                    }
                                    else if ((e.KeyData & Keys.Control) != 0)
                                    {
                                        using (ITextInfo info = !simpleNavigation
                                            ? textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value)
                                            : new TextServiceSimple().AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                                        {
                                            int previous;
                                            info.PreviousWordBoundary(index, out previous);
                                            index = previous;
                                        }
                                    }
                                    else
                                    {
                                        using (ITextInfo info = !simpleNavigation
                                            ? textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value)
                                            : new TextServiceSimple().AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                                        {
                                            int previous;
                                            info.PreviousCharBoundary(index, out previous);
                                            index = previous;
                                        }
                                    }
                                }
                                else
                                {
                                    if (lineIndex > 0)
                                    {
                                        lineIndex -= 1;
                                        index = textStorage[lineIndex].Length;
                                    }
                                }
                            }
                            MoveExtend(lineIndex, index, extend);
                        }
                        SetStickyX();
                        ScrollToSelectionActiveEnd();
                        e.Handled = true;
                        break;

                    case Keys.End:
                    case Keys.Right:
                    case Keys.Right | Keys.Control:
                        if (SelectionNonEmpty && !extend)
                        {
                            if (((e.KeyData & Keys.Control) != 0) && selectStartIsActive)
                            {
                                // set active end to end
                                SetActiveEnd(false/*start*/);
                            }
                            else
                            {
                                // collapse to IP
                                SetInsertionPoint(selectEndLine, selectEndCharPlusOne);
                            }
                        }
                        else
                        {
                            int lineIndex = selectStartIsActive ? selectStartLine : selectEndLine;
                            int index = selectStartIsActive ? selectStartChar : selectEndCharPlusOne;
                            IDecodedTextLine decodedLine = textStorage[lineIndex].Decode_MustDispose();
                            if (!SelectionNonEmpty || extend)
                            {
                                if (index < decodedLine.Length)
                                {
                                    if (code == Keys.End)
                                    {
                                        index = decodedLine.Length;
                                    }
                                    else if ((e.KeyData & Keys.Control) != 0)
                                    {
                                        using (ITextInfo info = !simpleNavigation
                                            ? textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value)
                                            : new TextServiceSimple().AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                                        {
                                            int next;
                                            info.NextWordBoundary(index, out next);
                                            index = next;
                                        }
                                    }
                                    else
                                    {
                                        using (ITextInfo info = !simpleNavigation
                                            ? textService.AnalyzeText(graphics, Font, fontHeight, decodedLine.Value)
                                            : new TextServiceSimple().AnalyzeText(graphics, Font, fontHeight, decodedLine.Value))
                                        {
                                            int next;
                                            info.NextCharBoundary(index, out next);
                                            index = next;
                                        }
                                    }
                                }
                                else
                                {
                                    if (lineIndex < textStorage.Count - 1)
                                    {
                                        lineIndex += 1;
                                        index = 0;
                                    }
                                }
                            }
                            MoveExtend(lineIndex, index, extend);
                        }
                        SetStickyX();
                        ScrollToSelectionActiveEnd();
                        e.Handled = true;
                        break;

                    case Keys.PageUp:
                        {
                            int newPosition = (selectStartIsActive ? selectStartLine : selectEndLine)
                                - ClientHeight / fontHeight;
                            if (newPosition < 0)
                            {
                                newPosition = 0;
                            }
#if false
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newPosition, /* previous line */
                                (int)ScreenXFromCharIndex(
                                    graphics,
                                    selectStartIsActive
                                        ? selectStartLine
                                        : selectEndLine, /* this line */
                                    selectStartIsActive
                                        ? selectStartChar
                                        : selectEndCharPlusOne));
#else
                            int savedStickyX = stickyX;
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newPosition,
                                stickyX);
#endif
                            MoveExtend(newPosition, newPoint, extend);
#if true
                            stickyX = savedStickyX;
#endif
                            ScrollToSelectionActiveEnd();
                        }
                        e.Handled = true;
                        break;

                    case Keys.PageDown:
                        {
                            int newPosition = (selectStartIsActive ? selectStartLine : selectEndLine)
                                + ClientHeight / fontHeight;
                            if (newPosition > textStorage.Count - 1)
                            {
                                newPosition = textStorage.Count - 1;
                            }
#if false
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newPosition, /* next line */
                                (int)ScreenXFromCharIndex(
                                    graphics,
                                    selectStartIsActive
                                        ? selectStartLine
                                        : selectEndLine, /* this line */
                                    selectStartIsActive
                                        ? selectStartChar
                                        : selectEndCharPlusOne));
#else
                            int savedStickyX = stickyX;
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newPosition,
                                stickyX);
#endif
                            MoveExtend(newPosition, newPoint, extend);
#if true
                            stickyX = savedStickyX;
#endif
                            ScrollToSelectionActiveEnd();
                        }
                        e.Handled = true;
                        break;

                    case Keys.Home | Keys.Control:
                    case Keys.Up:
                    case Keys.Up | Keys.Control:
                        if (code == (Keys.Home | Keys.Control))
                        {
                            MoveExtend(0, 0, extend);
                            ScrollToSelectionActiveEnd();
                        }
                        else
                        {
                            int newLineIndex = (selectStartIsActive ? selectStartLine : selectEndLine) - 1;
                            if (newLineIndex < 0)
                            {
                                newLineIndex = 0;
                            }
                            /* snap it to the closest point on the next line */
#if false
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newLineIndex, /* previous line */
                                (int)ScreenXFromCharIndex(
                                    graphics,
                                    selectStartIsActive
                                        ? selectStartLine
                                        : selectEndLine, /* this line */
                                    selectStartIsActive
                                        ? selectStartChar
                                        : selectEndCharPlusOne));
#else
                            int savedStickyX = stickyX;
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newLineIndex,
                                stickyX);
#endif
                            if (!extend)
                            {
                                SetInsertionPoint(newLineIndex, newPoint);
                            }
                            else
                            {
                                SetSelection(
                                    selectStartIsActive ? newLineIndex : selectStartLine,
                                    selectStartIsActive ? newPoint : selectStartChar,
                                    selectStartIsActive ? selectEndLine : newLineIndex,
                                    selectStartIsActive ? selectEndCharPlusOne : newPoint,
                                    selectStartIsActive);
                            }
#if true
                            stickyX = savedStickyX;
#endif
                            ScrollToSelectionActiveEnd();
                        }
                        e.Handled = true;
                        break;

                    case Keys.End | Keys.Control:
                    case Keys.Down:
                    case Keys.Down | Keys.Control:
                        if (code == (Keys.End | Keys.Control))
                        {
                            MoveExtend(textStorage.Count - 1, textStorage[textStorage.Count - 1].Length, extend);
                            ScrollToSelectionActiveEnd();
                        }
                        else
                        {
                            int newLineIndex = (selectStartIsActive
                                ? selectStartLine
                                : selectEndLine) + 1;
                            if (newLineIndex > textStorage.Count - 1)
                            {
                                newLineIndex = textStorage.Count - 1;
                            }
                            /* snap it to the closest point on the next line */
#if false
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newLineIndex, /* next line */
                                (int)ScreenXFromCharIndex(
                                    graphics,
                                    selectStartIsActive
                                        ? selectStartLine
                                        : selectEndLine, /* this line */
                                    selectStartIsActive
                                        ? selectStartChar
                                        : selectEndCharPlusOne));
#else
                            int savedStickyX = stickyX;
                            int newPoint = CharIndexFromScreenX(
                                graphics,
                                newLineIndex,
                                stickyX);
#endif
                            if (!extend)
                            {
                                SetInsertionPoint(newLineIndex, newPoint);
                            }
                            else
                            {
                                SetSelection(
                                    selectStartIsActive ? newLineIndex : selectStartLine,
                                    selectStartIsActive ? newPoint : selectStartChar,
                                    selectStartIsActive ? selectEndLine : newLineIndex,
                                    selectStartIsActive ? selectEndCharPlusOne : newPoint,
                                    selectStartIsActive);
                            }
#if true
                            stickyX = savedStickyX;
#endif
                            ScrollToSelectionActiveEnd();
                        }
                        e.Handled = true;
                        break;
                }
            }
        }


        // public text data accessors

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Modified
        {
            get
            {
                return textStorage.Modified;
            }
            set
            {
                textStorage.Modified = value;
            }
        }

        [Browsable(false)]
        public int Count
        {
            get
            {
                return textStorage.Count;
            }
        }

        // hookable listener for recording undo records
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected ITextEditorChangeTracking ChangeListener { get { return changeListener; } set { changeListener = value; } }

        public ITextStorage GetRange(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne)
        {
            if ((startLine > endLine) || ((startLine == endLine) && (startChar > endCharPlusOne)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startLine < 0) || (endLine >= textStorage.Count)
                || (startChar < 0) || (startChar > textStorage[startLine].Length)
                || (endCharPlusOne < 0) || (endCharPlusOne > textStorage[endLine].Length))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            ITextStorage copy = textStorage.CloneSection(
                startLine,
                startChar,
                endLine,
                endCharPlusOne);
            return copy;
        }

        public string GetRangeAsString(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne)
        {
            return GetRange(
                startLine,
                startChar,
                endLine,
                endCharPlusOne).GetText(lineFeed);
        }

        public void ReplaceRangeAndSelect(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne,
            ITextStorage replacement,
            int? select)
        {
            if (replacement.Count < 1)
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startLine > endLine) || ((startLine == endLine) && (startChar > endCharPlusOne)))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }
            if ((startLine < 0) || (endLine >= textStorage.Count)
                || (startChar < 0) || (startChar > textStorage[startLine].Length)
                || (endCharPlusOne < 0) || (endCharPlusOne > textStorage[endLine].Length))
            {
                Debug.Assert(false);
                throw new ArgumentException();
            }

            int replacedEndLine = startLine + replacement.Count - 1;
            int replacedEndCharPlusOne = replacement[replacement.Count - 1].Length;
            if (replacement.Count == 1)
            {
                replacedEndCharPlusOne += startChar;
            }

            int firstRedrawLine = Math.Min(startLine, selectStartLine);
            int lastRedrawLine = Math.Max(endLine, selectEndLine);

            bool shift = endLine - startLine != replacement.Count - 1;

            ITextStorage deleted = GetRange(
                startLine,
                startChar,
                endLine,
                endCharPlusOne);

            if (changeListener != null)
            {
                changeListener.ReplacingRange(
                    startLine,
                    startChar,
                    deleted,
                    replacedEndLine,
                    replacedEndCharPlusOne);
            }

            lineWidthCache.Delete(startLine, endLine - startLine);
            lineWidthCache.Invalidate(startLine);
            textStorage.DeleteSection(
                startLine,
                startChar,
                endLine,
                endCharPlusOne);

            lineWidthCache.Insert(startLine + 1, replacement.Count - 1);
            lineWidthCache.Invalidate(startLine);
            textStorage.InsertSection(
                startLine,
                startChar,
                replacement);

            RecomputeCanvasSizeIncremental();

            if (!select.HasValue)
            {
            }
            else if (select.Value < 0)
            {
                SetInsertionPoint(startLine, startChar);
            }
            else if (select.Value > 0)
            {
                SetInsertionPoint(replacedEndLine, replacedEndCharPlusOne);
            }
            else
            {
                SetSelection(startLine, startChar, replacedEndLine, replacedEndCharPlusOne);
            }

            RedrawRange(
                firstRedrawLine,
                !shift
                    ? lastRedrawLine
                    : Math.Min(lastRedrawLine, textStorage.Count - 1) + ClientHeight / fontHeight + 1);

            OnTextChanged(EventArgs.Empty);
        }

        public void ReplaceRangeAndSelect(
            SelPoint start,
            SelPoint end,
            ITextStorage replacement,
            int? select)
        {
            ReplaceRangeAndSelect(
                start.Line,
                start.Column,
                end.Line,
                end.Column,
                replacement,
                select);
        }

        public void ReplaceRangeAndSelect(
            SelRange range,
            ITextStorage replacement,
            int? select)
        {
            ReplaceRangeAndSelect(
                range.Start.Line,
                range.Start.Column,
                range.End.Line,
                range.End.Column,
                replacement,
                select);
        }

        public void ReplaceRangeAndSelect(
            int startLine,
            int startChar,
            int endLine,
            int endCharPlusOne,
            string replacement,
            int? select)
        {
            replacement = !String.IsNullOrEmpty(replacement) ? replacement : String.Empty;
            ITextStorage text = textStorageFactory.FromUtf16Buffer(
                replacement,
                0,
                replacement.Length,
                lineFeed);
            ReplaceRangeAndSelect(
                startLine,
                startChar,
                endLine,
                endCharPlusOne,
                text,
                select);
        }

        public void ReplaceRangeAndSelect(
            SelPoint start,
            SelPoint end,
            string replacement,
            int? select)
        {
            ReplaceRangeAndSelect(
                start.Line,
                start.Column,
                end.Line,
                end.Column,
                replacement,
                select);
        }

        public void ReplaceRangeAndSelect(
            SelRange range,
            string replacement,
            int? select)
        {
            ReplaceRangeAndSelect(
                range.Start.Line,
                range.Start.Column,
                range.End.Line,
                range.End.Column,
                replacement,
                select);
        }

        public ITextLine GetLine(int index)
        {
            return textStorage[index];
        }

        public void SetLine(int index, ITextLine value)
        {
            int currSelectStartLine = selectStartLine;
            int currSelectStartChar = selectStartChar;
            int currSelectEndLine = selectEndLine;
            int currSelectEndCharPlusOne = selectEndCharPlusOne;
            bool currSelectStartIsActive = selectStartIsActive;

            bool start = (selectStartLine == index);
            bool startEnd = start && (selectStartChar == textStorage[index].Length);
            bool end = (selectEndLine == index);
            bool endEnd = end && (selectEndCharPlusOne == textStorage[index].Length);

            IDecodedTextLine decodedValue = (value != null)
                ? value.Decode_MustDispose()
                : textStorageFactory.NewDecoded_MustDispose(null, 0, 0);
            ITextStorage line = textStorageFactory.FromUtf16Buffer(
                decodedValue.Value,
                0,
                decodedValue.Length,
                lineFeed);

            ReplaceRangeAndSelect(
                index,
                0,
                index,
                textStorage[index].Length,
                line,
                null);

            bool set = false;
            if (start || end)
            {
                if (startEnd)
                {
                    currSelectStartChar = textStorage[index].Length;
                    set = true;
                }
                if (endEnd)
                {
                    currSelectEndCharPlusOne = textStorage[index].Length;
                    set = true;
                }
                if (start)
                {
                    currSelectStartChar = Math.Min(currSelectStartChar, textStorage[index].Length);
                    set = true;
                }
                if (end)
                {
                    currSelectEndCharPlusOne = Math.Min(currSelectEndCharPlusOne, textStorage[index].Length);
                    set = true;
                }
            }

            if (set)
            {
                SetSelection(
                    currSelectStartLine,
                    currSelectStartChar,
                    currSelectEndLine,
                    currSelectEndCharPlusOne,
                    currSelectStartIsActive);
            }
        }

        [Browsable(false)]
        public ITextLine this[int index]
        {
            get
            {
                return GetLine(index);
            }
            set
            {
                SetLine(index, value);
            }
        }

        [Browsable(true), Category("Appearance"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get
            {
                if (DesignMode && (textStorage == null))
                {
                    return null;
                }
                return textStorage.GetText(lineFeed);
            }
            set
            {
                if (DesignMode && (textStorage == null))
                {
                    return;
                }
                ReplaceRangeAndSelect(
                    All,
                    value,
                    1);
            }
        }

        // accessor for NewLine sequence used by .Text and this[int] accessors
        [Browsable(true), Category("Behavior"), DefaultValue("\r\n")]
        public string NewLine
        {
            get
            {
                return lineFeed;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException();
                }
                lineFeed = value;

                if (textStorageFactory.PreservesLineEndings)
                {
                    // TODO: change line endings for file
                }
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get
            {
                return this.SelectedTextStorage.GetText(lineFeed);
            }
            set
            {
                value = !String.IsNullOrEmpty(value) ? value : String.Empty;
                ITextStorage text = textStorageFactory.FromUtf16Buffer(value, 0, value.Length, lineFeed);
                this.SelectedTextStorage = text;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ITextStorage SelectedTextStorage
        {
            /* get a copy of the selected area */
            get
            {
                return GetRange(
                    selectStartLine,
                    selectStartChar,
                    selectEndLine,
                    selectEndCharPlusOne);
            }
            /* insert a new block of data at the insertion point, deleting any existing */
            /* selection first.  if this fails, the block may have been partially inserted */
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (value.Count < 1)
                {
                    throw new ArgumentException();
                }

                ReplaceRangeAndSelect(
                    selectStartLine,
                    selectStartChar,
                    selectEndLine,
                    selectEndCharPlusOne,
                    value,
                    0);
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ITextStorage AllText
        {
            get
            {
                return textStorageFactory.Copy(textStorage);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (value.Count < 1)
                {
                    throw new ArgumentException();
                }

                ReplaceRangeAndSelect(
                    0,
                    0,
                    textStorage.Count - 1,
                    textStorage[textStorage.Count - 1].Length,
                    value,
                    0);
            }
        }

        public void Cut()
        {
            Copy();
            Clear(); // sends notification to changeListener
        }

        public void Copy()
        {
            ITextStorage copy = this.SelectedTextStorage;
            string text = copy.GetText(Environment.NewLine);
            if (!String.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
            else
            {
                Clipboard.Clear();
            }
        }

        public void Paste()
        {
            if (Clipboard.ContainsText())
            {
                string scrap = Clipboard.GetText();
                ITextStorage text = textStorageFactory.FromUtf16Buffer(scrap, 0, scrap.Length, Environment.NewLine);

                ReplaceRangeAndSelect(
                    selectStartLine,
                    selectStartChar,
                    selectEndLine,
                    selectEndCharPlusOne,
                    text,
                    1);

                ScrollToSelection();
            }
        }

        public void Clear()
        {
            ReplaceRangeAndSelect(
                selectStartLine,
                selectStartChar,
                selectEndLine,
                selectEndCharPlusOne,
                textStorageFactory.New(),
                1);

            ScrollToSelection();
        }

        protected void InsertChar(char c)
        {
            if (deferredHighSurrogate.HasValue && !Char.IsLowSurrogate(c))
            {
                Debug.Assert(false);
                deferredHighSurrogate = null;
                throw new InvalidOperationException();
            }

            char[] buffer;
            if (!deferredHighSurrogate.HasValue)
            {
                buffer = new char[1] { c };
            }
            else
            {
                buffer = new char[2] { deferredHighSurrogate.Value, c };
                deferredHighSurrogate = null;
            }
            string s = new String(buffer);

            ReplaceRangeAndSelect(
                selectStartLine,
                selectStartChar,
                selectEndLine,
                selectEndCharPlusOne,
                textStorageFactory.FromUtf16Buffer(s, 0, s.Length, lineFeed),
                1);

            ScrollToSelection();
        }

        protected void DeleteCharLeft()
        {
            if (SelectionNonEmpty)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if (selectStartChar == 0)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            ReplaceRangeAndSelect(
                selectStartLine,
                selectStartChar - 1,
                selectEndLine,
                selectEndCharPlusOne,
                textStorageFactory.New(),
                -1);

            ScrollToSelection();
        }

        protected void DeleteCharRight()
        {
            if (SelectionNonEmpty)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if (selectStartChar == textStorage[selectStartLine].Length)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            ReplaceRangeAndSelect(
                selectStartLine,
                selectStartChar,
                selectEndLine,
                selectEndCharPlusOne + 1,
                textStorageFactory.New(),
                1);

            ScrollToSelection();
        }

        protected void InsertLineBreak(string prefix)
        {
            prefix = !String.IsNullOrEmpty(prefix) ? prefix : String.Empty;

            // separate line break from auto-indent (prefix) for separate undo records

            ReplaceRangeAndSelect(
                selectStartLine,
                selectStartChar,
                selectEndLine,
                selectEndCharPlusOne,
                textStorageFactory.FromUtf16Buffer(Environment.NewLine, 0, Environment.NewLine.Length, Environment.NewLine),
                1);

            ReplaceRangeAndSelect(
                selectStartLine,
                selectStartChar,
                selectEndLine,
                selectEndCharPlusOne,
                textStorageFactory.FromUtf16Buffer(prefix, 0, prefix.Length, Environment.NewLine),
                1);

            ScrollToSelection();
        }

        protected void DeleteLineBreakLeft()
        {
            if (SelectionNonEmpty)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((selectStartLine == 0) || (selectStartChar != 0))
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            ReplaceRangeAndSelect(
                selectStartLine - 1,
                GetLine(selectStartLine - 1).Length,
                selectEndLine,
                selectEndCharPlusOne,
                textStorageFactory.New(),
                -1);

            ScrollToSelection();
        }

        protected void DeleteLineBreakRight()
        {
            if (SelectionNonEmpty)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }
            if ((selectStartLine == textStorage.Count - 1) || (selectStartChar != textStorage[selectStartLine].Length))
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            ReplaceRangeAndSelect(
                selectStartLine,
                GetLine(selectStartLine).Length,
                selectEndLine + 1,
                0,
                textStorageFactory.New(),
                1);

            ScrollToSelection();
        }

        public SelPoint AdjustForInsert(SelPoint point, SelPoint start, ITextStorage insert)
        {
            if (point < start)
            {
                return point;
            }
            if (insert.Count == 1)
            {
                if (point.Line == start.Line)
                {
                    return new SelPoint(
                        point.Line,
                        point.Column + insert[0].Length);
                }
                else
                {
                    return point;
                }
            }
            return new SelPoint(
                point.Line + insert.Count - 1,
                point.Column - start.Column + insert[insert.Count - 1].Length);
        }

        public SelPoint AdjustForRemove(SelPoint point, SelRange remove)
        {
            if (point <= remove.Start)
            {
                return point;
            }
            if (point <= remove.End)
            {
                return remove.Start;
            }
            if (remove.Start.Line == remove.End.Line)
            {
                if (point.Line == remove.Start.Line)
                {
                    return new SelPoint(
                        point.Line,
                        point.Column - (remove.End.Column - remove.Start.Column));
                }
                else
                {
                    return point;
                }
            }
            return new SelPoint(
                point.Line - (remove.End.Line - remove.Start.Line),
                point.Column - remove.End.Column + remove.Start.Column);
        }
    }

    // change tracking for undo - "OnBefore..." semantics
    public interface ITextEditorChangeTracking
    {
        void ReplacingRange(
            int startLine,
            int startChar,
            ITextStorage deleted,
            int replacedEndLine,
            int replacedEndCharPlusOne);
    }
}
