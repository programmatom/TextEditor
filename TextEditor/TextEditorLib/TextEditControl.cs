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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class TextEditControl : TextViewControl
    {
        private bool acceptsReturn = true;
        private bool acceptsTab = true;
        private bool readOnly;
        private bool autoIndent;
        private bool insertTabAsSpaces;

        private bool undoEnabled = true;
        private TextUndoTracker undo;
        private TextUndoTracker redo;

        public TextEditControl()
            : base()
        {
            InitializeComponent();

            undo = new TextUndoTracker(this, true/*clearRedo*/);
            this.ChangeListener = undo;
        }

        public TextEditControl(ITextStorageFactory textStorageFactory)
            : base(textStorageFactory)
        {
            InitializeComponent();

            undo = new TextUndoTracker(this, true/*clearRedo*/);
            this.ChangeListener = undo;
        }


        //

        [Browsable(true), Category("Appearance")]
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                if (DataBindings.Count != 0)
                {
                    ClearUndoRedo();
                }
            }
        }

        public override void Reload(
            ITextStorageFactory factory,
            ITextStorage storage)
        {
            ClearUndoRedo();
            base.Reload(factory, storage);
        }


        // public behavior properties

        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool AutoIndent { get { return autoIndent; } set { autoIndent = value; } }

        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool InsertTabAsSpaces { get { return insertTabAsSpaces; } set { insertTabAsSpaces = value; } }

        [Browsable(true), Category("Behavior"), DefaultValue(true)]
        public bool AcceptsReturn { get { return acceptsReturn; } set { acceptsReturn = value; } }

        [Browsable(true), Category("Behavior"), DefaultValue(true)]
        public bool AcceptsTab { get { return acceptsTab; } set { acceptsTab = value; } }

        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool ReadOnly { get { return readOnly; } set { readOnly = value; } }

        [Browsable(true), Category("Behavior"), DefaultValue(true)]
        public bool UndoEnabled
        {
            get { return undoEnabled; }
            set
            {
                if (undoEnabled == value)
                {
                    return;
                }

                undoEnabled = value;
                undo = null;
                if (undoEnabled)
                {
                    undo = new TextUndoTracker(this, true/*clearRedo*/);
                }
                redo = null;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public event EventHandler ErrorNotify;


        // internal mouse and keyboard handling

        protected override bool IsInputChar(char charCode)
        {
            if (acceptsReturn && (charCode == '\r'))
            {
                return !readOnly;
            }
            if (acceptsTab && (charCode == '\t'))
            {
                return !readOnly;
            }

            return base.IsInputChar(charCode);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Back:
                case Keys.Delete:
                    return !readOnly;

                case Keys.Return:
                    if (acceptsReturn)
                    {
                        return !readOnly;
                    }
                    break;

                case Keys.Tab:
                    if (acceptsTab)
                    {
                        return !readOnly;
                    }
                    break;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.Handled)
            {
                return;
            }

            if (e.KeyChar < 32)
            {
                switch (e.KeyChar)
                {
                    case '\r':
                        if (!readOnly)
                        {
                            int insertionLine = SelectionStartLine;
                            int insertionChar = SelectionStartChar;

                            string prefix = String.Empty;

                            using (IDecodedTextLine decodedLine = GetLine(insertionLine).Decode_MustDispose())
                            {
                                if (autoIndent)
                                {
                                    int prefixEndChar = 0;
                                    while ((prefixEndChar < insertionChar)
                                        && ((decodedLine[prefixEndChar] == '\t') || (decodedLine[prefixEndChar] == ' ')))
                                    {
                                        prefixEndChar++;
                                    }
                                    prefix = decodedLine.Value.Substring(0, prefixEndChar); // TODO: security
                                }
                            }

                            InsertLineBreak(prefix);
                        }
                        e.Handled = true;
                        break;

                    case '\t':
                        if (!readOnly)
                        {
                            if (!insertTabAsSpaces)
                            {
                                InsertChar('\t');
                            }
                            else
                            {
                                int column = GetColumnFromCharIndex(SelectionStartLine, SelectionStartChar);
                                int count = TabSize - (column % TabSize);
                                string spaces = new String(' ', count);
                                ReplaceRangeAndSelect(
                                    SelectionStartLine,
                                    SelectionStartChar,
                                    SelectionEndLine,
                                    SelectionEndCharPlusOne,
                                    TextStorageFactory.FromUtf16Buffer(spaces, 0, spaces.Length, Environment.NewLine),
                                    1);
                                ScrollToSelection();
                            }
                        }
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                if (!readOnly)
                {
                    InsertChar(e.KeyChar);
                }
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Back:
                    PerformBackspaceKeyAction();
                    e.Handled = true;
                    break;

                case Keys.Delete:
                    PerformDeleteKeyAction();
                    e.Handled = true;
                    break;
            }
        }

        public void PerformBackspaceKeyAction()
        {
            if (!readOnly)
            {
                if (SelectionNonEmpty)
                {
                    Clear(); // notifies changeListener
                }
                else
                {
                    if (SelectionStartChar == 0)
                    {
                        if (SelectionStartLine > 0)
                        {
                            /* delete carriage return */
                            DeleteLineBreakLeft();
                        }
                        /* else, can't delete past start of file */
                    }
                    else
                    {
                        DeleteCharLeft();
                    }
                }
            }
        }

        public void PerformDeleteKeyAction()
        {
            if (!readOnly)
            {
                if (SelectionNonEmpty)
                {
                    Clear(); // notifies changeListener
                }
                else
                {
                    if (SelectionStartChar == GetLine(SelectionStartLine).Length)
                    {
                        if (SelectionStartLine < this.Count - 1)
                        {
                            /* delete carriage return */
                            DeleteLineBreakRight();
                        }
                        /* else, can't delete past end of file */
                    }
                    else
                    {
                        DeleteCharRight();
                    }
                }
            }
        }


        // public specialty methods

        /* shift the selection toward the left margin by deleting one tab (or spaces) */
        /* from the beginning of the line.  It will not remove non-whitespace characters */
        public void ShiftSelectionLeftOneTab()
        {
            ProcessLines(delegate (ITextStorage text)
            {
                for (int i = 0; i < text.Count; i++)
                {
                    int charIndex = 0;
                    int columnCount = 0;
                    using (IDecodedTextLine decodedLine = text[i].Decode_MustDispose())
                    {
                        while ((charIndex < decodedLine.Length) && (columnCount < TabSize))
                        {
                            if (decodedLine[charIndex] == ' ')
                            {
                                charIndex += 1;
                                columnCount += 1;
                            }
                            else if (decodedLine[charIndex] == '\t')
                            {
                                charIndex += 1;
                                columnCount = TabSize; /* cause loop termination */
                            }
                            else
                            {
                                columnCount = TabSize; /* cause loop termination */
                            }
                        }
                    }

                    text.DeleteSection(
                        i,
                        0,
                        i,
                        charIndex);
                }
            });
        }

        /* shift selection toward the right margin by inserting a tab at the */
        /* beginning of each line. */
        public void ShiftSelectionRightOneTab()
        {
            ProcessLines(delegate (ITextStorage text)
            {
                ITextStorage tab = TextStorageFactory.FromUtf16Buffer("\t", 0, 1, Environment.NewLine);

                for (int i = 0; i < text.Count; i++)
                {
                    if (text[i].Length != 0)
                    {
                        text.InsertSection(i, 0, tab);
                    }
                }
            });
        }

        public void TrimTrailingSpaces()
        {
            ProcessLines(delegate (ITextStorage text)
            {
                for (int i = 0; i < text.Count; i++)
                {
                    ITextLine line = text[i];
                    using (IDecodedTextLine decodedLine = line.Decode_MustDispose())
                    {
                        int index = decodedLine.Length;
                        while ((index > 0) && Char.IsWhiteSpace(decodedLine[index - 1]))
                        {
                            index--;
                        }
                        if (index != decodedLine.Length)
                        {
                            ITextStorage data = TextStorageFactory.FromUtf16Buffer(
                                decodedLine.Value,
                                0,
                                index,
                                Environment.NewLine);
                            text.DeleteSection(
                                i,
                                0,
                                i,
                                line.Length);
                            text.InsertSection(
                                i,
                                0,
                                data);
                        }
                    }
                }
            });
        }

        /* convert all tab characters in the selection to the appropriate number of spaces */
        public void ConvertTabsToSpaces()
        {
            ProcessLines(delegate (ITextStorage text)
            {
                for (int i = 0; i < text.Count; i++)
                {
                    bool tabsFound;
                    using (IDecodedTextLine line2 = GetSpaceFromTabLineMustDispose(i, out tabsFound))
                    {
                        if (tabsFound)
                        {
                            ITextStorage data = TextStorageFactory.FromUtf16Buffer(
                                line2.Value,
                                0,
                                line2.Length,
                                Environment.NewLine);
                            text.DeleteSection(
                                i,
                                0,
                                i,
                                GetLine(i).Length);
                            text.InsertSection(
                                i,
                                0,
                                data);
                        }
                    }
                }
            });
        }

        /* extend the current selection to show balanced parentheses, or beep if */
        /* the parentheses are not balanced */
        // TODO: ignore grouping symbols quotes (string literals)
        public void BalanceParens()
        {
            const int MaxStackSize = 1024;

            char form;
            char[] stack = new char[MaxStackSize];
            int stackIndex;
            int backLine;
            int backChar;
            int forwardLine;
            int forwardChar;

            backLine = SelectionStartLine;
            backChar = SelectionStartChar;
            forwardLine = SelectionEndLine;
            forwardChar = SelectionEndCharPlusOne;
            if ((backLine == forwardLine) && (backChar == forwardChar))
            {
                /* just an insertion point.  In this case, if it's like this: */
                /* (...)|  or like this:  |(...), then the group immediately */
                /* next to the insertion point should be selected */
                using (IDecodedTextLine decodedLine = GetLine(backLine).Decode_MustDispose())
                {
                    if (backChar > 0)
                    {
                        if ((decodedLine[backChar - 1] == ')')
                            || (decodedLine[backChar - 1] == '}')
                            || (decodedLine[backChar - 1] == ']'))
                        {
                            /* move insertion point left */
                            backChar -= 1;
                            forwardChar -= 1;
                            goto InitialSetupSkipOutPoint;
                        }
                    }
                    if (backChar < decodedLine.Length/*no -1*/)
                    {
                        /* notice we don't use BackChar + 1 here, because the insertion point */
                        /* is BETWEEN two characters (the x-1 and the x character) */
                        if ((decodedLine[backChar] == '(')
                            || (decodedLine[backChar] == '{')
                            || (decodedLine[backChar] == '['))
                        {
                            /* move insertion point right */
                            backChar += 1;
                            forwardChar += 1;
                            goto InitialSetupSkipOutPoint;
                            /* BackChar and ForwardChar could be equal to HeapBlockLength(GetDefaultHeap(), Line) */
                            /* after this. */
                        }
                    }
                }
            /* jump here when the insertion point has been adjusted */
            InitialSetupSkipOutPoint:
                ;
            }
            stackIndex = 0;
            bool first = true;
            while (backLine >= 0)
            {
                using (IDecodedTextLine decodedLine = GetLine(backLine).Decode_MustDispose())
                {
                    if (!first)
                    {
                        backChar = decodedLine.Length;
                    }
                    first = false;
                    while (backChar > 0)
                    {
                        backChar -= 1;
                        char c = decodedLine[backChar];
                        if ((c == ')') || (c == '}') || (c == ']'))
                        {
                            /* we ran into the trailing end of a grouping, so we increment */
                            /* the count and look for the beginning end. */
                            stack[stackIndex] = c;
                            stackIndex += 1;
                            if (stackIndex >= MaxStackSize)
                            {
                                /* expression is too complex to be analyzed */
                                ErrorBeep();
                                return;
                            }
                        }
                        else if ((c == '(') || (c == '{') || (c == '['))
                        {
                            /* here we found a beginning end of some sort.  If it's the */
                            /* beginning of a group we aren't in, then check to see that */
                            /* it matches */
                            if (stackIndex == 0)
                            {
                                /* there are no other blocks we had to go through so this */
                                /* begin must enclose us */
                                form = c;
                                goto ForwardScanEntryPoint;
                            }
                            stackIndex -= 1;
                            if (((c == '(') && (stack[stackIndex] == ')'))
                                || ((c == '{') && (stack[stackIndex] == '}'))
                                || ((c == '[') && (stack[stackIndex] == ']')))
                            {
                                /* good */
                            }
                            else
                            {
                                /* bad */
                                ErrorBeep();
                                return;
                            }
                        }
                    }
                    backLine -= 1;
                }
            }
            ErrorBeep();
            return;
        ForwardScanEntryPoint:
            stackIndex = 0;
            while (forwardLine < this.Count)
            {
                using (IDecodedTextLine decodedLine = GetLine(forwardLine).Decode_MustDispose())
                {
                    while (forwardChar < decodedLine.Length)
                    {
                        char c = decodedLine[forwardChar];
                        forwardChar += 1;
                        if ((c == '(') || (c == '{') || (c == '['))
                        {
                            /* we ran into the leading end of a grouping, so we increment */
                            /* the count and look for the end end. */
                            stack[stackIndex] = c;
                            stackIndex += 1;
                            if (stackIndex >= MaxStackSize)
                            {
                                /* expression is too complex to be analyzed */
                                ErrorBeep();
                                return;
                            }
                        }
                        else if ((c == ')') || (c == '}') || (c == ']'))
                        {
                            /* here we found an end of some sort.  If it's the */
                            /* end of a group we aren't in, then check to see that */
                            /* it matches */
                            if (stackIndex == 0)
                            {
                                /* there are no other blocks we had to go through so this */
                                /* end must enclose us */
                                if (((form == '(') && (c == ')'))
                                    || ((form == '{') && (c == '}'))
                                    || ((form == '[') && (c == ']')))
                                {
                                    SetSelection(
                                        backLine,
                                        backChar,
                                        forwardLine,
                                        forwardChar,
                                        SelectionStartIsActive);
                                    return;
                                }
                                else
                                {
                                    ErrorBeep();
                                    return;
                                }
                            }
                            stackIndex -= 1;
                            if (((c == ')') && (stack[stackIndex] == '('))
                                || ((c == '}') && (stack[stackIndex] == '{'))
                                || ((c == ']') && (stack[stackIndex] == '[')))
                            {
                                /* good */
                            }
                            else
                            {
                                /* bad */
                                ErrorBeep();
                                return;
                            }
                        }
                    }
                }
                forwardLine += 1;
                forwardChar = 0;
            }
            ErrorBeep();
            return;
        }

        // from http://stackoverflow.com/questions/8924556/force-window-to-blink-when-a-particular-event-occurs-in-c-sharp-wpf

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            /// <summary>
            /// The size of the structure in bytes.
            /// </summary>
            public uint cbSize;
            /// <summary>
            /// A Handle to the Window to be Flashed. The window can be either opened or minimized.
            /// </summary>
            public IntPtr hwnd;
            /// <summary>
            /// The Flash Status.
            /// </summary>
            public uint dwFlags;
            /// <summary>
            /// The number of times to Flash the window.
            /// </summary>
            public uint uCount;
            /// <summary>
            /// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
            /// </summary>
            public uint dwTimeout;
        }

        /// <summary>
        /// Stop flashing. The system restores the window to its original stae.
        /// </summary>
        private const uint FLASHW_STOP = 0;

        /// <summary>
        /// Flash the window caption.
        /// </summary>
        private const uint FLASHW_CAPTION = 1;

        /// <summary>
        /// Flash the taskbar button.
        /// </summary>
        private const uint FLASHW_TRAY = 2;

        /// <summary>
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        /// </summary>
        private const uint FLASHW_ALL = 3;

        /// <summary>
        /// Flash continuously, until the FLASHW_STOP flag is set.
        /// </summary>
        private const uint FLASHW_TIMER = 4;

        /// <summary>
        /// Flash continuously until the window comes to the foreground.
        /// </summary>
        private const uint FLASHW_TIMERNOFG = 12;

        public static void DefaultErrorBeep(IntPtr hwnd)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
            fi.hwnd = hwnd;
            fi.dwFlags = FLASHW_CAPTION;
            fi.uCount = 3;
            fi.dwTimeout = 50;
            FlashWindowEx(ref fi);
        }

        public void ErrorBeep()
        {
            if (ErrorNotify != null)
            {
                ErrorNotify.Invoke(this, EventArgs.Empty);
            }
            else
            {
                DefaultErrorBeep(this.ParentForm.Handle);
            }
        }

        private static bool CurrentCultureEquals( // TODO: security review for allocations
            IDecodedTextLine decodedA,
            IDecodedTextLine decodedB,
            CompareOptions options)
        {
            return 0 == CultureInfo.CurrentCulture.CompareInfo.Compare(decodedA.Value, decodedB.Value, options);
        }

        private static int CurrentCultureIndexOf( // TODO: security review for allocations
            IDecodedTextLine decodedText,
            IDecodedTextLine decodedPattern,
            int startIndex,
            int count,
            CompareOptions options)
        {
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                decodedText.Value,
                decodedPattern.Value,
                startIndex,
                count,
                options);
        }

        // TODO: make this work with complex scripts (currently too much Char.IsLetterOrDigit, etc)
        public bool IsMatch(
            ITextStorage pattern,
            bool caseSensitive,
            bool matchWholeWord,
            int startLine,
            int startChar)
        {
            if (pattern.Count == 1)
            {
                // within-line search case

                using (IDecodedTextLine decodedPattern = pattern[0].Decode_MustDispose())
                {
                    using (IDecodedTextLine decodedStartLine = GetLine(startLine).Decode_MustDispose())
                    {
                        if (startChar + decodedPattern.Length > decodedStartLine.Length)
                        {
                            return false;
                        }
                        if (startChar != CurrentCultureIndexOf(
                            decodedStartLine,
                            decodedPattern,
                            startChar,
                            decodedPattern.Length,
                            caseSensitive
                                ? CompareOptions.None
                                : CompareOptions.IgnoreCase))
                        {
                            return false;
                        }
                        if (matchWholeWord)
                        {
                            if (Char.IsLetterOrDigit(decodedPattern[0]))
                            {
                                if ((startChar - 1 >= 0) && Char.IsLetterOrDigit(decodedStartLine[startChar - 1]))
                                {
                                    return false;
                                }
                            }
                            if (Char.IsLetterOrDigit(decodedPattern[decodedPattern.Length - 1]))
                            {
                                if ((startChar + decodedPattern.Length < decodedStartLine.Length)
                                    && Char.IsLetterOrDigit(decodedStartLine[startChar + decodedPattern.Length]))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // multi-line search case

                if (startLine + pattern.Count - 1 >= this.Count)
                {
                    return false;
                }

                // first line
                using (IDecodedTextLine decodedPatternFirstLine = pattern[0].Decode_MustDispose())
                {
                    using (IDecodedTextLine decodedStartLine = GetLine(startLine).Decode_MustDispose())
                    {
                        if (startChar + decodedPatternFirstLine.Length != decodedStartLine.Length)
                        {
                            return false;
                        }
                        if (startChar != CurrentCultureIndexOf(
                            decodedStartLine,
                            decodedPatternFirstLine,
                            startChar,
                            decodedPatternFirstLine.Length,
                            caseSensitive
                                ? CompareOptions.None
                                : CompareOptions.IgnoreCase))
                        {
                            return false;
                        }
                        if (matchWholeWord)
                        {
                            if (Char.IsLetterOrDigit(decodedPatternFirstLine[0]))
                            {
                                if ((startChar - 1 >= 0) && Char.IsLetterOrDigit(decodedStartLine[startChar - 1]))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                // interior lines
                for (int i = 1; i < pattern.Count - 1; i++)
                {
                    using (IDecodedTextLine decodedPatternLine = pattern[i].Decode_MustDispose())
                    {
                        using (IDecodedTextLine decodedLine = GetLine(startLine + i).Decode_MustDispose())
                        {
                            if (!CurrentCultureEquals(
                                decodedLine,
                                decodedPatternLine,
                                caseSensitive
                                    ? CompareOptions.None
                                    : CompareOptions.IgnoreCase))
                            {
                                return false;
                            }
                        }
                    }
                }

                // last line
                using (IDecodedTextLine decodedPatternLastLine = pattern[pattern.Count - 1].Decode_MustDispose())
                {
                    using (IDecodedTextLine decodedLastLine = GetLine(startLine + pattern.Count - 1).Decode_MustDispose())
                    {
                        if (decodedLastLine.Length < decodedPatternLastLine.Length)
                        {
                            return false;
                        }
                        if (0 != CurrentCultureIndexOf(
                            decodedLastLine,
                            decodedPatternLastLine,
                            0,
                            decodedPatternLastLine.Length,
                            caseSensitive
                                ? CompareOptions.None
                                : CompareOptions.IgnoreCase))
                        {
                            return false;
                        }
                        if (matchWholeWord)
                        {
                            if (Char.IsLetterOrDigit(decodedPatternLastLine[decodedPatternLastLine.Length - 1]))
                            {
                                if ((decodedPatternLastLine.Length < decodedLastLine.Length)
                                    && Char.IsLetterOrDigit(decodedLastLine[decodedPatternLastLine.Length]))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        /* find the specified search string starting at the current selection. */
        public bool Find(
            ITextStorage pattern,
            bool caseSensitive,
            bool matchWholeWord,
            bool wrap,
            bool up)
        {
            int line = !wrap ? SelectionStartLine : (!up ? 0 : this.Count - 1);
            int col;
            if (!wrap)
            {
                if (SelectionNonEmpty)
                {
                    /* if there is a selection, assume it's from a previous search.  we need */
                    /* to have + 1 so we don't find what we found again. */
                    col = !up ? SelectionStartChar + 1 : SelectionStartChar - 1;
                }
                else
                {
                    /* if no selection, start search at current position.  this lets us find */
                    /* patterns at the very beginning of the file. */
                    col = SelectionStartChar;
                }
            }
            else
            {
                col = !up ? 0 : GetLine(this.Count - 1).Length;
            }
            while (!up ? line < this.Count : line >= 0)
            {
                ITextLine testLine = GetLine(line);
                int colEnd = testLine.Length - pattern[0].Length;
                while (!up ? col <= colEnd : col >= 0)
                {
                    if (IsMatch(pattern, caseSensitive, matchWholeWord, line, col))
                    {
                        /* found it! */
                        SetSelection(
                            line,
                            col,
                            line + pattern.Count - 1,
                            (pattern.Count == 1 ? col : 0) + pattern[pattern.Count - 1].Length,
                            SelectionStartIsActive);
                        return true;
                    }
                    col = !up ? col + 1 : col - 1;
                }
                line = !up ? line + 1 : line - 1;
                col = !up ? 0 : (line >= 0 ? GetLine(line).Length - pattern[0].Length : Int32.MaxValue);
            }
            ErrorBeep(); /* selection not found */
            return false;
        }

        private delegate void DoMethod(ITextStorage text);
        private void ProcessLines(DoMethod action)
        {
            using (IDisposable undoGroup = UndoOpenGroup())
            {
                UndoSaveSelection();

                int selectEndLine = SelectionEndLine;
                if ((SelectionStartLine != SelectionEndLine) && (SelectionEndCharPlusOne == 0))
                {
                    selectEndLine--;
                }
                bool savedStartIsActive = SelectionStartIsActive;

                SetSelection(
                    SelectionStartLine,
                    0,
                    selectEndLine,
                    GetLine(selectEndLine).Length,
                    SelectionStartIsActive);

                ITextStorage text = this.SelectedTextStorage;

                action(text);

                ReplaceRangeAndSelect(
                    SelectionStartLine,
                    SelectionStartChar,
                    SelectionEndLine,
                    SelectionEndCharPlusOne,
                    text,
                    0);
                if (savedStartIsActive != SelectionStartIsActive)
                {
                    SwapEnd();
                }
            }
        }


        // public undo/redo methods

        [Browsable(false)]
        public bool UndoAvailable
        {
            get
            {
                return (undo != null) && !undo.Empty;
            }
        }

        [Browsable(false)]
        public bool RedoAvailable
        {
            get
            {
                return (redo != null) && !redo.Empty;
            }
        }

        public void Undo()
        {
            if (UndoAvailable)
            {
                if (redo == null)
                {
                    redo = new TextUndoTracker(this, false/*clearRedo*/);
                }

                using (IDisposable redoGroup = redo.OpenGroup())
                {
                    try
                    {
                        this.ChangeListener = redo;

                        undo.Undo();
                    }
                    finally
                    {
                        this.ChangeListener = undo;
                    }
                }

                ScrollToSelection();
            }
        }

        public void Redo()
        {
            if (RedoAvailable)
            {
                TextUndoTracker localRedo = redo; // undo will nuke it

                using (IDisposable undoGroup = undo.OpenGroup())
                {
                    localRedo.Undo();
                }

                redo = localRedo;

                ScrollToSelection();
            }
        }

        public void ClearUndoRedo()
        {
            if (undo != null)
            {
                undo.Clear();
            }
            redo = null;
        }

        public void UndoSaveSelection()
        {
            if (ChangeListener != null)
            {
                ((TextUndoTracker)ChangeListener).SaveSelection(); // hack
            }
        }

        public IDisposable UndoOpenGroup()
        {
            if (undo != null)
            {
                return undo.OpenGroup();
            }
            return null;
        }

        // uses Dispose pattern for convenient coding and to encourage correctness
        // -- there are no non-managed resources to release.
        private class UndoGroupCloser : IDisposable
        {
            private TextUndoTracker tracker;

            public UndoGroupCloser(TextUndoTracker tracker)
            {
                this.tracker = tracker;
            }

            public void Dispose()
            {
                tracker.CloseGroup();
            }
        }

        private abstract class UndoRecord
        {
            public UndoRecord Next;

            public abstract void Undo(TextViewControl textView);
        }

        private class SelectionUndoRecord : UndoRecord
        {
            public readonly int selectStartLine;
            public readonly int selectStartChar;
            public readonly int selectEndLine;
            public readonly int selectEndCharPlusOne;
            public readonly bool selectionStartIsActive;

            public SelectionUndoRecord(TextViewControl textView)
            {
                textView.GetSelectionExtent(
                    out selectStartLine,
                    out selectStartChar,
                    out selectEndLine,
                    out selectEndCharPlusOne,
                    out selectionStartIsActive);
            }

            public override void Undo(TextViewControl textView)
            {
                ((TextEditControl)textView).UndoSaveSelection(); // hack

                textView.SetSelection(
                    selectStartLine,
                    selectStartChar,
                    selectEndLine,
                    selectEndCharPlusOne,
                    selectionStartIsActive);
            }
        }

        private class GroupStartUndoRecord : UndoRecord
        {
            public override void Undo(TextViewControl textView)
            {
            }
        }

        private class GroupEndUndoRecord : UndoRecord
        {
            public readonly GroupStartUndoRecord Start;

            public GroupEndUndoRecord(GroupStartUndoRecord start)
            {
                this.Start = start;
            }

            public override void Undo(TextViewControl textView)
            {
            }
        }

        private class ReplaceRangeUndoRecord : UndoRecord
        {
            public readonly int StartLine;
            public int StartChar; // hackable
            public readonly ITextStorage Deleted;
            public readonly int ReplacedEndLine;
            public int ReplacedEndCharPlusOne; // hackable

            public int EndLine { get { return StartLine + Deleted.Count - 1; } }
            public int EndCharPlusOne { get { return (Deleted.Count == 1 ? StartChar : 0) + Deleted[Deleted.Count - 1].Length; } }

            public ReplaceRangeUndoRecord(
                int startLine,
                int startChar,
                ITextStorage deleted,
                int replacedEndLine,
                int replacedEndCharPlusOne)
            {
                this.StartLine = startLine;
                this.StartChar = startChar;
                this.Deleted = deleted;
                this.ReplacedEndLine = replacedEndLine;
                this.ReplacedEndCharPlusOne = replacedEndCharPlusOne;
            }

            public override void Undo(TextViewControl textView)
            {
                textView.ReplaceRangeAndSelect(
                    StartLine,
                    StartChar,
                    ReplacedEndLine,
                    ReplacedEndCharPlusOne,
                    Deleted,
                    1);
            }
        }

        private class TextUndoTracker : ITextEditorChangeTracking
        {
            private readonly bool clearRedo;
            private readonly TextEditControl textEdit;

            private UndoRecord records;
            private GroupStartUndoRecord groupStart;

            public TextUndoTracker(TextEditControl textEdit, bool clearRedo)
            {
                this.textEdit = textEdit;
                this.clearRedo = clearRedo;
            }

            public bool Empty
            {
                get
                {
                    return records == null;
                }
            }

            public void Clear()
            {
                records = null;
            }

            public IDisposable OpenGroup()
            {
                // defer this until close group - avoids clearing redo if group was empty
                //if (clearRedo)
                //{
                //    textEdit.redo = null;
                //}

                if (groupStart == null)
                {
                    groupStart = new GroupStartUndoRecord();
                    groupStart.Next = records;
                    records = groupStart;

                    return new UndoGroupCloser(this);
                }
                else
                {
                    return null;
                }
            }

            public void CloseGroup()
            {
                if (groupStart == null)
                {
                    Debug.Assert(false);
                    throw new InvalidOperationException();
                }

                if (groupStart == records)
                {
                    // delete empty undo group to eliminate no-op entries on user's undo stack
                    records = records.Next;
                    return;
                }

                if (clearRedo)
                {
                    textEdit.redo = null;
                }

                SelectionUndoRecord selection = new SelectionUndoRecord(textEdit);
                selection.Next = records;
                records = selection;

                GroupEndUndoRecord end = new GroupEndUndoRecord(groupStart);
                end.Next = records;
                records = end;

                groupStart = null;
            }

            public void SaveSelection()
            {
                SelectionUndoRecord selection = new SelectionUndoRecord(textEdit);
                selection.Next = records;
                records = selection;
            }

            void ITextEditorChangeTracking.ReplacingRange(
                int startLine,
                int startChar,
                ITextStorage deleted,
                int replacedEndLine,
                int replacedEndCharPlusOne)
            {
                if (clearRedo)
                {
                    textEdit.redo = null;
                }

                //SelectionUndoRecord selection = new SelectionUndoRecord(textEdit);
                //selection.next = records;
                //records = selection;

                ReplaceRangeUndoRecord newRange = new ReplaceRangeUndoRecord(
                    startLine,
                    startChar,
                    deleted,
                    replacedEndLine,
                    replacedEndCharPlusOne);

                UndoRecord last = records;
                while ((last != null) && !(last is ReplaceRangeUndoRecord))
                {
                    last = last.Next;
                }

                if ((newRange.StartLine == newRange.EndLine)
                    && (newRange.StartChar == newRange.EndCharPlusOne)
                    && (newRange.StartLine == newRange.ReplacedEndLine)
                    && (newRange.StartChar + 1 == newRange.ReplacedEndCharPlusOne))
                {
                    // if current is a keypress (single char insertion) try to coalesce with previous record
                    ReplaceRangeUndoRecord lastRange = last != null ? (ReplaceRangeUndoRecord)last : null;
                    if ((lastRange != null)
                        && lastRange.Deleted.Empty
                        && (lastRange.StartLine == lastRange.EndLine)
                        && (lastRange.StartLine == newRange.StartLine)
                        && (lastRange.ReplacedEndCharPlusOne == newRange.StartChar))
                    {
                        lastRange.ReplacedEndCharPlusOne++;
                        return;
                    }
                }
                else if ((newRange.StartLine == newRange.EndLine)
                    && (newRange.StartChar == newRange.EndCharPlusOne - 1)
                    && (newRange.StartLine == newRange.ReplacedEndLine)
                    && (newRange.StartChar == newRange.ReplacedEndCharPlusOne))
                {
                    // if current is a backspace/del (single char removal) try to coalesce with previous record
                    ReplaceRangeUndoRecord lastRange = last != null ? (ReplaceRangeUndoRecord)last : null;
                    if ((lastRange != null)
                        && (lastRange.StartLine == lastRange.EndLine)
                        && (lastRange.StartLine == newRange.StartLine)
                        && (lastRange.StartChar == newRange.EndCharPlusOne))
                    {
                        lastRange.StartChar--;
                        lastRange.ReplacedEndCharPlusOne--;
                        lastRange.Deleted.InsertSection(0, 0, newRange.Deleted);
                        return;
                    }
                    else if ((lastRange != null)
                        && (lastRange.StartLine == lastRange.EndLine)
                        && (lastRange.StartChar == lastRange.ReplacedEndCharPlusOne)
                        && (lastRange.StartLine == newRange.StartLine)
                        && (lastRange.StartChar == newRange.StartChar))
                    {
                        lastRange.Deleted.InsertSection(0, lastRange.Deleted[0].Length, newRange.Deleted);
                        return;
                    }
                }

                newRange.Next = records;
                records = newRange;
            }

            public void Undo()
            {
                if (records != null)
                {
                    if (records is GroupEndUndoRecord)
                    {
                        do
                        {
                            UndoRecord one = records;
                            records = records.Next;

                            one.Undo(textEdit);
                        }
                        while (!(records is GroupStartUndoRecord));
                        records = records.Next; // also remove group start
                    }
                    else
                    {
                        UndoRecord one = records;
                        records = records.Next;

                        one.Undo(textEdit);
                    }
                }
            }
        }
    }
}
