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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class TextEditorWindow : Form, IFindInFilesWindow
    {
        private static readonly Encoding Encoding_ANSI = new ANSIEncoding();
        private static readonly Encoding Encoding_UTF8 = new UTF8Encoding(false/*encoderShouldEmitUTF8Identifier*/);
        private static readonly Encoding Encoding_UTF16 = new UnicodeEncoding(false/*bigEndian*/, false/*byteOrderMark*/);
        private static readonly Encoding Encoding_UTF16BigEndian = new UnicodeEncoding(true/*bigEndian*/, false/*byteOrderMark*/);

        // TODO: show inconsistent linebreaks and permit changing them

        // TODO: autosave?

        private string linefeed = Environment.NewLine;

        private Encoding encoding = Encoding_ANSI;
        private bool includeBom;

        private string path;

        private bool startedEmpty = true;

        private BackingStore effectiveBackingStore = MainClass.Config.BackingStore;

        protected TextEditorWindow(bool createBackingStore)
        {
            EditorConfig config = MainClass.Config[0];

            InitializeComponent();
            this.Icon = TextEditorApp.Properties.Resources.Icon2;

            if (createBackingStore)
            {
                this.textEditControl.TextStorageFactory = GetBackingStore(MainClass.Config.BackingStore);
            }

            try
            {
                textEditControl.TextService = MainClass.Config.TextService;
            }
            catch (FileNotFoundException exception)
            {
                MessageBox.Show(String.Format("Unable to load program component. To solve this problem, make sure the Visual Studio 2015 Redistributable is installed on the computer. (Internal exception: {0})", exception.Message));
                throw;
            }
            textEditControl.Font = config.Font;
            textEditControl.TabSize = config.TabSize;
            textEditControl.AutoIndent = config.AutoIndent;
            textEditControl.InsertTabAsSpaces = config.InsertTabAsSpaces;
            textEditControl.SimpleNavigation = config.SimpleNavigation;

            this.Text = "Untitled";

            textEditControl.SelectionChanged += new EventHandler(textEditControl_SelectionChanged);
            toolStripTextBoxLine.Validated += new EventHandler(UserEditedLineCharHandler);
            toolStripTextBoxCharacter.Validated += new EventHandler(UserEditedLineCharHandler);
            toolStripTextBoxColumn.Validated += new EventHandler(UserEditedColumnHandler);

            menuStrip.MenuActivate += new EventHandler(menuStrip1_MenuActivate);
        }

        public TextEditorWindow()
            : this(true/*createBackingStore*/)
        {
            UserEditedLineCharHandler(null, null);
        }

        public TextEditorWindow(string path)
            : this(false/*createBackingStore*/)
        {
            LoadFile(path);
            UserEditedLineCharHandler(null, null);
        }

        private ITextStorageFactory GetBackingStore(BackingStore effectiveBackingStore)
        {
            ITextStorageFactory factory;
            switch (effectiveBackingStore)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case BackingStore.String:
                    effectiveBackingStore = BackingStore.String;
                    factory = this.stringStorageFactory;
                    break;
                case BackingStore.Protected:
                    effectiveBackingStore = BackingStore.Protected;
                    factory = this.protectedStorageFactory;
                    break;
                case BackingStore.Utf8SplayGapBuffer:
                    effectiveBackingStore = BackingStore.Utf8SplayGapBuffer;
                    factory = this.utf8SplayGapBufferFactory;
                    break;
            }
            return factory;
        }

        [DllImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsTextUnicode(IntPtr buffer, int byteCount, ref IsTextUnicodeFlags flags);

        [Flags]
        private enum IsTextUnicodeFlags : int
        {
            IS_TEXT_UNICODE_ASCII16 = 0x0001,
            IS_TEXT_UNICODE_REVERSE_ASCII16 = 0x0010,

            IS_TEXT_UNICODE_STATISTICS = 0x0002,
            IS_TEXT_UNICODE_REVERSE_STATISTICS = 0x0020,

            IS_TEXT_UNICODE_CONTROLS = 0x0004,
            IS_TEXT_UNICODE_REVERSE_CONTROLS = 0x0040,

            IS_TEXT_UNICODE_SIGNATURE = 0x0008,
            IS_TEXT_UNICODE_REVERSE_SIGNATURE = 0x0080,

            IS_TEXT_UNICODE_ILLEGAL_CHARS = 0x0100,
            IS_TEXT_UNICODE_ODD_LENGTH = 0x0200,
            IS_TEXT_UNICODE_DBCS_LEADBYTE = 0x0400,
            IS_TEXT_UNICODE_NULL_BYTES = 0x1000,

            IS_TEXT_UNICODE_UNICODE_MASK = 0x000F,
            IS_TEXT_UNICODE_REVERSE_MASK = 0x00F0,
            IS_TEXT_UNICODE_NOT_UNICODE_MASK = 0x0F00,
            IS_TEXT_UNICODE_NOT_ASCII_MASK = 0xF000
        }

        public void LoadFile(string path, EncodingInfo encodingInfo)
        {
            if (textEditControl.Modified || (textEditControl.Count != 1) || (textEditControl.GetLine(0).Length != 0))
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            EditorConfig config = MainClass.Config.Find(Path.GetExtension(path));

            textEditControl.Font = config.Font;
            textEditControl.TabSize = config.TabSize;
            textEditControl.AutoIndent = config.AutoIndent;
            textEditControl.InsertTabAsSpaces = config.InsertTabAsSpaces;
            textEditControl.SimpleNavigation = config.SimpleNavigation;

            this.Text = Path.GetFileName(path);

            this.path = path;
            this.encoding = encodingInfo.Encoding;
            this.includeBom = encodingInfo.BomLength != 0;

            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                long length = stream.Length;

                stream.Seek(encodingInfo.BomLength, SeekOrigin.Begin);

                // must use our own reader rather than TextReader since we want to also determine
                // which kind of line ending the file used.
                LineEndingInfo lineEndingInfo;
                ITextStorage text = textEditControl.TextStorageFactory.FromStream(
                    stream,
                    encoding,
                    out lineEndingInfo);
                linefeed = Environment.NewLine;
                string lineFeedName = "Windows";
                if (lineEndingInfo.unixLFCount > 2 * (lineEndingInfo.windowsLFCount + lineEndingInfo.macintoshLFCount))
                {
                    linefeed = "\n";
                    lineFeedName = "UNIX";
                }
                else if (lineEndingInfo.macintoshLFCount > 2 * (lineEndingInfo.windowsLFCount + lineEndingInfo.unixLFCount))
                {
                    linefeed = "\r";
                    lineFeedName = "Macintosh";
                }

                int m = 0;
                m += (lineEndingInfo.windowsLFCount != 0 ? 1 : 0);
                m += (lineEndingInfo.macintoshLFCount != 0 ? 1 : 0);
                m += (lineEndingInfo.unixLFCount != 0 ? 1 : 0);
                if (m > 1)
                {
                    if (!textEditControl.TextStorageFactory.PreservesLineEndings)
                    {
                        MessageBox.Show(String.Format("The file contains inconsistent line endings. All line endings will be converted to the most common one, {0}.", lineFeedName), "Text Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("The file contains inconsistent line endings, which will be preserved. Select a new line ending on the menu to make all line endings consistent.", "Text Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                if ((length >= 4096) && (length / text.Count >= 5000))
                {
                    DialogResult result = MessageBox.Show(
                        "The file data contains a small number of very long lines, indicating the encoding used to open it may be incorrect. Continue trying to open? (It may take a long time.)",
                        "Encoding",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);
                    if (result != DialogResult.OK)
                    {
                        throw new ApplicationException();
                    }
                }

                textEditControl.Reload(
                    textEditControl.TextStorageFactory,
                    text);
            }

            textEditControl.ClearUndoRedo();
            textEditControl.SetInsertionPoint(0, 0);
            textEditControl.Modified = false;

            startedEmpty = false;
        }

        public void LoadFile(string path)
        {
            string qualifier = null;
            string qualifier2 = null;

            int colon = path.IndexOf(':', Path.IsPathRooted(path) ? 2 : 0);
            if (colon >= 0)
            {
                qualifier = path.Substring(colon + 1);
                path = path.Substring(0, colon);
            }
            if (qualifier != null)
            {
                colon = qualifier.IndexOf(':');
                if (colon >= 0)
                {
                    qualifier2 = qualifier.Substring(colon + 1);
                    qualifier = qualifier.Substring(0, colon);
                }
            }

            ITextStorageFactory factory = null;
            if (!String.IsNullOrEmpty(qualifier2))
            {
                try
                {
                    effectiveBackingStore = (BackingStore)Enum.Parse(typeof(BackingStore), qualifier2);
                }
                catch (ArgumentException)
                {
                    throw new Exception(String.Format("Buffer qualifier '{0}' is not recognized - should be one of '{1}', '{2}', or '{3}'", qualifier, BackingStore.String, BackingStore.Protected, BackingStore.Utf8SplayGapBuffer));
                }
                factory = GetBackingStore(effectiveBackingStore);
            }
            else if (this.textEditControl.TextStorageFactory == null)
            {
                factory = GetBackingStore(MainClass.Config.BackingStore);
            }
            if (factory != null)
            {
                this.textEditControl.TextStorageFactory = factory;
            }

            if (String.IsNullOrEmpty(qualifier))
            {
                EncodingInfo encodingInfo = GuessEncoding(path);
                Type[] permittedEncodings = null;
                if (this.textEditControl.TextStorageFactory != null)
                {
                    permittedEncodings = this.textEditControl.TextStorageFactory.PermittedEncodings;
                }
                if (permittedEncodings != null)
                {
                    if (Array.FindIndex(permittedEncodings, delegate (Type candidate) { return candidate.IsInstanceOfType(encodingInfo.Encoding); }) < 0)
                    {
                        MessageBox.Show("Specified encoding is not permitted by specified backing store. Falling back to 'String' backing store.", "Text Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        TextEditorWindow replacementWindow = new TextEditorWindow(false/*createBackingStore*/);
                        replacementWindow.textEditControl.TextStorageFactory = replacementWindow.GetBackingStore(BackingStore.String);
                        replacementWindow.LoadFile(path, encodingInfo);
                        replacementWindow.Show();
                        throw new ApplicationException(); // cancel the old window
                    }
                }
                LoadFile(path, encodingInfo);
            }
            else
            {
                switch (qualifier.ToLower())
                {
                    default:
                        throw new Exception(String.Format("Encoding qualifier '{0}' is not recognized - should be one of 'ansi', 'utf8', 'utf16', or 'utf16be'", qualifier));
                    case "ansi":
                        LoadFile(path, new EncodingInfo(Encoding_ANSI, 0));
                        break;
                    case "utf8":
                        LoadFile(path, new EncodingInfo(Encoding_UTF8, 0));
                        break;
                    case "utf16":
                        LoadFile(path, new EncodingInfo(Encoding_UTF16, 0));
                        break;
                    case "utf16be":
                        LoadFile(path, new EncodingInfo(Encoding_UTF16BigEndian, 0));
                        break;
                }
            }
        }

        public struct EncodingInfo
        {
            public readonly Encoding Encoding;
            public readonly int BomLength;

            public EncodingInfo(Encoding Encoding, int BomLength)
            {
                this.Encoding = Encoding;
                this.BomLength = BomLength;
            }
        }

        private static EncodingInfo GuessEncoding(string path)
        {
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Encoding encoding;
                int bomLength = 0;

                byte[] bom = new byte[3];
                stream.Read(bom, 0, 3);

                if ((bom[0] == 0xFF) && (bom[1] == 0xFE))
                {
                    encoding = Encoding_UTF16;
                    bomLength = 2;
                }
                else if ((bom[0] == 0xFE) && (bom[1] == 0xFF))
                {
                    encoding = Encoding_UTF16BigEndian;
                    bomLength = 2;
                }
                else if ((bom[0] == 0xEF) && (bom[1] == 0xBB) && (bom[2] == 0xBF))
                {
                    encoding = Encoding_UTF8;
                    bomLength = 3;
                }
                else
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    using (Pin<byte[]> pinBuffer = new Pin<byte[]>(new byte[4096]))
                    {
                        int c = stream.Read(pinBuffer.Ref, 0, pinBuffer.Ref.Length);

                        IsTextUnicodeFlags flags = unchecked((IsTextUnicodeFlags)0xffffffff);
                        bool unicode = IsTextUnicode(pinBuffer.AddrOfPinnedObject(), c, ref flags);
                        if (unicode)
                        {
                            if ((flags & IsTextUnicodeFlags.IS_TEXT_UNICODE_UNICODE_MASK) != 0)
                            {
                                encoding = Encoding_UTF16;
                            }
                            else
                            {
                                encoding = Encoding_UTF16BigEndian;
                            }
                        }
                        else
                        {
                            encoding = Encoding_ANSI;
                        }
                    }
                }

                return new EncodingInfo(encoding, bomLength);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            if ((MainClass.Config.Width != 0) && (MainClass.Config.Height != 0))
            {
                int left = DesktopBounds.Location.X;
                int top = DesktopBounds.Location.Y;
                if (left + MainClass.Config.Width > Screen.PrimaryScreen.WorkingArea.Width)
                {
                    left -= left + MainClass.Config.Width - Screen.PrimaryScreen.WorkingArea.Width;
                }
                if (top + MainClass.Config.Height > Screen.PrimaryScreen.WorkingArea.Height)
                {
                    top -= top + MainClass.Config.Height - Screen.PrimaryScreen.WorkingArea.Height;
                }
                DesktopBounds = new Rectangle(left, top, MainClass.Config.Width, MainClass.Config.Height);
            }

            toolStripLabelBackingStore.Text = String.Format(
                "[{0}, {2}, {1}]",
                effectiveBackingStore,
                textEditControl.Hardened ? "Hardened" : "Not Hardened",
                textEditControl.TextService);

            base.OnShown(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            if (this.Size != this.MaximumSize)
            {
                MainClass.Config.Width = this.Width;
                MainClass.Config.Height = this.Height;
                MainClass.SaveSettings();
            }
        }

        public bool Closable()
        {
            if (!textEditControl.Modified)
            {
                return true;
            }
            if (startedEmpty && (textEditControl.End == new SelPoint(0, 0)))
            {
                return true;
            }

            using (UnsavedDialog dialog = new UnsavedDialog(path != null ? Path.GetFileName(path) : "Untitled"))
            {
                switch (dialog.ShowDialog())
                {
                    default:
                        Debug.Assert(false);
                        return false;
                    case DialogResult.Yes:
                        return SaveOrSaveAsHelper(); // user can cancel the "Save As" dialog
                    case DialogResult.No:
                        textEditControl.Modified = false;
                        return true;
                    case DialogResult.Cancel:
                        return false;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!Closable())
            {
                e.Cancel = true;
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (MainClass.defaultEmptyForm == this)
            {
                MainClass.defaultEmptyForm = null;
            }
            base.OnFormClosed(e);
        }

        private void textEditControl_SelectionChanged(object sender, EventArgs e)
        {
            toolStripTextBoxLine.Validated -= new EventHandler(UserEditedLineCharHandler);
            toolStripTextBoxCharacter.Validated -= new EventHandler(UserEditedLineCharHandler);
            toolStripTextBoxColumn.Validated -= new EventHandler(UserEditedColumnHandler);

            toolStripTextBoxLine.Text = (textEditControl.SelectionActiveLine + 1).ToString();
            toolStripTextBoxCharacter.Text = (textEditControl.SelectionActiveChar + 1).ToString();
            toolStripTextBoxColumn.Text = (textEditControl.GetColumnFromCharIndex(textEditControl.SelectionActiveLine, textEditControl.SelectionActiveChar) + 1).ToString();

            toolStripTextBoxLine.Validated += new EventHandler(UserEditedLineCharHandler);
            toolStripTextBoxCharacter.Validated += new EventHandler(UserEditedLineCharHandler);
            toolStripTextBoxColumn.Validated += new EventHandler(UserEditedColumnHandler);
        }

        private void UserEditedLineCharHandler(object sender, EventArgs e)
        {
            int line, character;
            Int32.TryParse(toolStripTextBoxLine.Text, out line);
            Int32.TryParse(toolStripTextBoxCharacter.Text, out character);
            line--;
            character--;
            line = Math.Min(Math.Max(line, 0), textEditControl.Count - 1);
            character = Math.Min(Math.Max(character, 0), textEditControl.GetLine(line).Length);
            if (textEditControl.SelectionNonEmpty)
            {
                int otherLine = textEditControl.SelectionStartIsActive
                    ? textEditControl.SelectionEndLine
                    : textEditControl.SelectionStartLine;
                int otherChar = textEditControl.SelectionStartIsActive
                    ? textEditControl.SelectionEndCharPlusOne
                    : textEditControl.SelectionStartChar;
                textEditControl.SetSelection(
                    line,
                    character,
                    otherLine,
                    otherChar,
                    true/*startIsActive*/);
            }
            else
            {
                textEditControl.SetInsertionPoint(line, character);
            }
        }

        private void UserEditedColumnHandler(object sender, EventArgs e)
        {
            int line = textEditControl.SelectionActiveLine;
            int column;
            Int32.TryParse(toolStripTextBoxColumn.Text, out column);
            int character = textEditControl.GetCharIndexFromColumn(line, column);
            if (textEditControl.SelectionNonEmpty)
            {
                int otherLine = textEditControl.SelectionStartIsActive
                    ? textEditControl.SelectionEndLine
                    : textEditControl.SelectionStartLine;
                int otherChar = textEditControl.SelectionStartIsActive
                    ? textEditControl.SelectionEndCharPlusOne
                    : textEditControl.SelectionStartChar;
                textEditControl.SetSelection(
                    line,
                    character,
                    otherLine,
                    otherChar,
                    true/*startIsActive*/);
            }
            else
            {
                textEditControl.SetInsertionPoint(line, character);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (helper.ProcessCmdKeyDelegate(ref msg, keyData))
            {
                return true;
            }

            if (keyData == (Keys.Control | Keys.K))
            {
                System.GC.Collect();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            helper.MenuActivateDelegate();

            autoIndentToolStripMenuItem.Checked = textEditControl.AutoIndent;
            tabSizeToolStripMenuItem.Text = String.Format("&Tab Size ({0})...", textEditControl.TabSize);
            insertTabAsSpacesToolStripMenuItem.Checked = textEditControl.InsertTabAsSpaces;
            simpleNavigationToolStripMenuItem.Checked = textEditControl.SimpleNavigation;
            fontToolStripMenuItem.Text = String.Format("&Font ({0}, {1}{2})...", textEditControl.Font.FontFamily.Name, textEditControl.Font.Style != FontStyle.Regular ? textEditControl.Font.Style.ToString().ToLower() + " ," : null, textEditControl.Font.SizeInPoints);
            macintoshLinebreaksToolStripMenuItem.Checked = String.Equals(linefeed, "\r");
            uNIXLinebreaksToolStripMenuItem.Checked = String.Equals(linefeed, "\n");
            windowsLineBreaksToolStripMenuItem.Checked = String.Equals(linefeed, "\r\n");
            raw8bitToolStripMenuItem.Checked = (encoding == Encoding_ANSI);
            uTF8ToolStripMenuItem.Checked = (encoding == Encoding_UTF8);
            uTF16ToolStripMenuItem.Checked = (encoding == Encoding_UTF16);
            uTF16BigEndianToolStripMenuItem.Checked = (encoding == Encoding_UTF16BigEndian);
            includeByteOrderMarkToolStripMenuItem.Checked = includeBom && !(encoding == Encoding_ANSI);
            includeByteOrderMarkToolStripMenuItem.Enabled = !(encoding == Encoding_ANSI);
        }

        private void windowsLineBreaksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            linefeed = "\r\n";
            if (textEditControl.TextStorageFactory.PreservesLineEndings)
            {
                textEditControl.NewLine = linefeed;
            }
        }

        private void macintoshLinebreaksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            linefeed = "\r";
            if (textEditControl.TextStorageFactory.PreservesLineEndings)
            {
                textEditControl.NewLine = linefeed;
            }
        }

        private void uNIXLinebreaksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            linefeed = "\n";
            if (textEditControl.TextStorageFactory.PreservesLineEndings)
            {
                textEditControl.NewLine = linefeed;
            }
        }

        private void autoIndentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textEditControl.AutoIndent = !textEditControl.AutoIndent;
        }

        private void tabSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (TabSizeDialog dialog = new TabSizeDialog(textEditControl.TabSize))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textEditControl.TabSize = dialog.Value;
                }
            }
        }

        private void insertTabAsSpacesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textEditControl.InsertTabAsSpaces = !textEditControl.InsertTabAsSpaces;
        }

        private void simpleNavigationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textEditControl.SimpleNavigation = !textEditControl.SimpleNavigation;
        }

        private void raw8bitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            encoding = Encoding_ANSI;
        }

        private void uTF8ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            encoding = Encoding_UTF8;
        }

        private void uTF16ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            encoding = Encoding_UTF16;
        }

        private void uTF16BigEndianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            encoding = Encoding_UTF16BigEndian;
        }

        private void includeByteOrderMarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            includeBom = !includeBom;
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FontDialog dialog = new FontDialog())
            {
                dialog.Font = textEditControl.Font;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textEditControl.Font = dialog.Font;
                }
            }
        }

        private void defaultSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Settings dialog = new Settings(MainClass.Config))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    MainClass.Config = dialog.Config;
                    MainClass.SaveSettings();
                }
            }
        }

        // info about UTF-16 surrogates ("Supplementary Characters")
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dd374069%28v=vs.85%29.aspx
        // examples here:
        // https://en.wikipedia.org/wiki/UTF-16#Examples
        // http://www.i18nguy.com/unicode-example-plane1.html

        private void nextUTF16SurrogatePairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelPoint position = textEditControl.SelectionActive;
            ITextLine currentLine = null;
            IDecodedTextLine currentLineDecoded = null;
            try
            {
                while (position < textEditControl.End)
                {
                    if (currentLine == null)
                    {
                        currentLine = textEditControl[position.Line];
                        currentLineDecoded = currentLine.Decode_MustDispose();
                    }

                    if (position.Column <= currentLineDecoded.Length - 2)
                    {
                        if (Char.IsHighSurrogate(currentLineDecoded[position.Column])
                            && Char.IsLowSurrogate(currentLineDecoded[position.Column + 1]))
                        {
                            textEditControl.SetSelection(
                                position,
                                new SelPoint(position.Line, position.Column + 2),
                                false/*selectStartIsActive*/);
                            return;
                        }
                    }

                    position.Column++;
                    if (position.Column >= currentLine.Length)
                    {
                        position = new SelPoint(position.Line + 1, 0);
                        currentLine = null;
                        if (currentLineDecoded != null)
                        {
                            currentLineDecoded.Dispose();
                            currentLineDecoded = null;
                        }
                    }
                }
            }
            finally
            {
                if (currentLineDecoded != null)
                {
                    currentLineDecoded.Dispose();
                }
            }
            textEditControl.ErrorBeep();
        }

        private void previousUTF16SurrogatePairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelPoint position = textEditControl.SelectionActive;
            ITextLine currentLine = null;
            IDecodedTextLine currentLineDecoded = null;
            SelPoint zero = new SelPoint();
            try
            {
                while (position > zero)
                {
                    if (currentLine == null)
                    {
                        currentLine = textEditControl[position.Line];
                        currentLineDecoded = currentLine.Decode_MustDispose();
                    }

                    if (position.Column >= 2)
                    {
                        if (Char.IsHighSurrogate(currentLineDecoded[position.Column - 2])
                            && Char.IsLowSurrogate(currentLineDecoded[position.Column - 1]))
                        {
                            textEditControl.SetSelection(
                                new SelPoint(position.Line, position.Column - 2),
                                position,
                                true/*selectStartIsActive*/);
                            return;
                        }
                    }

                    position.Column--;
                    if (position.Column < 0)
                    {
                        position = new SelPoint(position.Line - 1, 0);
                        currentLine = null;
                        if (currentLineDecoded != null)
                        {
                            currentLineDecoded.Dispose();
                            currentLineDecoded = null;
                        }
                        if (position.Line >= 0)
                        {
                            position.Column = textEditControl[position.Line].Length;
                        }
                    }
                }
            }
            finally
            {
                if (currentLineDecoded != null)
                {
                    currentLineDecoded.Dispose();
                }
            }
            textEditControl.ErrorBeep();
        }

        private static string OpenFileDialogHelper(string title)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (title != null)
                {
                    dialog.Title = title;
                }
                dialog.Filter = "Text File (.txt)|*.txt|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return null;
                }
                string path = dialog.FileName;
                return path;
            }
        }

        public static TextEditorWindow GetWindowForLoadHelper()
        {
            TextEditorWindow window;
            if ((MainClass.defaultEmptyForm != null)
                && !MainClass.defaultEmptyForm.textEditControl.Modified
                && MainClass.defaultEmptyForm.textEditControl.Count == 1
                && MainClass.defaultEmptyForm.textEditControl.GetLine(0).Length == 0)
            {
                window = MainClass.defaultEmptyForm;
                MainClass.defaultEmptyForm = null;
                window.Hide();
            }
            else
            {
                window = new TextEditorWindow();
            }
            return window;
        }

        private delegate void LoadFileMethod(TextEditorWindow window, string path);
        private void OpenNewOrExistingWindowPattern(string prompt, LoadFileMethod loadFile)
        {
            string path = OpenFileDialogHelper(prompt);
            if (path != null)
            {
                TextEditorWindow window = GetWindowForLoadHelper();
                try
                {
                    loadFile(window, path);
                    window.Show();
                }
                catch (ApplicationException)
                {
                    // thrown if user cancels load after prompted for possible wrong encoding
                    window.Dispose();
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewOrExistingWindowPattern(
                null,
                delegate (TextEditorWindow window, string path)
                {
                    window.LoadFile(path);
                });
        }

        private void openANSIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewOrExistingWindowPattern(
                "Open ANSI Encoded",
                delegate (TextEditorWindow window, string path)
                {
                    window.LoadFile(path, new EncodingInfo(Encoding_ANSI, 0));
                });
        }

        private void openUTF8ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewOrExistingWindowPattern(
                "Open UTF-8 Encoded",
                delegate (TextEditorWindow window, string path)
                {
                    window.LoadFile(path, new EncodingInfo(Encoding_UTF8, 0));
                });
        }

        private void openUTF16ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewOrExistingWindowPattern(
                "Open UTF-16 Encoded",
                delegate (TextEditorWindow window, string path)
                {
                    window.LoadFile(path, new EncodingInfo(Encoding_UTF16, 0));
                });
        }

        private void openUTF16BigEndianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewOrExistingWindowPattern(
                "Open UTF-16 Big Endian Encoded",
                delegate (TextEditorWindow window, string path)
                {
                    window.LoadFile(path, new EncodingInfo(Encoding_UTF16BigEndian, 0));
                });
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new TextEditorWindow().Show();
        }

        private void SaveHelper(string path)
        {
            this.Text = Path.GetFileName(path);

            ITextStorage text = textEditControl.AllText;

            Stream tempStream = null;
            string temp = null;
            Random rnd = new Random();
            while (tempStream == null)
            {
                int c = rnd.Next();
                temp = Path.Combine(Path.GetDirectoryName(path), c.ToString() + ".txt");
                try
                {
                    tempStream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                }
                catch (IOException exception)
                {
                    int hr = Marshal.GetHRForException(exception);
                    if (hr != unchecked((int)0x80070050)) // file exists
                    {
                        throw;
                    }
                    tempStream = null;
                }
            }

            using (Stream stream = tempStream)
            {
                if (includeByteOrderMarkToolStripMenuItem.Checked)
                {
                    if (encoding == Encoding_UTF16)
                    {
                        stream.Write(new byte[2] { 0xFF, 0xFE }, 0, 2);
                    }
                    else if (encoding == Encoding_UTF16BigEndian)
                    {
                        stream.Write(new byte[2] { 0xFE, 0xFF }, 0, 2);
                    }
                    else if (encoding == Encoding_UTF8)
                    {
                        stream.Write(new byte[3] { 0xEF, 0xBB, 0xBF }, 0, 3);
                    }
                }

                text.ToStream(stream, encoding, linefeed);
            }

            File.Delete(path);
            File.Move(temp, path);

            textEditControl.Modified = false;
        }

        private bool SaveAsHelper()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text File (.txt)|*.txt|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return false;
                }
                path = dialog.FileName;
            }
            SaveHelper(path);
            return true;
        }

        private bool SaveOrSaveAsHelper()
        {
            if (String.IsNullOrEmpty(path))
            {
                return SaveAsHelper();
            }
            else
            {
                SaveHelper(path);
                return true;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveOrSaveAsHelper();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsHelper();
        }

        private void saveACopyAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string copyPath;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text File (.txt)|*.txt|Any File Type (*)|*";
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
                copyPath = dialog.FileName;
            }
            SaveHelper(copyPath);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Application.Exit() is supposed to do all this but does not work because it enumerates the
            // list Application.OpenForms directly - and showing our "Save unsaved changes?" dialog modifies
            // that list, causing their enumerator the throw an exception.

            List<Form> openForms = new List<Form>();
            foreach (Form form in Application.OpenForms)
            {
                openForms.Add(form);
            }
            foreach (Form form in openForms)
            {
                if (form is TextEditorWindow)
                {
                    if (((TextEditorWindow)form).Closable())
                    {
                        form.Close();
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    form.Close();
                }
            }
            // main loop will exit when there are no windows
        }

        // TODO: implement this fully.
        // See http://blogs.msdn.com/b/vsarabic/archive/2011/08/21/text-rendering.aspx for RTL example
        private void rightToLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textEditControl.RightToLeft == RightToLeft.Yes)
            {
                textEditControl.RightToLeft = RightToLeft.No;
                rightToLeftToolStripMenuItem.Checked = false;
            }
            else
            {
                textEditControl.RightToLeft = RightToLeft.Yes;
                rightToLeftToolStripMenuItem.Checked = true;
            }
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
            {
                drgevent.Effect = DragDropEffects.Copy;
            }
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            string[] files = (string[])drgevent.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                TextEditorWindow window = GetWindowForLoadHelper();
                try
                {
                    window.LoadFile(file);
                    window.Show();
                }
                catch (ApplicationException)
                {
                    // thrown if user cancels load after prompted for possible wrong encoding
                    window.Dispose();
                }
            }
        }

        private void findInFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FindInFiles(new FindInFilesApplication(), true/*showPathAndExtensions*/, "Find in Files").Show();
        }

        public void SetSelection(int startLine, int startChar, int endLine, int endCharP1)
        {
            textEditControl.SetSelection(startLine, startChar, endLine, endCharP1);
            textEditControl.ScrollToSelection();
        }

        public void SetSelection(IFindInFilesItem item, int startLine, int startChar, int endLine, int endCharP1)
        {
            SetSelection(startLine, startChar, endLine, endCharP1);
        }

        private void testInlineModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new TestInlineMode().Show();
        }
    }
}
