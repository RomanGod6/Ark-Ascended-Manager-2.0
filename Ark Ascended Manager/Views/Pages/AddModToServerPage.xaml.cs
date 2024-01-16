using Ark_Ascended_Manager.ViewModels.Pages;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static Ark_Ascended_Manager.Views.Pages.CurseForgeModPage;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class AddModToServerPage : Page
    {
        public ObservableCollection<ServerViewModel> Servers { get; set; }

        public AddModToServerPage()
        {
            InitializeComponent();
            LoadServers();
        }

        private void LoadServers()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var servers = JsonConvert.DeserializeObject<List<Server>>(json);
                Servers = new ObservableCollection<ServerViewModel>(
                    servers.Select(s => new ServerViewModel { Server = s, IsSelected = false })
                );
                ServersListView.ItemsSource = Servers;
            }
            else
            {
                MessageBox.Show("servers.json file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // The mod ID to add, replace with the actual ID
            int modIdToAdd = ModSelectionService.CurrentModId;
            Debug.WriteLine(modIdToAdd);

            foreach (var serverViewModel in Servers)
            {
                if (serverViewModel.IsSelected)
                {
                    if (serverViewModel.Server.Mods == null)
                    {
                        serverViewModel.Server.Mods = new List<string>(); // Initialize the list if it's null
                    }

                    if (!serverViewModel.Server.Mods.Contains(modIdToAdd.ToString()))
                    {
                        serverViewModel.Server.Mods.Add(modIdToAdd.ToString());
                    }
                    else
                    {
                        // Notify user that the mod already exists
                        MessageBox.Show($"Mod already exists on server {serverViewModel.Server.ProfileName}.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            // Save the updated list back to the JSON file
            SaveServers(Servers.Select(vm => vm.Server).ToList(), modIdToAdd.ToString());
        }


        private void SaveServers(List<Server> servers, string newModId)
        {
            foreach (var server in servers)
            {
                // If Mods is null, initialize it with an empty string
                if (server.Mods == null)
                {
                    server.Mods = new List<string> { "" };
                }
                else
                {
                    // If Mods contains any actual mod IDs, remove all empty strings
                    if (server.Mods.Any(mod => !string.IsNullOrEmpty(mod)))
                    {
                        server.Mods.RemoveAll(string.IsNullOrEmpty);
                    }
                    // If there are no mod IDs, ensure there is a single empty string
                    else if (!server.Mods.Any())
                    {
                        server.Mods.Add("");
                    }
                }

                // Assuming the Server class has a property called BatFilePath or similar
                string serverPath = server.ServerPath;
                string batFilePath = Path.Combine(serverPath, "LaunchServer.bat");

                if (File.Exists(batFilePath))
                {
                    // Read the current launch parameters from the .bat file
                    string batContents = File.ReadAllText(batFilePath);

                    // Extract the current mods line
                    string modsLine = batContents.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                                                 .FirstOrDefault(line => line.StartsWith("set mods="));

                    // If there's a mod line and we have mods to add
                    if (modsLine != null && !string.IsNullOrWhiteSpace(newModId))
                    {
                        // Get the current list of mods from the line
                        string currentMods = modsLine.Replace("set mods=", "").Trim();
                        List<string> modList = currentMods.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(m => m.Trim())
                                                          .ToList();

                        // Add the new mod if it's not already present
                        if (!modList.Contains(newModId))
                        {
                            modList.Add(newModId);
                        }

                        // Reconstruct the mods line and update the .bat contents
                        string newModsLine = "set mods=" + string.Join(",", modList);
                        batContents = batContents.Replace(modsLine, newModsLine);

                        // Write the updated launch parameters to the .bat file
                        File.WriteAllText(batFilePath, batContents);
                    }
                }
                else
                {
                    // Handle the error scenario where the bat file doesn't exist
                    MessageBox.Show($"The .bat file for server {server.ServerName} does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Convert the list of servers to JSON
            string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
            // Define the path where the JSON file will be saved
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");
            // Write the JSON string to the file
            File.WriteAllText(filePath, json);

            // Inform the user that both the servers and launch parameters have been updated successfully
            MessageBox.Show("Servers and launch parameters updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }




    }

    // ViewModel for each server in the ListView
    public class ServerViewModel
    {
        public Server Server { get; set; }
        public bool IsSelected { get; set; }
    }

    // Model for server data
    public class Server
    {
        public string ProfileName { get; set; }
        public string ServerStatus { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        public string AppId { get; set; }
        public bool IsRunning { get; set; }
        public string ServerName { get; set; }
        public int ListenPort { get; set; }
        public int RCONPort { get; set; }
        public List<string> Mods { get; set; }
        public int MaxPlayerCount { get; set; }
        public string AdminPassword { get; set; }
        public string ServerPassword { get; set; }
        public bool UseBattlEye { get; set; }
        public bool ForceRespawnDinos { get; set; }
        public bool PreventSpawnAnimation { get; set; }
        public int ChangeNumber { get; set; }
        public string ChangeNumberStatus { get; set; }
        // Add any other properties that are in your JSON
    }




}


