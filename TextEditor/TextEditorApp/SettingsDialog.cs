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
using System.Text;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class Settings : Form
    {
        private EditorConfigList config;
        private List<SettingsPanel> panels = new List<SettingsPanel>();

        public Settings(EditorConfigList configArg)
        {
            this.config = new EditorConfigList(configArg);

            InitializeComponent();

            switch (config.BackingStore)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case BackingStore.String:
                    comboBoxBackingStore.SelectedItem = "String";
                    break;
                case BackingStore.Protected:
                    comboBoxBackingStore.SelectedItem = "Protected";
                    break;
                case BackingStore.Utf8SplayGapBuffer:
                    comboBoxBackingStore.SelectedItem = "Utf8SplayGapBuffer";
                    break;
            }

            switch (config.TextService)
            {
                default:
                    Debug.Assert(false);
                    throw new ArgumentException();
                case TextService.Simple:
                    comboBoxTextService.SelectedItem = "Simple";
                    break;
                case TextService.Uniscribe:
                    comboBoxTextService.SelectedItem = "Uniscribe";
                    break;
                case TextService.DirectWrite:
                    comboBoxTextService.SelectedItem = "DirectWrite";
                    break;
            }

            ((SettingsPanel)((TabPage)tabControlSettings.Controls[0]).Controls[0]).All = this.config[0];
            for (int i = 1; i < this.config.Count; i++)
            {
                AddPage(this.config[i]);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            for (int i = 0; i < config.Count; i++)
            {
                EditorConfig config1 = ((SettingsPanel)((TabPage)tabControlSettings.Controls[i]).Controls[0]).All;
                if ((config1.Extension == null) && (i > 0))
                {
                    config1.Extension = String.Empty;
                }
                config[i] = config1;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EditorConfigList Config
        {
            get
            {
                switch ((string)comboBoxBackingStore.SelectedItem)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case "String":
                        config.BackingStore = BackingStore.String;
                        break;
                    case "Protected":
                        config.BackingStore = BackingStore.Protected;
                        break;
                    case "Utf8SplayGapBuffer":
                        config.BackingStore = BackingStore.Utf8SplayGapBuffer;
                        break;
                }

                switch ((string)comboBoxTextService.SelectedItem)
                {
                    default:
                        Debug.Assert(false);
                        throw new ArgumentException();
                    case "Simple":
                        config.TextService = TextService.Simple;
                        break;
                    case "Uniscribe":
                        config.TextService = TextService.Uniscribe;
                        break;
                    case "DirectWrite":
                        config.TextService = TextService.DirectWrite;
                        break;
                }

                return config;
            }
        }

        private TabPage AddPage(EditorConfig config)
        {
            SettingsPanel settings = new SettingsPanel();
            if (config.Extension == null)
            {
                config.Extension = String.Empty;
            }
            settings.All = config;

            // copied from SettingsDialog.Designer.cs
            settings.AutoSize = true;
            settings.Dock = System.Windows.Forms.DockStyle.Fill;
            settings.Location = new System.Drawing.Point(3, 3);
            settings.MinimumSize = new System.Drawing.Size(248, 114);
            settings.Padding = new System.Windows.Forms.Padding(0, 7, 0, 7);
            settings.Size = new System.Drawing.Size(440, 172);

            TabPage tabPage = new TabPage();
            tabPage.Text = settings.Extension;

            // copied from SettingsDialog.Designer.cs
            tabPage.Location = new System.Drawing.Point(4, 22);
            tabPage.Padding = new System.Windows.Forms.Padding(3);
            tabPage.Size = new System.Drawing.Size(446, 178);
            tabPage.UseVisualStyleBackColor = true;

            tabPage.Controls.Add(settings);
            tabControlSettings.Controls.Add(tabPage);

            settings.OnDelete += new EventHandler(settings_OnDelete);
            settings.OnExtensionChanged += new EventHandler(settings_OnExtensionChanged);

            return tabPage;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            // clone currently visible config
            EditorConfig config1 = ((SettingsPanel)tabControlSettings.SelectedTab.Controls[0]).All;
            config1.Extension = String.Empty;
            config.Add(config1);

            TabPage added = AddPage(config1);
            tabControlSettings.SelectedTab = added;
        }

        private void settings_OnDelete(object sender, EventArgs e)
        {
            for (int i = 0; i < config.Count; i++)
            {
                SettingsPanel settingsPanel = (SettingsPanel)(tabControlSettings.Controls[i]).Controls[0];
                if (sender == settingsPanel)
                {
                    tabControlSettings.Controls.RemoveAt(i);
                    config.RemoveAt(i);
                    return;
                }
            }
            Debug.Assert(false);
        }

        private void settings_OnExtensionChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < config.Count; i++)
            {
                SettingsPanel settingsPanel = (SettingsPanel)(tabControlSettings.Controls[i]).Controls[0];
                if (sender == settingsPanel)
                {
                    tabControlSettings.Controls[i].Text = settingsPanel.Extension;
                    return;
                }
            }
            Debug.Assert(false);
        }
    }
}
