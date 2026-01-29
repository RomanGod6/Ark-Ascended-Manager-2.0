using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class RestorePage : Page
    {
        private ObservableCollection<BackupInfo> backupsList = new ObservableCollection<BackupInfo>();
        private ServerConfigs serverConfig;
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
            serverConfig = JsonConvert.DeserializeObject<ServerConfigs>(serverConfigJson);

            // Check if the deserialization was successful
            if (serverConfig == null)
            {
                MessageBox.Show("Failed to load server configuration.");
                return;
            }

            // Construct the path to the NEW backup folder structure
            string backupRootPath = Path.Combine(serverConfig.ServerPath, "Backups");

            // Ensure the backup directory exists
            if (!Directory.Exists(backupRootPath))
            {
                MessageBox.Show("No backups found. The Backups directory does not exist.");
                return;
            }

            // Get all timestamped backup directories
            DirectoryInfo backupRoot = new DirectoryInfo(backupRootPath);
            var backupDirectories = backupRoot.GetDirectories()
                .OrderByDescending(d => d.CreationTime)
                .ToList();

            if (!backupDirectories.Any())
            {
                MessageBox.Show("No backup directories found.");
                return;
            }

            foreach (var backupDir in backupDirectories)
            {
                // Each backup directory should contain SavedArks folder
                string savedArksPath = Path.Combine(backupDir.FullName, "SavedArks");
                if (Directory.Exists(savedArksPath))
                {
                    backupsList.Add(new BackupInfo
                    {
                        FileName = backupDir.Name,
                        BackupDate = backupDir.CreationTime,
                        BackupPath = backupDir.FullName
                    });
                }
            }

            if (!backupsList.Any())
            {
                MessageBox.Show("No valid backups found in the Backups directory.");
            }

            // Bind the backups list to the ComboBox's ItemsSource
            cbBackups.ItemsSource = backupsList;
        }

        private void RestoreSelectedBackup_Click(object sender, RoutedEventArgs e)
        {
            if (cbBackups.SelectedItem is BackupInfo selectedBackup && serverConfig != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to restore the backup from {selectedBackup.BackupDate}?\n\n" +
                    "WARNING: This will overwrite your current save data. Make sure the server is stopped before proceeding.",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    // Source: the SavedArks folder inside the backup directory
                    string sourceBackupPath = Path.Combine(selectedBackup.BackupPath, "SavedArks");

                    // Destination: the current SavedArks folder
                    string destinationPath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Saved", "SavedArks");

                    if (!Directory.Exists(sourceBackupPath))
                    {
                        MessageBox.Show($"Backup data not found at: {sourceBackupPath}");
                        return;
                    }

                    // Create a backup of the current state before restoring
                    string emergencyBackupPath = Path.Combine(serverConfig.ServerPath, "Backups", $"PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}");
                    Directory.CreateDirectory(emergencyBackupPath);
                    CopyDirectory(destinationPath, Path.Combine(emergencyBackupPath, "SavedArks"));

                    // Delete current SavedArks content
                    if (Directory.Exists(destinationPath))
                    {
                        Directory.Delete(destinationPath, true);
                    }

                    // Restore from backup
                    Directory.CreateDirectory(destinationPath);
                    CopyDirectory(sourceBackupPath, destinationPath);

                    MessageBox.Show(
                        $"Successfully restored backup from {selectedBackup.BackupDate}.\n\n" +
                        $"A pre-restore backup was saved to:\n{emergencyBackupPath}",
                        "Restore Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _navigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error restoring backup: {ex.Message}", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a backup to restore.");
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Copy all subdirectories
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }

    // Define the BackupInfo class with properties for FileName, BackupDate, and BackupPath
    public class BackupInfo
    {
        public string FileName { get; set; }
        public DateTime BackupDate { get; set; }
        public string BackupPath { get; set; }
    }

    // Define the ServerConfigs class with properties that match your JSON structure
    public class ServerConfigs
    {
        public string ProfileName { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
    }
}
