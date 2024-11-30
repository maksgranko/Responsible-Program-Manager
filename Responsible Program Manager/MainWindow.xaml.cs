using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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

        public CultureInfo currentCulture = null;
        public ResourceManager rm = null;

        bool initialize = true;

        public MainWindow()
        {
            initialize = true;
            if (Properties.Settings.Default.language != "en")
            {
                Translator.SetLanguage("ru");
            }
            else
            {
                Translator.SetLanguage("en");
            }
            UpdateDatabase();
            //AllFileSystemItems = dbm.GetAllFileSystemItems();
            InitializeComponent();
            AllApps_lbm = new ListBoxManager(AllApps_ExplorerListBox);
            SelectedApps_lbm = new ListBoxManager(Selected_ExplorerListBox);
            cached_AllApps_lbm = new ListBoxManager(cached_AllApps_ExplorerListBox);
            cached_SelectedApps_lbm = new ListBoxManager(cached_Selected_ExplorerListBox);
            ReloadUI();

            Dispatcher.Invoke(() =>
            {
                status_label.Text = $"{Translator.Translate("Status_LoadDatas")}...";
            });
            ReloadAppsFromDB();
            Dispatcher.Invoke(() =>
            {
                status_label.Text = $"{Translator.Translate("Status_LoadCachedDatas")}...";
            });
            ReloadCachedAppsFromDB();
            Dispatcher.Invoke(() =>
            {
                status_label.Text = "";
            });
            initialize = false;
        }

        private void ReloadUI()
        {
            foreach (ComboBoxItem i in LanguageComboBox.Items)
            {
                if ((string)i.Tag == Properties.Settings.Default.language)
                {
                    LanguageComboBox.SelectedValue = i;
                    break;
                }
            }

            apply_btn.Content = Translator.Translate("UI_Apply");
            cached_selected_apps_label.Content = selected_apps_label.Content = Translator.Translate("UI_SelectedPrograms") + ":";
            cached_all_apps_label.Content = all_apps_label.Content = Translator.Translate("Applications") + ":";
            cached_categories_label.Content = categories_label.Content = Translator.Translate("UI_Categories") + ":";
            all_soft_btn.Content = Translator.Translate("UI_GetPrograms");
            cached_soft_btn.Content = Translator.Translate("UI_CachedPrograms");
            settings_btn.Content = Translator.Translate("UI_Settings");
            about_btn.Content = Translator.Translate("UI_AboutUs");
            status_label.Text = Translator.Translate("UI_AppIsReady");
            cached_apply_btn.Content = Translator.Translate("UI_Apply");
            open_cache_folder_cb.Content = Translator.Translate("UI_OpenCacheFolderAfterLoading");
            save_settings_btn.Content = Translator.Translate("UI_SaveSettings");
            clear_cache_btn.Content = Translator.Translate("UI_ClearDownloadCache");
            open_cache_path_btn.Content = Translator.Translate("UI_OpenCacheFolder");
            md5_verify_cb.Content = Translator.Translate("UI_DisableMD5Validating");
            DownloadAndInstall_label.Content = Translator.Translate("UI_DownloadAndInstall");
            OnlyInstall_label.Content = Translator.Translate("UI_OnlyInstall");
            OnlyDownload_label.Content = Translator.Translate("UI_OnlyDownload");
            all_search_textbox.Text = Translator.Translate("UI_Search");
            clear_selected_list_btn.Content = Translator.Translate("UI_ClearSelectedList");

        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialize) return;
            if (MessageBox.Show($"{Translator.Translate("UI_ChangeLanguageQuestion")}", $"{Translator.Translate("UI_ChangeLanguage")}", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string selectedLanguage = ((ComboBoxItem)e.AddedItems[0]).Tag.ToString();
                Translator.SetLanguage(selectedLanguage);
                Properties.Settings.Default.language = selectedLanguage;
                Properties.Settings.Default.Save();
                Application.Current.MainWindow = new MainWindow();
                Close();
                new MainWindow().ShowDialog();
                Environment.Exit(0);
            }

            initialize = true;
            foreach (ComboBoxItem i in LanguageComboBox.Items)
            {
                if ((string)i.Tag == Properties.Settings.Default.language)
                {
                    LanguageComboBox.SelectedValue = i;
                    break;
                }
            }
            initialize = false;
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
                MessageBox.Show($"{Translator.Translate("Error_WhenUpdateDB")}: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                    EnsureIconCached(item);
                }

                AllApps_lbm.Clear();
                SelectedApps_lbm.Clear();
                AllApps_lbm.AddItems(AllFileSystemItems);

                PopulateCategoriesComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Translator.Translate("Error_WhenLoadDataFromDB")}: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateCategoriesComboBox()
        {
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

            categories_cb.Items.Clear();
            foreach (var category in uniqueCategories)
            {
                categories_cb.Items.Add(category);
            }

            categories_cb.Items.Insert(0, Translator.Translate("UI_AllCategories"));
            categories_cb.SelectedIndex = 0;
        }
        private void AllApps_ExplorerListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AllApps_ExplorerListBox.SelectedItem is FileSystemItem selectedItem)
            {
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
                MessageBox.Show($"{Translator.Translate("UI_DownloadSource")}: {selectedItem.DownloadPath}", Translator.Translate("UI_AllCategories"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string ProcessInstallArguments(string installArguments, string cacheDirectory)
        {
            if (string.IsNullOrWhiteSpace(installArguments))
                throw new ArgumentException($"InstallArguments {Translator.Translate("dbg_err_WontBeEmpty")}");

            var parts = installArguments
                .Split(';')
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            if (parts.Length == 0)
                throw new ArgumentException($"InstallArguments {Translator.Translate("dbg_err_DontContainsValidDatas")}");

            string filePattern = parts[0];
            string installFilePath = Directory
                .GetFiles(cacheDirectory, filePattern)
                .FirstOrDefault();

            if (installFilePath == null)
                throw new FileNotFoundException($"{Translator.Translate("dbg_err_FilepatternDontContainsInFolder_part1")} '{filePattern}', {Translator.Translate("dbg_err_FilepatternDontContainsInFolder_part2")} {cacheDirectory}");

            string arguments = parts.Length > 1
                ? string.Join(" ", parts.Skip(1))
                : string.Empty;

            return $"\"{installFilePath}\" {arguments}";
        }

        private void CategoriesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categories_cb.SelectedItem is string selectedCategory)
            {
                AllApps_lbm.Clear();

                var selectedItems = SelectedApps_lbm.GetAllItems().Select(item => item.CodeName).ToHashSet();

                if (selectedCategory == Translator.Translate("UI_AllCategories"))
                {
                    var filteredItems = AllFileSystemItems
                        .Where(item => !selectedItems.Contains(item.CodeName))
                        .ToList();

                    AllApps_lbm.AddItems(filteredItems);
                }
                else
                {
                    var filteredItems = AllFileSystemItems
                        .Where(item => item.Categories?.Split(';').Contains(selectedCategory) ?? false)
                        .Where(item => !selectedItems.Contains(item.CodeName))
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
            bool shouldOpenCache = false;
            var successfullyInstalledItems = new List<FileSystemItem>();

            OnWaiting();

            Dispatcher.Invoke(() =>
            {
                status_label.Text = $"{Translator.Translate("UI_LoadingApps")}...";
            });
            foreach (var item in SelectedApps_lbm.GetAllItems())
            {
                try
                {

                    Dispatcher.Invoke(() =>
                    {
                        status_label.Text = $"{Translator.Translate("UI_Downloading")} {item.Name}";
                    });
                    if (string.IsNullOrWhiteSpace(item.CachedPath) || !File.Exists(item.CachedPath))
                    {
                        await Task.Run(() => CacheFile(item));
                    }

                    Dispatcher.Invoke(() =>
                    {
                        status_label.Text = $"{Translator.Translate("UI_Downloading")} {item.Name} {Translator.Translate("UI_Complete_lower")}";
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Translator.Translate("Error_WhenLoadingApp")} '{item.Name}': {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            Dispatcher.Invoke(() =>
            {
                status_label.Text = $"{Translator.Translate("UI_InstallingApps")}...";
            });
            foreach (var item in SelectedApps_lbm.GetAllItems())
            {
                switch (apply_mode_combobox.SelectedIndex)
                {
                    case 1:
                    case 2:
                        if (await Task.Run(() => InstallFile(item)))
                        {
                            successfullyInstalledItems.Add(item);
                        }

                        if (apply_mode_combobox.SelectedIndex == 1 && !string.IsNullOrWhiteSpace(item.CachedPath) && !File.Exists(item.CachedPath))
                        {
                            await Task.Run(() => DeleteCachedFile(item));
                        }
                        if (apply_mode_combobox.SelectedIndex == 2)
                        {
                            shouldOpenCache = true;
                        }
                        break;

                    case 0:
                        shouldOpenCache = true;
                        break;
                }
            }

            OffWaiting();

            if (shouldOpenCache && Properties.Settings.Default.openCacheEveryDownload)
            {
                OpenCacheFolder(cacheDirectory);
            }

            if (Selected_ExplorerListBox.Items.Count == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Text = Translator.Translate("Error_NoSelectedApps");
                });
                MessageBox.Show(Translator.Translate("Error_NoSelectedApps"), Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (apply_mode_combobox.SelectedIndex == 1 || apply_mode_combobox.SelectedIndex == 2)
            {

                if (successfullyInstalledItems.Count == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        status_label.Text = Translator.Translate("Error_AnyAppsWontInstalled");
                    });
                    MessageBox.Show(Translator.Translate("Error_AnyAppsWontInstalled"), Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                else if ((SelectedApps_lbm.GetAllItems().Count() == successfullyInstalledItems.Count) && successfullyInstalledItems.Count != 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        status_label.Text = $"{Translator.Translate("UI_AllSelectedAppsInstalledSuccessfully")}.";
                    });
                    MessageBox.Show($"{Translator.Translate("UI_AllSelectedAppsInstalledSuccessfully")}!", Translator.Translate("UI_Done"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        status_label.Text = $"{Translator.Translate("UI_OneOfSelectedAppsWontBeInstalled")}.";
                    });
                    MessageBox.Show($"{Translator.Translate("UI_OneOfSelectedAppsWontBeInstalled")}.", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Text = $"{Translator.Translate("UI_OperationCompleted")}";
                });
                MessageBox.Show($"{Translator.Translate("UI_OperationCompleted")}!", Translator.Translate("UI_Done"), MessageBoxButton.OK, MessageBoxImage.Information);
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
        
        private void OpenCacheFolder(string cacheDirectory)
        {
            try
            {
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                System.Diagnostics.Process.Start("explorer.exe", cacheDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Translator.Translate("Error_FolderWontBeOpened")} Cache: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CacheFile(FileSystemItem item)
        {
            try
            {
                string cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                string cachePath;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3600);

                    using (var response = client.GetAsync(item.DownloadPath, HttpCompletionOption.ResponseHeadersRead).Result)
                    {
                        response.EnsureSuccessStatusCode();

                        var contentDisposition = response.Content.Headers.ContentDisposition;
                        if (contentDisposition != null && !string.IsNullOrWhiteSpace(contentDisposition.FileName))
                        {
                            cachePath = Path.Combine(cacheDirectory, contentDisposition.FileNameStar.Trim('"'));
                        }
                        else
                        {
                            string fileName = Path.GetFileName(new Uri(item.DownloadPath).AbsolutePath);
                            if (string.IsNullOrWhiteSpace(fileName))
                            {
                                fileName = $"{item.CodeName}.exe";
                            }

                            cachePath = Path.Combine(cacheDirectory, fileName);
                        }

                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var stream = response.Content.ReadAsStreamAsync().Result)
                        using (var fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;

                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (canReportProgress)
                                {
                                    int progress = (int)((totalRead * 100L) / totalBytes);
                                    Dispatcher.Invoke(() =>
                                    {
                                        status_label.Text = $"{Translator.Translate("UI_Downloading")} {item.Name} : {progress}%";
                                    });
                                }
                                else
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        status_label.Text = $"{Translator.Translate("UI_Downloading")} {item.Name} : {totalRead / 1024} {Translator.Translate("UI_KB")}";
                                    });
                                }
                            }
                        }
                    }
                }

                if (!Properties.Settings.Default.md5Verify)
                {
                    if (!string.IsNullOrWhiteSpace(item.MD5_hash))
                    {
                        string calculatedMD5 = CalculateMD5(cachePath);
                        if (calculatedMD5 != item.MD5_hash)
                        {
                            File.Delete(cachePath);
                            throw new Exception($"{Translator.Translate("dbg_err_BadMD5Hash_part1")} {item.Name} {Translator.Translate("dbg_err_BadMD5Hash")}.");
                        }
                    }
                }

                item.CachedPath = cachePath;

                dbm.UpdateCachedPath(item.CodeName, cachePath);

                Dispatcher.Invoke(() =>
                {
                    status_label.Text = $"{Translator.Translate("UI_Downloading")} {item.Name} {Translator.Translate("UI_Complete_lower")}!";
                });
            }
            catch (TaskCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Text = $"{Translator.Translate("UI_Downloading")} {item.Name} {Translator.Translate("UI_TimeoutLimitReached_Lower")}!";
                });
                MessageBox.Show($"{Translator.Translate("dbg_err_FileDownloadingStoppedByTimeout_part1")} {item.Name} {Translator.Translate("dbg_err_FileDownloadingStoppedByTimeout_part2")}.", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Text = $"{Translator.Translate("UI_Downloading")}  {item.Name} {Translator.Translate("UI_NotCompleted_lower")}!";
                });
                MessageBox.Show($"{Translator.Translate("Error_WhenDownloadingFile")} {item.Name}: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

            Dispatcher.Invoke(() =>
            {
                status_label.Text = $"{Translator.Translate("UI_InstallingApps_1")}  {item.Name} ...";
            });
            try
            {
                if (string.IsNullOrWhiteSpace(item.CachedPath) && !File.Exists(item.CachedPath))
                {
                    Dispatcher.Invoke(() =>
                    {
                        status_label.Text = Translator.Translate("Error_CachedFileNotFound");
                    });
                    throw new FileNotFoundException(Translator.Translate("Error_CachedFileNotFound"), item.CachedPath);
                }

                if (Path.GetExtension(item.CachedPath).Contains("exe"))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = item.CachedPath,
                        Arguments = item.InstallArguments,
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                status_label.Text = $"{Translator.Translate("Application")} {item.Name} {Translator.Translate("UI_AppInstalledSuccessfully")}.";
                            });
                            MessageBox.Show($"{Translator.Translate("Application")} {item.Name} {Translator.Translate("UI_AppInstalledSuccessfully")}.", Translator.Translate("UI_Done"), MessageBoxButton.OK, MessageBoxImage.Information);
                            return true;
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                status_label.Text = $"{Translator.Translate("Application")} {item.Name} {Translator.Translate("Error_AppEndSetupByError")} {process.ExitCode}).";
                            });
                            MessageBox.Show($"{Translator.Translate("Application")} {item.Name} {Translator.Translate("Error_AppEndSetupByError")} {process.ExitCode}).", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"{Translator.Translate("Application")} {item.Name} {Translator.Translate("UI_AppWannaManualSetup")}.", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    status_label.Text = $"{Translator.Translate("Error_WhenSetupApp")}: {ex.Message}";
                });
                MessageBox.Show($"{Translator.Translate("Error_WhenSetupApp")}: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Directory.CreateDirectory(cacheDirectory);
                }

                string iconPath = Path.Combine(cacheDirectory, $"{item.CodeName}.png");

                if (!File.Exists(iconPath) && !string.IsNullOrEmpty(item.IconUrl))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var iconBytes = client.GetByteArrayAsync(item.IconUrl).GetAwaiter().GetResult();
                        File.WriteAllBytes(iconPath, iconBytes);
                    }
                }

                item.IconPath = iconPath;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Ошибка при работе с иконкой '{item.Name}': {ex.Message}");
            }
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
            MessageBox.Show(Translator.Translate("UI_AboutUsLong"), Translator.Translate("UI_About"));
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
                cached_AllFileSystemItems.Clear();
                List<FileSystemItem> items = dbm.GetAllCachedFileSystemItems();

                if (items.Count > 0)
                {
                    cached_AllFileSystemItems.AddRange(items);
                }
                else
                {
                }

                foreach (var item in cached_AllFileSystemItems)
                {
                    EnsureIconCached(item);
                }

                cached_AllApps_lbm.Clear();
                cached_SelectedApps_lbm.Clear();
                cached_AllApps_lbm.AddItems(cached_AllFileSystemItems);

                PopulateCachedCategoriesComboBox();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"{Translator.Translate("Error_WhenLoadDataFromDB")}: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void PopulateCachedCategoriesComboBox()
        {
            try
            {
                var categories = cached_AllFileSystemItems
                    .SelectMany(item => item.Categories?.Split(';') ?? Array.Empty<string>())
                    .Distinct()
                    .OrderBy(category => category)
                    .ToList();

                categories.Insert(0, Translator.Translate("UI_AllCategories"));

                cached_categories_cb.ItemsSource = categories;
                cached_categories_cb.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Translator.Translate("Error_WhenParseCategories")}: {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void settings_btn_Click(object sender, RoutedEventArgs e)
        {
            AllSoft_grid.Visibility = Visibility.Hidden;
            Cachedsoft_grid.Visibility = Visibility.Hidden;
            Settings_grid.Visibility = Visibility.Visible;

            open_cache_folder_cb.IsChecked = Properties.Settings.Default.openCacheEveryDownload;
            md5_verify_cb.IsChecked = Properties.Settings.Default.md5Verify;
        }

        private void cached_ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (cached_Selected_ExplorerListBox.Items.Count == 0) { MessageBox.Show($"{Translator.Translate("Error_NoSelectedApps")}!", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error); return; }
            OnWaiting();

            var selectedItems = cached_SelectedApps_lbm.GetAllItems();
            var successfullyInstalledItems = new List<FileSystemItem>();

            foreach (var item in selectedItems)
            {
                try
                {

                    if (InstallFile(item) == true) successfullyInstalledItems.Add(item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Translator.Translate("Error_WhenSetupApp")} '{item.Name}': {ex.Message}", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            OffWaiting();

            if (selectedItems.Count() == successfullyInstalledItems.Count)
            {
                Dispatcher.Invoke(() => { status_label.Text = $"{Translator.Translate("UI_AllSelectedAppsInstalledSuccessfully")}!"; });
                MessageBox.Show($"{Translator.Translate("UI_AllSelectedAppsInstalledSuccessfully")}!", Translator.Translate("UI_Done"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (successfullyInstalledItems.Count == 0)
            {

                Dispatcher.Invoke(() =>
                {
                    status_label.Text = $"{Translator.Translate("Error_AnyAppsWontInstalled")}.";
                });
                MessageBox.Show($"{Translator.Translate("Error_AnyAppsWontInstalled")}.", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Dispatcher.Invoke(() => { status_label.Text = $"{Translator.Translate("UI_OneOfSelectedAppsWontBeInstalled")}."; });
                MessageBox.Show($"{Translator.Translate("UI_OneOfSelectedAppsWontBeInstalled")}.", Translator.Translate("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (textBox.Text == Translator.Translate("UI_Search") && textBox.IsFocused == false) { textBox.Text = ""; }

            Dispatcher.Invoke(() =>
            {
                status_label.Text = Translator.Translate("UI_Search_Long") + ".";
            });
        }

        private void all_search_textbox_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) { return; }
            if (textBox.Text == "" && textBox.IsFocused == false) { textBox.Text = Translator.Translate("UI_Search"); }
            Dispatcher.Invoke(() =>
            {
                status_label.Text = "";
            });
        }

        private void all_search_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AllApps_lbm == null || cached_AllApps_lbm == null || cached_SelectedApps_lbm == null) return;
            TextBox textbox = sender as TextBox;
            string search = textbox?.Text?.ToLowerInvariant() ?? "";
            HashSet<string> selectedItems = null;
            if (string.IsNullOrWhiteSpace(search) || search == Translator.Translate("UI_Search_lower"))
            {
                CategoriesComboBox_SelectionChanged(null, null);
                return;
            }
            if (Selected_ExplorerListBox.Items.Count != 0)
            {
                selectedItems = SelectedApps_lbm.GetAllItems().Select(item => item.CodeName).ToHashSet();
            }
            var filteredItems = AllFileSystemItems
                .Where(item =>
                    (item.Name?.ToLowerInvariant().Contains(search) == true ||
                     item.Publisher?.ToLowerInvariant().Contains(search) == true) &&
                    !selectedItems.Contains(item.CodeName))
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
            Image image = sender as Image;
            if (image == null) return;
            image.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/reload.png"));
        }

        private void refresh_btn_Click(object sender, RoutedEventArgs e)
        {
            ReloadCachedAppsFromDB();
        }

        private void clear_cache_btn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translator.Translate("UI_ClearCacheWarning"), Translator.Translate("Warning") + "!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"), true);

                try
                {
                    string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
                    if (Directory.Exists(cachePath))
                        Directory.Delete(cachePath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Translator.Translate("Error_CacheClearFailed")}: {ex.Message}");
                }
                try
                {
                    string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconCache");
                    if (Directory.Exists(cachePath))
                        Directory.Delete(cachePath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Translator.Translate("Error_IconCacheClearFailed")}: {ex.Message}");
                }
            }
        }

        private void open_cache_path_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"))) Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"));
            OpenCacheFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache"));
        }

        private void save_settings_btn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.openCacheEveryDownload = (bool)open_cache_folder_cb.IsChecked;
            Properties.Settings.Default.md5Verify = (bool)md5_verify_cb.IsChecked;
            Properties.Settings.Default.Save();
            MessageBox.Show($"{Translator.Translate("UI_SettingsSaved")}!", $"{Translator.Translate("Saved")}!");
        }

        private void clear_selected_list_btn_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show(Translator.Translate("UI_YouSureClearSelected")+"?",Translator.Translate("Warning"),MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SelectedApps_lbm.Clear();
                Selected_ExplorerListBox.Items.Clear();
                ReloadAppsFromDB();
            }
        }
    }
}
