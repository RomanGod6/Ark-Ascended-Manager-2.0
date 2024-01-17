using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class PluginManagementAutoInstallPage : Page
    {
        private List<ServerConfig> servers; // Define a class ServerConfig to represent server data
        private List<string> plugins; // List of plugin file names

        public PluginManagementAutoInstallPage()
        {
            InitializeComponent();

            // Load servers
            LoadServers();

            // Load plugin file names from the plugins folder
            LoadPlugins();

            // Create checkboxes dynamically for servers
            CreateServerCheckboxes();

            // Create checkboxes dynamically for plugins
            CreatePluginCheckboxes();
        }

        private void LoadServers()
        {
            servers = ReadAllServerConfigs();
        }

        private void LoadPlugins()
        {
            string pluginsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "plugins");
            if (Directory.Exists(pluginsFolderPath))
            {
                plugins = new List<string>(Directory.GetFiles(pluginsFolderPath, "*.zip").Select(Path.GetFileName));
            }
            else
            {
                plugins = new List<string>();
            }
        }

        private void CreateServerCheckboxes()
        {
            foreach (var server in servers)
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = server.ProfileName,
                    Tag = server // Store the server info in the Tag property
                };

                ServersStackPanel.Children.Add(checkBox);
            }
        }

        private void CreatePluginCheckboxes()
        {
            foreach (var plugin in plugins)
            {
                CheckBox checkBox = new CheckBox
                {
                    Content = plugin
                };

                PluginsStackPanel.Children.Add(checkBox);
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the ArkApi plugin directory exists
            if (!CheckArkApiPluginDirectory())
            {
                // The directory doesn't exist, inform the user
                MessageBox.Show("The ArkApi Plugins directory does not exist. Please install the ArkApi plugin first.");
                return; // Abort the installation process
            }

            // Iterate through server checkboxes to determine selected servers
            foreach (CheckBox checkBox in ServersStackPanel.Children)
            {
                if (checkBox.IsChecked == true)
                {
                    ServerConfig selectedServer = checkBox.Tag as ServerConfig;
                    if (selectedServer != null)
                    {
                        // Perform installation for the selected server
                        InstallPluginsToServer(selectedServer);
                    }
                }
            }

            // Iterate through plugin checkboxes to determine selected plugins
            foreach (CheckBox checkBox in PluginsStackPanel.Children)
            {
                if (checkBox.IsChecked == true)
                {
                    string selectedPlugin = checkBox.Content as string;
                    if (!string.IsNullOrEmpty(selectedPlugin))
                    {
                        // Perform installation for the selected plugin
                        InstallPlugin(selectedPlugin);
                    }
                }
            }
        }

        private bool CheckArkApiPluginDirectory()
        {
            string arkApiPluginDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins");
            return Directory.Exists(arkApiPluginDirectory);
        }

        public class ServerConfig
        {
            public string ChangeNumberStatus { get; set; }
            public string ProfileName { get; set; }
            public string ServerStatus { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }
            public bool IsRunning { get; set; }
            public int ChangeNumber { get; set; }
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
        private void InstallPluginsToServer(ServerConfig server)
        {
            // Define the destination directory for installing plugins
            string installDirectory = Path.Combine(server.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins");

            foreach (CheckBox checkBox in PluginsStackPanel.Children)
            {
                if (checkBox.IsChecked == true)
                {
                    string selectedPlugin = checkBox.Content as string;
                    if (!string.IsNullOrEmpty(selectedPlugin))
                    {
                        // Get the source path of the plugin from the plugins folder
                        string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "plugins", selectedPlugin);

                        // Generate the destination path for the plugin in the server directory
                        string destinationPath = Path.Combine(installDirectory, selectedPlugin);

                        try
                        {
                            // Check if the plugin file exists
                            if (File.Exists(sourcePath))
                            {
                                // Create the destination directory if it doesn't exist
                                Directory.CreateDirectory(installDirectory);

                                // Extract the ZIP file to the destination directory
                                ZipFile.ExtractToDirectory(sourcePath, installDirectory);

                                // Log the installation
                                Debug.WriteLine($"Installed {selectedPlugin} to {server.ProfileName}");
                            }
                            else
                            {
                                Debug.WriteLine($"Plugin file not found: {selectedPlugin}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error installing {selectedPlugin}: {ex.Message}");
                        }
                    }
                }
            }

            MessageBox.Show($"Installed selected plugins to {server.ProfileName}");
        }

        private void InstallPlugin(string pluginName)
        {
            // Logic to install the selected plugin
            MessageBox.Show($"Installing {pluginName}");
        }


        private List<ServerConfig> ReadAllServerConfigs()
        {
            Debug.WriteLine("ReadAllServerConfigs: Method called.");

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");
            Debug.WriteLine($"ReadAllServerConfigs: File path - {filePath}");

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Debug.WriteLine("ReadAllServerConfigs: JSON content read successfully.");
                    List<ServerConfig> serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(json);
                    return serverConfigs ?? new List<ServerConfig>();
                }
                else
                {
                    Debug.WriteLine("ReadAllServerConfigs: JSON file does not exist.");
                    return new List<ServerConfig>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReadAllServerConfigs: Exception occurred - {ex.Message}");
                return new List<ServerConfig>();
            }
        }
    }
}
