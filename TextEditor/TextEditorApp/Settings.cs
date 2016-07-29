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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

namespace TextEditor
{
    public enum BackingStore
    {
        String,
        Utf8SplayGapBuffer,
    }

    public struct EditorConfig
    {
        public string Extension;
        public Font Font;
        public int TabSize;
        public bool AutoIndent;
        public bool InsertTabAsSpaces;
        public bool SimpleNavigation;

        public EditorConfig(
            string extension,
            Font font,
            int tabSize,
            bool autoIndent,
            bool insertTabAsSpaces,
            bool simpleNavigation)
        {
            this.Extension = extension;
            this.Font = font;
            this.TabSize = tabSize;
            this.AutoIndent = autoIndent;
            this.InsertTabAsSpaces = insertTabAsSpaces;
            this.SimpleNavigation = simpleNavigation;
        }

        public EditorConfig(EditorConfig original)
        {
            this.Extension = original.Extension;
            this.Font = original.Font;
            this.TabSize = original.TabSize;
            this.AutoIndent = original.AutoIndent;
            this.InsertTabAsSpaces = original.InsertTabAsSpaces;
            this.SimpleNavigation = original.SimpleNavigation;
        }
    }

    public class EditorConfigList
    {
        private List<EditorConfig> configs = new List<EditorConfig>();
        private int width;
        private int height;
        private BackingStore backingStore = BackingStore.String;
        private TextService textService = TextService.Simple;
        private SearchCombos searchPaths = new SearchCombos();
        private SearchCombos searchExtensions = new SearchCombos();

        public EditorConfigList()
        {
            configs.Add(
                new EditorConfig(
                    null/*extension*/,
                    Form.DefaultFont,
                    8,
                    false/*autoIndent*/,
                    false/*insertTabAsSpaces*/,
                    false/*simpleNavigation*/));
        }

        public EditorConfigList(EditorConfigList original)
        {
            this.width = original.width;
            this.height = original.height;
            this.backingStore = original.backingStore;
            this.textService = original.textService;
            this.searchPaths = new SearchCombos(original.searchPaths);
            this.searchExtensions = new SearchCombos(original.searchExtensions);

            configs.Clear();
            foreach (EditorConfig origConfig in original.configs)
            {
                configs.Add(new EditorConfig(origConfig));
            }
        }

        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public BackingStore BackingStore
        {
            get
            {
                return backingStore;
            }
            set
            {
                backingStore = value;
            }
        }

        public TextService TextService
        {
            get
            {
                return textService;
            }
            set
            {
                textService = value;
            }
        }

        public SearchCombos SearchPaths
        {
            get
            {
                return searchPaths;
            }
            set
            {
                searchPaths = value;
            }
        }

        public SearchCombos SearchExtensions
        {
            get
            {
                return searchExtensions;
            }
            set
            {
                searchExtensions = value;
            }
        }

        public int Count
        {
            get
            {
                return configs.Count;
            }
        }

        public EditorConfig this[int index]
        {
            get
            {
                return configs[index];
            }
            set
            {
                configs[index] = value;
            }
        }

        public void Add(EditorConfig config)
        {
            configs.Add(new EditorConfig(config));
        }

        public void RemoveAt(int i)
        {
            configs.RemoveAt(i);
        }

        public EditorConfig Find(string extension)
        {
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }
            for (int i = 1; i < configs.Count; i++)
            {
                foreach (string ext in configs[i].Extension.Split(';'))
                {
                    string ext2 = ext;
                    if (ext2 == null)
                    {
                        ext2 = String.Empty;
                    }
                    ext2 = ext2.Trim();
                    if (ext2.StartsWith("."))
                    {
                        ext2 = ext2.Substring(1);
                    }
                    if (String.Equals(ext2, extension, StringComparison.OrdinalIgnoreCase))
                    {
                        return configs[i];
                    }
                }
            }
            return configs[0];
        }

        public EditorConfigList(string path)
            : this()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(path);
            bool defaultWasSet = false;

