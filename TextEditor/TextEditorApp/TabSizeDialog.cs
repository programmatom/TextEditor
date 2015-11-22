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
    public partial class TabSizeDialog : Form
    {
        private readonly int originalValue;

        public TabSizeDialog(int value)
        {
            this.originalValue = value;

            InitializeComponent();
            this.Icon = TextEditorApp.Properties.Resources.Icon2;

            textBoxTabSize.Text = value.ToString();
        }

        public int Value
        {
            get
            {
                int value;
                if (!Int32.TryParse(textBoxTabSize.Text, out value))
                {
                    value = originalValue;
                }
                value = Math.Min(Math.Max(value, 1), 255);
                return value;
            }
        }
    }
}
