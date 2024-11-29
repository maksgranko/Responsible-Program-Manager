using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Threading.Tasks;

namespace Responsible_Program_Manager
{
    public partial class MainWindow : Window
    {
        private List<FileSystemItem> AllFileSystemItems = new List<FileSystemItem>();
        private List<FileSystemItem> cached_AllFileSystemItems = new List<FileSystemItem>(); 
        public ListBoxManager AllApps_lbm = null;
        public ListBoxManager SelectedApps_lbm = null;
        public ListBoxManager cached_AllApps_lbm = null;
        public ListBoxManager cached_SelectedApps_lbm = null;
        public DatabaseManager dbm = new DatabaseManager("apps.db");
        public bool ready = true;

        public MainWindow()
        {
            UpdateDatabase();
            //AllFileSystemItems = dbm.GetAllFileSystemItems();
            InitializeComponent();
            AllApps_lbm = new ListBoxManager(AllApps_ExplorerListBox);
            SelectedApps_lbm = new ListBoxManager(Selected_ExplorerListBox);
            cached_AllApps_lbm = new ListBoxManager(cached_AllApps_ExplorerListBox); 
            cached_SelectedApps_lbm = new ListBoxManager(cached_Selected_ExplorerListBox);
            
            ReloadAppsFromDB();
            ReloadCachedAppsFromDB();
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
                            item.IconUrl,
                            item.Categories,
                            item.InstallArguments,
                            item.DownloadPath,
                            item.MD5_hash
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

                foreach (var item in AllFileSystemItems)
                {
                    EnsureIconCached(item); // Проверяем и загружаем иконки
                }

                AllApps_lbm.Clear();
                SelectedApps_lbm.Clear();
                AllApps_lbm.AddItems(AllFileSystemItems);

                // Обновляем категории в ComboBox
                PopulateCategoriesComboBox();
            }
            catch (Exception ex)
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
            ApplyButton_action();
        }
        private async void ApplyButton_action()
        {
            string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
            bool shouldOpenCache = false; // Флаг, указывающий, нужно ли открывать папку
            var successfullyInstalledItems = new List<FileSystemItem>(); // Список успешно установленных элементов

            OnWaiting();

            // Сначала загружаем все файлы
            foreach (var item in SelectedApps_lbm.GetAllItems())
            {
                try
                {
                    // Проверка наличия файла в кэше
                    if (string.IsNullOrWhiteSpace(item.CachedPath) || !File.Exists(item.CachedPath))
                    {
                        // Если файл не в кэше, загружаем его
                        await Task.Run(() => CacheFile(item));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке приложения '{item.Name}': {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Затем выполняем действия (установка/удаление кэша)
            foreach (var item in SelectedApps_lbm.GetAllItems())
            {
                switch (apply_mode_combobox.SelectedIndex)
                {
                    case 1: // Только установить
                    case 2: // Загрузить и установить
                            // Устанавливаем файл
                        if (await Task.Run(() => InstallFile(item)))
                        {
                            successfullyInstalledItems.Add(item);
                        }

                        // Если только устанавливаем, проверяем, если файл нет в кэше - удаляем кэш
                        if (apply_mode_combobox.SelectedIndex == 1 && !string.IsNullOrWhiteSpace(item.CachedPath) && !File.Exists(item.CachedPath))
                        {
                            await Task.Run(() => DeleteCachedFile(item));
                        }
                        if (apply_mode_combobox.SelectedIndex == 2)
                        {
                            shouldOpenCache = true; // Отмечаем, что нужно открыть папку после загрузки
                        }
                        break;

                    case 0: // Только загрузить
                        shouldOpenCache = true; // Если только загружаем, тоже отмечаем
                        break;
                }
            }

            OffWaiting();

            // Открываем папку Cache, если это необходимо
            if (shouldOpenCache)
            {
                OpenCacheFolder(cacheDirectory);
            }

            // Сообщаем пользователю о результате операции
            if(Selected_ExplorerListBox.Items.Count == 0)
            {
                MessageBox.Show("Не выбрано ни одно приложение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (successfullyInstalledItems.Count == 0)
            {
                MessageBox.Show("Ни одного приложения не было установлено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (apply_mode_combobox.SelectedIndex == 1 || apply_mode_combobox.SelectedIndex == 2)
            {
                if ((SelectedApps_lbm.GetAllItems().Count() == successfullyInstalledItems.Count) && successfullyInstalledItems.Count != 0)
                {
                    MessageBox.Show("Все выбранные приложения успешно установлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Некоторые приложения не были установлены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Операция выполнена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void OnWaiting()
        {
            Cursor = Cursors.Wait;

            about_btn.IsEnabled = false;
            all_soft_btn.IsEnabled = false;
            apply_btn.IsEnabled = false;
            settings_btn.IsEnabled = false;
            cached_soft_btn.IsEnabled = false;

            this.UpdateLayout();
            Thread.Sleep(100);
            this.UpdateLayout();
        }
        private void OffWaiting()
        {
            Cursor = Cursors.Arrow;

            about_btn.IsEnabled = true;
            all_soft_btn.IsEnabled = true;
            apply_btn.IsEnabled = true;
            settings_btn.IsEnabled = true;
            cached_soft_btn.IsEnabled = true;

            this.UpdateLayout();
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
                    // Устанавливаем таймаут для загрузки
                    client.Timeout = TimeSpan.FromSeconds(1200);

                    // Создаём запрос на загрузку
                    using (var response = client.GetAsync(item.DownloadPath, HttpCompletionOption.ResponseHeadersRead).Result)
                    {
                        response.EnsureSuccessStatusCode();

                        // Получаем размер файла
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        // Открываем поток для записи
                        using (var stream = response.Content.ReadAsStreamAsync().Result)
                        using (var fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;

                            // Читаем и записываем данные
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                // Обновляем статус загрузки в UI-потоке
                                if (canReportProgress)
                                {
                                    int progress = (int)((totalRead * 100L) / totalBytes);
                                    Dispatcher.Invoke(() =>
                                    {
                                        status_label.Content = $"Загрузка  {item.Name} : {progress}%";
                                    });
                                }
                                else
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        status_label.Content = $"Загрузка  {item.Name} : {totalRead / 1024} KB";
                                    });
                                }
                            }
                        }
                    }
                }

                // Проверка MD5, если хеш указан
                if (!string.IsNullOrWhiteSpace(item.MD5_hash))
                {
                    string calculatedMD5 = CalculateMD5(cachePath);
                    if (calculatedMD5 != item.MD5_hash)
                    {
                        // Если хеши не совпадают, удаляем файл и выбрасываем исключение
                        File.Delete(cachePath);
                        throw new Exception($"MD5 хеш файла {item.Name} не совпадает с ожидаемым значением.");
                    }
                }

                item.CachedPath = cachePath;

                // Обновляем путь в базе данных
                dbm.UpdateCachedPath(item.CodeName, cachePath);

                // Сообщение об успешной загрузке
                Dispatcher.Invoke(() =>
                {
                    status_label.Content = $"Загрузка {item.Name} завершена!";
                });
            }
            catch (TaskCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Content = $"Ошибка: загрузка {item.Name} превышает таймаут!";
                });
                MessageBox.Show($"Загрузка файла {item.Name} была прервана из-за таймаута.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Content = $"Ошибка: загрузка {item.Name} не удалась!";
                });
                MessageBox.Show($"Ошибка при загрузке файла {item.Name}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для вычисления MD5 хеша файла
        private string CalculateMD5(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }


        private bool InstallFile(FileSystemItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.CachedPath) && !File.Exists(item.CachedPath))
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
                        return true;
                    }
                    else
                    {
                        MessageBox.Show($"Приложение {item.Name} завершило установку с ошибкой (код {process.ExitCode}).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при установке приложения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        private void EnsureIconCached(FileSystemItem item)
        {
            try
            {
                string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconCache");
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory); // Создать папку, если её нет
                }

                string iconPath = Path.Combine(cacheDirectory, $"{item.CodeName}.png");

                // Проверяем, существует ли файл
                if (!File.Exists(iconPath) && !string.IsNullOrEmpty(item.IconUrl))
                {
                    // Загружаем иконку из IconUrl
                    using (HttpClient client = new HttpClient())
                    {
                        var iconBytes = client.GetByteArrayAsync(item.IconUrl).GetAwaiter().GetResult();
                        File.WriteAllBytes(iconPath, iconBytes);
                    }
                }

                item.IconPath = iconPath; // Устанавливаем путь к иконке
            }
            catch (Exception ex)
            {
                LogError($"Ошибка при работе с иконкой '{item.Name}': {ex.Message}");
            }
        }

        private void LogError(string message)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }


        private void DeleteCachedFile(FileSystemItem item)
        {
            if (File.Exists(item.CachedPath))
            {
                File.Delete(item.CachedPath);
            }
        }

        private void About_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Программист, тестировщик, разработчик: Гранько Максим\n\nРазработано специально для областной Выставки научно-технического творчества \"Техника молодежи\". \n2024","О разработчиках");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AllSoft_grid.Visibility = Visibility.Visible;
            Cachedsoft_grid.Visibility = Visibility.Hidden;
            Settings_grid.Visibility = Visibility.Hidden;
        }

        private void cached_soft_btn_Click(object sender, RoutedEventArgs e)
        {
            AllSoft_grid.Visibility = Visibility.Hidden;
            Cachedsoft_grid.Visibility = Visibility.Visible;
            Settings_grid.Visibility = Visibility.Hidden;
        }

        private void ReloadCachedAppsFromDB()
        {
            try
            {
                cached_AllFileSystemItems.Clear(); // Очищаем текущий список

                    // Получаем следующую пачку данных
                    List<FileSystemItem> items = dbm.GetAllCachedFileSystemItems();

                    if (items.Count > 0)
                    {
                        cached_AllFileSystemItems.AddRange(items); // Добавляем элементы в общий список
                    }
                    else
                    {
                    }

                foreach (var item in cached_AllFileSystemItems)
                {
                    EnsureIconCached(item); // Проверяем и загружаем иконки
                }

                // Обновляем интерфейсные списки
                cached_AllApps_lbm.Clear();
                cached_SelectedApps_lbm.Clear();
                cached_AllApps_lbm.AddItems(cached_AllFileSystemItems);

                // Обновляем категории в ComboBox
                PopulateCachedCategoriesComboBox();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных из базы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void PopulateCachedCategoriesComboBox()
        {
            try
            {
                // Получаем все уникальные категории
                var categories = cached_AllFileSystemItems
                    .SelectMany(item => item.Categories?.Split(';') ?? Array.Empty<string>())
                    .Distinct()
                    .OrderBy(category => category) // Сортируем категории
                    .ToList();

                categories.Insert(0, "Все категории"); // Добавляем опцию для отображения всех категорий

                cached_categories_cb.ItemsSource = categories;
                cached_categories_cb.SelectedIndex = 0; // Выбираем первую категорию по умолчанию
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при заполнении категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void settings_btn_Click(object sender, RoutedEventArgs e)
        {
            AllSoft_grid.Visibility = Visibility.Hidden;
            Cachedsoft_grid.Visibility = Visibility.Hidden;
            Settings_grid.Visibility = Visibility.Visible;
        }

        private void cached_ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if(cached_Selected_ExplorerListBox.Items.Count == 0) { MessageBox.Show("Не выбрано ни одного приложения для установки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            OnWaiting();

            // Получаем список выделенных приложений
            var selectedItems = cached_SelectedApps_lbm.GetAllItems();
            var successfullyInstalledItems = new List<FileSystemItem>();

            foreach (var item in selectedItems)
            {
                try
                {
                    
                    if (InstallFile(item) == true )successfullyInstalledItems.Add(item); // Добавляем в список успешно установленных
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при установке приложения '{item.Name}': {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            OffWaiting();

            // Отображаем сообщение об успешной установке
            if (selectedItems.Count() == successfullyInstalledItems.Count)
            {
                MessageBox.Show("Все выбранные приложения успешно установлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (successfullyInstalledItems.Count == 0) {
                MessageBox.Show("Ни одного приложения не было установлено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show("Некоторые приложения не были установлены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void cached_AllApps_ExplorerListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (cached_AllApps_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
                if (!cached_SelectedApps_lbm.GetAllItems().Any(item => item.CodeName == selectedItem.CodeName))
                {
                    cached_AllApps_lbm.RemoveSelectedItem();
                    cached_SelectedApps_lbm.AddItem(selectedItem);
                }
            }
        }

        private void all_search_textbox_MouseEnter(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) { return; }
            if (textBox.Text == "Поиск" && textBox.IsFocused == false) { textBox.Text = ""; }
        }

        private void all_search_textbox_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) { return; }
            if (textBox.Text == "" && textBox.IsFocused == false) { textBox.Text = "Поиск"; }
        }

        private void all_search_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            string search = textbox?.Text?.ToLowerInvariant() ?? "";

            if (string.IsNullOrWhiteSpace(search) || search == "поиск")
            {
                CategoriesComboBox_SelectionChanged(null, null);
                return;
            }

            var selectedItems = SelectedApps_lbm.GetAllItems().Select(item => item.CodeName).ToHashSet();

            var filteredItems = AllFileSystemItems
                .Where(item =>
                    (item.Name?.ToLowerInvariant().Contains(search) == true ||
                     item.Publisher?.ToLowerInvariant().Contains(search) == true) &&
                    !selectedItems.Contains(item.CodeName)) // Исключаем уже выбранные
                .ToList();

            AllApps_lbm.Clear();
            AllApps_lbm.AddItems(filteredItems);
        }


        private void cached_Selected_ExplorerListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (cached_Selected_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
                if (!cached_AllApps_lbm.GetAllItems().Any(item => item.CodeName == selectedItem.CodeName))
                {
                    cached_SelectedApps_lbm.RemoveSelectedItem();
                    cached_AllApps_lbm.AddItem(selectedItem);
                }
            }
        }

        private void refresh_icon_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Image image = sender as System.Windows.Controls.Image;
            if (image == null) return;
            image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/reload.png"));
        }

        private void refresh_btn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
