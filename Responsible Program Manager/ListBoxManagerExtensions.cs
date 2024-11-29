using System;
using System.Windows.Controls;

namespace Responsible_Program_Manager
{
    public static class ListBoxManagerExtensions
    {
        public static void AddItem(this ListBoxManager manager, string codeName, string name, string publisher = null, string installedVersion = null,
                           string version = null, string iconPath = null, string iconUrl = null, string categories = null,
                           string installArguments = null, string downloadPath = null, string cachedPath = null, string md5_hash = null)
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
                IconPath = iconPath,
                IconUrl = iconUrl,
                Categories = categories,
                InstallArguments = installArguments,
                DownloadPath = downloadPath,
                CachedPath = cachedPath,
                MD5_hash = md5_hash
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
