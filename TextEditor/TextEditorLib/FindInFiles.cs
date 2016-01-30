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
        private readonly IFindInFilesApplication clientApplication;
        private readonly BindingList<FindInFilesEntry> results = new BindingList<FindInFilesEntry>();
        private FindInFilesTask task;
        private readonly Dictionary<IFindInFilesItem, IFindInFilesWindow> windows = new Dictionary<IFindInFilesItem, IFindInFilesWindow>();

        private static int lastPathNumber;
        private static int pathCounter;
        private static Dictionary<string, int> searchedPaths;

        private static int lastExtensionNumber;
        private static int extensionCounter;
        private static Dictionary<string, int> searchedExtensions;

        public FindInFiles(
            IFindInFilesApplication windowManager,
            bool showPathAndExtension,
            string windowTitle)
        {
            this.clientApplication = windowManager;

            InitializeComponent();
            this.Icon = clientApplication.ApplicationIcon;
            this.Text = windowTitle;

            SetUpComboBox(
                ref pathCounter,
                ref lastPathNumber,
                ref searchedPaths,
                clientApplication.Config_SearchPaths,
                comboBoxSearchPath);
            comboBoxSearchPath.SelectedValueChanged += new EventHandler(comboBoxSearchPath_SelectedValueChanged);

            SetUpComboBox(
                ref extensionCounter,
                ref lastExtensionNumber,
                ref searchedExtensions,
                clientApplication.Config_SearchExtensions,
                comboBoxSearchExtensions);
            comboBoxSearchExtensions.SelectedValueChanged += new EventHandler(comboBoxSearchExtensions_SelectedValueChanged);

            dataGridViewFindResults.DataSource = new BindingSource(results, null);
            dataGridViewFindResults.CellMouseDoubleClick += new DataGridViewCellMouseEventHandler(dataGridViewFindResults_CellMouseDoubleClick);
            dataGridViewFindResults.EnterKeyPressed += new EventHandler(dataGridViewFindResults_EnterKeyPressed);

            if (!showPathAndExtension)
            {
                comboBoxSearchPath.Visible = false;
                comboBoxSearchExtensions.Visible = false;
                buttonFileDialog.Visible = false;
                labelSearchRoot.Visible = false;
                labelExtensions.Visible = false;
            }
        }

        private static void SetUpComboBox(
            ref int counter,
            ref int lastNumber,
            ref Dictionary<string, int> searched,
            SearchCombos searchCombos,
            ComboBox comboBox)
        {
            if (searched == null)
            {
                searched = new Dictionary<string, int>();
                foreach (string item in searchCombos.items)
                {
                    int current = counter++;
                    searched.Add(item, current);
                    if (String.Equals(item, searchCombos.last))
                    {
                        lastNumber = current;
                    }
                }
            }
            KeyValuePair<string, int>[] searched2 = new KeyValuePair<string, int>[searched.Count];
            int i = 0;
            foreach (KeyValuePair<string, int> item in searched)
            {
                searched2[i++] = item;
            }
            Array.Sort(searched2, delegate (KeyValuePair<string, int> l, KeyValuePair<string, int> r) { return l.Value.CompareTo(r.Value); });
            foreach (KeyValuePair<string, int> item in searched2)
            {
                comboBox.Items.Add(item.Key);
                if (item.Value == lastNumber)
                {
                    comboBox.SelectedItem = item.Key;
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            dpiChangeHelper.WndProcDelegate(ref m);
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (task != null)
            {
                task.CancelAsync();
            }
            base.OnFormClosing(e);
        }

        private void comboBoxSearchPath_SelectedValueChanged(object sender, EventArgs e)
        {
            int i;
            if (searchedPaths.TryGetValue(this.comboBoxSearchPath.Text, out i))
            {
                lastPathNumber = i;
            }
        }

        private void comboBoxSearchExtensions_SelectedValueChanged(object sender, EventArgs e)
        {
            int i;
            if (searchedExtensions.TryGetValue(this.comboBoxSearchExtensions.Text, out i))
            {
                lastExtensionNumber = i;
            }
        }

        private void dataGridViewFindResults_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (dataGridViewFindResults.CurrentRow == null)
            {
                return;
            }
            int r = dataGridViewFindResults.CurrentRow.Index; //e.RowIndex;
            FindInFilesEntry entry = results[r];
            IFindInFilesWindow window;
            if (windows.TryGetValue(entry.Item, out window))
            {
                if (window.IsDisposed)
                {
                    windows.Remove(entry.Item);
                    window = null;
                }
            }
            if (window == null)
            {
                window = clientApplication.Open(entry.Item);
                windows.Add(entry.Item, window);
            }
            else
            {
                window.Activate();
            }
            window.SetSelection(entry.Item, Math.Max(entry.LineNumber - 1, 0), entry.StartChar, Math.Max(entry.LineNumber - 1, 0), entry.EndCharP1);
        }

        private void dataGridViewFindResults_EnterKeyPressed(object sender, EventArgs e)
        {
            dataGridViewFindResults_CellMouseDoubleClick(sender, null);
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            if (task == null)
            {
                if (String.IsNullOrEmpty(this.textBoxSearchFor.Text))
                {
                    MessageBox.Show("Search string can't be empty", clientApplication.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // validate seach root path
                string path = this.comboBoxSearchPath.Text;
                try
                {
                    IFindInFilesNode rootNode = clientApplication.GetNodeForPath(path);
                    rootNode.GetFiles();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(String.Format("Search path is invalid: {0}", exception.Message), clientApplication.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int pathIndex;
                if (!searchedPaths.TryGetValue(path, out pathIndex))
                {
                    pathIndex = pathCounter++;
                    searchedPaths.Add(path, pathIndex);
                }
                lastPathNumber = pathIndex;
                int extensionIndex;
                if (!searchedExtensions.TryGetValue(comboBoxSearchExtensions.Text, out extensionIndex))
                {
                    extensionIndex = extensionCounter++;
                    searchedExtensions.Add(comboBoxSearchExtensions.Text, extensionIndex);
                }
                lastExtensionNumber = extensionIndex;
                UpdateSettings();

                if (!comboBoxSearchPath.Items.Contains(path))
                {
                    comboBoxSearchPath.Items.Add(path);
                    comboBoxSearchPath.SelectedValue = path;
                }
                if (!comboBoxSearchExtensions.Items.Contains(comboBoxSearchExtensions.Text))
                {
                    string text = comboBoxSearchExtensions.Text;
                    comboBoxSearchExtensions.Items.Add(text);
                    comboBoxSearchExtensions.SelectedValue = text;
                }

                results.Clear();
                task = new FindInFilesTask(
                    this.textBoxSearchFor.Text,
                    clientApplication.GetNodeForPath(path),
                    comboBoxSearchExtensions.Text,
                    this.checkBoxCaseSensitive.Checked,
                    this.checkBoxMatchWholeWord.Checked);
                task.ProgressChanged += new ProgressChangedEventHandler(task_ProgressChanged);
                task.RunWorkerCompleted += new RunWorkerCompletedEventHandler(task_RunWorkerCompleted);
                buttonFind.Text = "Stop Find";
                task.RunWorkerAsync();
                timerStatusUpdate.Start();

                dataGridViewFindResults.Focus();
            }
            else
            {
                task.CancelAsync();
                task.ProgressChanged -= new ProgressChangedEventHandler(task_ProgressChanged);
                task.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(task_RunWorkerCompleted);
                buttonFind.Text = "Find";
                task = null;
                labelStatus.Text = null;
                timerStatusUpdate.Stop();
            }
        }

        private static SearchCombos GetCombo(
            Dictionary<string, int> searched,
            int lastNumber)
        {
            List<string> items = new List<string>();
            string last = null;
            foreach (KeyValuePair<string, int> item in searched)
            {
                items.Add(item.Key);
                if (lastNumber == item.Value)
                {
                    last = item.Key;
                }
            }
            return new SearchCombos(items.ToArray(), last);
        }

        private void UpdateSettings()
        {
            clientApplication.Config_SearchPaths = GetCombo(searchedPaths, lastPathNumber);
            clientApplication.Config_SearchExtensions = GetCombo(searchedExtensions, lastExtensionNumber);
        }

        private void task_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!IsDisposed)
            {
                FindInFilesEntry[] entries = (FindInFilesEntry[])e.UserState;
                for (int i = 0; i < entries.Length; i++)
                {
                    results.Add(entries[i]);
                }
                task.Release();
            }
        }

        private void task_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            buttonFind.Text = "Find";
            task = null;
            labelStatus.Text = null;
            timerStatusUpdate.Stop();
        }

        private void timerStatusUpdate_Tick(object sender, EventArgs e)
        {
            string status = String.Empty;
            if (task != null)
            {
                status = task.CurrentPath;
            }
            labelStatus.Text = status.Replace("&", "&&");
        }

        private void buttonFileDialog_Click(object sender, EventArgs e)
        {
#if false
            // FolderBrowserDialog is unusable crap.
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    comboBoxSearchPath.Text = dialog.SelectedPath;
                }
            }
#else
            // HACK: based on what WinMerge does - usability is suspect but still much better than FolderBrowserDialog.
            // See: http://www.codeproject.com/Articles/44914/Select-file-or-folder-from-the-same-dialog
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = false;
                dialog.Title = "Select a Folder";
                dialog.FileName = "Select Folder.";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    comboBoxSearchPath.Text = Path.GetDirectoryName(dialog.FileName);
                }
            }
