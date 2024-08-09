using Wpf.Ui.Controls;
using System.Collections.Generic; // If using collections
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Windows.Input;
using Ark_Ascended_Manager.Views.Pages;
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Threading;
using static Ark_Ascended_Manager.Services.DiscordBotService;
using System.Net;
using Newtonsoft.Json.Linq;
using static Ark_Ascended_Manager.ViewModels.Pages.ConfigPageViewModel;
using Ark_Ascended_Manager.Services;
using ServerConfig = Ark_Ascended_Manager.Views.Pages.CreateServersPage.ServerConfig;
using YourNamespace.Helpers;
namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class ServersViewModel : ObservableObject, INavigationAware
    {
        private SemaphoreSlim fileSemaphore = new SemaphoreSlim(1, 1);

        private readonly INavigationService _navigationService;
        public ICommand SaveServerProfileCommand { get; }
        public ICommand StartServerCommand { get; private set; }
        public ICommand StopServerCommand { get; private set; }

        public ICommand RestartServerCommand { get; private set; }
        public ICommand UpdateServerCommand { get; private set; }
        private DispatcherTimer _statusUpdateTimer;
        private ServerConfig _currentServerInfo;
        public ServerConfig CurrentServerInfo { get; private set; }
        public ICommand StopAllServersCommand { get; private set; }

        public ICommand UpdateAllServersCommand { get; private set; }
        public ICommand StartAllServersCommand { get; private set; }





        public ServersViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            SaveServerProfileCommand = new RelayCommand<ServerConfig>(SaveServerProfileAndNavigate);
            StartServerCommand = new RelayCommand<ServerConfig>(StartServer);
            StopServerCommand = new RelayCommand<ServerConfig>(StopServer);
            RestartServerCommand = new RelayCommand<ServerConfig>(RestartServer);
            UpdateServerCommand = new RelayCommand<ServerConfig>(UpdateServer);
            _statusUpdateTimer = new DispatcherTimer();
            _statusUpdateTimer.Interval = TimeSpan.FromSeconds(1); // Set the desired interval
            _statusUpdateTimer.Tick += ServerStatusTimer_Tick;
            _statusUpdateTimer.Start();
            UpdateAllServersCommand = new RelayCommand(UpdateAllServers);
            StopAllServersCommand = new RelayCommand(StopAllServers);
            StartAllServersCommand = new RelayCommand(StartAllServers);
        }
        public void ServerStatusTimer_Tick(object sender, EventArgs e)
        {
            var servers = LoadServerConfigs();
            bool anyServerStatusChanged = false;

            // Hypothetical method to load the ChangeNumber from the sanitized JSON
           

            foreach (var serverConfig in servers)
            {
                int sanitizedChangeNumber = LoadSanitizedChangeNumber(serverConfig);
                bool isRunning = IsServerRunning(serverConfig);
                string newStatus = isRunning ? "Online" : "Offline";

                // Load server ChangeNumber (assumed part of serverConfig for this example)
                int serverChangeNumber = serverConfig.ChangeNumber;

                // Compare the ChangeNumbers
                if (serverChangeNumber < sanitizedChangeNumber)
                {
                    serverConfig.ChangeNumberStatus = "Server is Not Up to Date"; // Update this property
                }
                else if (serverChangeNumber == sanitizedChangeNumber)
                {
                    serverConfig.ChangeNumberStatus = "Servers Up To Date"; // Update this property
                }

                // Check if the status has changed before updating
                if (serverConfig.ServerStatus != newStatus)
                {
                    serverConfig.ServerStatus = newStatus;
                    anyServerStatusChanged = true;
                }
            }

            // If any server status has changed, save the updated statuses back to the JSON file
            if (anyServerStatusChanged)
            {
                SaveServerConfigs(servers);
            }
        }
        private async void StartAllServers()
        {
            // Prompt the user for the delay in seconds
            string delayInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the delay in seconds between starting each server:",
                "Start Delay",
                "10"
            );

            if (!int.TryParse(delayInput, out int delaySeconds))
            {
                System.Windows.MessageBox.Show("Invalid input for delay.");
                return;
            }

            var serverConfigsCopy = ServerConfigs.ToList();

            foreach (var serverConfig in serverConfigsCopy)
            {
                StartServer(serverConfig);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        private async void StopAllServers()
        {
            var stopTasks = new List<Task>();

            foreach (var serverConfig in ServerConfigs)
            {
                await RemoveServerFromMonitoringAsync(serverConfig.ServerPath);

                // Launch the stop operation for each server and store the task
                var stopTask = Task.Run(() => StopServer(serverConfig));
                stopTasks.Add(stopTask);
            }

            // Wait for all stop operations to complete
            await Task.WhenAll(stopTasks);
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
            await RemoveServerFromMonitoringAsync(serverConfig.ServerPath);

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

            try
            {
                // Display the server information in a MessageBox for debugging
                string serverInfo = $"Server Name: {serverConfig.ServerName}\n" +
                                    $"Server IP: {serverConfig.ServerIP}\n" +
                                    $"RCON Port: {serverConfig.RCONPort}\n" +
                                    $"Admin Password: {serverConfig.AdminPassword}";
                System.Windows.MessageBox.Show(serverInfo, "Server Information");

                // Create an instance of ArkRCONService using server details
                var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword, serverConfig.ServerPath);

                // Connect to RCON
                await rconService.ConnectAsync();

                // Initiate server shutdown with the user-defined countdown and reason
                await rconService.ShutdownServerAsync(countdownMinutes, reason);

                // Optionally, disconnect after command execution
                rconService.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to shutdown server: {ex.Message}");
            }
        }

        private async void RestartServer(ServerConfig serverConfig)
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
            await RemoveServerFromMonitoringAsync(serverConfig.ServerPath);

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

            try
            {
                // Display the server information in a MessageBox for debugging
                string serverInfo = $"Server Name: {serverConfig.ServerName}\n" +
                                    $"Server IP: {serverConfig.ServerIP}\n" +
                                    $"RCON Port: {serverConfig.RCONPort}\n" +
                                    $"Admin Password: {serverConfig.AdminPassword}";
                System.Windows.MessageBox.Show(serverInfo, "Server Information");

                // Create an instance of ArkRCONService using server details
                var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword, serverConfig.ServerPath);

                // Connect to RCON
                await rconService.ConnectAsync();

                // Initiate server shutdown with the user-defined countdown and reason
                await rconService.ShutdownServerAsync(countdownMinutes, reason);

                // Optionally, disconnect after command execution
                rconService.Dispose();


                Thread.Sleep(20000);

                StartServer(serverConfig);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to shutdown server: {ex.Message}");
            }
        }

        private void UpdateAllServers()
        {
            var runningServers = new List<string>();

            foreach (var serverConfig in ServerConfigs)
            {
                if (IsServerRunning(serverConfig))
                {
                    // Add the name of the running server to the list
                    runningServers.Add(serverConfig.ProfileName);
                    continue;
                }

                UpdateServerBasedOnJson(serverConfig);
            }

            // Check if there are any running servers
            if (runningServers.Any())
            {
                // Create a message with all running server names
                string message = "The following servers are currently running and cannot be updated:\n\n" + string.Join("\n", runningServers);
                System.Windows.MessageBox.Show(message, "Servers Running", System.Windows.MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }



        private int LoadSanitizedChangeNumber(ServerConfig serverConfig)
        {
            
            string appId = serverConfig.AppId; // Replace with the actual AppId if it's dynamic
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataFolder, "Ark Ascended Manager", appId, "sanitizedsteamdata_" + appId + ".json");

            try
            {
                // Read the JSON file
                string jsonData = File.ReadAllText(filePath);

                // Parse the JSON to get the ChangeNumber
                var jsonObject = JObject.Parse(jsonData);
                int changeNumber = jsonObject["ChangeNumber"].Value<int>();

                return changeNumber;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, parsing errors)
                Console.WriteLine("Error reading or parsing the JSON file: " + ex.Message);
                return 0; // Return a default value or handle the error appropriately
            }
        }

        private void SaveServerConfigs(List<ServerConfig> servers)
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");
                JsonHelper.WriteJsonFile(filePath, servers);
                System.Windows.MessageBox.Show("Server configurations saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save server configurations: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }



        // Helper method to get server status from JSON for a specific server profile
        private bool GetServerStatusFromJson(string profileName)
        {
            try
            {
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");
                var servers = JsonHelper.ReadJsonFile<List<ServerConfig>>(filePath);
                var server = servers?.FirstOrDefault(s => s.ProfileName == profileName);
                return server != null && server.ServerStatus == "Online";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error retrieving server status: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        public void SetCurrentServerInfo(ServerConfig serverInfo)
        {
            _currentServerInfo = serverInfo;
            _statusUpdateTimer.Start();
        }

        public void OnNavigatedTo()
        {
            LoadServerConfigs();
            _statusUpdateTimer.Start();





            // Initialization logic specific to the servers page
        }
        public void OnNavigatedFrom()
        {
            _statusUpdateTimer.Stop();

        }
        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status)); // Notify the UI that the property has changed
                }
            }
        }
        private void UpdateServerStatus(CreateServersPage.ServerConfig serverConfig)
        {
            Status = IsServerRunning(serverConfig) ? "Online" : "Offline";
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
                        var process = Process.Start(batchFilePath);
                        int pid = process.Id;
                        SaveMonitoringInfo(new MonitoringInfo { ServerDirectory = serverDirectory, Pid = pid });
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
       
        private void SaveMonitoringInfo(MonitoringInfo info)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string jsonFilePath = Path.Combine(appDataPath, "Ark Ascended Manager", "crashdetection.json");

            // You can decide whether to append to the file if it exists or overwrite it
            List<MonitoringInfo> monitoringInfos;
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                monitoringInfos = JsonConvert.DeserializeObject<List<MonitoringInfo>>(json);
                if (monitoringInfos == null) monitoringInfos = new List<MonitoringInfo>();
            }
            else
            {
                monitoringInfos = new List<MonitoringInfo>();
            }

            monitoringInfos.Add(info);

            string newJson = JsonConvert.SerializeObject(monitoringInfos, Formatting.Indented);
            File.WriteAllText(jsonFilePath, newJson);
        }
        public class MonitoringInfo
        {
            public string ServerDirectory { get; set; }
            public int Pid { get; set; }
            // ... any other relevant information
        }
        private async Task RemoveServerFromMonitoringAsync(string serverDirectory)
        {
            await fileSemaphore.WaitAsync();
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string jsonFilePath = Path.Combine(appDataPath, "Ark Ascended Manager", "crashdetection.json");

                if (File.Exists(jsonFilePath))
                {
                    string json = File.ReadAllText(jsonFilePath);
                    var monitoringInfos = JsonConvert.DeserializeObject<List<MonitoringInfo>>(json);

                    if (monitoringInfos != null)
                    {
                        monitoringInfos.RemoveAll(info => info.ServerDirectory.Equals(serverDirectory, StringComparison.OrdinalIgnoreCase));
                        json = JsonConvert.SerializeObject(monitoringInfos, Formatting.Indented);
                        File.WriteAllText(jsonFilePath, json);
                    }
                }
            }
            finally
            {
                fileSemaphore.Release();
            }
        }
        
        public bool IsServerRunning(ServerConfig serverConfig)
        {
            bool isServerRunning = false; // Initialize the flag as false

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
                        isServerRunning = true; // Set the flag to true if the server process is found
                        break; // No need to continue checking once we found a running server
                    }
                }
                catch (Exception ex)
                {
                    // This catch block can handle exceptions due to accessing process.MainModule which may require administrative privileges
                    Ark_Ascended_Manager.Services.Logger.Log($"Error checking process: {ex.Message}");
                }
            }

            return isServerRunning; // Return the flag indicating whether the server is running
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

                // After running SteamCMD, update the change number
                UpdateChangeNumberFromJson(serverConfig);
            }
            else
            {
                Ark_Ascended_Manager.Services.Logger.Log("Could not update the server, App ID not found or Server Config is null");
            }
        }
        private void UpdateChangeNumberFromJson(ServerConfig serverConfig)
        {
            Debug.WriteLine("UpdateChangeNumberFromJson: Method called.");

            if (serverConfig == null || string.IsNullOrEmpty(serverConfig.AppId))
            {
                Debug.WriteLine("UpdateChangeNumberFromJson: ServerConfig is null or AppId is empty.");
                return;
            }

            string jsonFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ark Ascended Manager",
                serverConfig.AppId,
                $"sanitizedsteamdata_{serverConfig.AppId}.json");

            Debug.WriteLine($"UpdateChangeNumberFromJson: JSON file path - {jsonFilePath}");

            if (!File.Exists(jsonFilePath))
            {
                Debug.WriteLine("UpdateChangeNumberFromJson: JSON file does not exist.");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                Debug.WriteLine("UpdateChangeNumberFromJson: JSON content read successfully.");

                dynamic json = JsonConvert.DeserializeObject(jsonContent);
                if (json != null && json.ChangeNumber != null)
                {
                    Debug.WriteLine($"UpdateChangeNumberFromJson: JSON ChangeNumber found - {json.ChangeNumber}");
                    serverConfig.ChangeNumber = json.ChangeNumber;
                    SaveUpdatedServerConfig(serverConfig);
                }
                else
                {
                    Debug.WriteLine("UpdateChangeNumberFromJson: JSON is invalid or doesn't contain ChangeNumber.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateChangeNumberFromJson: Exception occurred - {ex.Message}");
            }
        }
        private void SaveUpdatedServerConfig(ServerConfig updatedConfig)
        {
            Debug.WriteLine("SaveUpdatedServerConfig: Method called.");

            var allServerConfigs = ReadAllServerConfigs();
            // Use both AppId and ProfileName for a more accurate match
            var serverToUpdate = allServerConfigs.FirstOrDefault(s => s.AppId == updatedConfig.AppId && s.ProfileName.Equals(updatedConfig.ProfileName, StringComparison.OrdinalIgnoreCase));

            if (serverToUpdate != null)
            {
                Debug.WriteLine($"SaveUpdatedServerConfig: Current ChangeNumber for server '{serverToUpdate.ProfileName}' is {serverToUpdate.ChangeNumber}");
                serverToUpdate.ChangeNumber = updatedConfig.ChangeNumber;
                Debug.WriteLine($"SaveUpdatedServerConfig: New ChangeNumber to be set for server '{serverToUpdate.ProfileName}' is {updatedConfig.ChangeNumber}");

                WriteAllServerConfigs(allServerConfigs);
            }
            else
            {
                Debug.WriteLine("SaveUpdatedServerConfig: Server configuration not found.");
            }
        }



        private List<ServerConfig> ReadAllServerConfigs()
        {
            Debug.WriteLine("ReadAllServerConfigs: Method called.");

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");
            Debug.WriteLine($"ReadAllServerConfigs: File path - {filePath}");

            try
            {
                var serverConfigs = JsonHelper.ReadJsonFile<List<ServerConfig>>(filePath);
                if (serverConfigs != null)
                {
                    Debug.WriteLine("ReadAllServerConfigs: JSON content read successfully.");
                    return serverConfigs;
                }
                else
                {
                    Debug.WriteLine("ReadAllServerConfigs: JSON file does not exist or contains no data.");
                    return new List<ServerConfig>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReadAllServerConfigs: Exception occurred - {ex.Message}");
                return new List<ServerConfig>();
            }
        }


        private void WriteAllServerConfigs(List<ServerConfig> configs)
        {
            Debug.WriteLine("WriteAllServerConfigs: Method called.");

            // Optional: Debug print for verification
            var serverForDebug = configs.FirstOrDefault(s => s.ProfileName == "YourServerProfileName"); // Replace with the actual profile name
            if (serverForDebug != null)
            {
                Debug.WriteLine($"WriteAllServerConfigs: ChangeNumber for server '{serverForDebug.ProfileName}' before writing is {serverForDebug.ChangeNumber}");
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");
            Debug.WriteLine($"WriteAllServerConfigs: File path - {filePath}");

            try
            {
                string json = JsonConvert.SerializeObject(configs, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Debug.WriteLine("WriteAllServerConfigs: JSON file written successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WriteAllServerConfigs: Exception occurred - {ex.Message}");
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
            // Define the JSON file path in the app data directory
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appDataPath, "SteamCmdPath.json");

            // Try to read the path from the JSON file
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string json = File.ReadAllText(jsonFilePath);
                    dynamic pathData = JsonConvert.DeserializeObject<dynamic>(json);
                    string savedPath = pathData?.SteamCmdPath;
                    if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                    {
                        return savedPath;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur during reading and deserializing the JSON file
                    // For example, log the exception and proceed to prompt the user
                }
            }

            // Prompt the user to locate steamcmd.exe if the path is not found or not valid
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Save the selected path to the JSON file for future use
                SaveSteamCmdPath(openFileDialog.FileName, jsonFilePath);
                return openFileDialog.FileName;
            }

            // Return null if the path could not be found or the user cancelled the dialog
            return null;
        }

        private void SaveSteamCmdPath(string path, string jsonFilePath)
        {
            var pathData = new { SteamCmdPath = path };
            string json = JsonConvert.SerializeObject(pathData, Formatting.Indented);
            File.WriteAllText(jsonFilePath, json);
        }


        



        private CoreRCON.RCON rcon;
        private string lastUsedRconPort;
        private string lastUsedAdminPassword;

        private async Task SendRconCommandAsync(string command, string rconPort, string adminPassword)
        {
            try
            {
                if (rcon == null || lastUsedRconPort != rconPort || lastUsedAdminPassword != adminPassword)
                {
                    
                    rcon = new CoreRCON.RCON(IPAddress.Parse("127.0.0.1"), ushort.Parse(rconPort), adminPassword);
                    await rcon.ConnectAsync();
                    lastUsedRconPort = rconPort;
                    lastUsedAdminPassword = adminPassword;
                }

                string response = await rcon.SendCommandAsync(command);
                Ark_Ascended_Manager.Services.Logger.Log($"RCON command response: {response}");
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception sending RCON command: {ex.Message}");
            }
        }
        private string _changeNumberStatus;
        public string ChangeNumberStatus
        {
            get => _changeNumberStatus;
            set
            {
                if (_changeNumberStatus != value)
                {
                    _changeNumberStatus = value;
                    OnPropertyChanged(nameof(ChangeNumberStatus));
                }
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








        
        public ObservableCollection<ServerConfig> ServerConfigs { get; } = new ObservableCollection<ServerConfig>();

        // This method would be called when a server card is clicked


        private void SaveServerProfileAndNavigate(ServerConfig serverConfig)
        {
            // Define the path where you want to save the JSON
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentServerConfig.json");

            // Use JsonHelper to save the ServerConfig object to a JSON file
            JsonHelper.WriteJsonFile(filePath, serverConfig);

            // Navigate to the ConfigPage
            _navigationService.Navigate(typeof(ConfigPage));
        }



        public List<ServerConfig> LoadServerConfigs()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            // Use JsonHelper to load the list of ServerConfig objects from the JSON file
            var servers = JsonHelper.ReadJsonFile<List<ServerConfig>>(filePath) ?? new List<ServerConfig>();

            if (servers != null)
            {
                ServerConfigs.Clear(); // Clear existing configs before loading new ones
                foreach (var server in servers)
                {
                    ServerConfigs.Add(server);
                }
            }

            return servers;
        }





        // Other methods and properties as needed
    }
}
