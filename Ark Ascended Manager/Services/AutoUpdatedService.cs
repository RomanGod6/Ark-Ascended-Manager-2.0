using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ark_Ascended_Manager.Views.Pages;
using CoreRCON;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Services
{
    internal class AutoUpdateService
    {
        private readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private readonly string _arkAscendedManagerFolder;
        private readonly string _serversConfigPath;
        private readonly string _globalSettingsPath;
        private readonly string _crashDetectionPath;
        private System.Threading.Timer _updateCheckTimer;

        public AutoUpdateService()
        {
            _arkAscendedManagerFolder = Path.Combine(_appDataPath, "Ark Ascended Manager");
            _serversConfigPath = Path.Combine(_arkAscendedManagerFolder, "servers.json");
            _globalSettingsPath = Path.Combine(_arkAscendedManagerFolder, "AAMGlobalSettings.json");
            _crashDetectionPath = Path.Combine(_arkAscendedManagerFolder, "crashdetection.json");
        }
        public void StartCheckingUpdates()
        {
            // Set up the timer to trigger CheckAndUpdateServers method every 10 minutes
            _updateCheckTimer = new System.Threading.Timer(async _ =>
            {
                await CheckAndUpdateServers();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }

        // Ensure you have a method to cleanly stop the timer if needed, for example on application exit
        public void StopCheckingUpdates()
        {
            _updateCheckTimer?.Change(Timeout.Infinite, 0);
            _updateCheckTimer?.Dispose();
        }

        public async Task CheckAndUpdateServers()
        {
            // Load global settings to determine if auto-update is enabled
            var globalSettings = LoadGlobalSettings(); // Assume this method now also loads the UpdateCountdownTimer
            if (!globalSettings.AutoUpdateServersWhenNewUpdateAvailable)
            {
                Debug.WriteLine("Auto-update is disabled.");
                return;
            }

            // Load server profiles
            var servers = LoadServerProfiles(_serversConfigPath);
            bool updatesMade = false;

            // Convert UpdateCountdownTimer from string to int
            if (!int.TryParse(globalSettings.UpdateCountdownTimer, out int countdownStart))
            {
                Debug.WriteLine("Invalid UpdateCountdownTimer value. Defaulting to 10 minutes.");
                countdownStart = 10; // Default value in case of parsing failure
            }

            foreach (var server in servers)
            {
                // Check if the server is online
                if (IsServerOnline(server))
                {
                    var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                    await rcon.ConnectAsync();

                    // Send countdown messages starting from the UpdateCountdownTimer value
                    for (int i = countdownStart; i > 0; i--)
                    {
                        Debug.WriteLine($"Sending shutdown countdown message: {i} minute(s) remaining.");
                        await rcon.SendCommandAsync($"Broadcast Server will shut down in {i} minute(s)...New Update Detected");
                        await Task.Delay(60000); // Wait for 1 minute between each notification
                    }

                    // Shutdown command
                    Debug.WriteLine("Sending RCON shutdown command.");
                    await rcon.SendCommandAsync("doexit");
                    Debug.WriteLine("RCON shutdown command sent successfully.");
                    continue;
                }

                // Check for updates and update server if necessary
                var updateNeeded = await UpdateServerIfNecessary(server);
                if (updateNeeded)
                {
                    updatesMade = true;
                }
            }

            // If any updates were made, save the updated server configurations
            if (updatesMade)
            {
                SaveServerConfiguration(servers);
            }
        }
        public class GlobalSettings
        {
            public bool AutoUpdateServersOnReboot { get; set; }
            public bool AutoUpdateServersWhenNewUpdateAvailable { get; set; }
            public string UpdateCountdownTimer { get; set; }
        }


        private GlobalSettings LoadGlobalSettings()
        {
            try
            {
                // Ensure the global settings file exists
                if (!File.Exists(_globalSettingsPath))
                {
                    Debug.WriteLine($"Global settings file not found at {_globalSettingsPath}.");
                    return new GlobalSettings(); // Return default settings if file does not exist
                }

                // Read the JSON content from the file
                string jsonContent = File.ReadAllText(_globalSettingsPath);

                // Deserialize the JSON content to a GlobalSettings object
                var globalSettings = JsonConvert.DeserializeObject<GlobalSettings>(jsonContent);

                // Check if deserialization was successful
                if (globalSettings == null)
                {
                    Debug.WriteLine("Failed to deserialize the global settings. Returning default settings.");
                    return new GlobalSettings(); // Return default settings if deserialization fails
                }

                return globalSettings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while loading global settings: {ex.Message}");
                return new GlobalSettings(); // Return default settings in case of an error
            }
        }


        private List<ServerConfig> LoadServerProfiles(string configPath)
        {
            if (!File.Exists(configPath))
            {
                Debug.WriteLine($"Server configuration file not found at {configPath}.");
                return new List<ServerConfig>(); // Return an empty list if the file does not exist
            }

            try
            {
                // Read the JSON content from the file
                string jsonContent = File.ReadAllText(configPath);

                // Deserialize the JSON content to a List<ServerConfig> object
                var serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(jsonContent);

                // Check if deserialization was successful
                if (serverConfigs == null)
                {
                    Debug.WriteLine("Failed to deserialize the server configurations. Returning an empty list.");
                    return new List<ServerConfig>(); // Return an empty list if deserialization fails
                }

                return serverConfigs;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while loading server profiles: {ex.Message}");
                return new List<ServerConfig>(); // Return an empty list in case of an error
            }
        }


        private bool IsServerOnline(ServerConfig server)
        {
            // The IsRunning property indicates if the server is currently online
            // Return true if IsRunning is true, indicating the server is online
            // Return false otherwise, indicating the server is offline
            return server.IsRunning;
        }


        private async Task<bool> UpdateServerIfNecessary(ServerConfig server)
        {
            var sanitizedData = LoadSanitizedData(server.AppId);
            if (sanitizedData != null && server.ChangeNumber < sanitizedData.ChangeNumber)
            {
                // Check if the server is currently online
                if (IsServerOnline(server))
                {
                    Debug.WriteLine($"Server {server.ProfileName} is online. Handling updates for online servers is not yet implemented.");
                    return false;
                }

                Debug.WriteLine($"Updating server {server.ProfileName}...");
                await UpdateServerWithSteamCMD(server);

                // Update the server's ChangeNumber
                server.ChangeNumber = sanitizedData.ChangeNumber;

                // Indicate that an update was necessary and performed
                return true;
            }
            // No update was necessary
            return false;
        }

        private void SaveServerConfiguration(List<ServerConfig> servers)
        {
            // Serialize the list to JSON and save it back to the servers.json file
            string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
            File.WriteAllText(_serversConfigPath, json);
            Debug.WriteLine("Server configurations have been updated.");
        }
        private async Task UpdateServerWithSteamCMD(ServerConfig server)
        {
            string scriptPath = CreateSteamCMDScript(server);
            RunSteamCMD(scriptPath);
            // After the server is updated, you can delete the script file if needed.
        }

        private string CreateSteamCMDScript(ServerConfig server)
        {
            string scriptContent = @$"
        force_install_dir ""{server.ServerPath}""
        login anonymous
        app_update {server.AppId} validate
        quit
    ";

            string scriptPath = Path.Combine(Path.GetTempPath(), $"steamcmd_script_{server.AppId}.txt");
            File.WriteAllText(scriptPath, scriptContent);
            return scriptPath;
        }
        private void RunSteamCMD(string scriptPath)
        {
            string steamCmdPath = FindSteamCmdPath();
            if (string.IsNullOrEmpty(steamCmdPath))
            {
                // Handle the error: steamcmd.exe not found
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = steamCmdPath,
                Arguments = $"+runscript \"{scriptPath}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                process.WaitForExit();
            }
        }
        private string FindSteamCmdPath()
        {
            // Define the JSON file path in the app data folder
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appDataPath, "SteamCmdPath.json");

            // Check if the app data directory exists, if not, create it
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            // Try to read the path from the JSON file
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                dynamic pathData = JsonConvert.DeserializeObject<dynamic>(json);
                string savedPath = pathData?.SteamCmdPath;
                if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                {
                    return savedPath;
                }
            }

            // If the path is not found in the JSON file, prompt the user with OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)

            {
                // Save the selected path to the JSON file
                SaveSteamCmdPath(openFileDialog.FileName, jsonFilePath);
                return openFileDialog.FileName;
            }

            return null; // or handle this case appropriately
        }
        private void SaveSteamCmdPath(string path, string jsonFilePath)
        {
            var pathData = new { SteamCmdPath = path };
            string json = JsonConvert.SerializeObject(pathData, Formatting.Indented);
            File.WriteAllText(jsonFilePath, json);
        }
        private SanitizedSteamData LoadSanitizedData(string appId)
        {
            // Adjust the file path to include the AppId as a directory
            string filePath = Path.Combine(_arkAscendedManagerFolder, appId, $"sanitizedsteamdata_{appId}.json");

            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                var sanitizedData = JsonConvert.DeserializeObject<SanitizedSteamData>(jsonContent);
                return sanitizedData;
            }
            else
            {
                Debug.WriteLine($"Sanitized data file does not exist for AppId {appId} at path: {filePath}");
                return null; // Return null if file doesn't exist or deserialization fails
            }
        }


        // Reuse or adapt the existing methods you've described, such as LoadSanitizedData and UpdateServerWithSteamCMD
    }



    // Define GlobalSettings, ServerConfig, and other necessary classes or structs if not already defined
}
