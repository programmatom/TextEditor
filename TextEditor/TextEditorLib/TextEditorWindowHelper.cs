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
using System.Text;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class TextEditorWindowHelper : Component
    {
        private TextEditControl textEditControl;

        private bool respectFocus = true;

        private Keys modifierKeys;

        private bool lastFindFailed;
        private int lastFindLine;
        private int lastFindCharPlusOne;

        private bool delegatedMode; // false: fire events using MenuItem.Click() event; true: caller invokes via ProcessMenuItemDelegate(MenuItem)

        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private ToolStripMenuItem shiftLeftToolStripMenuItem;
        private ToolStripMenuItem shiftRightToolStripMenuItem;
        private ToolStripMenuItem trimTrailingSpacesToolStripMenuItem;
        private ToolStripMenuItem balanceToolStripMenuItem;
        private ToolStripMenuItem convertTabsToSpacesToolStripMenuItem;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem findAgainToolStripMenuItem;
        private ToolStripMenuItem replaceAndFindAgainToolStripMenuItem;
        private ToolStripMenuItem enterSelectionToolStripMenuItem;
        private ToolStripMenuItem goToLineToolStripMenuItem;

        public TextEditorWindowHelper()
        {
            InitializeComponent();
        }

        public TextEditorWindowHelper(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }


        [Browsable(true), Category("Edit Control")]
        public TextEditControl TextEditControl
        {
            get { return textEditControl; }
            set
            {
                textEditControl = value;
            }
        }


        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem UndoToolStripMenuItem
        {
            get { return undoToolStripMenuItem; }
            set
            {
                if (undoToolStripMenuItem != null)
                {
                    undoToolStripMenuItem.Click -= new EventHandler(undoToolStripMenuItem_Click);
                }

                undoToolStripMenuItem = value;

                if ((undoToolStripMenuItem != null) && !delegatedMode)
                {
                    undoToolStripMenuItem.Click += new EventHandler(undoToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem RedoToolStripMenuItem
        {
            get { return redoToolStripMenuItem; }
            set
            {
                if (redoToolStripMenuItem != null)
                {
                    redoToolStripMenuItem.Click -= new EventHandler(redoToolStripMenuItem_Click);
                }

                redoToolStripMenuItem = value;

                if ((redoToolStripMenuItem != null) && !delegatedMode)
                {
                    redoToolStripMenuItem.Click += new EventHandler(redoToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem CutToolStripMenuItem
        {
            get { return cutToolStripMenuItem; }
            set
            {
                if (cutToolStripMenuItem != null)
                {
                    cutToolStripMenuItem.Click -= new EventHandler(cutToolStripMenuItem_Click);
                }

                cutToolStripMenuItem = value;

                if ((cutToolStripMenuItem != null) && !delegatedMode)
                {
                    cutToolStripMenuItem.Click += new EventHandler(cutToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem CopyToolStripMenuItem
        {
            get { return copyToolStripMenuItem; }
            set
            {
                if (copyToolStripMenuItem != null)
                {
                    copyToolStripMenuItem.Click -= new EventHandler(copyToolStripMenuItem_Click);
                }

                copyToolStripMenuItem = value;

                if ((copyToolStripMenuItem != null) && !delegatedMode)
                {
                    copyToolStripMenuItem.Click += new EventHandler(copyToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem PasteToolStripMenuItem
        {
            get { return pasteToolStripMenuItem; }
            set
            {
                if (pasteToolStripMenuItem != null)
                {
                    pasteToolStripMenuItem.Click -= new EventHandler(pasteToolStripMenuItem_Click);
                }

                pasteToolStripMenuItem = value;

                if ((pasteToolStripMenuItem != null) && !delegatedMode)
                {
                    pasteToolStripMenuItem.Click += new EventHandler(pasteToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem ClearToolStripMenuItem
        {
            get { return clearToolStripMenuItem; }
            set
            {
                if (clearToolStripMenuItem != null)
                {
                    clearToolStripMenuItem.Click -= new EventHandler(clearToolStripMenuItem_Click);
                }

                clearToolStripMenuItem = value;

                if ((clearToolStripMenuItem != null) && !delegatedMode)
                {
                    clearToolStripMenuItem.Click += new EventHandler(clearToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem SelectAllToolStripMenuItem
        {
            get { return selectAllToolStripMenuItem; }
            set
            {
                if (selectAllToolStripMenuItem != null)
                {
                    selectAllToolStripMenuItem.Click -= new EventHandler(selectAllToolStripMenuItem_Click);
                }

                selectAllToolStripMenuItem = value;

                if ((selectAllToolStripMenuItem != null) && !delegatedMode)
                {
                    selectAllToolStripMenuItem.Click += new EventHandler(selectAllToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem ShiftLeftToolStripMenuItem
        {
            get { return shiftLeftToolStripMenuItem; }
            set
            {
                if (shiftLeftToolStripMenuItem != null)
                {
                    shiftLeftToolStripMenuItem.Click -= new EventHandler(shiftLeftToolStripMenuItem_Click);
                }

                shiftLeftToolStripMenuItem = value;

                if ((shiftLeftToolStripMenuItem != null) && !delegatedMode)
                {
                    shiftLeftToolStripMenuItem.Click += new EventHandler(shiftLeftToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem ShiftRightToolStripMenuItem
        {
            get { return shiftRightToolStripMenuItem; }
            set
            {
                if (shiftRightToolStripMenuItem != null)
                {
                    shiftRightToolStripMenuItem.Click -= new EventHandler(shiftRightToolStripMenuItem_Click);
                }

                shiftRightToolStripMenuItem = value;

                if ((shiftRightToolStripMenuItem != null) && !delegatedMode)
                {
                    shiftRightToolStripMenuItem.Click += new EventHandler(shiftRightToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem TrimTrailingSpacesToolStripMenuItem
        {
            get { return trimTrailingSpacesToolStripMenuItem; }
            set
            {
                if (trimTrailingSpacesToolStripMenuItem != null)
                {
                    trimTrailingSpacesToolStripMenuItem.Click -= new EventHandler(trimTrailingSpacesToolStripMenuItem_Click);
                }

                trimTrailingSpacesToolStripMenuItem = value;

                if ((trimTrailingSpacesToolStripMenuItem != null) && !delegatedMode)
                {
                    trimTrailingSpacesToolStripMenuItem.Click += new EventHandler(trimTrailingSpacesToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem BalanceToolStripMenuItem
        {
            get { return balanceToolStripMenuItem; }
            set
            {
                if (balanceToolStripMenuItem != null)
                {
                    balanceToolStripMenuItem.Click -= new EventHandler(balanceToolStripMenuItem_Click);
                }

                balanceToolStripMenuItem = value;

                if ((balanceToolStripMenuItem != null) && !delegatedMode)
                {
                    balanceToolStripMenuItem.Click += new EventHandler(balanceToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem ConvertTabsToSpacesToolStripMenuItem
        {
            get { return convertTabsToSpacesToolStripMenuItem; }
            set
            {
                if (convertTabsToSpacesToolStripMenuItem != null)
                {
                    convertTabsToSpacesToolStripMenuItem.Click -= new EventHandler(convertTabsToSpacesToolStripMenuItem_Click);
                }

                convertTabsToSpacesToolStripMenuItem = value;

                if ((convertTabsToSpacesToolStripMenuItem != null) && !delegatedMode)
                {
                    convertTabsToSpacesToolStripMenuItem.Click += new EventHandler(convertTabsToSpacesToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem FindToolStripMenuItem
        {
            get { return findToolStripMenuItem; }
            set
            {
                if (findToolStripMenuItem != null)
                {
                    findToolStripMenuItem.Click -= new EventHandler(findToolStripMenuItem_Click);
                }

                findToolStripMenuItem = value;

                if ((findToolStripMenuItem != null) && !delegatedMode)
                {
                    findToolStripMenuItem.Click += new EventHandler(findToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem FindAgainToolStripMenuItem
        {
            get { return findAgainToolStripMenuItem; }
            set
            {
                if (findAgainToolStripMenuItem != null)
                {
                    findAgainToolStripMenuItem.Click -= new EventHandler(findAgainToolStripMenuItem_Click);
                }

                findAgainToolStripMenuItem = value;

                if ((findAgainToolStripMenuItem != null) && !delegatedMode)
                {
                    findAgainToolStripMenuItem.Click += new EventHandler(findAgainToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem ReplaceAndFindAgainToolStripMenuItem
        {
            get { return replaceAndFindAgainToolStripMenuItem; }
            set
            {
                if (replaceAndFindAgainToolStripMenuItem != null)
                {
                    replaceAndFindAgainToolStripMenuItem.Click -= new EventHandler(replaceAndFindAgainToolStripMenuItem_Click);
                }

                replaceAndFindAgainToolStripMenuItem = value;

                if ((replaceAndFindAgainToolStripMenuItem != null) && !delegatedMode)
                {
                    replaceAndFindAgainToolStripMenuItem.Click += new EventHandler(replaceAndFindAgainToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem EnterSelectionToolStripMenuItem
        {
            get { return enterSelectionToolStripMenuItem; }
            set
            {
                if (enterSelectionToolStripMenuItem != null)
                {
                    enterSelectionToolStripMenuItem.Click -= new EventHandler(enterSelectionToolStripMenuItem_Click);
                }

                enterSelectionToolStripMenuItem = value;

                if ((enterSelectionToolStripMenuItem != null) && !delegatedMode)
                {
                    enterSelectionToolStripMenuItem.Click += new EventHandler(enterSelectionToolStripMenuItem_Click);
                }
            }
        }

        [Browsable(true), Category("Menu Items")]
        public ToolStripMenuItem GoToLineToolStripMenuItem
        {
            get { return goToLineToolStripMenuItem; }
            set
            {
                if (goToLineToolStripMenuItem != null)
                {
                    goToLineToolStripMenuItem.Click -= new EventHandler(goToLineToolStripMenuItem_Click);
                }

                goToLineToolStripMenuItem = value;

                if ((goToLineToolStripMenuItem != null) && !delegatedMode)
                {
                    goToLineToolStripMenuItem.Click += new EventHandler(goToLineToolStripMenuItem_Click);
                }
            }
        }


        [Browsable(true), Category("Behavior"), DefaultValue(true)]
        public bool RespectFocus
        {
            get
            {
                return respectFocus;
            }
            set
            {
                respectFocus = value;
            }
        }

        [Browsable(true), Category("Behavior"), DefaultValue(false)]
        public bool DelegatedMode
        {
            get
            {
                return delegatedMode;
            }
            set
            {
                delegatedMode = value;
            }
        }

        private bool Enable
        {
            get
            {
                return !respectFocus || textEditControl.Focused;
            }
        }

        public bool MenuActivateDelegate()
        {
            if ((undoToolStripMenuItem != null) && Enable)
            {
                undoToolStripMenuItem.Enabled = !textEditControl.ReadOnly && textEditControl.UndoAvailable;
            }
            if ((redoToolStripMenuItem != null) && Enable)
            {
                redoToolStripMenuItem.Enabled = !textEditControl.ReadOnly && textEditControl.RedoAvailable;
            }
            if ((cutToolStripMenuItem != null) && Enable)
            {
                cutToolStripMenuItem.Enabled = !textEditControl.ReadOnly && textEditControl.SelectionNonEmpty;
            }
            if ((copyToolStripMenuItem != null) && Enable)
            {
                copyToolStripMenuItem.Enabled = textEditControl.SelectionNonEmpty;
            }
            if ((pasteToolStripMenuItem != null) && Enable)
            {
                pasteToolStripMenuItem.Enabled = !textEditControl.ReadOnly
                    && (Clipboard.ContainsText() || (textEditControl.Hardened && (secureClipboard != null)));
            }
            if ((clearToolStripMenuItem != null) && Enable)
            {
                clearToolStripMenuItem.Enabled = !textEditControl.ReadOnly && textEditControl.SelectionNonEmpty;
            }
            if ((selectAllToolStripMenuItem != null) && Enable)
            {
                selectAllToolStripMenuItem.Enabled = true;
            }
            if ((shiftLeftToolStripMenuItem != null) && Enable)
            {
                shiftLeftToolStripMenuItem.Enabled = !textEditControl.ReadOnly;
            }
            if ((shiftRightToolStripMenuItem != null) && Enable)
            {
                shiftRightToolStripMenuItem.Enabled = !textEditControl.ReadOnly;
            }
            if ((trimTrailingSpacesToolStripMenuItem != null) && Enable)
            {
                trimTrailingSpacesToolStripMenuItem.Enabled = !textEditControl.ReadOnly && textEditControl.SelectionNonEmpty;
            }
            if ((balanceToolStripMenuItem != null) && Enable)
            {
                balanceToolStripMenuItem.Enabled = true;
            }
            if ((convertTabsToSpacesToolStripMenuItem != null) && Enable)
            {
                convertTabsToSpacesToolStripMenuItem.Enabled = !textEditControl.ReadOnly && textEditControl.SelectionNonEmpty;
            }
            if ((findToolStripMenuItem != null) && Enable)
            {
                findToolStripMenuItem.Enabled = true;
            }
            if ((findAgainToolStripMenuItem != null) && Enable)
            {
                findAgainToolStripMenuItem.Enabled = !String.IsNullOrEmpty(FindDialog.DefaultSettings.FindText);
            }
            if ((replaceAndFindAgainToolStripMenuItem != null) && Enable)
            {
                replaceAndFindAgainToolStripMenuItem.Enabled = !textEditControl.ReadOnly
                    && !String.IsNullOrEmpty(FindDialog.DefaultSettings.FindText);
            }
            if ((enterSelectionToolStripMenuItem != null) && Enable)
            {
                enterSelectionToolStripMenuItem.Enabled = textEditControl.SelectionNonEmpty;
            }
            if ((goToLineToolStripMenuItem != null) && Enable)
            {
                goToLineToolStripMenuItem.Enabled = true;
            }
            return Enable;
        }

        public bool ProcessCmdKeyDelegate(ref Message msg, Keys keyData)
        {
            bool result = false;

            modifierKeys = keyData & Keys.Modifiers; // used for Shift-F3 to reverse search direction

            if ((keyData & Keys.Control) != 0)
            {
                MenuActivateDelegate();
            }

            if ((keyData == (Keys.F3 | Keys.Shift)) && findAgainToolStripMenuItem.Enabled && Enable)
            {
                findAgainToolStripMenuItem_Click(null, null);
                result = true;
            }

            modifierKeys = 0;

            return result;
        }

        public bool ProcessMenuItemDelegate(ToolStripMenuItem menuItem)
        {
            if (menuItem == undoToolStripMenuItem)
            {
                undoToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == redoToolStripMenuItem)
            {
                redoToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == cutToolStripMenuItem)
            {
                cutToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == copyToolStripMenuItem)
            {
                copyToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == pasteToolStripMenuItem)
            {
                pasteToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == clearToolStripMenuItem)
            {
                clearToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == selectAllToolStripMenuItem)
            {
                selectAllToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == shiftLeftToolStripMenuItem)
            {
                shiftLeftToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == shiftRightToolStripMenuItem)
            {
                shiftRightToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == trimTrailingSpacesToolStripMenuItem)
            {
                trimTrailingSpacesToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == balanceToolStripMenuItem)
            {
                balanceToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == convertTabsToSpacesToolStripMenuItem)
            {
                convertTabsToSpacesToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == findToolStripMenuItem)
            {
                findToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == findAgainToolStripMenuItem)
            {
                findAgainToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == replaceAndFindAgainToolStripMenuItem)
            {
                replaceAndFindAgainToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == enterSelectionToolStripMenuItem)
            {
                enterSelectionToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            else if (menuItem == goToLineToolStripMenuItem)
            {
                goToLineToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.Undo();
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.Redo();
            }
        }

        private static ITextStorage secureClipboard;

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                if (!textEditControl.Hardened)
                {
                    secureClipboard = null;
                    textEditControl.Cut();
                }
                else
                {
                    Clipboard.Clear();
                    secureClipboard = textEditControl.SelectedTextStorage;
                    textEditControl.Clear();
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                if (!textEditControl.Hardened)
                {
                    secureClipboard = null;
                    textEditControl.Copy();
                }
                else
                {
                    Clipboard.Clear();
                    secureClipboard = textEditControl.SelectedTextStorage;
                }
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                if (!textEditControl.Hardened || Clipboard.ContainsText())
                {
                    secureClipboard = null;
                    textEditControl.Paste();
                }
                else
                {
                    textEditControl.ReplaceRangeAndSelect(
                        textEditControl.Selection,
                        secureClipboard,
                        1);
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.Clear();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                textEditControl.SelectAll();
            }
        }

        private void shiftLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.ShiftSelectionLeftOneTab();
            }
        }

        private void shiftRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.ShiftSelectionRightOneTab();
            }
        }

        private void trimTrailingSpacesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.TrimTrailingSpaces();
            }
        }

        private void balanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                textEditControl.BalanceParens();
            }
        }

        private void convertTabsToSpacesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                textEditControl.ConvertTabsToSpaces();
            }
        }

        private bool FindHelper(FindDialog.SettingsInfo settings)
        {
            bool wrap = lastFindFailed
                && (lastFindLine == textEditControl.SelectionActiveLine)
                && (lastFindCharPlusOne == textEditControl.SelectionActiveChar);
            bool result = textEditControl.Find(
                textEditControl.TextStorageFactory.FromUtf16Buffer(
                    settings.FindText,
                    0,
                    settings.FindText.Length,
                    Environment.NewLine),
                settings.CaseSensitive,
                settings.MatchWholeWord,
                wrap,
                (modifierKeys & Keys.Shift) == 0 ? settings.Up : !settings.Up);
            lastFindFailed = !result;
            if (lastFindFailed)
            {
                lastFindLine = textEditControl.SelectionActiveLine;
                lastFindCharPlusOne = textEditControl.SelectionActiveChar;
            }
            else
            {
                textEditControl.ScrollToSelection();
            }
            return result;
        }

        private void Find(FindDialog.SettingsInfo settings)
        {
            FindHelper(settings);
        }

        private void ReplaceAndFindAgain(FindDialog.SettingsInfo settings)
        {
            if (textEditControl.SelectionNonEmpty)
            {
                ITextStorage find = textEditControl.TextStorageFactory.FromUtf16Buffer(
                    settings.FindText,
                    0,
                    settings.FindText.Length,
                    Environment.NewLine);
                if (textEditControl.SelectionEndLine
                        == (textEditControl.SelectionStartLine + find.Count - 1)
                    && (textEditControl.SelectionEndCharPlusOne
                        == (find.Count == 1
                            ? textEditControl.SelectionStartChar + find[0].Length
                            : find[find.Count - 1].Length))
                    && textEditControl.IsMatch(
                        find,
                        settings.CaseSensitive,
                        settings.MatchWholeWord,
                        textEditControl.SelectionStartLine,
                        textEditControl.SelectionStartChar))
                {
                    ITextStorage replace = textEditControl.TextStorageFactory.FromUtf16Buffer(
                        settings.ReplaceText,
                        0,
                        settings.ReplaceText.Length,
                        Environment.NewLine);
                    textEditControl.SelectedTextStorage = replace;
                    textEditControl.SetInsertionPoint(textEditControl.SelectionEndLine, textEditControl.SelectionEndCharPlusOne);
                }
                else
                {
                    textEditControl.ErrorBeep();
                    return;
                }
            }
            FindHelper(settings);
        }

        private void ReplaceAll(FindDialog.SettingsInfo settings)
        {
            if (String.IsNullOrEmpty(settings.FindText))
            {
                return;
            }

            ITextStorage find = textEditControl.TextStorageFactory.FromUtf16Buffer(
                settings.FindText,
                0,
                settings.FindText.Length,
                Environment.NewLine);
            ITextStorage replace = textEditControl.TextStorageFactory.FromUtf16Buffer(
                settings.ReplaceText,
                0,
                settings.ReplaceText.Length,
                Environment.NewLine);

            using (IDisposable undoGroup = textEditControl.UndoOpenGroup())
            {
                SelPoint start, end;
                if (settings.RestrictToSelection)
                {
                    textEditControl.UndoSaveSelection();
                    start = textEditControl.SelectionStart;
                    end = textEditControl.SelectionEnd;
                }
                else
                {
                    start = new SelPoint(0, 0);
                    end = new SelPoint(textEditControl.Count - 1, textEditControl.GetLine(textEditControl.Count - 1).Length);
                }

                textEditControl.SetInsertionPoint(start);
                while (textEditControl.Find(find, settings.CaseSensitive, settings.MatchWholeWord, false/*wrap*/, false/*up*/)
                    && (textEditControl.SelectionStart <= end)
                    && (textEditControl.SelectionEnd <= end))
                {
                    end = textEditControl.AdjustForRemove(end, textEditControl.Selection);
                    end = textEditControl.AdjustForInsert(end, textEditControl.SelectionStart, replace);
                    textEditControl.SelectedTextStorage = replace;
                    textEditControl.SetInsertionPoint(textEditControl.SelectionEndLine, textEditControl.SelectionEndCharPlusOne);
                }

                if (settings.RestrictToSelection)
                {
                    textEditControl.SetSelection(start, end, false/*startIsActive*/);
                }
            }

            textEditControl.ScrollToSelection();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                using (FindDialog dialog = new FindDialog(
                    Find,
                    !textEditControl.ReadOnly ? ReplaceAndFindAgain : (FindDialog.DoMethod)null,
                    !textEditControl.ReadOnly ? ReplaceAll : (FindDialog.DoMethod)null))
                {
                    dialog.ShowDialog();
                }
            }
        }

        private void findAgainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                Find(FindDialog.DefaultSettings);
            }
        }

        private void replaceAndFindAgainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable && !textEditControl.ReadOnly)
            {
                ReplaceAndFindAgain(FindDialog.DefaultSettings);
            }
        }

        private void enterSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                FindDialog.DefaultFindText = textEditControl.SelectedTextStorage.GetText(Environment.NewLine);
            }
        }

        private void goToLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Enable)
            {
                int line = textEditControl.SelectionStartIsActive ? textEditControl.SelectionStartLine : textEditControl.SelectionEndLine;
                using (GoToDialog dialog = new GoToDialog(line + 1))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        line = Math.Max(Math.Min(dialog.Value - 1, textEditControl.Count - 1), 0);
                        textEditControl.SetInsertionPoint(line, 0);
                        textEditControl.ScrollToSelection();
                    }
                }
            }
        }
    }
}
