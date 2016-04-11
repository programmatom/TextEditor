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
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace TextEditor
{
    public static class MainClass
    {
        public static TextEditorWindow defaultEmptyForm;

        public static EditorConfigList Config = new EditorConfigList();

        private const string SettingsFileName = "Settings.xml";
        private const string LocalApplicationDirectoryName = "TextEditor";
        private static string GetSettingsPath(bool create)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.None);
            string dir = Path.Combine(root, LocalApplicationDirectoryName);
            if (create)
            {
                Directory.CreateDirectory(dir);
            }
            string path = Path.Combine(dir, SettingsFileName);
            return path;
        }

        public static void LoadSettings()
        {
            string path = GetSettingsPath(false/*create*/);
            if (File.Exists(path))
            {
                try
                {
                    Config = new EditorConfigList(path);
                }
                catch (Exception exception)
                {
                    //Debug.Assert(false);
                    MessageBox.Show(String.Format("Exception reading settings file: {0}", exception));
                }
            }
        }

        public static void SaveSettings()
        {
            Config.Save(GetSettingsPath(true/*create*/));
        }

        private static string GetLocalAppDataPath(bool create, bool roaming)
        {
            string applicationDataPath = Environment.GetEnvironmentVariable(roaming ? "APPDATA" : "LOCALAPPDATA");
            if (applicationDataPath == null)
            {
                applicationDataPath = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Application Data"); // Windows XP fallback
            }
            applicationDataPath = Path.Combine(applicationDataPath, LocalApplicationDirectoryName);
            if (create && !Directory.Exists(applicationDataPath))
            {
                Directory.CreateDirectory(applicationDataPath);
            }
            return applicationDataPath;
        }

        [STAThread]
        private static void Main(string[] args)
        {
            LoadSettings();

            #region Debugger Attach Helper
            {
                bool waitDebugger = false;
                if ((args.Length > 0) && String.Equals(args[0], "-waitdebugger"))
                {
                    waitDebugger = true;
                    Array.Copy(args, 1, args, 0, args.Length - 1);
                    Array.Resize(ref args, args.Length - 1);
                }

                bool debuggerBreak = false;
                if ((args.Length > 0) && String.Equals(args[0], "-break"))
                {
                    debuggerBreak = true;
                    Array.Copy(args, 1, args, 0, args.Length - 1);
                    Array.Resize(ref args, args.Length - 1);
                }

                if (waitDebugger)
                {
                    while (!Debugger.IsAttached)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                if (debuggerBreak)
                {
                    Debugger.Break();
                }
            }
            #endregion

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length != 0)
            {
                foreach (string arg in args)
                {
                    try
                    {
                        TextEditorWindow window = new TextEditorWindow(arg);
                        window.Show();
                    }
                    catch (ApplicationException)
                    {
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                defaultEmptyForm = new TextEditorWindow();
                defaultEmptyForm.Show();
            }

            Application.Idle += new EventHandler(Application_Idle);
            Application.Run();
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count == 0)
            {
                Application.Exit();
            }
        }
    }
}
