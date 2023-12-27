using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using Ookii.Dialogs.Wpf;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using WpfMessageBox = Wpf.Ui.Controls.MessageBox;
using SystemMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = Wpf.Ui.Controls.MessageBoxButton;
using SystemMessageBoxButton = System.Windows.MessageBoxButton;
using System.Diagnostics;
using System.Text.Json;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ImportServersPage : INavigableView<ImportServersPageViewModel> // Assuming DataViewModel is still appropriate
    {
        private readonly INavigationService _navigationService;
        public ImportServersPageViewModel ViewModel { get; }

        public ImportServersPage(ImportServersPageViewModel viewModel)
        {
            InitializeComponent();
            
            ViewModel = viewModel;
            DataContext = ViewModel;
        }
        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select the folder for the server path";

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.SelectedPath;

                // Check if a ProfileName is set in the ViewModel
                if (string.IsNullOrEmpty(ViewModel.ProfileName))
                {
                    MessageBox.Show("Please enter a Profile Name before selecting a folder. Please note the Profile name MUST match the folder name for the import to be successful.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Exit the method if ProfileName is not set
                }

                // Check if the selected folder's name matches the ProfileName
                string selectedFolderName = new DirectoryInfo(folderPath).Name;
                if (selectedFolderName != ViewModel.ProfileName)
                {
                    MessageBox.Show($"The selected folder name must match the Profile Name '{ViewModel.ProfileName}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Exit the method if folder name doesn't match
                }

                // Use the folder path as is
                ViewModel.ServerPath = folderPath;
            }
        }






        private void ResetViewModel()
        {
            // Reset all properties in ViewModel to their default values
            ViewModel.ProfileName = string.Empty;
            ViewModel.ServerPath = string.Empty;
            ViewModel.SelectedOption = null; // Or your default selection if applicable
                                             // For AppId, you may not need to reset it as it is a lookup based on the selected map.
            ViewModel.ServerName = string.Empty;
            ViewModel.ListenPort = 27015; // Default port, or empty string if you want it to be filled each time.
            ViewModel.RCONPort = 27020; // Default RCON port, or empty string if you want it to be filled each time.
            ViewModel.Mods = string.Empty; // Assuming this is a comma-separated string in your ViewModel.
            ViewModel.AdminPassword = string.Empty;
            ViewModel.ServerPassword = string.Empty;
            ViewModel.UseBattlEye = false;
            ViewModel.ForceRespawnDinos = false;
            ViewModel.PreventSpawnAnimation = false;
            //... reset other properties as needed
        }
        private void SaveServerConfig(ServerConfig config)
        {
            try
            {
                // Get the AppData path for the current user
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string applicationFolderPath = Path.Combine(appDataPath, "Ark Ascended Manager");

                // Ensure the directory exists
                Directory.CreateDirectory(applicationFolderPath);

                // Define the servers.json file path
                string serversFilePath = Path.Combine(applicationFolderPath, "servers.json");

                // Initialize the servers list
                List<ServerConfig> servers = new List<ServerConfig>();

                // Read existing servers if the file exists
                if (File.Exists(serversFilePath))
                {
                    string existingJson = File.ReadAllText(serversFilePath);
                    servers = JsonSerializer.Deserialize<List<ServerConfig>>(existingJson) ?? new List<ServerConfig>();
                }

                // Add the new server configuration
                servers.Add(config);

                // Serialize the updated list of servers to JSON
                string updatedJson = JsonSerializer.Serialize(servers, new JsonSerializerOptions { WriteIndented = true });

                // Write the JSON to the servers.json file
                File.WriteAllText(serversFilePath, updatedJson);

                // Inform the user of success
                MessageBox.Show("Server configuration saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Inform the user of any errors
                MessageBox.Show($"Failed to save server configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ... [Other code]

        // This method creates a new ServerConfig object from the ViewModel and calls the save method.
        private void SaveImportedServer()
        {
            try
            {
                // Validation to ensure the ServerPath ends with the ProfileName
                if (!ViewModel.ServerPath.EndsWith(ViewModel.ProfileName))
                {
                    MessageBox.Show($"The Server Path must end with the Profile Name '{ViewModel.ProfileName}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Exit the method if validation fails
                }

                // Create a new ServerConfig object from the ViewModel properties
                ServerConfig newServerConfig = new ServerConfig
                {
                    ProfileName = ViewModel.ProfileName,
                    ServerPath = ViewModel.ServerPath,
                    MapName = ViewModel.SelectedOption,
                    AppId = ViewModel.MapToAppId[ViewModel.SelectedOption],
                    ServerName = ViewModel.ServerName,
                    ListenPort = Convert.ToInt32(ViewModel.ListenPort),
                    RCONPort = Convert.ToInt32(ViewModel.RCONPort),
                    Mods = ViewModel.Mods?.Split(',').ToList(),
                    AdminPassword = ViewModel.AdminPassword,
                    ServerPassword = ViewModel.ServerPassword,
                    UseBattlEye = ViewModel.UseBattlEye,
                    MaxPlayerCount = Convert.ToInt32(ViewModel.MaxPlayerCount),
                    ForceRespawnDinos = ViewModel.ForceRespawnDinos,
                    PreventSpawnAnimation = ViewModel.PreventSpawnAnimation
                };

                // Call the SaveServerConfig method to save the new server
                SaveServerConfig(newServerConfig);

                // Reset the ViewModel properties to clear the form
                ResetViewModel();
            }
            catch (Exception ex)
            {
                // Handle exceptions here, such as showing an error message
                MessageBox.Show($"An error occurred while saving the server configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportServer_Click(object sender, RoutedEventArgs e)
        {
            SaveImportedServer();
        }




        public class ServerConfig
        {
            public string ProfileName { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }

            public string ServerName { get; set; }
            public int ListenPort { get; set; } // Ports are typically integers
            public int RCONPort { get; set; }   // Ports are typically integers
            public List<string> Mods { get; set; } // Assuming Mods can be a list
            public int MaxPlayerCount { get; set; }
            public string AdminPassword { get; set; }
            public string ServerPassword { get; set; }
            public bool UseBattlEye { get; set; } // Use bool for checkboxes
            public bool ForceRespawnDinos { get; set; } // Use bool for checkboxes
            public bool PreventSpawnAnimation { get; set; } // Use bool for checkboxes

            // ... other relevant details
        }

    }





}