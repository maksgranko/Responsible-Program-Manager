using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Responsible_Program_Manager
{
    public static class ListBoxManagerExtensions
    {
        public static void AddItem(this ListBoxManager manager, string codeName, string name, string publisher = null, string installedVersion = null, string version = null, string iconPath = null, string[] categories = null)
        {
            if (string.IsNullOrWhiteSpace(codeName) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("CodeName и Name обязательны для добавления элемента.");
            }

            var item = new FileSystemItem
            {
                CodeName = codeName,
                Name = name,
                Publisher = publisher,
                InstalledVersion = installedVersion,
                Version = version,
                Categories = categories,
                IconPath = iconPath
            };

            manager.AddItem(item);
        }

        private static void AddItem(this ListBoxManager manager, FileSystemItem item)
        {
            var listBox = typeof(ListBoxManager).GetField("listBox", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(manager) as ListBox;
            listBox?.Items.Add(item);
        }
    }
}
