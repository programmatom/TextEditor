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
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class FindDialog : Form
    {
        private static SettingsInfo defaultSettings = new SettingsInfo();
        private static int lastX, lastY;

        private readonly DoMethod findAction;
        private readonly DoMethod replaceAndFindAction;
        private readonly DoMethod replaceAllAction;

        private bool controlKey;
        private bool restrictToSelection;

        public FindDialog(
            DoMethod findAction,
            DoMethod replaceAndFindAction,
            DoMethod replaceAllAction)
        {
            this.findAction = findAction;
            this.replaceAndFindAction = replaceAndFindAction;
            this.replaceAllAction = replaceAllAction;

            if ((lastX != 0) && (lastY != 0))
            {
                this.StartPosition = FormStartPosition.Manual;
                this.DesktopLocation = new Point(lastX, lastY);
            }

            InitializeComponent();

            timerReleaseControl.Tick += new EventHandler(timerReleaseControl_Tick);

            this.Settings = defaultSettings;

            this.buttonReplaceAndFindNext.Enabled = (replaceAndFindAction != null);
            this.buttonReplaceAll.Enabled = (replaceAllAction != null);
        }

        public class SettingsInfo
        {
            public readonly string FindText;
            public readonly string ReplaceText;
            public readonly bool CaseSensitive;
            public readonly bool MatchWholeWord;
            public readonly bool RestrictToSelection; // for Replace All only
            public readonly bool Up;

            public SettingsInfo()
            {
                this.FindText = String.Empty;
                this.ReplaceText = String.Empty;
            }

            public SettingsInfo(
                string FindText,
                string ReplaceText,
                bool CaseSensitive,
                bool MatchWholeWord,
                bool RestrictToSelection,
                bool Up)
            {
                this.FindText = FindText;
                this.ReplaceText = ReplaceText;
                this.CaseSensitive = CaseSensitive;
                this.MatchWholeWord = MatchWholeWord;
                this.RestrictToSelection = RestrictToSelection;
                this.Up = Up;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                buttonFindNext_Click(null, null);
                buttonDone_Click(null, null);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        public delegate void DoMethod(SettingsInfo settings);

        public string FindText
        {
            get
            {
                return textBoxFind.Text;
            }
            set
            {
                textBoxFind.Text = value;
            }
        }

        public string ReplaceText
        {
            get
            {
                return textBoxReplace.Text;
            }
            set
            {
                textBoxReplace.Text = value;
            }
        }

        public static string DefaultFindText
        {
            get
            {
                return defaultSettings.FindText;
            }
            set
            {
                defaultSettings = new SettingsInfo(
                    value,
                    defaultSettings.ReplaceText,
                    defaultSettings.CaseSensitive,
                    defaultSettings.MatchWholeWord,
                    defaultSettings.RestrictToSelection,
                    defaultSettings.Up);
            }
        }

        public static string DefaultReplaceText
        {
            get
            {
                return defaultSettings.ReplaceText;
            }
            set
            {
                defaultSettings = new SettingsInfo(
                    defaultSettings.FindText,
                    value,
                    defaultSettings.CaseSensitive,
                    defaultSettings.MatchWholeWord,
                    defaultSettings.RestrictToSelection,
                    defaultSettings.Up);
            }
        }

        public bool MatchCase
        {
            get
            {
                return checkBoxCaseSensitive.Checked;
            }
            set
            {
                checkBoxCaseSensitive.Checked = value;
            }
        }

        public bool MatchWholeWord
        {
            get
            {
                return checkBoxMatchWholeWord.Checked;
            }
            set
            {
                checkBoxMatchWholeWord.Checked = value;
            }
        }

        public bool RestrictToSelection
        {
            get
            {
                return restrictToSelection;
            }
            // no set method
        }

        public bool Up
        {
            get
            {
                return checkBoxUp.Checked;
            }
            set
            {
                checkBoxUp.Checked = value;
            }
        }

        public static SettingsInfo DefaultSettings
        {
            get
            {
                return defaultSettings;
            }
            set
            {
                defaultSettings = value;
            }
        }

        public SettingsInfo Settings
        {
            get
            {
                return new SettingsInfo(
                    textBoxFind.Text,
                    textBoxReplace.Text,
                    checkBoxCaseSensitive.Checked,
                    checkBoxMatchWholeWord.Checked,
                    RestrictToSelection,
                    checkBoxUp.Checked);
            }
            set
            {
                textBoxFind.Text = value.FindText;
                textBoxReplace.Text = value.ReplaceText;
                checkBoxCaseSensitive.Checked = value.CaseSensitive;
                checkBoxMatchWholeWord.Checked = value.MatchWholeWord;
                checkBoxUp.Checked = value.Up;
            }
        }

        private void buttonFindNext_Click(object sender, EventArgs e)
        {
            findAction(this.Settings);
        }

        private void buttonReplaceAndFindNext_Click(object sender, EventArgs e)
        {
            replaceAndFindAction(this.Settings);
        }

        private void buttonReplaceAll_Click(object sender, EventArgs e)
        {
            restrictToSelection = controlKey;
            replaceAllAction(this.Settings);
            buttonDone_Click(sender, e);
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            defaultSettings = this.Settings;

            lastX = DesktopBounds.Left;
            lastY = DesktopBounds.Top;

            Close();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            ControlKey = ((keyData & Keys.Control) != 0);
            return base.ProcessDialogKey(keyData);
        }

        private void timerReleaseControl_Tick(object sender, EventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == 0)
            {
                timerReleaseControl.Stop();
                ControlKey = false;
            }
        }

        private bool ControlKey
        {
            set
            {
                if (controlKey != value)
                {
                    controlKey = value;
                    if (controlKey)
                    {
                        buttonReplaceAll.Text = "Replace All In Selection";
                        timerReleaseControl.Start();
                    }
                    else
                    {
                        buttonReplaceAll.Text = "Replace All";
                    }
                }
            }
        }
    }
}
