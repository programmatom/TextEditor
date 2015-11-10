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
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TextEditor
{
    public partial class FindInFiles : Form
    {
        private readonly BindingList<FindInFilesEntry> results = new BindingList<FindInFilesEntry>();
        private FindInFilesTask task;
        private readonly Dictionary<string, TextEditorWindow> windows = new Dictionary<string, TextEditorWindow>();

        private int last;
        private static int counter;
        private static Dictionary<string, int> everSearched;

        public FindInFiles()
        {
            InitializeComponent();

            if (everSearched == null)
            {
                everSearched = new Dictionary<string, int>();
                SearchPaths searchPaths = MainClass.Config.SearchPaths;
                foreach (string path in searchPaths.paths)
                {
                    int current = counter++;
                    everSearched.Add(path, current);
                    if (String.Equals(path, searchPaths.last))
                    {
                        last = current;
                    }
                }
            }
            KeyValuePair<string, int>[] everSearched2 = new KeyValuePair<string, int>[everSearched.Count];
            int i = 0;
            foreach (KeyValuePair<string, int> item in everSearched)
            {
                everSearched2[i++] = item;
            }
            Array.Sort(everSearched2, delegate(KeyValuePair<string, int> l, KeyValuePair<string, int> r) { return l.Value.CompareTo(r.Value); });
            foreach (KeyValuePair<string, int> item in everSearched2)
            {
                comboBoxSearchPath.Items.Add(item.Key);
                if (item.Value == last)
                {
                    comboBoxSearchPath.SelectedItem = item.Key;
                }
            }
            comboBoxSearchPath.SelectedValueChanged += new EventHandler(comboBoxSearchPath_SelectedValueChanged);

            dataGridViewFindResults.DataSource = new BindingSource(results, null);
            dataGridViewFindResults.CellMouseDoubleClick += new DataGridViewCellMouseEventHandler(dataGridViewFindResults_CellMouseDoubleClick);
        }

        private void comboBoxSearchPath_SelectedValueChanged(object sender, EventArgs e)
        {
            int i;
            if (everSearched.TryGetValue(this.comboBoxSearchPath.Text, out i))
            {
                last = i;
            }
        }

        private void dataGridViewFindResults_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            int r = e.RowIndex;
            FindInFilesEntry entry = results[r];
            TextEditorWindow window;
            if (windows.TryGetValue(entry.Path, out window))
            {
                if (window.IsDisposed)
                {
                    windows.Remove(entry.Path);
                    window = null;
                }
            }
            if (window == null)
            {
                window = TextEditorWindow.GetWindowForLoadHelper();
                window.LoadFile(entry.Path);
                window.Show();
                windows.Add(entry.Path, window);
            }
            else
            {
                window.Activate();
            }
            window.SetSelection(Math.Max(entry.LineNumber - 1, 0), entry.StartChar, Math.Max(entry.LineNumber - 1, 0), entry.EndCharP1);
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            if (task == null)
            {
                if (String.IsNullOrEmpty(this.textBoxSearchFor.Text))
                {
                    MessageBox.Show("Search string can't be empty", "Text Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(this.comboBoxSearchPath.Text);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(String.Format("Search path is invalid: {0}", exception.Message), "Text Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!Directory.Exists(fullPath))
                {
                    MessageBox.Show("Search path directory does not exist.", "Text Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int pathIndex;
                if (!everSearched.TryGetValue(fullPath, out pathIndex))
                {
                    pathIndex = counter++;
                    everSearched.Add(fullPath, pathIndex);
                }
                last = pathIndex;
                UpdateSettings();

                results.Clear();
                task = new FindInFilesTask(
                    this.textBoxSearchFor.Text,
                    fullPath,
                    this.checkBoxCaseSensitive.Checked,
                    this.checkBoxMatchWholeWord.Checked);
                task.ProgressChanged += new ProgressChangedEventHandler(task_ProgressChanged);
                task.RunWorkerCompleted += new RunWorkerCompletedEventHandler(task_RunWorkerCompleted);
                buttonFind.Text = "Stop Find";
                task.RunWorkerAsync();
            }
            else
            {
                task.CancelAsync();
                task.ProgressChanged -= new ProgressChangedEventHandler(task_ProgressChanged);
                task.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(task_RunWorkerCompleted);
                buttonFind.Text = "Find";
                task = null;
            }
        }

        private void UpdateSettings()
        {
            List<string> paths = new List<string>();
            string lastPath = null;
            foreach (KeyValuePair<string, int> path in everSearched)
            {
                paths.Add(path.Key);
                if (last == path.Value)
                {
                    lastPath = path.Key;
                }
            }
            MainClass.Config.SearchPaths = new SearchPaths(paths.ToArray(), lastPath);
            MainClass.SaveSettings();
        }

        private void task_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            results.Add((FindInFilesEntry)e.UserState);
        }

        private void task_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            buttonFind.Text = "Find";
            task = null;
        }
    }

    public class FindInFilesEntry
    {
        private readonly string path;
        private readonly string displayPath;
        private readonly string line;
        private readonly int lineNumber;
        private readonly int startChar;
        private readonly int endCharP1;

        public FindInFilesEntry(string path, string displayPath, string line, int lineNumber, int startChar, int endCharP1)
        {
            this.path = path;
            this.displayPath = displayPath;
            this.line = line;
            this.lineNumber = lineNumber;
            this.startChar = startChar;
            this.endCharP1 = endCharP1;
        }

        public string Path { get { return path; } }
        public string DisplayPath { get { return displayPath; } }
        public int LineNumber { get { return lineNumber; } }
        public string FormattedLine { get { return line; } }
        public int StartChar { get { return startChar; } }
        public int EndCharP1 { get { return endCharP1; } }
    }

    public class FindInFilesTask : BackgroundWorker
    {
        private readonly string pattern;
        private readonly string root;
        private readonly bool caseSensitive;
        private readonly bool matchWholeWords;

        public FindInFilesTask(
            string pattern,
            string root,
            bool caseSensitive,
            bool matchWholeWords)
        {
            if (String.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException();
            }
            string fullRoot = Path.GetFullPath(root);
            if (!Directory.Exists(fullRoot))
            {
                throw new ArgumentException();
            }

            this.pattern = pattern;
            this.root = fullRoot;
            this.caseSensitive = caseSensitive;
            this.matchWholeWords = matchWholeWords;

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            bool cancelled;
            EnumerateRecursive(root, ".", out cancelled);
            e.Cancel = cancelled;
        }

        private void EnumerateRecursive(string root, string relative, out bool cancelled)
        {
            cancelled = false;
            foreach (string file in Directory.GetFiles(root))
            {
                if (CancellationPending)
                {
                    cancelled = true;
                    return;
                }
                TestFile(file, relative);
            }
            foreach (string dir in Directory.GetDirectories(root))
            {
                if (CancellationPending)
                {
                    cancelled = true;
                    return;
                }
                EnumerateRecursive(dir, Path.Combine(relative, Path.GetFileName(dir)), out cancelled);
            }
        }

        private void TestFile(string path, string relativeRoot)
        {
            string displayPath = Path.Combine(relativeRoot, Path.GetFileName(path));
            const string Prefix = @".\";
            if (displayPath.StartsWith(Prefix))
            {
                displayPath = displayPath.Substring(Prefix.Length);
            }

            try
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (TextReader reader = new StreamReader(stream, true/*detectEncoding*/))
                    {
                        int lineNumber = 0;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lineNumber++;
                            int i = -1;
                            do
                            {
                                i = line.IndexOf(pattern, i + 1, caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
                                bool skip = false;
                                if (matchWholeWords && (i >= 0))
                                {
                                    if (Char.IsLetterOrDigit(pattern[0]))
                                    {
                                        if ((i - 1 >= 0) && Char.IsLetterOrDigit(line[i - 1]))
                                        {
                                            skip = true;
                                        }
                                    }
                                    if (Char.IsLetterOrDigit(pattern[pattern.Length - 1]))
                                    {
                                        if ((i + pattern.Length < line.Length)
                                            && Char.IsLetterOrDigit(line[i + pattern.Length]))
                                        {
                                            skip = true;
                                        }
                                    }
                                }
                                if ((i >= 0) && !skip)
                                {
                                    ReportProgress(0, new FindInFilesEntry(path, displayPath, line, lineNumber, i, i + pattern.Length));
                                }
                            } while (i >= 0);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ReportProgress(0, new FindInFilesEntry(path, displayPath, String.Format("Unable to open: {0}", exception.Message), 0, 0, 0));
            }
        }
    }
}
