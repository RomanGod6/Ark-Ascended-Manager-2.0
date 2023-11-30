﻿using Wpf.Ui.Controls;
using System.Collections.Generic; // If using collections
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Windows.Input;
using Ark_Ascended_Manager.Views.Pages;
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.Diagnostics;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class ServersViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        public ICommand SaveServerProfileCommand { get; }
        public ICommand StartServerCommand { get; private set; }
        public ICommand StopServerCommand { get; private set; }
        public ICommand UpdateServerCommand { get; private set; }


        public ServersViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            SaveServerProfileCommand = new RelayCommand<ServerConfig>(SaveServerProfileAndNavigate);
            StartServerCommand = new RelayCommand<ServerConfig>(StartServer);
            StopServerCommand = new RelayCommand<ServerConfig>(StopServer);
            UpdateServerCommand = new RelayCommand<ServerConfig>(UpdateServer);
        




    }

        public void OnNavigatedTo()
        {
            LoadServerConfigs();
            // Initialization logic specific to the servers page
        }
        private void StartServer(ServerConfig serverConfig)
        {
            // Check if the serverConfig is not null
            if (serverConfig != null)
            {
                if (IsServerRunning(serverConfig))
                {
                    System.Windows.MessageBox.Show("The server is already running.");
                    return;
                }

                // Retrieve the server directory from the ServerConfig
                string serverDirectory = serverConfig.ServerPath;

                // Check if the server directory exists
                if (!string.IsNullOrEmpty(serverDirectory) && Directory.Exists(serverDirectory))
                {
                    // Construct the path to the batch file
                    string batchFilePath = Path.Combine(serverDirectory, "LaunchServer.bat");

                    // Check if the batch file exists
                    if (File.Exists(batchFilePath))
                    {
                        // Start the batch file process
                        Process.Start(batchFilePath);
                    }
                    else
                    {
                        // Handle the case where the batch file is not found
                        System.Windows.MessageBox.Show("Batch file not found.");
                    }
                }
                else
                {
                    // Handle the case where the server directory is invalid or empty
                    System.Windows.MessageBox.Show("Invalid server directory.");
                }
            }
        }
        private async void StopServer(ServerConfig serverConfig)
        {
            if (serverConfig == null)
            {
                System.Windows.MessageBox.Show("Server configuration is not provided.");
                return;
            }
            if (!IsServerRunning(serverConfig))
            {
                System.Windows.MessageBox.Show("The server is not currently running.");
                return;
            }

            // Prompt the user for the countdown time
            string timeInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the countdown time in minutes for shutdown:",
                "Shutdown Timer",
                "10"
            );

            if (!int.TryParse(timeInput, out int countdownMinutes))
            {
                System.Windows.MessageBox.Show("Invalid input for countdown timer.");
                return;
            }

            // Prompt the user for the reason for shutdown
            string reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the reason for the shutdown:",
                "Shutdown Reason",
                "Maintenance"
            );

            if (string.IsNullOrEmpty(reason))
            {
                System.Windows.MessageBox.Show("No reason for shutdown provided.");
                return;
            }

            // Extract RCON details from the batch file
            string batchFilePath = Path.Combine(serverConfig.ServerPath, "LaunchServer.bat");
            string batchFileContent = File.ReadAllText(batchFilePath);
            string rconPort = ExtractRconPort(batchFileContent);
            string adminPassword = ExtractAdminPassword(batchFileContent);

            if (string.IsNullOrEmpty(rconPort) || string.IsNullOrEmpty(adminPassword))
            {
                System.Windows.MessageBox.Show("Failed to extract RCON details from batch file.");
                return;
            }

            // Initiate server shutdown with the user-defined countdown and reason
            await InitiateServerShutdownAsync(countdownMinutes, reason, rconPort, adminPassword);
        }
        private bool IsServerRunning(ServerConfig serverConfig)
        {
            // Get the name of the server executable without the extension
            string serverExeName = Path.GetFileNameWithoutExtension("ArkAscendedServer.exe");
            string asaApiLoaderExeName = Path.GetFileNameWithoutExtension("AsaApiLoader.exe");

            // Get the full path to the server executable
            string serverExePath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe");
            string asaApiLoaderExePath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "AsaApiLoader.exe");

            // Check if there's a process running from the server's executable path
            var allProcesses = Process.GetProcesses();
            foreach (var process in allProcesses)
            {
                try
                {
                    // Check if the process is a server process and if it's running from the expected path
                    if ((process.ProcessName.Equals(serverExeName, StringComparison.OrdinalIgnoreCase) && process.MainModule.FileName.Equals(serverExePath, StringComparison.OrdinalIgnoreCase)) ||
                        (process.ProcessName.Equals(asaApiLoaderExeName, StringComparison.OrdinalIgnoreCase) && process.MainModule.FileName.Equals(asaApiLoaderExePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // This catch block can handle exceptions due to accessing process.MainModule which may require administrative privileges
                    Debug.WriteLine($"Error checking process: {ex.Message}");
                }
            }

            return false;
        }
        private void UpdateServer(ServerConfig serverConfig)
        {
            if (serverConfig == null)
            {
                System.Windows.MessageBox.Show("Server configuration is not provided.");
                return;
            }

            // Check if the server is running before updating
            if (IsServerRunning(serverConfig))
            {
                System.Windows.MessageBox.Show("The server is currently running. Please stop the server before updating.");
                return;
            }

            // Call the update method with the current server configuration
            UpdateServerBasedOnJson(serverConfig);
        }

        public void UpdateServerBasedOnJson(ServerConfig serverConfig)
        {
            if (serverConfig != null && !string.IsNullOrEmpty(serverConfig.AppId))
            {
                string scriptPath = Path.Combine(Path.GetTempPath(), "steamcmd_update_script.txt");
                File.WriteAllLines(scriptPath, new string[]
                {
            $"force_install_dir \"{serverConfig.ServerPath}\"",
            "login anonymous",
            $"app_update {serverConfig.AppId} validate",
            "quit"
                });
                RunSteamCMD(scriptPath);
            }
            else
            {
                Debug.WriteLine("Could not update the server, App ID not found or Server Config is null");
            }
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
            // Your implementation to find steamcmd.exe
            // Example:
            string defaultPath = @"C:\SteamCMD\steamcmd.exe";
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }
            // Additional logic to find steamcmd.exe if not in the default location
            return null;
        }


        public async Task InitiateServerShutdownAsync(int countdownMinutes, string reason, string rconPort, string adminPassword)
        {
            Debug.WriteLine("Shutdown initiated");

            // Countdown logic with messages at each minute
            for (int minute = countdownMinutes; minute > 0; minute--)
            {
                // Send a warning message to server with the reason
                await SendRconCommandAsync($"ServerChat Shutdown in {minute} minutes due to {reason}.", rconPort, adminPassword);
                // Wait for 1 minute
                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            // Optionally send a final message
            await SendRconCommandAsync($"ServerChat Server is shutting down NOW due to {reason}.", rconPort, adminPassword);

            // Save world before shutdown (if applicable)
            await SendRconCommandAsync("saveworld", rconPort, adminPassword);

            // Shutdown command
            await SendRconCommandAsync("doexit", rconPort, adminPassword);
        }


        private async Task SendRconCommandAsync(string command, string rconPort, string adminPassword)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    // Replace the placeholders with actual command, port and password values
                    Arguments = $"/C echo {command} | mcrcon 127.0.0.1 --password {adminPassword} -p {rconPort}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Debug.WriteLine($"RCON command output: {output}");
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.WriteLine($"RCON command error: {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception sending RCON command: {ex.Message}");
            }
        }


        // These methods are placeholders for extracting the details
        private string ExtractRconPort(string batchFileContent)
        {
            const string portKey = "set RconPort=";
            int startIndex = batchFileContent.IndexOf(portKey);
            if (startIndex == -1) return null;

            startIndex += portKey.Length;
            int endIndex = batchFileContent.IndexOf("\n", startIndex);
            return endIndex == -1 ? batchFileContent.Substring(startIndex) : batchFileContent.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private string ExtractAdminPassword(string batchFileContent)
        {
            const string passwordKey = "set AdminPassword=";
            int startIndex = batchFileContent.IndexOf(passwordKey);
            if (startIndex == -1) return null;

            startIndex += passwordKey.Length;
            int endIndex = batchFileContent.IndexOf("\n", startIndex);
            return endIndex == -1 ? batchFileContent.Substring(startIndex) : batchFileContent.Substring(startIndex, endIndex - startIndex).Trim();
        }








        public void OnNavigatedFrom() { }
        public ObservableCollection<ServerConfig> ServerConfigs { get; } = new ObservableCollection<ServerConfig>();

        // This method would be called when a server card is clicked


        private void SaveServerProfileAndNavigate(ServerConfig serverConfig)
        {
            // Define the path where you want to save the JSON
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentServerConfig.json");

            // Serialize the ServerConfig object to JSON
            string json = JsonSerializer.Serialize(serverConfig);

            // Write the JSON to a file
            File.WriteAllText(filePath, json);

            // Navigate to the ConfigPage
            _navigationService.Navigate(typeof(ConfigPage));
        }


        public void LoadServerConfigs()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var servers = JsonSerializer.Deserialize<List<ServerConfig>>(json);

                if (servers != null)
                {
                    ServerConfigs.Clear(); // Clear existing configs before loading new ones
                    foreach (var server in servers)
                    {
                        ServerConfigs.Add(server);
                    }
                }
            }
        }

        // Other methods and properties as needed
    }
}
