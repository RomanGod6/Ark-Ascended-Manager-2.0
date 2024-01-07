using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class RestorePage : Page
    {
        private ObservableCollection<BackupInfo> backupsList = new ObservableCollection<BackupInfo>();
        private ServerConfigs serverConfig; // ServerConfigs as a field within the class
        private readonly INavigationService _navigationService;
        public RestorePage(INavigationService navigationService)
        {
            InitializeComponent();
            LoadBackupsList();
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        private void LoadBackupsList()
        {
            // Define the path to your JSON configuration file
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appNameFolder, "RestoreBackUpDataStruc.json");

            // Check if the JSON configuration file exists
            if (!File.Exists(jsonFilePath))
            {
                MessageBox.Show("Configuration file not found.");
                return;
            }

            // Read the JSON content from the configuration file
            string serverConfigJson = File.ReadAllText(jsonFilePath);
            serverConfig = JsonConvert.DeserializeObject<ServerConfigs>(serverConfigJson); // Assigning to the field

            // Check if the deserialization was successful
            if (serverConfig == null)
            {
                MessageBox.Show("Failed to load server configuration.");
                return;
            }

            // Construct the path to the backup folder
            string backupFolderPath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Saved", "SavedArks", serverConfig.MapName);

            // Ensure the backup directory exists
            if (!Directory.Exists(backupFolderPath))
            {
                MessageBox.Show("Backup directory does not exist.");
                return;
            }

            // Get the directory info and list all .ark files except the current one
            DirectoryInfo di = new DirectoryInfo(backupFolderPath);
            FileInfo currentArkFile = di.GetFiles("TheIsland_WP.ark").FirstOrDefault();

            foreach (FileInfo file in di.GetFiles("*.ark"))
            {
                if (currentArkFile == null || !file.FullName.Equals(currentArkFile.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    backupsList.Add(new BackupInfo { FileName = file.Name, BackupDate = file.LastWriteTime });
                }
            }

            // Bind the backups list to the ComboBox's ItemsSource
            cbBackups.ItemsSource = backupsList;
        }

        private void RestoreSelectedBackup_Click(object sender, RoutedEventArgs e)
        {
            if (cbBackups.SelectedItem is BackupInfo selectedBackup && serverConfig != null) // Ensure serverConfig is not null
            {
                string backupFolderPath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Saved", "SavedArks", serverConfig.MapName);
                string currentArkPath = Path.Combine(backupFolderPath, "TheIsland_WP.ark");
                string selectedBackupPath = Path.Combine(backupFolderPath, selectedBackup.FileName);

                try
                {
                    if (File.Exists(currentArkPath))
                    {
                        string backupCurrentArkPath = $"{currentArkPath}_{DateTime.Now.ToString("ddMMyyyy_HHmmss")}.bak";
                        File.Move(currentArkPath, backupCurrentArkPath);
                    }

                    File.Copy(selectedBackupPath, currentArkPath, true);
                    MessageBox.Show($"Successfully restored backup: {selectedBackup.FileName}");
                    _navigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error restoring backup: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please select a backup file to restore.");
            }
        }

        // Classes moved outside the RestorePage class for better organization
    }

    // Define the BackupInfo class with properties for FileName and BackupDate
    public class BackupInfo
    {
        public string FileName { get; set; }
        public DateTime BackupDate { get; set; }
    }

    // Define the ServerConfigs class with properties that match your JSON structure
    public class ServerConfigs
    {
        public string ProfileName { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        // ... other properties ...
    }
}
