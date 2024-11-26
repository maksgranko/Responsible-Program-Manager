
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System;

namespace Responsible_Program_Manager
{
    public partial class MainWindow : Window
    {
        private List<FileSystemItem> AllFileSystemItems = new List<FileSystemItem>();
        public ListBoxManager AllApps_lbm = null;
        public ListBoxManager SelectedApps_lbm = null;
        public DatabaseManager dbm = new DatabaseManager("apps.db");

        public MainWindow()
        {
            UpdateDatabase();
            //AllFileSystemItems = dbm.GetAllFileSystemItems();
            InitializeComponent();
            AllApps_lbm = new ListBoxManager(AllApps_ExplorerListBox);
            SelectedApps_lbm = new ListBoxManager(Selected_ExplorerListBox);
            ReloadAppsFromDB();
        }

        private void UpdateDatabase()
        {
            string apiUrl = "https://tosters-office.online/api/getAllAppLinks.php";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(apiUrl).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    List<FileSystemItem> itemsFromApi = JsonConvert.DeserializeObject<List<FileSystemItem>>(jsonResponse);

                    foreach (var item in itemsFromApi)
                    {
                        dbm.AddOrUpdateFileSystemItem(
                            item.CodeName,
                            item.Name,
                            item.Publisher,
                            item.InstalledVersion,
                            item.Version,
                            item.IconPath,
                            item.IconUrl,
                            item.Categories,
                            item.InstallArguments,
                            item.DownloadPath,
                            item.CachedPath
                        );
                    }
                }
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
                    string[] item_categories = item.Categories.Split(';');
                    foreach (var category in item_categories)
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
        private void AllApps_ExplorerListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AllApps_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
                // Проверяем, существует ли уже элемент в SelectedApps_lbm
                if (!SelectedApps_lbm.GetAllItems().Any(item => item.CodeName == selectedItem.CodeName))
                {
                    AllApps_lbm.RemoveSelectedItem();
                    SelectedApps_lbm.AddItem(selectedItem);
                }
            }
        }
        private void Selected_ExplorerListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Selected_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
                // Проверяем, существует ли уже элемент в AllApps_lbm
                if (!AllApps_lbm.GetAllItems().Any(item => item.CodeName == selectedItem.CodeName))
                {
                    SelectedApps_lbm.RemoveSelectedItem();
                    AllApps_lbm.AddItem(selectedItem);
                }
            }
        }


        private void AddItemFromContextMenu(object sender, RoutedEventArgs e)
        {
            if (AllApps_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
                AllApps_lbm.RemoveSelectedItem();
                SelectedApps_lbm.AddItem(selectedItem);
            }
        }

        private void ShowDownloadSource(object sender, RoutedEventArgs e)
        {
            if (AllApps_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
                MessageBox.Show($"Источник загрузки: {selectedItem.DownloadPath}", "Источник", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string ProcessInstallArguments(string installArguments, string cacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(installArguments))
                throw new ArgumentException("InstallArguments не может быть пустым");

            // Разбиваем строку аргументов, убирая пустые элементы вручную
            var parts = installArguments
                .Split(';')
                .Where(part => !string.IsNullOrWhiteSpace(part)) // Убираем пустые строки
                .ToArray();

            if (parts.Length == 0)
                throw new ArgumentException("InstallArguments не содержит валидных данных");

            // Первая часть — это шаблон имени файла
            string filePattern = parts[0];
            string installFilePath = Directory
                .GetFiles(cacheDirectory, filePattern)
                .FirstOrDefault();

            if (installFilePath == null)
                throw new FileNotFoundException($"Файл, соответствующий шаблону '{filePattern}', не найден в папке {cacheDirectory}");

            // Оставшиеся части — это дополнительные аргументы
            string arguments = parts.Length > 1
                ? string.Join(" ", parts.Skip(1)) // Объединяем оставшиеся аргументы через пробел
                : string.Empty;

            return $"\"{installFilePath}\" {arguments}";
        }

        private void CategoriesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categories_cb.SelectedItem is string selectedCategory)
            {
                AllApps_lbm.Clear();

                var selectedItems = SelectedApps_lbm.GetAllItems().Select(item => item.CodeName).ToHashSet();

                if (selectedCategory == "Все категории")
                {
                    // Исключаем уже выбранные элементы
                    var filteredItems = AllFileSystemItems
                        .Where(item => !selectedItems.Contains(item.CodeName))
                        .ToList();

                    AllApps_lbm.AddItems(filteredItems);
                }
                else
                {
                    var filteredItems = AllFileSystemItems
                        .Where(item => item.Categories?.Split(';').Contains(selectedCategory) ?? false)
                        .Where(item => !selectedItems.Contains(item.CodeName)) // Исключаем уже выбранные
                        .ToList();

                    AllApps_lbm.AddItems(filteredItems);
                }
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
            bool shouldOpenCache = false; // Флаг, указывающий, нужно ли открывать папку
            var successfullyCachedItems = new List<FileSystemItem>(); // Список успешно загруженных элементов

            // Сначала загружаем все файлы
            foreach (var item in SelectedApps_lbm.GetAllItems())
            {
                try
                {
                    CacheFile(item);
                    successfullyCachedItems.Add(item); // Добавляем в список успешно загруженных
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке приложения '{item.Name}': {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Затем выполняем действия (установка/удаление кэша)
            foreach (var item in successfullyCachedItems)
            {
                switch (apply_mode_combobox.SelectedIndex)
                {
                    case 1: // Только установить
                        InstallFile(item);
                        DeleteCachedFile(item);
                        break;

                    case 2: // Загрузить и установить
                        InstallFile(item);
                        shouldOpenCache = true; // Отмечаем, что нужно открыть папку после загрузки
                        break;

                    case 0: // Только загрузить
                        shouldOpenCache = true; // Если только загружаем, тоже отмечаем
                        break;
                }
            }

            // Открываем папку Cache только если это необходимо
            if (shouldOpenCache && successfullyCachedItems.Count > 0)
            {
                OpenCacheFolder(cacheDirectory);
            }

            MessageBox.Show("Операция выполнена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        // Метод для открытия папки Cache
        private void OpenCacheFolder(string cacheDirectory)
        {
            try
            {
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory); // Создаём папку, если её нет
                }

                System.Diagnostics.Process.Start("explorer.exe", cacheDirectory); // Открываем папку
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть папку Cache: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CacheFile(FileSystemItem item)
        {
            try
            {
                // Убедимся, что папка Cache существует
                string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                // Определяем путь для кэшированного файла
                string cachePath = Path.Combine(cacheDirectory, $"{item.CodeName}.exe");

                using (var client = new HttpClient())
                {
                    // Загружаем файл
                    var fileBytes = client.GetByteArrayAsync(item.DownloadPath).GetAwaiter().GetResult();
                    File.WriteAllBytes(cachePath, fileBytes);
                }

                item.CachedPath = cachePath;

                // Обновляем путь в базе данных
                dbm.UpdateCachedPath(item.CodeName, cachePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void InstallFile(FileSystemItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.CachedPath) || !File.Exists(item.CachedPath))
                {
                    throw new FileNotFoundException("Кэшированный файл не найден.", item.CachedPath);
                }

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.CachedPath,
                    Arguments = item.InstallArguments, // Аргументы установки
                    UseShellExecute = true,
                    Verb = "runas" // Запуск с правами администратора
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show($"Приложение {item.Name} успешно установлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Приложение {item.Name} завершило установку с ошибкой (код {process.ExitCode}).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при установке приложения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCachedFile(FileSystemItem item)
        {
            if (File.Exists(item.CachedPath))
            {
                File.Delete(item.CachedPath);
            }
        }
    }
}
