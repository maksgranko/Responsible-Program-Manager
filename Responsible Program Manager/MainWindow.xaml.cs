using System.Collections.Generic;
using System.Windows;

namespace Responsible_Program_Manager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
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

        private void UpdateDatabase()
        {

        }
        private void ReloadAppsFromDB()
        {

        }
    }

}
