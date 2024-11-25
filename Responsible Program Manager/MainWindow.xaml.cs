
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;

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
                            item.IconUrl,
                            item.Categories,
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

                // Обновляем категории в ComboBox
                PopulateCategoriesComboBox();

                MessageBox.Show("Данные успешно загружены из базы!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных из базы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateCategoriesComboBox()
        {
            // Получаем все категории из AllFileSystemItems
            var uniqueCategories = new HashSet<string>();

            foreach (var item in AllFileSystemItems)
            {
                if (item.Categories != null)
                {
                    foreach (var category in item.Categories)
                    {
                        uniqueCategories.Add(category.Trim());
                    }
                }
            }

            // Заполняем ComboBox уникальными категориями
            categories_cb.Items.Clear();
            foreach (var category in uniqueCategories)
            {
                categories_cb.Items.Add(category);
            }

            // Добавляем опцию "Все категории"
            categories_cb.Items.Insert(0, "Все категории");
            categories_cb.SelectedIndex = 0; // Выбираем "Все категории" по умолчанию
        }

        private void CategoriesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categories_cb.SelectedItem is string selectedCategory)
            {
                // Очищаем список приложений
                AllApps_lbm.Clear();

                // Если выбрана опция "Все категории", показываем все элементы
                if (selectedCategory == "Все категории")
                {
                    AllApps_lbm.AddItems(AllFileSystemItems);
                }
                else
                {
                    // Фильтруем элементы по выбранной категории
                    var filteredItems = AllFileSystemItems.Where(item =>
                        item.Categories != null && item.Categories.Contains(selectedCategory)).ToList();

                    AllApps_lbm.AddItems(filteredItems);
                }
            }
        }
    }
}
