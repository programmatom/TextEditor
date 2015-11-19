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
    public partial class SettingsPanel : UserControl
    {
        private Font font;
        private bool nullExtension;

        public SettingsPanel()
        {
            InitializeComponent();

            FontConfig = Font;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Extension
        {
            get
            {
                return !nullExtension ? textBoxExtension.Text : null;
            }
            set
            {
                textBoxExtension.Text = value;
                nullExtension = value == null;
                textBoxExtension.Enabled = !nullExtension;
                buttonDelete.Enabled = !nullExtension;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font FontConfig
        {
            get
            {
                return font;
            }
            set
            {
                font = value;
                labelFont.Text = String.Format(
                    "{0}, {1}{2}",
                    font.FontFamily.Name,
                    font.Style != FontStyle.Regular
                        ? font.Style.ToString().ToLower() + " ,"
                        : null,
                    font.SizeInPoints);
                labelFont.Font = font;
            }
        }

        private void buttonSetFont_Click(object sender, EventArgs e)
        {
            using (FontDialog dialog = new FontDialog())
            {
                dialog.Font = Font;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    FontConfig = dialog.Font;
                }
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TabSize
        {
            get
            {
                int value;
                Int32.TryParse(textBoxTabSize.Text, out value);
                value = Math.Min(Math.Max(value, 1), 255);
                return value;
            }
            set
            {
                textBoxTabSize.Text = value.ToString();
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AutoIndent
        {
            get
            {
                return checkBoxAutoIndent.Checked;
            }
            set
            {
                checkBoxAutoIndent.Checked = value;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool InsertTabAsSpaces
        {
            get
            {
                return checkBoxInsertTabAsSpaces.Checked;
            }
            set
            {
                checkBoxInsertTabAsSpaces.Checked = value;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SimpleNavigation
        {
            get
            {
                return checkBoxSimpleNavigation.Checked;
            }
            set
            {
                checkBoxSimpleNavigation.Checked = value;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EditorConfig All
        {
            get
            {
                return new EditorConfig(
                    this.Extension,
                    this.FontConfig,
                    this.TabSize,
                    this.AutoIndent,
                    this.InsertTabAsSpaces,
                    this.SimpleNavigation);
            }
            set
            {
                this.Extension = value.Extension;
                this.FontConfig = value.Font;
                this.TabSize = value.TabSize;
                this.AutoIndent = value.AutoIndent;
                this.InsertTabAsSpaces = value.InsertTabAsSpaces;
                this.SimpleNavigation = value.SimpleNavigation;
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (OnDelete != null)
            {
                OnDelete.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnDelete;

        private void textBoxExtension_TextChanged(object sender, EventArgs e)
        {
            if (OnExtensionChanged != null)
            {
                OnExtensionChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnExtensionChanged;
    }
}
