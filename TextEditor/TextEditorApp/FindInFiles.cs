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
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TextEditor
{
    public class FindInFilesApplication : IFindInFilesApplication
    {
        public Icon ApplicationIcon { get { return TextEditorApp.Properties.Resources.Icon2; } }
        public string ApplicationName { get { return Application.ProductName; } }

        public IFindInFilesWindow Open(IFindInFilesItem item)
        {
            TextEditorWindow window = TextEditorWindow.GetWindowForLoadHelper();
            window.LoadFile(item.GetPath());
            window.Show();
            return window;
        }

        public SearchCombos Config_SearchPaths
        {
            get
            {
                return MainClass.Config.SearchPaths;
            }
            set
            {
                MainClass.Config.SearchPaths = value;
                MainClass.SaveSettings();
            }
        }

        public SearchCombos Config_SearchExtensions
        {
            get
            {
                return MainClass.Config.SearchExtensions;
            }
            set
            {
                MainClass.Config.SearchExtensions = value;
                MainClass.SaveSettings();
            }
        }

        public IFindInFilesNode GetNodeForPath(string path)
        {
            return new FindInFilesNode(path);
        }
    }

    public class FindInFilesNode : IFindInFilesNode
    {
        private readonly string root;

        public FindInFilesNode(string root)
        {
            this.root = root;
        }

        public IFindInFilesNode[] GetDirectories()
        {
            string[] directories = Directory.GetDirectories(root);
            IFindInFilesNode[] nodes = new IFindInFilesNode[directories.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new FindInFilesNode(directories[i]);
            }
            return nodes;
        }

        public IFindInFilesItem[] GetFiles()
        {
            string[] files = Directory.GetFiles(root);
            IFindInFilesItem[] nodes = new IFindInFilesItem[files.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new FindInFilesItem(files[i]);
            }
            return nodes;
        }

        public string GetPath()
        {
            return root;
        }

        public string GetFileName()
        {
            return Path.GetFileName(root);
        }
    }

    public class FindInFilesItem : IFindInFilesItem
    {
        private readonly string path;

        public FindInFilesItem(string path)
        {
            this.path = path;
        }

        public string GetPath()
        {
            return path;
        }

        public string GetFileName()
        {
            return Path.GetFileName(path);
        }

        public string GetExtension()
        {
            return Path.GetExtension(path);
        }

        public Stream Open()
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            FindInFilesItem other = obj as FindInFilesItem;
            if (other == null)
            {
                return false;
            }
            return String.Equals(this.path, other.path);
        }
    }
}
