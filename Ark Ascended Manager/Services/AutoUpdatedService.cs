using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using static Ark_Ascended_Manager.ViewModels.Pages.SettingsViewModel;

namespace Ark_Ascended_Manager.Services
{
    public class AutoUpdateService
    {
        private readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private readonly string _arkAscendedManagerFolder;
        private readonly string _serversConfigPath;
        private readonly string _globalSettingsPath;
        private readonly string _crashDetectionPath;
        private System.Threading.Timer _updateCheckTimer;
        private bool _isUpdating = false; // Flag to track if an update is in progress

        public AutoUpdateService()
        {
            _arkAscendedManagerFolder = Path.Combine(_appDataPath, "Ark Ascended Manager");
            _serversConfigPath = Path.Combine(_arkAscendedManagerFolder, "servers.json");
            _globalSettingsPath = Path.Combine(_arkAscendedManagerFolder, "AAMGlobalSettings.json");
            _crashDetectionPath = Path.Combine(_arkAscendedManagerFolder, "crashdetection.json");
        }

        public void StartCheckingUpdates()
        {
            UpdateTimerInterval();
        }

        public void StopCheckingUpdates()
        {
            if (_updateCheckTimer != null)
            {
                _updateCheckTimer.Change(Timeout.Infinite, 0);
                _updateCheckTimer.Dispose();
                _updateCheckTimer = null;
            }
        }

        public void UpdateTimerInterval()
        {
            var globalSettings = LoadGlobalSettings();
            TimeSpan interval = TimeSpan.FromMinutes(globalSettings.UpdateCheckInterval);

            // Log the interval for checking updates
            Debug.WriteLine($"Update check interval updated to {interval.TotalMinutes} minutes.");

            StopCheckingUpdates(); // Stop the existing timer if it is running

            _updateCheckTimer = new System.Threading.Timer(async _ =>
            {
                // Log each time the timer is triggered
                Debug.WriteLine($"Update check timer triggered at {DateTime.Now}.");

                if (!_isUpdating)
                {
                    Debug.WriteLine("Starting update check...");
                    await CheckAndUpdateServers();
                    Debug.WriteLine("Update check completed.");
                }
                else
                {
                    Debug.WriteLine("Update check skipped because an update is already in progress.");
                }
            }, null, TimeSpan.Zero, interval);
        }

        public async Task CheckAndUpdateServers()
        {
            _isUpdating = true; // Set the flag to indicate that an update is in progress

            try
            {
                var globalSettings = LoadGlobalSettings();
                if (globalSettings == null || !globalSettings.AutoUpdateServersWhenNewUpdateAvailable)
                {
                    Debug.WriteLine("Auto-update is disabled or global settings could not be loaded.");
                    return;
                }

                var servers = LoadServerProfiles(_serversConfigPath);
                if (servers == null || !servers.Any())
                {
                    Debug.WriteLine("No servers found or failed to load server profiles.");
                    return;
                }

                bool updatesMade = false;

                if (!int.TryParse(globalSettings.UpdateCountdownTimer, out int countdownStart))
                {
                    Debug.WriteLine("Invalid UpdateCountdownTimer value. Defaulting to 10 minutes.");
                    countdownStart = 10;
                }

                foreach (var server in servers)
                {
                    try
                    {
                        if (IsServerOnline(server))
                        {
                            using var arkRCONService = new ArkRCONService("127.0.0.1", (ushort)server.RCONPort, server.AdminPassword, server.ServerPath);
                            await arkRCONService.ConnectAsync();

                            for (int i = countdownStart; i > 0; i--)
                            {
                                Debug.WriteLine($"Sending shutdown countdown message: {i} minute(s) remaining.");
                                await arkRCONService.SendServerChatAsync($"Server will shut down in {i} minute(s)...New Update Detected");
                                await Task.Delay(TimeSpan.FromMinutes(1));
                            }

                            Debug.WriteLine("Sending RCON shutdown command.");
                            await arkRCONService.SendCommandAsync("doexit");
                            Debug.WriteLine("RCON shutdown command sent successfully.");
                        }

                        var updateNeeded = await UpdateServerIfNecessary(server);
                        if (updateNeeded)
                        {
                            updatesMade = true;
                        }
                    }
                    catch (SocketException ex)
                    {
                        Debug.WriteLine($"SocketException: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception: {ex.Message}");
                    }
                }

                if (updatesMade)
                {
                    SaveServerConfiguration(servers);
                }
            }
            finally
            {
                _isUpdating = false; // Reset the flag to indicate that the update has finished
            }
        }

