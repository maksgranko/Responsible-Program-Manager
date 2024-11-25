using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows;

namespace Responsible_Program_Manager
{
    public partial class MainWindow : Window
    {
        private List<FileSystemItem> AllFileSystemItems;
        public ListBoxManager AllApps_lbm = null;
        public ListBoxManager SelectedApps_lbm = null;
        public DatabaseManager dbm = new DatabaseManager("apps.db");

        public MainWindow()
        {
            AllFileSystemItems = dbm.GetAllFileSystemItems();

            InitializeComponent();
            AllApps_lbm = new ListBoxManager(AllApps_ExplorerListBox);
            SelectedApps_lbm = new ListBoxManager(Selected_ExplorerListBox);
        }

        private async void UpdateDatabase()
        {
            string apiUrl = "https://api.example.com/apps";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    List<FileSystemItem> itemsFromApi = JsonConvert.DeserializeObject<List<FileSystemItem>>(jsonResponse);

                    foreach (var item in itemsFromApi)
                    {
                        dbm.AddFileSystemItem(
                            item.CodeName,
                            item.Name,
                            item.Publisher,
                            item.InstalledVersion,
                            item.Version,
                            item.IconPath,
                            item.InstallArguments,
                            item.DownloadPath
                        );
                    }
                }

                MessageBox.Show("Данные успешно обновлены из API!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadAppsFromDB()
        {
            try
            {
                int batchSize = 100;
                int offset = 0;
                bool hasMoreData = true;

                AllFileSystemItems.Clear();

                while (hasMoreData)
                {
                    List<FileSystemItem> items = dbm.GetFileSystemItemsBatch(offset, batchSize);

                    if (items.Count > 0)
                    {
                        AllFileSystemItems.AddRange(items);
                        offset += batchSize;
                    }
                    else
                    {
                        hasMoreData = false;
                    }
                }

                AllApps_lbm.Clear();
                SelectedApps_lbm.Clear();
                AllApps_lbm.AddItems(AllFileSystemItems);

                MessageBox.Show("Данные успешно загружены из базы!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных из базы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
