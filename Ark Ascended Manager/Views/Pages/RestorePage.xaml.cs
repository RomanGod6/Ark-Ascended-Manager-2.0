using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class RestorePage : Page
    {
        private ObservableCollection<BackupInfo> backupsList = new ObservableCollection<BackupInfo>();

        public RestorePage()
        {
            InitializeComponent();
            LoadBackupsList();
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
            var serverConfig = JsonConvert.DeserializeObject<ServerConfigs>(serverConfigJson);

            // Check if the deserialization was successful
            if (serverConfig == null)
            {
                MessageBox.Show("Failed to load server configuration.");
                return;
            }

            // Construct the path to the backup folder
            string backupFolderPath = Path.Combine(serverConfig.Path, "ShooterGame", "Saved", "SavedArks", serverConfig.MapName);

            // Ensure the backup directory exists
            if (!Directory.Exists(backupFolderPath))
            {
                MessageBox.Show("Backup directory does not exist.");
                return;
            }

            // Get the directory info and list the .ark files
            DirectoryInfo di = new DirectoryInfo(backupFolderPath);
            foreach (FileInfo file in di.GetFiles("*.ark"))
            {
                // Parse the backup date from the file name
                string dateFormat = "dd.MM.yyyy_HH.mm.ss";
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                string[] splitName = fileNameWithoutExtension.Split(new[] { '_' }, 4); // Split by underscores to isolate the date part

                if (splitName.Length < 4)
                {
                    MessageBox.Show($"Invalid file name format: {file.Name}");
                    continue;
                }

                // Reconstruct the date string from the split parts and parse it
                string dateString = string.Join("_", splitName[1], splitName[2], splitName[3]);
                if (DateTime.TryParseExact(dateString, dateFormat, null, System.Globalization.DateTimeStyles.None, out DateTime backupDate))
                {
                    backupsList.Add(new BackupInfo { FileName = file.Name, BackupDate = backupDate });
                }
                else
                {
                    MessageBox.Show($"Invalid date format in file name: {file.Name}");
                }
            }

            // Bind the backups list to the ListView's ItemsSource
            lvBackups.ItemsSource = backupsList;
        }

        // ServerConfig and BackupInfo classes should be defined as shown earlier




        private void RestoreSelectedBackup_Click(object sender, RoutedEventArgs e)
        {
            if (lvBackups.SelectedItem is BackupInfo selectedBackup)
            {
                // Your restore logic here
                MessageBox.Show($"Restoring: {selectedBackup.FileName}");
            }
        }

    }

    public class BackupInfo
    {
        public string FileName { get; set; }
        public DateTime BackupDate { get; set; }
    }

    public class ServerConfigs
    {
        public string ProfileName { get; set; }
        public string Path { get; set; }
        public string MapName { get; set; }
    }
}