#endif
        }
    }

    public class MyDataGridView : DataGridView
    {
        public event EventHandler EnterKeyPressed;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                if (EnterKeyPressed != null)
                {
                    EnterKeyPressed.Invoke(this, EventArgs.Empty);
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public class MyLabel : Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            TextFormatFlags flags = TextFormatFlags.PathEllipsis | TextFormatFlags.SingleLine;
            if (!UseMnemonic)
            {
                flags |= TextFormatFlags.HidePrefix;
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text, Font,
                ClientRectangle,
                ForeColor,
                BackColor,
                flags);
        }
    }

    public class FindInFilesEntry
    {
        private readonly IFindInFilesItem item;
        private readonly string displayPath;
        private readonly string line;
        private readonly int lineNumber;
        private readonly int startChar;
        private readonly int endCharP1;

        public FindInFilesEntry(IFindInFilesItem item, string displayPath, string line, int lineNumber, int startChar, int endCharP1)
        {
            this.item = item;
            this.displayPath = displayPath;
            this.line = line;
            this.lineNumber = lineNumber;
            this.startChar = startChar;
            this.endCharP1 = endCharP1;
        }

        public IFindInFilesItem Item { get { return item; } }
        public string DisplayPath { get { return displayPath; } }
        public int LineNumber { get { return lineNumber; } }
        public string FormattedLine { get { return line; } }
        public int StartChar { get { return startChar; } }
        public int EndCharP1 { get { return endCharP1; } }
    }

    public class FindInFilesTask : BackgroundWorker
    {
        private readonly string pattern;
        private readonly string[] extensionsIncluded = new string[0];
        private readonly string[] extensionsExcluded = new string[0];
        private readonly IFindInFilesNode root;
        private readonly bool caseSensitive;
        private readonly bool matchWholeWords;
        private IFindInFilesNode currentPath;
        private const int ChunkLength = 16;
        private readonly List<FindInFilesEntry> results = new List<FindInFilesEntry>(ChunkLength);
        private DateTime lastFlush;
        private AutoResetEvent interlock;

        public FindInFilesTask(
            string pattern,
            IFindInFilesNode root,
            string extensions,
            bool caseSensitive,
            bool matchWholeWords)
        {
            if (String.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException();
            }
            // ensure valid root
            try
            {
                root.GetFiles();
            }
            catch (Exception)
            {
                throw new ArgumentException();
            }

            this.pattern = pattern;
            this.root = root;
            if (!String.IsNullOrEmpty(extensions))
            {
                List<string> extensionsIncluded = new List<string>();
                List<string> extensionsExcluded = new List<string>();
                foreach (string extension1 in extensions.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string extension = extension1;
                    if (extension.StartsWith("*"))
                    {
                        extension = extension.Substring(1);
                    }
                    bool exclude = extension.StartsWith("!");
                    if (exclude)
                    {
                        extension = extension.Substring(1);
                    }
                    if (!extension.StartsWith("."))
                    {
                        extension = String.Concat(".", extension);
                    }
                    if (!exclude)
                    {
                        extensionsIncluded.Add(extension);
                    }
                    else
                    {
                        extensionsExcluded.Add(extension);
                    }
                }
                this.extensionsIncluded = extensionsIncluded.ToArray();
                this.extensionsExcluded = extensionsExcluded.ToArray();
            }
            this.caseSensitive = caseSensitive;
            this.matchWholeWords = matchWholeWords;

            this.WorkerReportsProgress = true;
            this.WorkerSupportsCancellation = true;
        }

        public string CurrentPath { get { return currentPath != null ? currentPath.GetPath() : String.Empty; } }

        public void Release()
        {
            AutoResetEvent interlock = this.interlock;
            if (interlock != null)
            {
                interlock.Set();
            }
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            bool cancelled = false;
            try
            {
                this.interlock = new AutoResetEvent(false);

                EnumerateRecursive(root, ".", out cancelled);
            }
            finally
            {
                Flush();
                e.Cancel = cancelled;

                AutoResetEvent interlock = this.interlock;
                this.interlock = null;
                interlock.Close();
            }
        }

        private void EnumerateRecursive(IFindInFilesNode root, string relative, out bool cancelled)
        {
            currentPath = root;

            cancelled = false;
            foreach (IFindInFilesItem file in root.GetFiles())
            {
                if (CancellationPending)
                {
                    cancelled = true;
                    return;
                }

                bool include;
                {
                    include = extensionsIncluded.Length == 0;
                    string extension = file.GetExtension().ToLowerInvariant();
                    if (!include && (Array.IndexOf(extensionsIncluded, extension) >= 0))
                    {
                        include = true;
                    }
                    if (include && (Array.IndexOf(extensionsExcluded, extension) >= 0))
                    {
                        include = false;
                    }
                }

                if (include)
                {
                    TestFile(file, relative);
                }
            }
            foreach (IFindInFilesNode dir in root.GetDirectories())
            {
                if (CancellationPending)
                {
                    cancelled = true;
                    return;
                }
                EnumerateRecursive(dir, Path.Combine(relative, dir.GetFileName()), out cancelled);
            }
        }

        private void TestFile(IFindInFilesItem item, string relativeRoot)
        {
            string displayPath = Path.Combine(relativeRoot, item.GetFileName());
            const string Prefix = @".\";
            if (displayPath.StartsWith(Prefix))
            {
                displayPath = displayPath.Substring(Prefix.Length);
            }

            try
            {
                using (Stream stream = item.Open())
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
                                    SendResult(new FindInFilesEntry(item, displayPath, line, lineNumber, i, i + pattern.Length));
                                }
                            } while (i >= 0);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                SendResult(new FindInFilesEntry(item, displayPath, String.Format("Unable to open: {0}", exception.Message), 0, 0, 0));
            }

            if (lastFlush.AddMilliseconds(250) < DateTime.UtcNow)
            {
                Flush();
            }
        }

        private void Flush()
        {
            if (results.Count > 0)
            {
                FindInFilesEntry[] entries = results.ToArray();
                results.Clear();

                ReportProgress(0, entries);

                // Alas, BackgroundWorker is not quite the complete solution one was hoping for.
                // It tends to swamp the message queue with events (since DataGridView for display is much
                // slower than this is at finding items), causing the application to hang up. This
                // event is used to interlock reporting of results so that search does not resume until
                // the DataGridView has consumed all the changes just sent.
                while (!CancellationPending)
                {
                    if (this.interlock.WaitOne(1000))
                    {
                        break;
                    }
                }

                lastFlush = DateTime.UtcNow;
            }
        }

        private void SendResult(FindInFilesEntry entry)
        {
            results.Add(entry);
            if (results.Count >= ChunkLength)
            {
                Flush();
            }
        }
    }

    public interface IFindInFilesApplication
    {
        Icon ApplicationIcon { get; }
        string ApplicationName { get; }
        IFindInFilesWindow Open(IFindInFilesItem item);
        SearchCombos Config_SearchPaths { get; set; }
        SearchCombos Config_SearchExtensions { get; set; }
        IFindInFilesNode GetNodeForPath(string path);
    }

    public interface IFindInFilesWindow
    {
        bool IsDisposed { get; }
        void Activate();
        void SetSelection(IFindInFilesItem item, int startLine, int startChar, int endLine, int endCharPlusOne);
    }

    public struct SearchCombos
    {
        public string[] items;
        public string last;

        public SearchCombos(string[] items, string last)
        {
            this.items = items;
            this.last = last;
        }

        public SearchCombos(SearchCombos orig)
        {
            this.items = orig.items;
            this.last = orig.last;
        }
    }

    public interface IFindInFilesNode
    {
        IFindInFilesItem[] GetFiles();
        IFindInFilesNode[] GetDirectories();
        string GetPath();
        string GetFileName();
    }

    public interface IFindInFilesItem
    {
        string GetPath();
        string GetFileName();
        string GetExtension();
        Stream Open();

        int GetHashCode();
        bool Equals(object obj);
    }
}