        private AAMGlobalSettings LoadGlobalSettings()
        {
            try
            {
                if (!File.Exists(_globalSettingsPath))
                {
                    Debug.WriteLine($"Global settings file not found at {_globalSettingsPath}.");
                    return new AAMGlobalSettings();
                }

                string jsonContent = File.ReadAllText(_globalSettingsPath);
                var globalSettings = JsonConvert.DeserializeObject<AAMGlobalSettings>(jsonContent);

                if (globalSettings == null)
                {
                    Debug.WriteLine("Failed to deserialize the global settings. Returning default settings.");
                    return new AAMGlobalSettings();
                }

                return globalSettings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while loading global settings: {ex.Message}");
                return new AAMGlobalSettings();
            }
        }

        private void SaveGlobalSettings(AAMGlobalSettings settings)
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_globalSettingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while saving global settings: {ex.Message}");
            }
        }

        private List<ServerConfig> LoadServerProfiles(string configPath)
        {
            if (!File.Exists(configPath))
            {
                Debug.WriteLine($"Server configuration file not found at {configPath}.");
                return new List<ServerConfig>();
            }

            try
            {
                string jsonContent = File.ReadAllText(configPath);
                var serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(jsonContent);

                if (serverConfigs == null)
                {
                    Debug.WriteLine("Failed to deserialize the server configurations. Returning an empty list.");
                    return new List<ServerConfig>();
                }

                return serverConfigs;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while loading server profiles: {ex.Message}");
                return new List<ServerConfig>();
            }
        }

        private bool IsServerOnline(ServerConfig server)
        {
            if (server.IsRunning)
            {
                return true;
            }

            string exePath1 = Path.Combine(server.ServerPath, @"ShooterGame\Binaries\Win64\ArkAscendedServer.exe");
            string exePath2 = Path.Combine(server.ServerPath, @"ShooterGame\Binaries\Win64\AsaApiLoader.exe");

            return IsProcessRunningForPath(exePath1) || IsProcessRunningForPath(exePath2);
        }

        private bool IsProcessRunningForPath(string fullPath)
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (string.Equals(process.MainModule.FileName, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        private async Task<bool> UpdateServerIfNecessary(ServerConfig server)
        {
            var sanitizedData = LoadSanitizedData(server.AppId);
            if (sanitizedData != null && server.ChangeNumber < sanitizedData.ChangeNumber)
            {
                if (IsServerOnline(server))
                {
                    Debug.WriteLine($"Server {server.ProfileName} is online. Handling updates for online servers is not yet implemented.");
                    return false;
                }

                Debug.WriteLine($"Updating server {server.ProfileName}...");
                await UpdateServerWithSteamCMD(server);
                server.ChangeNumber = sanitizedData.ChangeNumber;
                return true;
            }
            return false;
        }

        private void SaveServerConfiguration(List<ServerConfig> servers)
        {
            try
            {
                string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
                File.WriteAllText(_serversConfigPath, json);
                Debug.WriteLine("Server configurations have been updated.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while saving server configurations: {ex.Message}");
            }
        }

        private async Task UpdateServerWithSteamCMD(ServerConfig server)
        {
            string scriptPath = CreateSteamCMDScript(server);
            RunSteamCMD(scriptPath);
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
                Debug.WriteLine("SteamCMD path is not set or invalid.");
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
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appDataPath, "SteamCmdPath.json");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

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

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSteamCmdPath(openFileDialog.FileName, jsonFilePath);
                return openFileDialog.FileName;
            }

            return null;
        }

        private void SaveSteamCmdPath(string path, string jsonFilePath)
        {
            try
            {
                var pathData = new { SteamCmdPath = path };
                string json = JsonConvert.SerializeObject(pathData, Formatting.Indented);
                File.WriteAllText(jsonFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while saving SteamCMD path: {ex.Message}");
            }
        }

        private SanitizedSteamData LoadSanitizedData(string appId)
        {
            string filePath = Path.Combine(_arkAscendedManagerFolder, appId, $"sanitizedsteamdata_{appId}.json");

            if (File.Exists(filePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var sanitizedData = JsonConvert.DeserializeObject<SanitizedSteamData>(jsonContent);
                    return sanitizedData;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred while loading sanitized data for AppId {appId}: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine($"Sanitized data file does not exist for AppId {appId} at path: {filePath}");
                return null;
            }
        }
    }
}