            try
            {
                width = xml.CreateNavigator().SelectSingleNode("/settings/width").ValueAsInt;
                height = xml.CreateNavigator().SelectSingleNode("/settings/height").ValueAsInt;
                backingStore = (BackingStore)Enum.Parse(typeof(BackingStore), xml.CreateNavigator().SelectSingleNode("/settings/backingStore").Value);
                textService = (TextService)Enum.Parse(typeof(TextService), xml.CreateNavigator().SelectSingleNode("/settings/textService").Value);
            }
            catch (NullReferenceException)
            {
            }
            {
                List<string> searchPathStrings = new List<string>();
                string searchPathLast = null;
                foreach (XPathNavigator searchPath in xml.CreateNavigator().Select("/settings/searchPaths/searchPath"))
                {
                    searchPathStrings.Add(searchPath.Value);
                    XPathNavigator last = searchPath.SelectSingleNode("@last");
                    if ((last != null) && last.ValueAsBoolean)
                    {
                        searchPathLast = searchPath.Value;
                    }
                }
                searchPaths = new SearchCombos(searchPathStrings.ToArray(), searchPathLast);
            }
            {
                List<string> searchExtStrings = new List<string>();
                string searchExtLast = null;
                foreach (XPathNavigator searchExt in xml.CreateNavigator().Select("/settings/searchExtensions/searchExtension"))
                {
                    searchExtStrings.Add(searchExt.Value);
                    XPathNavigator last = searchExt.SelectSingleNode("@last");
                    if ((last != null) && last.ValueAsBoolean)
                    {
                        searchExtLast = searchExt.Value;
                    }
                }
                searchExtensions = new SearchCombos(searchExtStrings.ToArray(), searchExtLast);
            }

            foreach (XPathNavigator nav in xml.CreateNavigator().Select("/settings/config"))
            {
                EditorConfig config = new EditorConfig(
                    nav.SelectSingleNode("extension").Value,
                    ReadFont(nav.SelectSingleNode("font")),
                    nav.SelectSingleNode("tabSize").ValueAsInt,
                    nav.SelectSingleNode("autoIndent").ValueAsBoolean,
                    nav.SelectSingleNode("insertTabAsSpaces").ValueAsBoolean,
                    nav.SelectSingleNode("simpleNavigation").ValueAsBoolean);
                if (String.IsNullOrEmpty(config.Extension) && !defaultWasSet)
                {
                    config.Extension = null;
                    configs[0] = config;
                    defaultWasSet = true;
                    continue;
                }
                if (String.IsNullOrEmpty(config.Extension) && defaultWasSet)
                {
                    config.Extension = String.Empty;
                }
                configs.Add(config);
            }
        }

        public void Save(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (Stream output = new FileStream(path, FileMode.Create))
            {
                using (XmlWriter writer = XmlWriter.Create(output, settings))
                {
                    writer.WriteStartElement("settings");

                    writer.WriteStartElement("width");
                    writer.WriteValue(width);
                    writer.WriteEndElement();

                    writer.WriteStartElement("height");
                    writer.WriteValue(height);
                    writer.WriteEndElement();

                    writer.WriteStartElement("backingStore");
                    writer.WriteValue(backingStore.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("textService");
                    writer.WriteValue(textService.ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("searchPaths");
                    foreach (string searchPath in searchPaths.items)
                    {
                        writer.WriteStartElement("searchPath");
                        if (String.Equals(searchPath, SearchPaths.last))
                        {
                            writer.WriteStartAttribute("last");
                            writer.WriteValue(true);
                            writer.WriteEndAttribute();
                        }
                        writer.WriteValue(searchPath);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("searchExtensions");
                    foreach (string searchExt in searchExtensions.items)
                    {
                        writer.WriteStartElement("searchExtension");
                        if (String.Equals(searchExt, SearchExtensions.last))
                        {
                            writer.WriteStartAttribute("last");
                            writer.WriteValue(true);
                            writer.WriteEndAttribute();
                        }
                        writer.WriteValue(searchExt);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

                    foreach (EditorConfig config in configs)
                    {
                        writer.WriteStartElement("config");

                        writer.WriteStartElement("extension");
                        writer.WriteString(config.Extension);
                        writer.WriteEndElement();

                        WriteFont(writer, config.Font);

                        writer.WriteStartElement("tabSize");
                        writer.WriteValue(config.TabSize);
                        writer.WriteEndElement();

                        writer.WriteStartElement("autoIndent");
                        writer.WriteValue(config.AutoIndent);
                        writer.WriteEndElement();

                        writer.WriteStartElement("insertTabAsSpaces");
                        writer.WriteValue(config.InsertTabAsSpaces);
                        writer.WriteEndElement();

                        writer.WriteStartElement("simpleNavigation");
                        writer.WriteValue(config.SimpleNavigation);
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }
        }

        private Font ReadFont(XPathNavigator nav)
        {
            return new Font(
                nav.SelectSingleNode("familyName").Value,
                (float)nav.SelectSingleNode("emSize").ValueAsDouble,
                (FontStyle)nav.SelectSingleNode("fontStyle").ValueAsInt);
        }

        private void WriteFont(XmlWriter writer, Font font)
        {
            writer.WriteStartElement("font");

            writer.WriteStartElement("familyName");
            writer.WriteValue(font.FontFamily.Name);
            writer.WriteEndElement();

            writer.WriteStartElement("emSize");
            writer.WriteValue(font.Size);
            writer.WriteEndElement();

            writer.WriteStartElement("fontStyle");
            writer.WriteValue((int)font.Style);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
