using Ark_Ascended_Manager.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            // Iterate through server checkboxes to determine selected servers
            foreach (CheckBox checkBox in ServersStackPanel.Children)
            {
                if (checkBox.IsChecked == true)
                {
                    ServerConfig selectedServer = checkBox.Tag as ServerConfig;
                    if (selectedServer != null)
                    {
                        // Check if the ArkApi plugin directory exists for the selected server
                        if (!CheckArkApiPluginDirectory(selectedServer))
                        {
                            // The directory doesn't exist, inform the user
                            MessageBox.Show($"The ArkApi Plugins directory does not exist for server {selectedServer.ProfileName}. Please install the ArkApi plugin first.");
                            continue; // Skip to the next server
                        }

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


        private bool CheckArkApiPluginDirectory(ServerConfig server)
        {
            string arkApiPluginDirectory = Path.Combine(server.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins");
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
            Debug.WriteLine("Starting plugin installation process.");
            string installDirectory = Path.Combine(server.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins");
            string backupDirectory = Path.Combine(server.ServerPath, "BackupConfigs"); // Backup directory
            Directory.CreateDirectory(backupDirectory); // Ensure the backup directory exists

            foreach (CheckBox checkBox in PluginsStackPanel.Children)
            {
                if (checkBox.IsChecked == true)
                {
                    string selectedPlugin = checkBox.Content as string;
                    if (!string.IsNullOrEmpty(selectedPlugin))
                    {
                        string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "plugins", selectedPlugin);
                        string pluginNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedPlugin);
                        string destinationPluginPath = Path.Combine(installDirectory, pluginNameWithoutExtension);
                        string configPath = Path.Combine(destinationPluginPath, "config.json");
                        string backupConfigPath = Path.Combine(backupDirectory, pluginNameWithoutExtension + "_config_backup.json");

                        try
                        {
                            // Backup existing config.json if it exists
                            if (File.Exists(configPath))
                            {
                                File.Copy(configPath, backupConfigPath, overwrite: true);
                                Debug.WriteLine("Existing config.json backed up.");
                            }

                            // Delete existing plugin folder, then extract new plugin
                            if (Directory.Exists(destinationPluginPath))
                            {
                                Directory.Delete(destinationPluginPath, recursive: true);
                            }
                            ZipFile.ExtractToDirectory(sourcePath, installDirectory);
                            Debug.WriteLine("New plugin extracted and old version removed.");

                            // Merge the JSON configs
                            if (File.Exists(backupConfigPath))
                            {
                                MergeJsonConfigs(configPath, backupConfigPath);
                                Debug.WriteLine("Merged backup config.json with new config.json.");
                            }

                            Debug.WriteLine($"Installed {selectedPlugin} to {server.ProfileName}.");
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

        private void MergeJsonConfigs(string originalConfigPath, string backupConfigPath)
        {
            var originalConfig = JObject.Parse(File.ReadAllText(originalConfigPath));
            var backupConfig = JObject.Parse(File.ReadAllText(backupConfigPath));

            MergeJson(originalConfig, backupConfig);

            File.WriteAllText(originalConfigPath, originalConfig.ToString());
            Debug.WriteLine($"Config merged: {originalConfigPath}");
        }

        private void MergeJson(JObject original, JObject backup)
        {
            foreach (var property in backup.Properties())
            {
                JToken originalValue;
                if (original.TryGetValue(property.Name, out originalValue))
                {
                    // If it's an object, we need to go deeper
                    if (property.Value.Type == JTokenType.Object && originalValue.Type == JTokenType.Object)
                    {
                        MergeJson(originalValue as JObject, property.Value as JObject);
                    }
                    else // Otherwise, simply overwrite the value
                    {
                        original[property.Name] = property.Value;
                    }
                }
                // If the original config doesn't have this property, do nothing
            }
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
