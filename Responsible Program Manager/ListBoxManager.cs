
using System.Collections.Generic;
using System.Windows.Controls;

namespace Responsible_Program_Manager
{
    public class ListBoxManager
    {
        private ListBox listBox;

        public ListBoxManager(ListBox existingListBox)
        {
            listBox = existingListBox;
        }

        public void AddItem(FileSystemItem item)
        {
            listBox.Items.Add(item);
        }

        public void AddItems(IEnumerable<FileSystemItem> items)
        {
            foreach (var item in items)
            {
                listBox.Items.Add(item);
            }
        }

        public void Clear()
        {
            listBox.Items.Clear();
        }

        public void RemoveSelectedItem()
        {
            if (listBox.SelectedItem != null)
            {
                listBox.Items.Remove(listBox.SelectedItem);
            }
        }
    }

    public class FileSystemItem
    {
        public string CodeName { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string InstalledVersion { get; set; } // Только для SQLite
        public string Version { get; set; }
        public string IconPath { get; set; } // Путь к кэшированным данным
        public string IconUrl { get; set; } // Новый URL для удалённой базы
        public string Categories { get; set; }
        public string InstallArguments { get; set; }
        public string DownloadPath { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
