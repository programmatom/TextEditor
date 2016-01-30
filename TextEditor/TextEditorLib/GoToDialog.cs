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
    public partial class GoToDialog : Form
    {
        public GoToDialog(int value)
        {
            InitializeComponent();

            textBox.Text = value.ToString();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            textBox.Focus();
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        public int Value
        {
            get
            {
                int value;
                Int32.TryParse(textBox.Text, out value);
                return value;
            }
        }
    }
}
