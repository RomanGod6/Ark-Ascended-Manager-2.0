using System;
using System.Text.Json;
using Ark_Ascended_Manager.Models; // Ensure this is the correct namespace for ServerConfig
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;
using System.Diagnostics;
using System.Windows.Input;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using Ark_Ascended_Manager.Views.Pages;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public class ConfigPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        public ServerConfig CurrentServerConfig { get; private set; }
        public ICommand SaveGameUserSettingsCommand { get; private set; }
        public ICommand SaveGameIniSettingsCommand { get; private set; }
        public ICommand LoadLaunchServerSettingsCommand { get; private set; }
        public ICommand StartServerCommand { get; private set; }
        public ICommand UpdateServerCommand { get; private set; }
        public ICommand StopServerCommand { get; private set; }
        public ICommand SaveGameIniCommand { get; }
        public ICommand SaveGAMEIniFileCommand { get; private set; }
        public ICommand DeleteServerCommand { get; }
        public ICommand WipeServerCommand { get; }
        private string _iniContent;
        public string IniContent
        {
            get => _iniContent;
            set
            {
                if (_iniContent != value)
                {
                    _iniContent = value;
                    OnPropertyChanged(nameof(IniContent));
                }
            }
        }
        private string _gameiniContent;
        public string GameIniContent
        {
            get => _gameiniContent;
            set
            {
                if (_gameiniContent != value)
                {
                    _gameiniContent = value;
                    OnPropertyChanged(nameof(GameIniContent));
                }
            }
        }
     

        public ICommand SaveIniFileCommand { get; private set; }
        


        public ConfigPageViewModel(INavigationService navigationService)
        {
            {
                _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            }
            LoadServerProfile();
            SaveGameUserSettingsCommand = new RelayCommand(SaveGameUserSettings);
            SaveGameIniSettingsCommand = new RelayCommand(SaveGameIniSettings);
            LoadLaunchServerSettingsCommand = new RelayCommand(UpdateLaunchParameters);
            StartServerCommand = new RelayCommand(StartServer);
            UpdateServerCommand = new RelayCommand(UpdateServerBasedOnJson);
            StopServerCommand = new RelayCommand(async () => await InitiateServerShutdownAsync(10));
            LoadCustomGUSIniFile();
            SaveIniFileCommand = new RelayCommand(SaveCustomGUSIniFile);
            LoadCustomGAMEIniFile();
            SaveGAMEIniFileCommand = new RelayCommand(SaveCustomGAMEIniFile);
            DeleteServerCommand = new RelayCommand(DeleteServer);
            WipeServerCommand = new RelayCommand(WipeServer);

        }
        private void LoadCustomGAMEIniFile()
        {
            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string filePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            if (File.Exists(filePath))
            {
                GameIniContent = File.ReadAllText(filePath);
            }
        }
        private void WipeServer()
        {
            if (MessageBox.Show("Are you sure you want to wipe the server? This action cannot be undone.", "Confirm Wipe", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string savedArksPath = Path.Combine(CurrentServerConfig.ServerPath, "ShooterGame", "Saved", "SavedArks");
                    if (Directory.Exists(savedArksPath))
                    {
                        Directory.Delete(savedArksPath, true);
                        MessageBox.Show("The server has been wiped successfully.", "Wipe Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Navigate away or refresh the view as needed
                    }
                    else
                    {
                        MessageBox.Show("The server save directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while wiping the server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveCustomGAMEIniFile()
        {
            // Save the IniContent back to the file
            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string filePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            File.WriteAllText(filePath, GameIniContent);
            MessageBox.Show("GAME INI file saved successfully.");

        }
        private void LoadCustomGUSIniFile()
        {
            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string filePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
            if (File.Exists(filePath))
            {
                IniContent = File.ReadAllText(filePath);
            }
        }
        private void SaveCustomGUSIniFile()
        {
            // Save the IniContent back to the file
            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string filePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
            File.WriteAllText(filePath, IniContent);
            MessageBox.Show("GUS file saved successfully.");

        }
        private void DeleteServer()
        {
            // Show confirmation dialog and get the user input
            string userInput = ShowConfirmationDialog("Please type the server name to confirm deletion:");

            if (userInput.Equals(ServerName, StringComparison.OrdinalIgnoreCase))
            {
                // Logic to remove server from servers.json and delete the folder
                _navigationService.Navigate(typeof(DashboardPage));
                RemoveServerAndFolder(ServerName);


            }
            else
            {
                // Notify the user that the names didn't match
                MessageBox.Show("Server name does not match. Deletion cancelled.");
            }
        }

        // This method combines the deletion of the server folder and the removal from servers.json
        private void RemoveServerAndFolder(string serverName)
        {
            // Path to servers.json
            string jsonFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ark Ascended Manager",
                "servers.json"
            );

            // Load servers from JSON
            string json = File.ReadAllText(jsonFilePath);
            List<ServerConfig> servers = JsonConvert.DeserializeObject<List<ServerConfig>>(json);

            // Find the server
            ServerConfig serverConfig = servers.FirstOrDefault(s => s.ServerName.Equals(serverName, StringComparison.OrdinalIgnoreCase));
            if (serverConfig != null)
            {
                // Delete the server folder
                string serverFolderPath = serverConfig.ServerPath;
                if (Directory.Exists(serverFolderPath))
                {
                    Directory.Delete(serverFolderPath, recursive: true);
                }

                // Remove the server from the list
                servers.Remove(serverConfig);

                // Save the updated server list to JSON
                json = JsonConvert.SerializeObject(servers, Formatting.Indented);
                File.WriteAllText(jsonFilePath, json);

                
            }
            else
            {
                // Server was not found in the configuration
                MessageBox.Show("Server not found in the configuration. No action taken.");
            }
        }

        // Implement the ShowConfirmationDialog method
        private string ShowConfirmationDialog(string message)
        {
            // Prompt the user to enter the server name to confirm deletion.
            // The InputBox method is a simple way to get user input.
            string userInput = Interaction.InputBox(message, "Confirm Deletion", "", -1, -1);
            return userInput;
        }







        private void LoadServerProfile()
        {
            // Define the path where the JSON is saved
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentServerConfig.json");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON from the file
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON to a ServerConfig object
                CurrentServerConfig = JsonConvert.DeserializeObject<ServerConfig>(json);

               
            }
            LoadIniFile();
            LoadGameIniFile();
            LoadLaunchServerSettings();
        }
        public async Task InitiateServerShutdownAsync(int countdownMinutes)
        {
            Debug.WriteLine("Shutdown Clicked");
            if (CurrentServerConfig == null)
            {
                Debug.WriteLine("Current server configuration is not loaded.");
                return;
            }

            // Countdown logic
            for (int minute = countdownMinutes; minute > 0; minute--)
            {
                // Send a warning message to server
                await SendRconCommandAsync($"ServerChat Shutdown in {minute} minutes.");
                // Wait for 1 minute
                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            // Save world before shutdown
            await SendRconCommandAsync("saveworld");
            // Shutdown command
            await SendRconCommandAsync("doexit");
        }

        private async Task SendRconCommandAsync(string command)
        {
            if (CurrentServerConfig == null)
            {
                Debug.WriteLine("Current server configuration is not loaded.");
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C echo {command} | mcrcon 127.0.0.1 --password {CurrentServerConfig.AdminPassword} -p {CurrentServerConfig.RCONPort}",
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





        // Call this method when user clicks the stop button and enters the countdown time
        public async Task OnStopServerClicked()
        {
            const int defaultShutdownTimer = 10;
            await InitiateServerShutdownAsync(defaultShutdownTimer);
        }




        private string GetAppIdForMapFromJson(string mapName)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string jsonFilePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                List<ServerConfig> serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(json);

                foreach (var serverConfig in serverConfigs)
                {
                    if (serverConfig.MapName == mapName)
                    {
                        Debug.WriteLine($"App ID for {mapName} is {serverConfig.AppId}");
                        return serverConfig.AppId;
                    }
                }
            }

            Debug.WriteLine($"Map name {mapName} not found in servers.json");
            return null; // Or your default app ID
        }


        public void UpdateServerBasedOnJson()
        {
            LoadServerProfile(); // Ensure that CurrentServerConfig is loaded

            ServerConfig currentServerConfig = CurrentServerConfig; // Assuming CurrentServerConfig is a property or field

            if (currentServerConfig != null && !string.IsNullOrEmpty(currentServerConfig.AppId))
            {
                string scriptPath = Path.Combine(Path.GetTempPath(), "steamcmd_update_script.txt");
                File.WriteAllLines(scriptPath, new string[]
                {
            $"force_install_dir \"{currentServerConfig.ServerPath}\"",
            "login anonymous",
            $"app_update {currentServerConfig.AppId} validate",
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

        private Dictionary<string, string> _mapToAppId = new Dictionary<string, string>()
        {
            { "TheIsland_WP", "2430930" }
            // Add more mappings here
        };

        public Dictionary<string, string> MapToAppId
        {
            get { return _mapToAppId; }
            // Read-only if you do not plan to change it dynamically
        }
        


        public void LoadConfig(ServerConfig serverConfig)
        {
            
            CurrentServerConfig = serverConfig;

            // ... Set other properties as needed
        }
        private void StartServer()
        {
            string serverDirectory = CurrentServerConfig.ServerPath;
            string batchFilePath = Path.Combine(serverDirectory, "LaunchServer.bat");

            if (File.Exists(batchFilePath))
            {
                Process.Start(batchFilePath);
            }
            else
            {
                // Handle the case where the batch file is not found
          
            }
        }

        private void LoadLaunchServerSettings()
        {
            string serverPath = CurrentServerConfig.ServerPath;
            string batFilePath = Path.Combine(serverPath, "LaunchServer.bat");

            if (File.Exists(batFilePath))
            {
                string[] batFileLines = File.ReadAllLines(batFilePath);
                foreach (var line in batFileLines)
                {
                    if (line.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                    {
                        var splitLine = line.Substring(4).Split(new[] { '=' }, 2);
                        if (splitLine.Length == 2)
                        {
                            var key = splitLine[0].Trim();
                            var value = splitLine[1].Trim();

                            switch (key)
                            {
                                case "ServerName":
                                    ServerName = value;
                                    break;
                                case "Port":
                                    ListenPort = value;
                                    break;
                                case "RconPort":
                                    RconPort = value;
                                    break;
                                case "AdminPassword":
                                    AdminPassword = value;
                                    break;
                                case "ServerPassword":
                                    ServerPassword = value;
                                    break;
                                case "MaxPlayers":
                                    MaxPlayerCount = value;
                                    break;
                                    // Add cases for any other 'set' values you need to parse
                            }
                        }
                    }
                    else if (line.Trim().StartsWith("start ", StringComparison.OrdinalIgnoreCase))
                    {
                        UseBattleye = line.Contains("-UseBattleye");
                        ForceRespawnDinos = line.Contains("-ForceRespawnDinos");
                        PreventSpawnAnimation = line.Contains("-PreventSpawnAnimation");
                        DisableCrossPlatform = line.Contains("-ServerPlatform=PC-pconlymods");
                        Mods = ExtractModsValue(line); // Assuming you have a method to extract mods

                        // Extract Multihome and ServerIP values
                        MultihomeIP = ExtractParameterValue(line, "-multihome");
                        ServerIP = ExtractParameterValue(line, "-ServerIP");
                        PluginsEnabled = line.Contains(@"\AsaApiLoader.exe""");
                    }
                }
            }

            // Trigger UI updates if using data binding
            OnPropertyChanged(nameof(ServerName));
            OnPropertyChanged(nameof(ListenPort));
            OnPropertyChanged(nameof(RconPort));
            OnPropertyChanged(nameof(AdminPassword));
            OnPropertyChanged(nameof(ServerPassword));
            OnPropertyChanged(nameof(MaxPlayerCount));
            OnPropertyChanged(nameof(UseBattleye));
            OnPropertyChanged(nameof(ForceRespawnDinos));
            OnPropertyChanged(nameof(PreventSpawnAnimation));
            OnPropertyChanged(nameof(DisableCrossPlatform));
            OnPropertyChanged(nameof(Mods));
            OnPropertyChanged(nameof(MultihomeIP));
            OnPropertyChanged(nameof(ServerIP));
            OnPropertyChanged(nameof(PluginsEnabled));
        }

        private string ExtractParameterValue(string line, string parameterName)
        {
            var match = Regex.Match(line, $@"{parameterName}=(\S+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtractModsValue(string startCommand)
        {
            // Use a regular expression or string manipulation to extract the value after "-mods="
         
            Match match = Regex.Match(startCommand, @"-mods=([\w,]+)");
            if (match.Success)
            {
                // Replace the commas with newline characters or however you wish to separate the mods
                return match.Groups[1].Value.Replace(",", Environment.NewLine);
            }
            return string.Empty; // Return empty if not found
        }



        private void UpdateLaunchParameters()
        {
            string serverPath = CurrentServerConfig.ServerPath;
            string batFilePath = Path.Combine(serverPath, "LaunchServer.bat");
            string modsSetting = string.IsNullOrEmpty(Mods) ? "" : $"-mods={Mods}";
            string booleanSettings = ConstructBooleanSettings();

            // Determine the executable based on whether plugins are enabled
            string executable = PluginsEnabled ? "AsaApiLoader.exe" : "ArkAscendedServer.exe";

            // Construct the batch file content
            string newBatchFileContent = ConstructBatchFileContent(serverPath, executable, modsSetting, booleanSettings, MultihomeIP, ServerIP);

            // Write the updated content to the batch file
            File.WriteAllText(batFilePath, newBatchFileContent);

            Console.WriteLine("Launch parameters have been updated.");
        }

        private string ConstructBooleanSettings()
        {
            // Combine all boolean settings into a single string
            string booleanSettings = "";
            if (UseBattleye) booleanSettings += "-UseBattleye";
            if (ForceRespawnDinos) booleanSettings += "-ForceRespawnDinos";
            if (PreventSpawnAnimation) booleanSettings += "-PreventSpawnAnimation";
            if (DisableCrossPlatform) booleanSettings += "-ServerPlatform=PC-pconlymods";
            return booleanSettings;
        }

        private string ConstructBatchFileContent(string serverPath, string executable, string modsSetting, string booleanSettings, string multihomeIP, string serverIP)
        {
            string multihomeArgument = !string.IsNullOrWhiteSpace(multihomeIP) ? $" -multihome={multihomeIP}" : "";
            string serverIPArgument = !string.IsNullOrWhiteSpace(serverIP) ? $" -ServerIP={serverIP}" : "";

            // Always change the directory to the server's executable directory
            string batchFileContent = $@"
cd /d ""{serverPath}\\ShooterGame\\Binaries\\Win64""
set ServerName={ServerName}
set ServerPassword={ServerPassword}
set AdminPassword={AdminPassword}
set Port={ListenPort}
set RconPort={RconPort}
set MaxPlayers={MaxPlayerCount}

start {executable} TheIsland_WP?listen?SessionName=%ServerName%?RCONEnabled=True?ServerPassword=%ServerPassword%?Port=%Port%?RCONPort=%RconPort%?ServerAdminPassword=%AdminPassword% {booleanSettings}{modsSetting}{multihomeArgument}{serverIPArgument} -SecureSendArKPayload -ActiveEvent=none -NoTransferFromFiltering -forcerespawndinos -servergamelog -ServerRCONOutputTribeLogs -noundermeshkilling -nosteamclient -game -server -log -AutoDestroyStructures -UseBattlEye -NotifyAdminCommandsInChat
".Trim();

            // Remove spaces before dashes
            

            return batchFileContent;
        }





        private void UpdateOrAddSetting(ref List<string> lines, string key, string value)
        {
            // Find the index of the line containing the key
            int settingIndex = lines.FindIndex(line => line.StartsWith($"set {key}"));

            // Update or add the setting
            if (settingIndex != -1)
            {
                lines[settingIndex] = $"set {key}={value}";
            }
            else
            {
                lines.Add($"set {key}={value}");
            }
        }
        public string MultihomeIP { get; set; } // Bound to the Multihome IP TextBox in XAML
        public string ServerIP { get; set; } // Bound to the Server IP TextBox in XAML

        private bool _pluginsEnabled;
        public bool PluginsEnabled
        {
            get => _pluginsEnabled;
            set
            {
                _pluginsEnabled = value;
                OnPropertyChanged(nameof(PluginsEnabled));
                // Call UpdateLaunchParameters or another method if needed to immediately reflect changes
            }
        }


        private string _serverName;
        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        private string _listenPort;
        public string ListenPort
        {
            get => _listenPort;
            set => SetProperty(ref _listenPort, value);
        }

        private string _rconPort;
        public string RconPort
        {
            get => _rconPort;
            set => SetProperty(ref _rconPort, value);
        }

        private string _mods;
        public string Mods
        {
            get => _mods;
            set => SetProperty(ref _mods, value);
        }

        private string _adminPassword;
        public string AdminPassword
        {
            get => _adminPassword;
            set => SetProperty(ref _adminPassword, value);
        }

        private string _serverPassword;
        public string ServerPassword
        {
            get => _serverPassword;
            set => SetProperty(ref _serverPassword, value);
        }

        private string _maxPlayerCount;
        public string MaxPlayerCount
        {
            get => _maxPlayerCount;
            set => SetProperty(ref _maxPlayerCount, value);
        }

        private bool _useBattleye;
        public bool UseBattleye
        {
            get => _useBattleye;
            set => SetProperty(ref _useBattleye, value);
        }

        private bool _forceRespawnDinos;
        public bool ForceRespawnDinos
        {
            get => _forceRespawnDinos;
            set => SetProperty(ref _forceRespawnDinos, value);
        }

        private bool _preventSpawnAnimation;
        public bool PreventSpawnAnimation
        {
            get => _disableCrossPlatform;
            set => SetProperty(ref _disableCrossPlatform, value);
        }
        private bool _disableCrossPlatform;
        public bool DisableCrossPlatform
        {
            get => _disableCrossPlatform;
            set => SetProperty(ref _disableCrossPlatform, value);
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        private void LoadIniFile()
        {
            Console.WriteLine("INI file load has been initiated.");
            if (CurrentServerConfig == null)
            {
                Console.WriteLine("CurrentServerConfig is null.");
                return;
            }

            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
            Console.WriteLine($"INI File Path: {iniFilePath}");

            if (!File.Exists(iniFilePath))
            {
                Console.WriteLine("INI file does not exist.");
                return;
            }

            var lines = File.ReadAllLines(iniFilePath);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith(";") && line.Contains("="))
                {
                    var keyValue = line.Split(new[] { '=' }, 2);
                    var key = keyValue[0].Trim();
                    var value = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;

                    switch (key)
                    {
                        case "HarvestAmountMultiplier":
                            HarvestAmountMultiplier = value;
                            break;
                        case "AllowThirdPersonPlayer":
                            AllowThirdPersonPlayer = Convert.ToBoolean(value);
                            break;
                        case "AllowCaveBuildingPvE":
                            AllowCaveBuildingPvE = Convert.ToBoolean(value);
                            break;
                        case "AlwaysNotifyPlayerJoined":
                            AlwaysNotifyPlayerJoined = Convert.ToBoolean(value);
                            break;
                        case "AlwaysNotifyPlayerLeft":
                            AlwaysNotifyPlayerLeft = Convert.ToBoolean(value);
                            break;
                        case "AllowFlyerCarryPvE":
                            AllowFlyerCarryPvE = Convert.ToBoolean(value);
                            break;
                        case "DisableStructureDecayPvE":
                            DisableStructureDecayPvE = Convert.ToBoolean(value);
                            break;
                        case "GlobalVoiceChat":
                            GlobalVoiceChat = Convert.ToBoolean(value);
                            break;
                        case "MaxStructuresInRange":
                            MaxStructuresInRange = value;
                            break;
                        case "NoTributeDownloads":
                            NoTributeDownloads = Convert.ToBoolean(value);
                            break;
                        case "PreventDownloadSurvivors":
                            PreventDownloadSurvivors = Convert.ToBoolean(value);
                            break;
                        case "PreventDownloadItems":
                            PreventDownloadItems = Convert.ToBoolean(value);
                            break;
                        case "PreventDownloadDinos":
                            PreventDownloadDinos = Convert.ToBoolean(value);
                            break;
                        case "ProximityChat":
                            ProximityChat = Convert.ToBoolean(value);
                            break;
                        case "ResourceNoReplenishRadiusStructures":
                            ResourceNoReplenishRadiusStructures = value;
                            break;
                        case "ServerAdminPassword":
                            ServerAdminPassword = value;
                            break;
                        case "ServerCrosshair":
                            ServerCrosshair = Convert.ToBoolean(value);
                            break;
                        case "ServerForceNoHud":
                            ServerForceNoHud = Convert.ToBoolean(value);
                            break;
                        case "ServerHardcore":
                            ServerHardcore = Convert.ToBoolean(value);
                            break;
                        case "ServerPvE":
                            ServerPvE = Convert.ToBoolean(value);
                            break;
                        case "ShowMapPlayerLocation":
                            ShowMapPlayerLocation = Convert.ToBoolean(value);
                            break;
                        case "TamedDinoDamageMultiplier":
                            TamedDinoDamageMultiplier = value;
                            break;
                        case "TamedDinoResistanceMultiplier":
                            TamedDinoResistanceMultiplier = value;
                            break;
                        case "TamingSpeedMultiplier":
                            TamingSpeedMultiplier = value;
                            break;
                        case "XPMultiplier":
                            XPMultiplier = value;
                            break;
                        case "EnablePVPGamma":
                            EnablePVPGamma = Convert.ToBoolean(value);
                            break;
                        case "EnablePVEGamma":
                            EnablePVEGamma = Convert.ToBoolean(value);
                            break;
                        case "SpectatorPassword":
                            SpectatorPassword = value;
                            break;
                        case "DifficultyOffset":
                            DifficultyOffset = value;
                            break;
                        case "PvEStructureDecayDestructionPeriod":
                            PvEStructureDecayDestructionPeriod = value;
                            break;
                        case "Banlist":
                            Banlist = value;
                            break;
                        case "DisableDinoDecayPvE":
                            DisableDinoDecayPvE = Convert.ToBoolean(value);
                            break;
                        case "PvEDinoDecayPeriodMultiplier":
                            PvEDinoDecayPeriodMultiplier = value;
                            break;
                        case "AdminLogging":
                            AdminLogging = Convert.ToBoolean(value);
                            break;
                        case "MaxTamedDinos":
                            MaxTamedDinos = value;
                            break;
                        case "MaxNumbersofPlayersInTribe":
                            MaxNumbersofPlayersInTribe = value;
                            break;
                        case "BattleNumOfTribestoStartGame":
                            BattleNumOfTribestoStartGame = value;
                            break;
                        case "TimeToCollapseROD":
                            TimeToCollapseROD = value;
                            break;
                        case "BattleAutoStartGameInterval":
                            BattleAutoStartGameInterval = value;
                            break;
                        case "BattleSuddenDeathInterval":
                            BattleSuddenDeathInterval = value;
                            break;
                        case "KickIdlePlayersPeriod":
                            KickIdlePlayersPeriod = value;
                            break;
                        case "PerPlatformMaxStructuresMultiplier":
                            PerPlatformMaxStructuresMultiplier = value;
                            break;
                        case "ForceAllStructureLocking":
                            ForceAllStructureLocking = Convert.ToBoolean(value);
                            break;
                        case "AutoDestroyOldStructuresMultiplier":
                            AutoDestroyOldStructuresMultiplier = value;
                            break;
                        case "UseVSync":
                            UseVSync = Convert.ToBoolean(value);
                            break;
                        case "MaxPlatformSaddleStructureLimit":
                            MaxPlatformSaddleStructureLimit = value;
                            break;
                        case "PassiveDefensesDamageRiderlessDinos":
                            PassiveDefensesDamageRiderlessDinos = Convert.ToBoolean(value);
                            break;
                        case "AutoSavePeriodMinutes":
                            AutoSavePeriodMinutes = value;
                            break;
                        case "RCONServerGameLogBuffer":
                            RCONServerGameLogBuffer = value;
                            break;
                        case "OverrideStructurePlatformPrevention":
                            OverrideStructurePlatformPrevention = Convert.ToBoolean(value);
                            break;
                        case "PreventOfflinePvPInterval":
                            PreventOfflinePvPInterval = value;
                            break;
                        case "bPvPDinoDecay":
                            BPvPDinoDecay = Convert.ToBoolean(value);
                            break;
                        case "bPvPStructureDecay":
                            BPvPStructureDecay = Convert.ToBoolean(value);
                            break;
                        case "DisableImprintDinoBuff":
                            DisableImprintDinoBuff = Convert.ToBoolean(value);
                            break;
                        case "AllowAnyoneBabyImprintCuddle":
                            AllowAnyoneBabyImprintCuddle = Convert.ToBoolean(value);
                            break;
                        case "EnableExtraStructurePreventionVolumes":
                            EnableExtraStructurePreventionVolumes = Convert.ToBoolean(value);
                            break;
                        case "ShowFloatingDamageText":
                            ShowFloatingDamageText = Convert.ToBoolean(value);
                            break;
                        case "DestroyUnconnectedWaterPipes":
                            DestroyUnconnectedWaterPipes = Convert.ToBoolean(value);
                            break;
                        case "OverrideOfficialDifficulty":
                            OverrideOfficialDifficulty = value;
                            break;
                        case "TheMaxStructuresInRange":
                            TheMaxStructuresInRange = value;
                            break;
                        case "MinimumDinoReuploadInterval":
                            MinimumDinoReuploadInterval = Convert.ToBoolean(value);
                            break;
                        case "PvEAllowStructuresAtSupplyDrops":
                            PvEAllowStructuresAtSupplyDrops = Convert.ToBoolean(value);
                            break;
                        case "NPCNetworkStasisRangeScalePlayerCountStart":
                            NPCNetworkStasisRangeScalePlayerCountStart = value;
                            break;
                        case "NPCNetworkStasisRangeScalePlayerCountEnd":
                            NPCNetworkStasisRangeScalePlayerCountEnd = value;
                            break;
                        case "NPCNetworkStasisRangeScalePercentEnd":
                            NPCNetworkStasisRangeScalePercentEnd = value;
                            break;
                        case "MaxPersonalTamedDinos":
                            MaxPersonalTamedDinos = value;
                            break;
                        case "AutoDestroyDecayedDinos":
                            AutoDestroyDecayedDinos = Convert.ToBoolean(value);
                            break;
                        case "ClampItemSpoilingTimes":
                            ClampItemSpoilingTimes = Convert.ToBoolean(value);
                            break;
                        case "UseOptimizedHarvestingHealth":
                            UseOptimizedHarvestingHealth = Convert.ToBoolean(value);
                            break;
                        case "AllowCrateSpawnsOnTopOfStructures":
                            AllowCrateSpawnsOnTopOfStructures = Convert.ToBoolean(value);
                            break;
                        case "ForceFlyerExplosives":
                            ForceFlyerExplosives = Convert.ToBoolean(value);
                            break;
                        case "AllowFlyingStaminaRecovery":
                            AllowFlyingStaminaRecovery = Convert.ToBoolean(value);
                            break;
                        case "OxygenSwimSpeedStatMultiplier":
                            OxygenSwimSpeedStatMultiplier = value;
                            break;
                        case "bPvEDisableFriendlyFire":
                            BPvEDisableFriendlyFire = Convert.ToBoolean(value);
                            break;
                        case "ServerAutoForceRespawnWildDinosInterval":
                            ServerAutoForceRespawnWildDinosInterval = value;
                            break;
                        case "DisableWeatherFog":
                            DisableWeatherFog = Convert.ToBoolean(value);
                            break;
                        case "RandomSupplyCratePoints":
                            RandomSupplyCratePoints = Convert.ToBoolean(value);
                            break;
                        case "CrossARKAllowForeignDinoDownloads":
                            CrossARKAllowForeignDinoDownloads = Convert.ToBoolean(value);
                            break;
                        case "PersonalTamedDinosSaddleStructureCost":
                            PersonalTamedDinosSaddleStructureCost = value;
                            break;
                        case "StructurePreventResourceRadiusMultiplier":
                            StructurePreventResourceRadiusMultiplier = value;
                            break;
                        case "TribeNameChangeCooldown":
                            TribeNameChangeCooldown = value;
                            break;
                        case "PlatformSaddleBuildAreaBoundsMultiplier":
                            PlatformSaddleBuildAreaBoundsMultiplier = value;
                            break;
                        case "AlwaysAllowStructurePickup":
                            AlwaysAllowStructurePickup = Convert.ToBoolean(value);
                            break;
                        case "StructurePickupTimeAfterPlacement":
                            StructurePickupTimeAfterPlacement = value;
                            break;
                        case "StructurePickupHoldDuration":
                            StructurePickupHoldDuration = value;
                            break;
                        case "AllowHideDamageSourceFromLogs":
                            AllowHideDamageSourceFromLogs = Convert.ToBoolean(value);
                            break;
                        case "RaidDinoCharacterFoodDrainMultiplier":
                            RaidDinoCharacterFoodDrainMultiplier = value;
                            break;
                        case "ItemStackSizeMultiplier":
                            ItemStackSizeMultiplier = value;
                            break;
                        case "AllowHitMarkers":
                            AllowHitMarkers = Convert.ToBoolean(value);
                            break;
                        case "AllowMultipleAttachedC4":
                            AllowMultipleAttachedC4 = Convert.ToBoolean(value);
                            break;





                    }
                }
            }
        }
        
        private void SaveGameUserSettings()
        {
            string serverPath = CurrentServerConfig.ServerPath;
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");

            // Read all lines
            var lines = File.ReadAllLines(iniFilePath).ToList();

            // Update specific lines
            UpdateLine(ref lines, "HarvestAmountMultiplier", HarvestAmountMultiplier);
            UpdateLine(ref lines, "AllowThirdPersonPlayer", AllowThirdPersonPlayer.ToString());
            UpdateLine(ref lines, "AllowCaveBuildingPvE", AllowCaveBuildingPvE.ToString());
            UpdateLine(ref lines, "AlwaysNotifyPlayerJoined", AlwaysNotifyPlayerJoined.ToString());
            UpdateLine(ref lines, "AlwaysNotifyPlayerLeft", AlwaysNotifyPlayerLeft.ToString());
            UpdateLine(ref lines, "AllowFlyerCarryPvE", AllowFlyerCarryPvE.ToString());
            UpdateLine(ref lines, "DisableStructureDecayPvE", DisableStructureDecayPvE.ToString());
            UpdateLine(ref lines, "GlobalVoiceChat", GlobalVoiceChat.ToString());
            UpdateLine(ref lines, "MaxStructuresInRange", MaxStructuresInRange);
            UpdateLine(ref lines, "NoTributeDownloads", NoTributeDownloads.ToString());
            UpdateLine(ref lines, "PreventDownloadSurvivors", PreventDownloadSurvivors.ToString());
            UpdateLine(ref lines, "PreventDownloadItems", PreventDownloadItems.ToString());
            UpdateLine(ref lines, "PreventDownloadDinos", PreventDownloadDinos.ToString());
            UpdateLine(ref lines, "ProximityChat", ProximityChat.ToString());
            UpdateLine(ref lines, "ResourceNoReplenishRadiusStructures", ResourceNoReplenishRadiusStructures);
            UpdateLine(ref lines, "ServerAdminPassword", ServerAdminPassword);
            UpdateLine(ref lines, "ServerCrosshair", ServerCrosshair.ToString());
            UpdateLine(ref lines, "ServerForceNoHud", ServerForceNoHud.ToString());
            UpdateLine(ref lines, "ServerHardcore", ServerHardcore.ToString());
            UpdateLine(ref lines, "ServerPvE", ServerPvE.ToString());
            UpdateLine(ref lines, "ShowMapPlayerLocation", ShowMapPlayerLocation.ToString());
            UpdateLine(ref lines, "TamedDinoDamageMultiplier", TamedDinoDamageMultiplier);
            UpdateLine(ref lines, "TamedDinoResistanceMultiplier", TamedDinoResistanceMultiplier);
            UpdateLine(ref lines, "TamingSpeedMultiplier", TamingSpeedMultiplier);
            UpdateLine(ref lines, "XPMultiplier", XPMultiplier);
            UpdateLine(ref lines, "EnablePVPGamma", EnablePVPGamma.ToString());
            UpdateLine(ref lines, "EnablePVEGamma", EnablePVEGamma.ToString());
            UpdateLine(ref lines, "AllowFlyingStaminaRecovery", AllowFlyingStaminaRecovery.ToString());
            UpdateLine(ref lines, "SpectatorPassword", SpectatorPassword);
            UpdateLine(ref lines, "DifficultyOffset", DifficultyOffset);
            UpdateLine(ref lines, "PvEStructureDecayDestructionPeriod", PvEStructureDecayDestructionPeriod);
            UpdateLine(ref lines, "Banlist", Banlist);
            UpdateLine(ref lines, "ServerAutoForceRespawnWildDinosInterval", ServerAutoForceRespawnWildDinosInterval);
            UpdateLine(ref lines, "DisableDinoDecayPvE", DisableDinoDecayPvE.ToString());
            UpdateLine(ref lines, "PvEDinoDecayPeriodMultiplier", PvEDinoDecayPeriodMultiplier);
            UpdateLine(ref lines, "AdminLogging", AdminLogging.ToString());
            UpdateLine(ref lines, "MaxTamedDinos", MaxTamedDinos);
            UpdateLine(ref lines, "MaxNumbersofPlayersInTribe", MaxNumbersofPlayersInTribe);
            UpdateLine(ref lines, "BattleNumOfTribestoStartGame", BattleNumOfTribestoStartGame);
            UpdateLine(ref lines, "TimeToCollapseROD", TimeToCollapseROD);
            UpdateLine(ref lines, "BattleAutoStartGameInterval", BattleAutoStartGameInterval);
            UpdateLine(ref lines, "BattleSuddenDeathInterval", BattleSuddenDeathInterval);
            UpdateLine(ref lines, "KickIdlePlayersPeriod", KickIdlePlayersPeriod);
            UpdateLine(ref lines, "PerPlatformMaxStructuresMultiplier", PerPlatformMaxStructuresMultiplier);
            UpdateLine(ref lines, "ForceAllStructureLocking", ForceAllStructureLocking.ToString());
            UpdateLine(ref lines, "AutoDestroyOldStructuresMultiplier", AutoDestroyOldStructuresMultiplier);
            UpdateLine(ref lines, "UseVSync", UseVSync.ToString());
            UpdateLine(ref lines, "MaxPlatformSaddleStructureLimit", MaxPlatformSaddleStructureLimit);
            UpdateLine(ref lines, "PassiveDefensesDamageRiderlessDinos", PassiveDefensesDamageRiderlessDinos.ToString());
            UpdateLine(ref lines, "bPvEDisableFriendlyFire", BPvEDisableFriendlyFire.ToString());
            UpdateLine(ref lines, "AutoSavePeriodMinutes", AutoSavePeriodMinutes);
            UpdateLine(ref lines, "RCONServerGameLogBuffer", RCONServerGameLogBuffer);
            UpdateLine(ref lines, "OverrideStructurePlatformPrevention", OverrideStructurePlatformPrevention.ToString());
            UpdateLine(ref lines, "bPvPDinoDecay", BPvPDinoDecay.ToString());
            UpdateLine(ref lines, "bPvPStructureDecay", BPvPStructureDecay.ToString());
            UpdateLine(ref lines, "DisableImprintDinoBuff", DisableImprintDinoBuff.ToString());
            UpdateLine(ref lines, "AllowAnyoneBabyImprintCuddle", AllowAnyoneBabyImprintCuddle.ToString());
            UpdateLine(ref lines, "EnableExtraStructurePreventionVolumes", EnableExtraStructurePreventionVolumes.ToString());
            UpdateLine(ref lines, "ShowFloatingDamageText", ShowFloatingDamageText.ToString());
            UpdateLine(ref lines, "DestroyUnconnectedWaterPipes", DestroyUnconnectedWaterPipes.ToString());
            UpdateLine(ref lines, "OverrideOfficialDifficulty", OverrideOfficialDifficulty);
            UpdateLine(ref lines, "TheMaxStructuresInRange", TheMaxStructuresInRange);
            UpdateLine(ref lines, "MinimumDinoReuploadInterval", MinimumDinoReuploadInterval.ToString());
            UpdateLine(ref lines, "PvEAllowStructuresAtSupplyDrops", PvEAllowStructuresAtSupplyDrops.ToString());
            UpdateLine(ref lines, "NPCNetworkStasisRangeScalePlayerCountStart", NPCNetworkStasisRangeScalePlayerCountStart);
            UpdateLine(ref lines, "NPCNetworkStasisRangeScalePlayerCountEnd", NPCNetworkStasisRangeScalePlayerCountEnd);
            UpdateLine(ref lines, "NPCNetworkStasisRangeScalePercentEnd", NPCNetworkStasisRangeScalePercentEnd);
            UpdateLine(ref lines, "MaxPersonalTamedDinos", MaxPersonalTamedDinos);
            UpdateLine(ref lines, "PreventOfflinePvPInterval", PreventOfflinePvPInterval);
            UpdateLine(ref lines, "AutoDestroyDecayedDinos", AutoDestroyDecayedDinos.ToString());
            UpdateLine(ref lines, "ClampItemSpoilingTimes", ClampItemSpoilingTimes.ToString());
            UpdateLine(ref lines, "UseOptimizedHarvestingHealth", UseOptimizedHarvestingHealth.ToString());
            UpdateLine(ref lines, "AllowCrateSpawnsOnTopOfStructures", AllowCrateSpawnsOnTopOfStructures.ToString());
            UpdateLine(ref lines, "ForceFlyerExplosives", ForceFlyerExplosives.ToString());
            UpdateLine(ref lines, "AllowMultipleAttachedC4", AllowMultipleAttachedC4.ToString());
            UpdateLine(ref lines, "DisableWeatherFog", DisableWeatherFog.ToString());
            UpdateLine(ref lines, "RandomSupplyCratePoints", RandomSupplyCratePoints.ToString());
            UpdateLine(ref lines, "CrossARKAllowForeignDinoDownloads", CrossARKAllowForeignDinoDownloads.ToString());  
            UpdateLine(ref lines, "AlwaysAllowStructurePickup", AlwaysAllowStructurePickup.ToString());
            UpdateLine(ref lines, "AllowHideDamageSourceFromLogs", AllowHideDamageSourceFromLogs.ToString());
            UpdateLine(ref lines, "AllowHitMarkers", AllowHitMarkers.ToString());
            UpdateLine(ref lines, "OxygenSwimSpeedStatMultiplier", OxygenSwimSpeedStatMultiplier);
            UpdateLine(ref lines, "PersonalTamedDinosSaddleStructureCost", PersonalTamedDinosSaddleStructureCost);
            UpdateLine(ref lines, "StructurePreventResourceRadiusMultiplier", StructurePreventResourceRadiusMultiplier);
            UpdateLine(ref lines, "TribeNameChangeCooldown", TribeNameChangeCooldown);
            UpdateLine(ref lines, "PlatformSaddleBuildAreaBoundsMultiplier", PlatformSaddleBuildAreaBoundsMultiplier);
            UpdateLine(ref lines, "StructurePickupHoldDuration", StructurePickupHoldDuration);
            UpdateLine(ref lines, "StructurePickupTimeAfterPlacement", StructurePickupTimeAfterPlacement);
            UpdateLine(ref lines, "RaidDinoCharacterFoodDrainMultiplier", RaidDinoCharacterFoodDrainMultiplier);

            // ... Repeat for other properties ...

           // Write the updated lines back to the file
           File.WriteAllLines(iniFilePath, lines);
        }

        private void UpdateLine(ref List<string> lines, string key, string newValue)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(key))
                {
                    lines[i] = $"{key}={newValue}";
                    return;
                }
            }

            // If the key does not exist in the file, add it
            lines.Add($"{key}={newValue}");
        }



        private string _harvestAmountMultiplier;
        public string HarvestAmountMultiplier
        {
            get { return _harvestAmountMultiplier; }
            set
            {
                _harvestAmountMultiplier = value;
                OnPropertyChanged(nameof(HarvestAmountMultiplier)); // Notify the UI of the change
            }
        }
        private bool _allowThirdPersonPlayer;
        public bool AllowThirdPersonPlayer
        {
            get { return _allowThirdPersonPlayer; }
            set
            {
                _allowThirdPersonPlayer = value;
                OnPropertyChanged(nameof(AllowThirdPersonPlayer)); // Notify the UI of the change
            }
        }

        private bool _allowCaveBuildingPvE;
        public bool AllowCaveBuildingPvE
        {
            get { return _allowCaveBuildingPvE; }
            set
            {
                _allowCaveBuildingPvE = value;
                OnPropertyChanged(nameof(AllowCaveBuildingPvE)); // Notify the UI of the change
            }
        }

        private bool _alwaysNotifyPlayerJoined;
        public bool AlwaysNotifyPlayerJoined
        {
            get { return _alwaysNotifyPlayerJoined; }
            set
            {
                _alwaysNotifyPlayerJoined = value;
                OnPropertyChanged(nameof(AlwaysNotifyPlayerJoined)); // Notify the UI of the change
            }
        }

        private bool _alwaysNotifyPlayerLeft;
        public bool AlwaysNotifyPlayerLeft
        {
            get { return _alwaysNotifyPlayerLeft; }
            set
            {
                _alwaysNotifyPlayerLeft = value;
                OnPropertyChanged(nameof(AlwaysNotifyPlayerLeft)); // Notify the UI of the change
            }
        }

        private bool _allowFlyerCarryPvE;
        public bool AllowFlyerCarryPvE
        {
            get { return _allowFlyerCarryPvE; }
            set
            {
                _allowFlyerCarryPvE = value;
                OnPropertyChanged(nameof(AllowFlyerCarryPvE)); // Notify the UI of the change
            }
        }

        private bool _disableStructureDecayPvE;
        public bool DisableStructureDecayPvE
        {
            get { return _disableStructureDecayPvE; }
            set
            {
                _disableStructureDecayPvE = value;
                OnPropertyChanged(nameof(DisableStructureDecayPvE)); // Notify the UI of the change
            }
        }

        private bool _globalVoiceChat;
        public bool GlobalVoiceChat
        {
            get { return _globalVoiceChat; }
            set
            {
                _globalVoiceChat = value;
                OnPropertyChanged(nameof(GlobalVoiceChat)); // Notify the UI of the change
            }
        }

        private string _maxStructuresInRange;
        public string MaxStructuresInRange
        {
            get { return _maxStructuresInRange; }
            set
            {
                _maxStructuresInRange = value;
                OnPropertyChanged(nameof(MaxStructuresInRange)); // Notify the UI of the change
            }
        }

        private bool _noTributeDownloads;
        public bool NoTributeDownloads
        {
            get { return _noTributeDownloads; }
            set
            {
                _noTributeDownloads = value;
                OnPropertyChanged(nameof(NoTributeDownloads)); // Notify the UI of the change
            }
        }

        private bool _preventDownloadSurvivors;
        public bool PreventDownloadSurvivors
        {
            get { return _preventDownloadSurvivors; }
            set
            {
                _preventDownloadSurvivors = value;
                OnPropertyChanged(nameof(PreventDownloadSurvivors)); // Notify the UI of the change
            }
        }

        private bool _preventDownloadItems;
        public bool PreventDownloadItems
        {
            get { return _preventDownloadItems; }
            set
            {
                _preventDownloadItems = value;
                OnPropertyChanged(nameof(PreventDownloadItems)); // Notify the UI of the change
            }
        }

        private bool _preventDownloadDinos;
        public bool PreventDownloadDinos
        {
            get { return _preventDownloadDinos; }
            set
            {
                _preventDownloadDinos = value;
                OnPropertyChanged(nameof(PreventDownloadDinos)); // Notify the UI of the change
            }
        }

        private bool _proximityChat;
        public bool ProximityChat
        {
            get { return _proximityChat; }
            set
            {
                _proximityChat = value;
                OnPropertyChanged(nameof(ProximityChat)); // Notify the UI of the change
            }
        }

        private string _resourceNoReplenishRadiusStructures;
        public string ResourceNoReplenishRadiusStructures
        {
            get { return _resourceNoReplenishRadiusStructures; }
            set
            {
                _resourceNoReplenishRadiusStructures = value;
                OnPropertyChanged(nameof(ResourceNoReplenishRadiusStructures)); // Notify the UI of the change
            }
        }

        private string _serverAdminPassword;
        public string ServerAdminPassword
        {
            get { return _serverAdminPassword; }
            set
            {
                _serverAdminPassword = value;
                OnPropertyChanged(nameof(ServerAdminPassword)); // Notify the UI of the change
            }
        }

        private bool _serverCrosshair;
        public bool ServerCrosshair
        {
            get { return _serverCrosshair; }
            set
            {
                _serverCrosshair = value;
                OnPropertyChanged(nameof(ServerCrosshair)); // Notify the UI of the change
            }
        }

        private bool _serverForceNoHud;
        public bool ServerForceNoHud
        {
            get { return _serverForceNoHud; }
            set
            {
                _serverForceNoHud = value;
                OnPropertyChanged(nameof(ServerForceNoHud)); // Notify the UI of the change
            }
        }

        private bool _serverHardcore;
        public bool ServerHardcore
        {
            get { return _serverHardcore; }
            set
            {
                _serverHardcore = value;
                OnPropertyChanged(nameof(ServerHardcore)); // Notify the UI of the change
            }
        }


        private bool _serverPvE;
        public bool ServerPvE
        {
            get { return _serverPvE; }
            set
            {
                _serverPvE = value;
                OnPropertyChanged(nameof(ServerPvE)); // Notify the UI of the change
            }
        }

        private bool _showMapPlayerLocation;
        public bool ShowMapPlayerLocation
        {
            get { return _showMapPlayerLocation; }
            set
            {
                _showMapPlayerLocation = value;
                OnPropertyChanged(nameof(ShowMapPlayerLocation)); // Notify the UI of the change
            }
        }

        private string _tamedDinoDamageMultiplier;
        public string TamedDinoDamageMultiplier
        {
            get { return _tamedDinoDamageMultiplier; }
            set
            {
                _tamedDinoDamageMultiplier = value;
                OnPropertyChanged(nameof(TamedDinoDamageMultiplier)); // Notify the UI of the change
            }
        }

        private string _tamedDinoResistanceMultiplier;
        public string TamedDinoResistanceMultiplier
        {
            get { return _tamedDinoResistanceMultiplier; }
            set
            {
                _tamedDinoResistanceMultiplier = value;
                OnPropertyChanged(nameof(TamedDinoResistanceMultiplier)); // Notify the UI of the change
            }
        }

        private string _tamingSpeedMultiplier;
        public string TamingSpeedMultiplier
        {
            get { return _tamingSpeedMultiplier; }
            set
            {
                _tamingSpeedMultiplier = value;
                OnPropertyChanged(nameof(TamingSpeedMultiplier)); // Notify the UI of the change
            }
        }

        private string _xpMultiplier;
        public string XPMultiplier
        {
            get { return _xpMultiplier; }
            set
            {
                _xpMultiplier = value;
                OnPropertyChanged(nameof(XPMultiplier)); // Notify the UI of the change
            }
        }

        private bool _enablePVPGamma;
        public bool EnablePVPGamma
        {
            get { return _enablePVPGamma; }
            set
            {
                _enablePVPGamma = value;
                OnPropertyChanged(nameof(EnablePVPGamma)); // Notify the UI of the change
            }
        }

        private bool _enablePVEGamma;
        public bool EnablePVEGamma
        {
            get { return _enablePVEGamma; }
            set
            {
                _enablePVEGamma = value;
                OnPropertyChanged(nameof(EnablePVEGamma)); // Notify the UI of the change
            }
        }

        private string _spectatorPassword;
        public string SpectatorPassword
        {
            get { return _spectatorPassword; }
            set
            {
                _spectatorPassword = value;
                OnPropertyChanged(nameof(SpectatorPassword)); // Notify the UI of the change
            }
        }

        private string _difficultyOffset;
        public string DifficultyOffset
        {
            get { return _difficultyOffset; }
            set
            {
                _difficultyOffset = value;
                OnPropertyChanged(nameof(DifficultyOffset)); // Notify the UI of the change
            }
        }

        private string _pveStructureDecayDestructionPeriod;
        public string PvEStructureDecayDestructionPeriod
        {
            get { return _pveStructureDecayDestructionPeriod; }
            set
            {
                _pveStructureDecayDestructionPeriod = value;
                OnPropertyChanged(nameof(PvEStructureDecayDestructionPeriod)); // Notify the UI of the change
            }
        }

        private string _banlist;
        public string Banlist
        {
            get { return _banlist; }
            set
            {
                _banlist = value;
                OnPropertyChanged(nameof(Banlist)); // Notify the UI of the change
            }
        }



        private bool _disableDinoDecayPvE;
        public bool DisableDinoDecayPvE
        {
            get { return _disableDinoDecayPvE; }
            set
            {
                _disableDinoDecayPvE = value;
                OnPropertyChanged(nameof(DisableDinoDecayPvE)); // Notify the UI of the change
            }
        }

        private string _pveDinoDecayPeriodMultiplier;
        public string PvEDinoDecayPeriodMultiplier
        {
            get { return _pveDinoDecayPeriodMultiplier; }
            set
            {
                _pveDinoDecayPeriodMultiplier = value;
                OnPropertyChanged(nameof(PvEDinoDecayPeriodMultiplier)); // Notify the UI of the change
            }
        }

        private bool _adminLogging;
        public bool AdminLogging
        {
            get { return _adminLogging; }
            set
            {
                _adminLogging = value;
                OnPropertyChanged(nameof(AdminLogging)); // Notify the UI of the change
            }
        }

        private string _maxTamedDinos;
        public string MaxTamedDinos
        {
            get { return _maxTamedDinos; }
            set
            {
                _maxTamedDinos = value;
                OnPropertyChanged(nameof(MaxTamedDinos)); // Notify the UI of the change
            }
        }

        private string _maxNumbersofPlayersInTribe;
        public string MaxNumbersofPlayersInTribe
        {
            get { return _maxNumbersofPlayersInTribe; }
            set
            {
                _maxNumbersofPlayersInTribe = value;
                OnPropertyChanged(nameof(MaxNumbersofPlayersInTribe)); // Notify the UI of the change
            }
        }

        private string _battleNumOfTribestoStartGame;
        public string BattleNumOfTribestoStartGame
        {
            get { return _battleNumOfTribestoStartGame; }
            set
            {
                _battleNumOfTribestoStartGame = value;
                OnPropertyChanged(nameof(BattleNumOfTribestoStartGame)); // Notify the UI of the change
            }
        }

        private string _timeToCollapseROD;
        public string TimeToCollapseROD
        {
            get { return _timeToCollapseROD; }
            set
            {
                _timeToCollapseROD = value;
                OnPropertyChanged(nameof(TimeToCollapseROD)); // Notify the UI of the change
            }
        }

        private string _battleAutoStartGameInterval;
        public string BattleAutoStartGameInterval
        {
            get { return _battleAutoStartGameInterval; }
            set
            {
                _battleAutoStartGameInterval = value;
                OnPropertyChanged(nameof(BattleAutoStartGameInterval)); // Notify the UI of the change
            }
        }

        private string _battleSuddenDeathInterval;
        public string BattleSuddenDeathInterval
        {
            get { return _battleSuddenDeathInterval; }
            set
            {
                _battleSuddenDeathInterval = value;
                OnPropertyChanged(nameof(BattleSuddenDeathInterval)); // Notify the UI of the change
            }
        }

        private string _kickIdlePlayersPeriod;
        public string KickIdlePlayersPeriod
        {
            get { return _kickIdlePlayersPeriod; }
            set
            {
                _kickIdlePlayersPeriod = value;
                OnPropertyChanged(nameof(KickIdlePlayersPeriod)); // Notify the UI of the change
            }
        }

        private string _perPlatformMaxStructuresMultiplier;
        public string PerPlatformMaxStructuresMultiplier
        {
            get { return _perPlatformMaxStructuresMultiplier; }
            set
            {
                _perPlatformMaxStructuresMultiplier = value;
                OnPropertyChanged(nameof(PerPlatformMaxStructuresMultiplier)); // Notify the UI of the change
            }
        }

        private bool _forceAllStructureLocking;
        public bool ForceAllStructureLocking
        {
            get { return _forceAllStructureLocking; }
            set
            {
                _forceAllStructureLocking = value;
                OnPropertyChanged(nameof(ForceAllStructureLocking)); // Notify the UI of the change
            }
        }
        private string _autoDestroyOldStructuresMultiplier;
        public string AutoDestroyOldStructuresMultiplier
        {
            get { return _autoDestroyOldStructuresMultiplier; }
            set
            {
                _autoDestroyOldStructuresMultiplier = value;
                OnPropertyChanged(nameof(AutoDestroyOldStructuresMultiplier)); // Notify the UI of the change
            }
        }

        private bool _useVSync;
        public bool UseVSync
        {
            get { return _useVSync; }
            set
            {
                _useVSync = value;
                OnPropertyChanged(nameof(UseVSync)); // Notify the UI of the change
            }
        }

        private string _maxPlatformSaddleStructureLimit;
        public string MaxPlatformSaddleStructureLimit
        {
            get { return _maxPlatformSaddleStructureLimit; }
            set
            {
                _maxPlatformSaddleStructureLimit = value;
                OnPropertyChanged(nameof(MaxPlatformSaddleStructureLimit)); // Notify the UI of the change
            }
        }

        private bool _passiveDefensesDamageRiderlessDinos;
        public bool PassiveDefensesDamageRiderlessDinos
        {
            get { return _passiveDefensesDamageRiderlessDinos; }
            set
            {
                _passiveDefensesDamageRiderlessDinos = value;
                OnPropertyChanged(nameof(PassiveDefensesDamageRiderlessDinos)); // Notify the UI of the change
            }
        }

        private string _autoSavePeriodMinutes;
        public string AutoSavePeriodMinutes
        {
            get { return _autoSavePeriodMinutes; }
            set
            {
                _autoSavePeriodMinutes = value;
                OnPropertyChanged(nameof(AutoSavePeriodMinutes)); // Notify the UI of the change
            }
        }

        private string _rconServerGameLogBuffer;
        public string RCONServerGameLogBuffer
        {
            get { return _rconServerGameLogBuffer; }
            set
            {
                _rconServerGameLogBuffer = value;
                OnPropertyChanged(nameof(RCONServerGameLogBuffer)); // Notify the UI of the change
            }
        }

        private bool _overrideStructurePlatformPrevention;
        public bool OverrideStructurePlatformPrevention
        {
            get { return _overrideStructurePlatformPrevention; }
            set
            {
                _overrideStructurePlatformPrevention = value;
                OnPropertyChanged(nameof(OverrideStructurePlatformPrevention)); // Notify the UI of the change
            }
        }

        private string _preventOfflinePvPInterval;
        public string PreventOfflinePvPInterval
        {
            get { return _preventOfflinePvPInterval; }
            set
            {
                _preventOfflinePvPInterval = value;
                OnPropertyChanged(nameof(PreventOfflinePvPInterval)); // Notify the UI of the change
            }
        }

        private bool _bPvPDinoDecay;
        public bool BPvPDinoDecay
        {
            get { return _bPvPDinoDecay; }
            set
            {
                _bPvPDinoDecay = value;
                OnPropertyChanged(nameof(BPvPDinoDecay)); // Notify the UI of the change
            }
        }

        private bool _bPvPStructureDecay;
        public bool BPvPStructureDecay
        {
            get { return _bPvPStructureDecay; }
            set
            {
                _bPvPStructureDecay = value;
                OnPropertyChanged(nameof(BPvPStructureDecay)); // Notify the UI of the change
            }
        }

        private bool _disableImprintDinoBuff;
        public bool DisableImprintDinoBuff
        {
            get { return _disableImprintDinoBuff; }
            set
            {
                _disableImprintDinoBuff = value;
                OnPropertyChanged(nameof(DisableImprintDinoBuff)); // Notify the UI of the change
            }
        }

        private bool _allowAnyoneBabyImprintCuddle;
        public bool AllowAnyoneBabyImprintCuddle
        {
            get { return _allowAnyoneBabyImprintCuddle; }
            set
            {
                _allowAnyoneBabyImprintCuddle = value;
                OnPropertyChanged(nameof(AllowAnyoneBabyImprintCuddle)); // Notify the UI of the change
            }
        }

        private bool _enableExtraStructurePreventionVolumes;
        public bool EnableExtraStructurePreventionVolumes
        {
            get { return _enableExtraStructurePreventionVolumes; }
            set
            {
                _enableExtraStructurePreventionVolumes = value;
                OnPropertyChanged(nameof(EnableExtraStructurePreventionVolumes)); // Notify the UI of the change
            }
        }

        private bool _showFloatingDamageText;
        public bool ShowFloatingDamageText
        {
            get { return _showFloatingDamageText; }
            set
            {
                _showFloatingDamageText = value;
                OnPropertyChanged(nameof(ShowFloatingDamageText)); // Notify the UI of the change
            }
        }

        private bool _destroyUnconnectedWaterPipes;
        public bool DestroyUnconnectedWaterPipes
        {
            get { return _destroyUnconnectedWaterPipes; }
            set
            {
                _destroyUnconnectedWaterPipes = value;
                OnPropertyChanged(nameof(DestroyUnconnectedWaterPipes)); // Notify the UI of the change
            }
        }

        private string _overrideOfficialDifficulty;
        public string OverrideOfficialDifficulty
        {
            get { return _overrideOfficialDifficulty; }
            set
            {
                _overrideOfficialDifficulty = value;
                OnPropertyChanged(nameof(OverrideOfficialDifficulty)); // Notify the UI of the change
            }
        }

        private string _theMaxStructuresInRange;
        public string TheMaxStructuresInRange
        {
            get { return _theMaxStructuresInRange; }
            set
            {
                _theMaxStructuresInRange = value;
                OnPropertyChanged(nameof(TheMaxStructuresInRange)); // Notify the UI of the change
            }
        }

        private bool _minimumDinoReuploadInterval;
        public bool MinimumDinoReuploadInterval
        {
            get { return _minimumDinoReuploadInterval; }
            set
            {
                _minimumDinoReuploadInterval = value;
                OnPropertyChanged(nameof(MinimumDinoReuploadInterval)); // Notify the UI of the change
            }
        }

        private bool _pvEAllowStructuresAtSupplyDrops;
        public bool PvEAllowStructuresAtSupplyDrops
        {
            get { return _pvEAllowStructuresAtSupplyDrops; }
            set
            {
                _pvEAllowStructuresAtSupplyDrops = value;
                OnPropertyChanged(nameof(PvEAllowStructuresAtSupplyDrops)); // Notify the UI of the change
            }
        }

        private string _nPCNetworkStasisRangeScalePlayerCountStart;
        public string NPCNetworkStasisRangeScalePlayerCountStart
        {
            get { return _nPCNetworkStasisRangeScalePlayerCountStart; }
            set
            {
                _nPCNetworkStasisRangeScalePlayerCountStart = value;
                OnPropertyChanged(nameof(NPCNetworkStasisRangeScalePlayerCountStart)); // Notify the UI of the change
            }
        }

        private string _nPCNetworkStasisRangeScalePlayerCountEnd;
        public string NPCNetworkStasisRangeScalePlayerCountEnd
        {
            get { return _nPCNetworkStasisRangeScalePlayerCountEnd; }
            set
            {
                _nPCNetworkStasisRangeScalePlayerCountEnd = value;
                OnPropertyChanged(nameof(NPCNetworkStasisRangeScalePlayerCountEnd)); // Notify the UI of the change
            }
        }

        private string _nPCNetworkStasisRangeScalePercentEnd;
        public string NPCNetworkStasisRangeScalePercentEnd
        {
            get { return _nPCNetworkStasisRangeScalePercentEnd; }
            set
            {
                _nPCNetworkStasisRangeScalePercentEnd = value;
                OnPropertyChanged(nameof(NPCNetworkStasisRangeScalePercentEnd)); // Notify the UI of the change
            }
        }

        private string _maxPersonalTamedDinos;
        public string MaxPersonalTamedDinos
        {
            get { return _maxPersonalTamedDinos; }
            set
            {
                _maxPersonalTamedDinos = value;
                OnPropertyChanged(nameof(MaxPersonalTamedDinos)); // Notify the UI of the change
            }
        }

        private bool _autoDestroyDecayedDinos;
        public bool AutoDestroyDecayedDinos
        {
            get { return _autoDestroyDecayedDinos; }
            set
            {
                _autoDestroyDecayedDinos = value;
                OnPropertyChanged(nameof(AutoDestroyDecayedDinos)); // Notify the UI of the change
            }
        }

        private bool _clampItemSpoilingTimes;
        public bool ClampItemSpoilingTimes
        {
            get { return _clampItemSpoilingTimes; }
            set
            {
                _clampItemSpoilingTimes = value;
                OnPropertyChanged(nameof(ClampItemSpoilingTimes)); // Notify the UI of the change
            }
        }

        private bool _useOptimizedHarvestingHealth;
        public bool UseOptimizedHarvestingHealth
        {
            get { return _useOptimizedHarvestingHealth; }
            set
            {
                _useOptimizedHarvestingHealth = value;
                OnPropertyChanged(nameof(UseOptimizedHarvestingHealth)); // Notify the UI of the change
            }
        }

        private bool _allowCrateSpawnsOnTopOfStructures;
        public bool AllowCrateSpawnsOnTopOfStructures
        {
            get { return _allowCrateSpawnsOnTopOfStructures; }
            set
            {
                _allowCrateSpawnsOnTopOfStructures = value;
                OnPropertyChanged(nameof(AllowCrateSpawnsOnTopOfStructures)); // Notify the UI of the change
            }
        }

        private bool _forceFlyerExplosives;
        public bool ForceFlyerExplosives
        {
            get { return _forceFlyerExplosives; }
            set
            {
                _forceFlyerExplosives = value;
                OnPropertyChanged(nameof(ForceFlyerExplosives)); // Notify the UI of the change
            }
        }



        private bool _allowFlyingStaminaRecovery;
        public bool AllowFlyingStaminaRecovery
        {
            get { return _allowFlyingStaminaRecovery; }
            set
            {
                _allowFlyingStaminaRecovery = value;
                OnPropertyChanged(nameof(AllowFlyingStaminaRecovery)); // Notify the UI of the change
            }
        }

        private string _oxygenSwimSpeedStatMultiplier;
        public string OxygenSwimSpeedStatMultiplier
        {
            get { return _oxygenSwimSpeedStatMultiplier; }
            set
            {
                _oxygenSwimSpeedStatMultiplier = value;
                OnPropertyChanged(nameof(OxygenSwimSpeedStatMultiplier)); // Notify the UI of the change
            }
        }

        private bool _bPvEDisableFriendlyFire;
        public bool BPvEDisableFriendlyFire
        {
            get { return _bPvEDisableFriendlyFire; }
            set
            {
                _bPvEDisableFriendlyFire = value;
                OnPropertyChanged(nameof(BPvEDisableFriendlyFire)); // Notify the UI of the change
            }
        }

        private string _serverAutoForceRespawnWildDinosInterval;
        public string ServerAutoForceRespawnWildDinosInterval
        {
            get { return _serverAutoForceRespawnWildDinosInterval; }
            set
            {
                _serverAutoForceRespawnWildDinosInterval = value;
                OnPropertyChanged(nameof(ServerAutoForceRespawnWildDinosInterval)); // Notify the UI of the change
            }
        }

        private bool _disableWeatherFog;
        public bool DisableWeatherFog
        {
            get { return _disableWeatherFog; }
            set
            {
                _disableWeatherFog = value;
                OnPropertyChanged(nameof(DisableWeatherFog)); // Notify the UI of the change
            }
        }

        private bool _randomSupplyCratePoints;
        public bool RandomSupplyCratePoints
        {
            get { return _randomSupplyCratePoints; }
            set
            {
                _randomSupplyCratePoints = value;
                OnPropertyChanged(nameof(RandomSupplyCratePoints)); // Notify the UI of the change
            }
        }

        private bool _crossARKAllowForeignDinoDownloads;
        public bool CrossARKAllowForeignDinoDownloads
        {
            get { return _crossARKAllowForeignDinoDownloads; }
            set
            {
                _crossARKAllowForeignDinoDownloads = value;
                OnPropertyChanged(nameof(CrossARKAllowForeignDinoDownloads)); // Notify the UI of the change
            }
        }

        private string _personalTamedDinosSaddleStructureCost;
        public string PersonalTamedDinosSaddleStructureCost
        {
            get { return _personalTamedDinosSaddleStructureCost; }
            set
            {
                _personalTamedDinosSaddleStructureCost = value;
                OnPropertyChanged(nameof(PersonalTamedDinosSaddleStructureCost)); // Notify the UI of the change
            }
        }

        private string _structurePreventResourceRadiusMultiplier;
        public string StructurePreventResourceRadiusMultiplier
        {
            get { return _structurePreventResourceRadiusMultiplier; }
            set
            {
                _structurePreventResourceRadiusMultiplier = value;
                OnPropertyChanged(nameof(StructurePreventResourceRadiusMultiplier)); // Notify the UI of the change
            }
        }
        private string _tribeNameChangeCooldown;
        public string TribeNameChangeCooldown
        {
            get { return _tribeNameChangeCooldown; }
            set
            {
                _tribeNameChangeCooldown = value;
                OnPropertyChanged(nameof(TribeNameChangeCooldown)); // Notify the UI of the change
            }
        }

        private string _platformSaddleBuildAreaBoundsMultiplier;
        public string PlatformSaddleBuildAreaBoundsMultiplier
        {
            get { return _platformSaddleBuildAreaBoundsMultiplier; }
            set
            {
                _platformSaddleBuildAreaBoundsMultiplier = value;
                OnPropertyChanged(nameof(PlatformSaddleBuildAreaBoundsMultiplier)); // Notify the UI of the change
            }
        }

        private bool _alwaysAllowStructurePickup;
        public bool AlwaysAllowStructurePickup
        {
            get { return _alwaysAllowStructurePickup; }
            set
            {
                _alwaysAllowStructurePickup = value;
                OnPropertyChanged(nameof(AlwaysAllowStructurePickup)); // Notify the UI of the change
            }
        }

        private string _structurePickupTimeAfterPlacement;
        public string StructurePickupTimeAfterPlacement
        {
            get { return _structurePickupTimeAfterPlacement; }
            set
            {
                _structurePickupTimeAfterPlacement = value;
                OnPropertyChanged(nameof(StructurePickupTimeAfterPlacement)); // Notify the UI of the change
            }
        }

        private string _structurePickupHoldDuration;
        public string StructurePickupHoldDuration
        {
            get { return _structurePickupHoldDuration; }
            set
            {
                _structurePickupHoldDuration = value;
                OnPropertyChanged(nameof(StructurePickupHoldDuration)); // Notify the UI of the change
            }
        }

        private bool _allowHideDamageSourceFromLogs;
        public bool AllowHideDamageSourceFromLogs
        {
            get { return _allowHideDamageSourceFromLogs; }
            set
            {
                _allowHideDamageSourceFromLogs = value;
                OnPropertyChanged(nameof(AllowHideDamageSourceFromLogs)); // Notify the UI of the change
            }
        }

        private string _raidDinoCharacterFoodDrainMultiplier;
        public string RaidDinoCharacterFoodDrainMultiplier
        {
            get { return _raidDinoCharacterFoodDrainMultiplier; }
            set
            {
                _raidDinoCharacterFoodDrainMultiplier = value;
                OnPropertyChanged(nameof(RaidDinoCharacterFoodDrainMultiplier)); // Notify the UI of the change
            }
        }

        private string _itemStackSizeMultiplier;
        public string ItemStackSizeMultiplier
        {
            get { return _itemStackSizeMultiplier; }
            set
            {
                _itemStackSizeMultiplier = value;
                OnPropertyChanged(nameof(ItemStackSizeMultiplier)); // Notify the UI of the change
            }
        }

        private bool _allowHitMarkers;
        public bool AllowHitMarkers
        {
            get { return _allowHitMarkers; }
            set
            {
                _allowHitMarkers = value;
                OnPropertyChanged(nameof(AllowHitMarkers)); // Notify the UI of the change
            }
        }

        private bool _allowMultipleAttachedC4;
        public bool AllowMultipleAttachedC4
        {
            get { return _allowMultipleAttachedC4; }
            set
            {
                _allowMultipleAttachedC4 = value;
                OnPropertyChanged(nameof(AllowMultipleAttachedC4)); // Notify the UI of the change
            }
        }
        private void LoadGameIniFile()
        {
            Console.WriteLine("Game INI file load has been initiated.");
            if (CurrentServerConfig == null)
            {
                Console.WriteLine("CurrentServerConfig is null.");
                return;
            }

            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            Console.WriteLine($"INI File Path: {iniFilePath}");

            if (!File.Exists(iniFilePath))
            {
                Console.WriteLine("Game INI file does not exist.");
                return;
            }

            var lines = File.ReadAllLines(iniFilePath);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith(";") && line.Contains("="))
                {
                    var keyValue = line.Split(new[] { '=' }, 2);
                    var key = keyValue[0].Trim();
                    var value = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;

                    // Example for setting a property
                    switch (key)
                    {
                        case "BabyImprintingStatScaleMultiplier":
                            BabyImprintingStatScaleMultiplier = value;
                            break;
                        case "BabyCuddleIntervalMultiplier":
                            BabyCuddleIntervalMultiplier = value;
                            break;
                        case "BabyCuddleGracePeriodMultiplier":
                            BabyCuddleGracePeriodMultiplier = value;
                            break;
                        case "BabyCuddleLoseImprintQualitySpeedMultiplier":
                            BabyCuddleLoseImprintQualitySpeedMultiplier = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[0]":
                            PerLevelStatsMultiplier_DinoTamed_0 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[1]":
                            PerLevelStatsMultiplier_DinoTamed_1 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[2]":
                            PerLevelStatsMultiplier_DinoTamed_2 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[3]":
                            PerLevelStatsMultiplier_DinoTamed_3 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[4]":
                            PerLevelStatsMultiplier_DinoTamed_4 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[7]":
                            PerLevelStatsMultiplier_DinoTamed_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[8]":
                            PerLevelStatsMultiplier_DinoTamed_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[9]":
                            PerLevelStatsMultiplier_DinoTamed_9 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed[10]":
                            PerLevelStatsMultiplier_DinoTamed_10 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[0]":
                            PerLevelStatsMultiplier_DinoTamed_Add_0 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[1]":
                            PerLevelStatsMultiplier_DinoTamed_Add_1 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[2]":
                            PerLevelStatsMultiplier_DinoTamed_Add_2 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[3]":
                            PerLevelStatsMultiplier_DinoTamed_Add_3 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[4]":
                            PerLevelStatsMultiplier_DinoTamed_Add_4 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[5]":
                            PerLevelStatsMultiplier_DinoTamed_Add_5 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[6]":
                            PerLevelStatsMultiplier_DinoTamed_Add_6 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[7]":
                            PerLevelStatsMultiplier_DinoTamed_Add_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[8]":
                            PerLevelStatsMultiplier_DinoTamed_Add_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[9]":
                            PerLevelStatsMultiplier_DinoTamed_Add_9 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[10]":
                            PerLevelStatsMultiplier_DinoTamed_Add_10 = value;
                            break;
                        // ... Similar cases for PerLevelStatsMultiplier_DinoTamed_Add[1] to [10]
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[0]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_0 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[1]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_1 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[2]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_2 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[3]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_3 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[4]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_4 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[5]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_5 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[6]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_6 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[7]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[8]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[9]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_9 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[10]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_10 = value;
                            break;
                        // ... Similar cases for PerLevelStatsMultiplier_DinoTamed_Affinity[1] to [10]
                        case "PerLevelStatsMultiplier_DinoWild[0]":
                            PerLevelStatsMultiplier_DinoWild_0 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[1]":
                            PerLevelStatsMultiplier_DinoWild_1 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[2]":
                            PerLevelStatsMultiplier_DinoWild_2 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[3]":
                            PerLevelStatsMultiplier_DinoWild_3 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[4]":
                            PerLevelStatsMultiplier_DinoWild_4 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[5]":
                            PerLevelStatsMultiplier_DinoWild_5 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[6]":
                            PerLevelStatsMultiplier_DinoWild_6 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[7]":
                            PerLevelStatsMultiplier_DinoWild_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[8]":
                            PerLevelStatsMultiplier_DinoWild_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[9]":
                            PerLevelStatsMultiplier_DinoWild_9 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[10]":
                            PerLevelStatsMultiplier_DinoWild_10 = value;
                            break;
                        // ... Similar cases for PerLevelStatsMultiplier_DinoWild[1] to [10]
                        case "PerLevelStatsMultiplier_Player[0]":
                            PerLevelStatsMultiplier_Player_0 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[1]":
                            PerLevelStatsMultiplier_Player_1 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[2]":
                            PerLevelStatsMultiplier_Player_2 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[3]":
                            PerLevelStatsMultiplier_Player_3 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[4]":
                            PerLevelStatsMultiplier_Player_4 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[5]":
                            PerLevelStatsMultiplier_Player_5 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[6]":
                            PerLevelStatsMultiplier_Player_6 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[7]":
                            PerLevelStatsMultiplier_Player_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[8]":
                            PerLevelStatsMultiplier_Player_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[9]":
                            PerLevelStatsMultiplier_Player_9 = value;
                            break;
                        case "PerLevelStatsMultiplier_Player[10]":
                            PerLevelStatsMultiplier_Player_10 = value;
                            break;
                        // ... Similar cases for PerLevelStatsMultiplier_Player[1] to [10]
                        case "GlobalSpoilingTimeMultiplier":
                            GlobalSpoilingTimeMultiplier = value;
                            break;
                        case "GlobalItemDecompositionTimeMultiplier":
                            GlobalItemDecompositionTimeMultiplier = value;
                            break;
                        case "GlobalCorpseDecompositionTimeMultiplier":
                            GlobalCorpseDecompositionTimeMultiplier = value;
                            break;
                        case "PvPZoneStructureDamageMultiplier":
                            PvPZoneStructureDamageMultiplier = value;
                            break;
                        case "StructureDamageRepairCooldown":
                            StructureDamageRepairCooldown = value;
                            break;
                        case "IncreasePvPRespawnIntervalCheckPeriod":
                            IncreasePvPRespawnIntervalCheckPeriod = value;
                            break;
                        case "IncreasePvPRespawnIntervalMultiplier":
                            IncreasePvPRespawnIntervalMultiplier = value;
                            break;
                        case "IncreasePvPRespawnIntervalBaseAmount":
                            IncreasePvPRespawnIntervalBaseAmount = value;
                            break;
                        case "ResourceNoReplenishRadiusPlayers":
                            ResourceNoReplenishRadiusPlayers = value;
                            break;
                        case "CropGrowthSpeedMultiplier":
                            CropGrowthSpeedMultiplier = value;
                            break;
                        case "LayEggIntervalMultiplier":
                            LayEggIntervalMultiplier = value;
                            break;
                        case "PoopIntervalMultiplier":
                            PoopIntervalMultiplier = value;
                            break;
                        case "CropDecaySpeedMultiplier":
                            CropDecaySpeedMultiplier = value;
                            break;
                        case "MatingIntervalMultiplier":
                            MatingIntervalMultiplier = value;
                            break;
                        case "EggHatchSpeedMultiplier":
                            EggHatchSpeedMultiplier = value;
                            break;
                        case "BabyMatureSpeedMultiplier":
                            BabyMatureSpeedMultiplier = value;
                            break;
                        case "BabyFoodConsumptionSpeedMultiplier":
                            BabyFoodConsumptionSpeedMultiplier = value;
                            break;
                        case "DinoTurretDamageMultiplier":
                            DinoTurretDamageMultiplier = value;
                            break;
                        case "DinoHarvestingDamageMultiplier":
                            DinoHarvestingDamageMultiplier = value;
                            break;
                        case "PlayerHarvestingDamageMultiplier":
                            PlayerHarvestingDamageMultiplier = value;
                            break;
                        case "CustomRecipeEffectivenessMultiplier":
                            CustomRecipeEffectivenessMultiplier = value;
                            break;
                        case "CustomRecipeSkillMultiplier":
                            CustomRecipeSkillMultiplier = value;
                            break;
                        case "AutoPvEStartTimeSeconds":
                            AutoPvEStartTimeSeconds = value;
                            break;
                        case "AutoPvEStopTimeSeconds":
                            AutoPvEStopTimeSeconds = value;
                            break;
                        case "KillXPMultiplier":
                            KillXPMultiplier = value;
                            break;
                        case "HarvestXPMultiplier":
                            HarvestXPMultiplier = value;
                            break;
                        case "CraftXPMultiplier":
                            CraftXPMultiplier = value;
                            break;
                        case "GenericXPMultiplier":
                            GenericXPMultiplier = value;
                            break;
                        case "SpecialXPMultiplier":
                            SpecialXPMultiplier = value;
                            break;
                        case "FuelConsumptionIntervalMultiplier":
                            FuelConsumptionIntervalMultiplier = value;
                            break;
                        case "PhotoModeRangeLimit":
                            PhotoModeRangeLimit = value;
                            break;
                        case "DisablePhotoMode":
                            DisablePhotoMode = Convert.ToBoolean(value);
                            break;
                        case "IncreasePvPRespawnInterval":
                            IncreasePvPRespawnInterval = Convert.ToBoolean(value);
                            break;
                        case "AutoPvETimer":
                            AutoPvETimer = Convert.ToBoolean(value);
                            break;
                        case "AutoPvEUseSystemTime":
                            AutoPvEUseSystemTime = Convert.ToBoolean(value);
                            break;
                        case "DisableFriendlyFire":
                            DisableFriendlyFire = Convert.ToBoolean(value);
                            break;
                        case "FlyerPlatformAllowUnalignedDinoBasing":
                            FlyerPlatformAllowUnalignedDinoBasing = Convert.ToBoolean(value);
                            break;
                        case "DisableLootCrates":
                            DisableLootCrates = Convert.ToBoolean(value);
                            break;
                        case "AllowCustomRecipes":
                            AllowCustomRecipes = Convert.ToBoolean(value);
                            break;
                        case "PassiveDefensesDamageRiderlessDinos":
                            PassiveDefensesDamageRiderlessDinos = Convert.ToBoolean(value);
                            break;
                        case "PvEAllowTribeWar":
                            PvEAllowTribeWar = Convert.ToBoolean(value);
                            break;
                        case "PvEAllowTribeWarCancel":
                            PvEAllowTribeWarCancel = Convert.ToBoolean(value);
                            break;
                        case "MaxDifficulty":
                            MaxDifficulty = Convert.ToBoolean(value);
                            break;
                        case "UseSingleplayerSettings":
                            UseSingleplayerSettings = Convert.ToBoolean(value);
                            break;
                        case "UseCorpseLocator":
                            UseCorpseLocator = Convert.ToBoolean(value);
                            break;
                        case "ShowCreativeMode":
                            ShowCreativeMode = Convert.ToBoolean(value);
                            break;
                        case "HardLimitTurretsInRange":
                            HardLimitTurretsInRange = Convert.ToBoolean(value);
                            break;
                        case "DisableStructurePlacementCollision":
                            DisableStructurePlacementCollision = Convert.ToBoolean(value);
                            break;
                        case "AllowPlatformSaddleMultiFloors":
                            AllowPlatformSaddleMultiFloors = Convert.ToBoolean(value);
                            break;
                        case "AllowUnlimitedRespec":
                            AllowUnlimitedRespec = Convert.ToBoolean(value);
                            break;
                        case "DisableDinoTaming":
                            DisableDinoTaming = Convert.ToBoolean(value);
                            break;
                        case "OverrideMaxExperiencePointsDino":
                            OverrideMaxExperiencePointsDino = value;
                            break;
                        case "MaxNumberOfPlayersInTribe":
                            MaxNumberOfPlayersInTribe = value;
                            break;
                        case "ExplorerNoteXPMultiplier":
                            ExplorerNoteXPMultiplier = value;
                            break;
                        case "BossKillXPMultiplier":
                            BossKillXPMultiplier = value;
                            break;
                        case "AlphaKillXPMultiplier":
                            AlphaKillXPMultiplier = value;
                            break;
                        case "WildKillXPMultiplier":
                            WildKillXPMultiplier = value;
                            break;
                        case "CaveKillXPMultiplier":
                            CaveKillXPMultiplier = value;
                            break;
                        case "TamedKillXPMultiplier":
                            TamedKillXPMultiplier = value;
                            break;
                        case "UnclaimedKillXPMultiplier":
                            UnclaimedKillXPMultiplier = value;
                            break;
                        case "SupplyCrateLootQualityMultiplier":
                            SupplyCrateLootQualityMultiplier = value;
                            break;
                        case "FishingLootQualityMultiplier":
                            FishingLootQualityMultiplier = value;
                            break;
                        case "CraftingSkillBonusMultiplier":
                            CraftingSkillBonusMultiplier = value;
                            break;
                        case "AllowSpeedLeveling":
                            AllowSpeedLeveling = Convert.ToBoolean(value);
                            break;
                        case "AllowFlyerSpeedLeveling":
                            AllowFlyerSpeedLeveling = Convert.ToBoolean(value);
                            break;
                            // Add cases for all other settings
                            // ...
                    }
                }
            }
        }
        private void SaveGameIniSettings()
        {
            string serverPath = CurrentServerConfig.ServerPath;
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");

            // Read all lines
            var lines = File.ReadAllLines(iniFilePath).ToList();

            // Update specific lines
            UpdateLine(ref lines, "BabyImprintingStatScaleMultiplier", BabyImprintingStatScaleMultiplier);
            UpdateLine(ref lines, "BabyCuddleIntervalMultiplier", BabyCuddleIntervalMultiplier);
            UpdateLine(ref lines, "BabyCuddleGracePeriodMultiplier", BabyCuddleGracePeriodMultiplier);
            UpdateLine(ref lines, "BabyCuddleLoseImprintQualitySpeedMultiplier", BabyCuddleLoseImprintQualitySpeedMultiplier);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[0]", PerLevelStatsMultiplier_DinoTamed_0);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[1]", PerLevelStatsMultiplier_DinoTamed_1);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[2]", PerLevelStatsMultiplier_DinoTamed_2);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[3]", PerLevelStatsMultiplier_DinoTamed_3);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[4]", PerLevelStatsMultiplier_DinoTamed_4);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[7]", PerLevelStatsMultiplier_DinoTamed_7);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[8]", PerLevelStatsMultiplier_DinoTamed_8);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[9]", PerLevelStatsMultiplier_DinoTamed_9);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed[10]", PerLevelStatsMultiplier_DinoTamed_10);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[0]", PerLevelStatsMultiplier_DinoTamed_Add_0);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[1]", PerLevelStatsMultiplier_DinoTamed_Add_1);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[2]", PerLevelStatsMultiplier_DinoTamed_Add_2);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[3]", PerLevelStatsMultiplier_DinoTamed_Add_3);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[4]", PerLevelStatsMultiplier_DinoTamed_Add_4);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[5]", PerLevelStatsMultiplier_DinoTamed_Add_5);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[6]", PerLevelStatsMultiplier_DinoTamed_Add_6);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[7]", PerLevelStatsMultiplier_DinoTamed_Add_7);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[8]", PerLevelStatsMultiplier_DinoTamed_Add_8);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[9]", PerLevelStatsMultiplier_DinoTamed_Add_9);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Add[10]", PerLevelStatsMultiplier_DinoTamed_Add_10);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[0]", PerLevelStatsMultiplier_DinoTamed_Affinity_0);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[1]", PerLevelStatsMultiplier_DinoTamed_Affinity_1);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[2]", PerLevelStatsMultiplier_DinoTamed_Affinity_2);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[3]", PerLevelStatsMultiplier_DinoTamed_Affinity_3);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[4]", PerLevelStatsMultiplier_DinoTamed_Affinity_4);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[5]", PerLevelStatsMultiplier_DinoTamed_Affinity_5);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[6]", PerLevelStatsMultiplier_DinoTamed_Affinity_6);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[7]", PerLevelStatsMultiplier_DinoTamed_Affinity_7);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[8]", PerLevelStatsMultiplier_DinoTamed_Affinity_8);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[9]", PerLevelStatsMultiplier_DinoTamed_Affinity_9);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoTamed_Affinity[10]", PerLevelStatsMultiplier_DinoTamed_Affinity_10);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[1]", PerLevelStatsMultiplier_DinoWild_1);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[2]", PerLevelStatsMultiplier_DinoWild_2);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[3]", PerLevelStatsMultiplier_DinoWild_3);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[4]", PerLevelStatsMultiplier_DinoWild_4);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[5]", PerLevelStatsMultiplier_DinoWild_5);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[6]", PerLevelStatsMultiplier_DinoWild_6);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[7]", PerLevelStatsMultiplier_DinoWild_7);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[8]", PerLevelStatsMultiplier_DinoWild_8);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[9]", PerLevelStatsMultiplier_DinoWild_9);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_DinoWild[10]", PerLevelStatsMultiplier_DinoWild_10);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[1]", PerLevelStatsMultiplier_Player_1);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[2]", PerLevelStatsMultiplier_Player_2);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[3]", PerLevelStatsMultiplier_Player_3);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[4]", PerLevelStatsMultiplier_Player_4);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[5]", PerLevelStatsMultiplier_Player_5);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[6]", PerLevelStatsMultiplier_Player_6);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[7]", PerLevelStatsMultiplier_Player_7);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[8]", PerLevelStatsMultiplier_Player_8);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[9]", PerLevelStatsMultiplier_Player_9);
            UpdateLine(ref lines, "PerLevelStatsMultiplier_Player[10]", PerLevelStatsMultiplier_Player_10);
            UpdateLine(ref lines, "GlobalSpoilingTimeMultiplier", GlobalSpoilingTimeMultiplier);
            UpdateLine(ref lines, "GlobalItemDecompositionTimeMultiplier", GlobalItemDecompositionTimeMultiplier);
            UpdateLine(ref lines, "GlobalCorpseDecompositionTimeMultiplier", GlobalCorpseDecompositionTimeMultiplier);
            UpdateLine(ref lines, "PvPZoneStructureDamageMultiplier", PvPZoneStructureDamageMultiplier);
            UpdateLine(ref lines, "StructureDamageRepairCooldown", StructureDamageRepairCooldown);
            UpdateLine(ref lines, "IncreasePvPRespawnIntervalCheckPeriod", IncreasePvPRespawnIntervalCheckPeriod);
            UpdateLine(ref lines, "IncreasePvPRespawnIntervalMultiplier", IncreasePvPRespawnIntervalMultiplier);
            UpdateLine(ref lines, "IncreasePvPRespawnIntervalBaseAmount", IncreasePvPRespawnIntervalBaseAmount);
            UpdateLine(ref lines, "ResourceNoReplenishRadiusPlayers", ResourceNoReplenishRadiusPlayers);
            UpdateLine(ref lines, "CropGrowthSpeedMultiplier", CropGrowthSpeedMultiplier);
            UpdateLine(ref lines, "LayEggIntervalMultiplier", LayEggIntervalMultiplier);
            UpdateLine(ref lines, "PoopIntervalMultiplier", PoopIntervalMultiplier);
            UpdateLine(ref lines, "CropDecaySpeedMultiplier", CropDecaySpeedMultiplier);
            UpdateLine(ref lines, "MatingIntervalMultiplier", MatingIntervalMultiplier);
            UpdateLine(ref lines, "EggHatchSpeedMultiplier", EggHatchSpeedMultiplier);
            UpdateLine(ref lines, "BabyMatureSpeedMultiplier", BabyMatureSpeedMultiplier);
            UpdateLine(ref lines, "BabyFoodConsumptionSpeedMultiplier", BabyFoodConsumptionSpeedMultiplier);
            UpdateLine(ref lines, "DinoTurretDamageMultiplier", DinoTurretDamageMultiplier);
            UpdateLine(ref lines, "DinoHarvestingDamageMultiplier", DinoHarvestingDamageMultiplier);
            UpdateLine(ref lines, "PlayerHarvestingDamageMultiplier", PlayerHarvestingDamageMultiplier);
            UpdateLine(ref lines, "CustomRecipeEffectivenessMultiplier", CustomRecipeEffectivenessMultiplier);
            UpdateLine(ref lines, "CustomRecipeSkillMultiplier", CustomRecipeSkillMultiplier);
            UpdateLine(ref lines, "AutoPvEStartTimeSeconds", AutoPvEStartTimeSeconds);
            UpdateLine(ref lines, "AutoPvEStopTimeSeconds", AutoPvEStopTimeSeconds);
            UpdateLine(ref lines, "KillXPMultiplier", KillXPMultiplier);
            UpdateLine(ref lines, "HarvestXPMultiplier", HarvestXPMultiplier);
            UpdateLine(ref lines, "CraftXPMultiplier", CraftXPMultiplier);
            UpdateLine(ref lines, "GenericXPMultiplier", GenericXPMultiplier);
            UpdateLine(ref lines, "SpecialXPMultiplier", SpecialXPMultiplier);
            UpdateLine(ref lines, "FuelConsumptionIntervalMultiplier", FuelConsumptionIntervalMultiplier);
            UpdateLine(ref lines, "PhotoModeRangeLimit", PhotoModeRangeLimit);
            UpdateLine(ref lines, "DisablePhotoMode", DisablePhotoMode.ToString());
            UpdateLine(ref lines, "IncreasePvPRespawnInterval", IncreasePvPRespawnInterval.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "bAutoPvETimer", AutoPvETimer.ToString());
            UpdateLine(ref lines, "bAutoPvEUseSystemTime", AutoPvEUseSystemTime.ToString());
            UpdateLine(ref lines, "DisableFriendlyFire", DisableFriendlyFire.ToString());
            UpdateLine(ref lines, "FlyerPlatformAllowUnalignedDinoBasing", FlyerPlatformAllowUnalignedDinoBasing.ToString());
            UpdateLine(ref lines, "DisableLootCrates", DisableLootCrates.ToString());
            UpdateLine(ref lines, "AllowCustomRecipes", AllowCustomRecipes.ToString());
            UpdateLine(ref lines, "PassiveDefensesDamageRiderlessDinos", PassiveDefensesDamageRiderlessDinos.ToString());
            UpdateLine(ref lines, "PvEAllowTribeWar", PvEAllowTribeWar.ToString());
            UpdateLine(ref lines, "PvEAllowTribeWarCancel", PvEAllowTribeWarCancel.ToString());
            UpdateLine(ref lines, "MaxDifficulty", MaxDifficulty.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "UseSingleplayerSettings", UseSingleplayerSettings.ToString());
            UpdateLine(ref lines, "UseCorpseLocator", UseCorpseLocator.ToString());
            UpdateLine(ref lines, "ShowCreativeMode", ShowCreativeMode.ToString());
            UpdateLine(ref lines, "HardLimitTurretsInRange", HardLimitTurretsInRange.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "DisableStructurePlacementCollision", DisableStructurePlacementCollision.ToString());
            UpdateLine(ref lines, "AllowPlatformSaddleMultiFloors", AllowPlatformSaddleMultiFloors.ToString());
            UpdateLine(ref lines, "AllowUnlimitedRespec", AllowUnlimitedRespec.ToString());
            UpdateLine(ref lines, "DisableDinoTaming", DisableDinoTaming.ToString());
            UpdateLine(ref lines, "OverrideMaxExperiencePointsDino", OverrideMaxExperiencePointsDino);
            UpdateLine(ref lines, "MaxNumberOfPlayersInTribe", MaxNumberOfPlayersInTribe);
            UpdateLine(ref lines, "ExplorerNoteXPMultiplier", ExplorerNoteXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "BossKillXPMultiplier", BossKillXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "AlphaKillXPMultiplier", AlphaKillXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "WildKillXPMultiplier", WildKillXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "CaveKillXPMultiplier", CaveKillXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "TamedKillXPMultiplier", TamedKillXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "UnclaimedKillXPMultiplier", UnclaimedKillXPMultiplier.ToString(CultureInfo.InvariantCulture));
            UpdateLine(ref lines, "SupplyCrateLootQualityMultiplier", SupplyCrateLootQualityMultiplier);
            UpdateLine(ref lines, "FishingLootQualityMultiplier", FishingLootQualityMultiplier);
            UpdateLine(ref lines, "CraftingSkillBonusMultiplier", CraftingSkillBonusMultiplier);
            UpdateLine(ref lines, "AllowSpeedLeveling", AllowSpeedLeveling.ToString());
            UpdateLine(ref lines, "AllowFlyerSpeedLeveling", AllowFlyerSpeedLeveling.ToString());

            // ... Continue for all other properties

            // Write the updated lines back to the file
            File.WriteAllLines(iniFilePath, lines);
        }
        private string _babyImprintingStatScaleMultiplier;
        public string BabyImprintingStatScaleMultiplier
        {
            get { return _babyImprintingStatScaleMultiplier; }
            set
            {
                _babyImprintingStatScaleMultiplier = value;
                OnPropertyChanged(nameof(BabyImprintingStatScaleMultiplier));
            }
        }

        private string _babyCuddleIntervalMultiplier;
        public string BabyCuddleIntervalMultiplier
        {
            get { return _babyCuddleIntervalMultiplier; }
            set
            {
                _babyCuddleIntervalMultiplier = value;
                OnPropertyChanged(nameof(BabyCuddleIntervalMultiplier));
            }
        }

 private string _babyCuddleGracePeriodMultiplier;
        public string BabyCuddleGracePeriodMultiplier
        {
            get { return _babyCuddleGracePeriodMultiplier; }
            set
            {
                _babyCuddleGracePeriodMultiplier = value;
                OnPropertyChanged(nameof(BabyCuddleGracePeriodMultiplier));
            }
        }

 private string _babyCuddleLoseImprintQualitySpeedMultiplier;
        public string BabyCuddleLoseImprintQualitySpeedMultiplier
        {
            get { return _babyCuddleLoseImprintQualitySpeedMultiplier; }
            set
            {
                _babyCuddleLoseImprintQualitySpeedMultiplier = value;
                OnPropertyChanged(nameof(BabyCuddleLoseImprintQualitySpeedMultiplier));
            }
        }

 private string _perLevelStatsMultiplier_DinoTamed_0;
        public string PerLevelStatsMultiplier_DinoTamed_0
        {
            get { return _perLevelStatsMultiplier_DinoTamed_0; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_0 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_0));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_1;
        public string PerLevelStatsMultiplier_DinoTamed_1
        {
            get { return _perLevelStatsMultiplier_DinoTamed_1; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_1 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_1));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_2;
        public string PerLevelStatsMultiplier_DinoTamed_2
        {
            get { return _perLevelStatsMultiplier_DinoTamed_2; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_2 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_2));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_3;
        public string PerLevelStatsMultiplier_DinoTamed_3
        {
            get { return _perLevelStatsMultiplier_DinoTamed_3; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_3 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_3));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_4;
        public string PerLevelStatsMultiplier_DinoTamed_4
        {
            get { return _perLevelStatsMultiplier_DinoTamed_4; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_4 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_4));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_7;
        public string PerLevelStatsMultiplier_DinoTamed_7
        {
            get { return _perLevelStatsMultiplier_DinoTamed_7; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_7 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_7));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_8;
        public string PerLevelStatsMultiplier_DinoTamed_8
        {
            get { return _perLevelStatsMultiplier_DinoTamed_8; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_8 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_8));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_9;
        public string PerLevelStatsMultiplier_DinoTamed_9
        {
            get { return _perLevelStatsMultiplier_DinoTamed_9; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_9 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_9));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_10;
        public string PerLevelStatsMultiplier_DinoTamed_10
        {
            get { return _perLevelStatsMultiplier_DinoTamed_10; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_10 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_10));
            }
        }
        private string _perLevelStatsMultiplier_DinoTamed_Add_0;
        public string PerLevelStatsMultiplier_DinoTamed_Add_0
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_0; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_0 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_0));
            }
        }
        private string _perLevelStatsMultiplier_DinoTamed_Add_1;
        public string PerLevelStatsMultiplier_DinoTamed_Add_1
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_1; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_1 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_1));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_2;
        public string PerLevelStatsMultiplier_DinoTamed_Add_2
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_2; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_2 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_2));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_3;
        public string PerLevelStatsMultiplier_DinoTamed_Add_3
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_3; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_3 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_3));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_4;
        public string PerLevelStatsMultiplier_DinoTamed_Add_4
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_4; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_4 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_4));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_5;
        public string PerLevelStatsMultiplier_DinoTamed_Add_5
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_5; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_5 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_5));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_6;
        public string PerLevelStatsMultiplier_DinoTamed_Add_6
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_6; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_6 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_6));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_7;
        public string PerLevelStatsMultiplier_DinoTamed_Add_7
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_7; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_7 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_7));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_8;
        public string PerLevelStatsMultiplier_DinoTamed_Add_8
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_8; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_8 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_8));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_9;
        public string PerLevelStatsMultiplier_DinoTamed_Add_9
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_9; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_9 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_9));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Add_10;
        public string PerLevelStatsMultiplier_DinoTamed_Add_10
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Add_10; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Add_10 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Add_10));
            }
        }
        private string _perLevelStatsMultiplier_DinoTamed_Affinity_0;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_0
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_0; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_0 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_0));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_1;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_1
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_1; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_1 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_1));
            }
        }
        private string _perLevelStatsMultiplier_DinoTamed_Affinity_2;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_2
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_2; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_2 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_2));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_3;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_3
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_3; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_3 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_3));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_4;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_4
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_4; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_4 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_4));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_5;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_5
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_5; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_5 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_5));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_6;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_6
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_6; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_6 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_6));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_7;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_7
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_7; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_7 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_7));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_8;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_8
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_8; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_8 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_8));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_9;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_9
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_9; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_9 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_9));
            }
        }

        private string _perLevelStatsMultiplier_DinoTamed_Affinity_10;
        public string PerLevelStatsMultiplier_DinoTamed_Affinity_10
        {
            get { return _perLevelStatsMultiplier_DinoTamed_Affinity_10; }
            set
            {
                _perLevelStatsMultiplier_DinoTamed_Affinity_10 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoTamed_Affinity_10));
            }
        }
        private string _perLevelStatsMultiplier_DinoWild_0;
        public string PerLevelStatsMultiplier_DinoWild_0
        {
            get { return _perLevelStatsMultiplier_DinoWild_0; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_0 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_0));
            }
        }
        private string _perLevelStatsMultiplier_DinoWild_1;
        public string PerLevelStatsMultiplier_DinoWild_1
        {
            get { return _perLevelStatsMultiplier_DinoWild_1; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_1 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_1));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_2;
        public string PerLevelStatsMultiplier_DinoWild_2
        {
            get { return _perLevelStatsMultiplier_DinoWild_2; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_2 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_2));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_3;
        public string PerLevelStatsMultiplier_DinoWild_3
        {
            get { return _perLevelStatsMultiplier_DinoWild_3; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_3 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_3));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_4;
        public string PerLevelStatsMultiplier_DinoWild_4
        {
            get { return _perLevelStatsMultiplier_DinoWild_4; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_4 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_4));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_5;
        public string PerLevelStatsMultiplier_DinoWild_5
        {
            get { return _perLevelStatsMultiplier_DinoWild_5; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_5 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_5));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_6;
        public string PerLevelStatsMultiplier_DinoWild_6
        {
            get { return _perLevelStatsMultiplier_DinoWild_6; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_6 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_6));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_7;
        public string PerLevelStatsMultiplier_DinoWild_7
        {
            get { return _perLevelStatsMultiplier_DinoWild_7; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_7 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_7));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_8;
        public string PerLevelStatsMultiplier_DinoWild_8
        {
            get { return _perLevelStatsMultiplier_DinoWild_8; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_8 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_8));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_9;
        public string PerLevelStatsMultiplier_DinoWild_9
        {
            get { return _perLevelStatsMultiplier_DinoWild_9; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_9 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_9));
            }
        }

        private string _perLevelStatsMultiplier_DinoWild_10;
        public string PerLevelStatsMultiplier_DinoWild_10
        {
            get { return _perLevelStatsMultiplier_DinoWild_10; }
            set
            {
                _perLevelStatsMultiplier_DinoWild_10 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_DinoWild_10));
            }
        }
        private string _perLevelStatsMultiplier_Player_0;
        public string PerLevelStatsMultiplier_Player_0
        {
            get { return _perLevelStatsMultiplier_Player_0; }
            set
            {
                _perLevelStatsMultiplier_Player_0 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_0));
            }
        }
        private string _perLevelStatsMultiplier_Player_1;
        public string PerLevelStatsMultiplier_Player_1
        {
            get { return _perLevelStatsMultiplier_Player_1; }
            set
            {
                _perLevelStatsMultiplier_Player_1 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_1));
            }
        }

        private string _perLevelStatsMultiplier_Player_2;
        public string PerLevelStatsMultiplier_Player_2
        {
            get { return _perLevelStatsMultiplier_Player_2; }
            set
            {
                _perLevelStatsMultiplier_Player_2 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_2));
            }
        }

        private string _perLevelStatsMultiplier_Player_3;
        public string PerLevelStatsMultiplier_Player_3
        {
            get { return _perLevelStatsMultiplier_Player_3; }
            set
            {
                _perLevelStatsMultiplier_Player_3 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_3));
            }
        }

        private string _perLevelStatsMultiplier_Player_4;
        public string PerLevelStatsMultiplier_Player_4
        {
            get { return _perLevelStatsMultiplier_Player_4; }
            set
            {
                _perLevelStatsMultiplier_Player_4 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_4));
            }
        }

        private string _perLevelStatsMultiplier_Player_5;
        public string PerLevelStatsMultiplier_Player_5
        {
            get { return _perLevelStatsMultiplier_Player_5; }
            set
            {
                _perLevelStatsMultiplier_Player_5 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_5));
            }
        }

        private string _perLevelStatsMultiplier_Player_6;
        public string PerLevelStatsMultiplier_Player_6
        {
            get { return _perLevelStatsMultiplier_Player_6; }
            set
            {
                _perLevelStatsMultiplier_Player_6 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_6));
            }
        }

        private string _perLevelStatsMultiplier_Player_7;
        public string PerLevelStatsMultiplier_Player_7
        {
            get { return _perLevelStatsMultiplier_Player_7; }
            set
            {
                _perLevelStatsMultiplier_Player_7 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_7));
            }
        }

        private string _perLevelStatsMultiplier_Player_8;
        public string PerLevelStatsMultiplier_Player_8
        {
            get { return _perLevelStatsMultiplier_Player_8; }
            set
            {
                _perLevelStatsMultiplier_Player_8 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_8));
            }
        }

        private string _perLevelStatsMultiplier_Player_9;
        public string PerLevelStatsMultiplier_Player_9
        {
            get { return _perLevelStatsMultiplier_Player_9; }
            set
            {
                _perLevelStatsMultiplier_Player_9 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_9));
            }
        }

        private string _perLevelStatsMultiplier_Player_10;
        public string PerLevelStatsMultiplier_Player_10
        {
            get { return _perLevelStatsMultiplier_Player_10; }
            set
            {
                _perLevelStatsMultiplier_Player_10 = value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_10));
            }
        }
        private string _globalSpoilingTimeMultiplier;
        public string GlobalSpoilingTimeMultiplier
        {
            get { return _globalSpoilingTimeMultiplier; }
            set
            {
                _globalSpoilingTimeMultiplier = value;
                OnPropertyChanged(nameof(GlobalSpoilingTimeMultiplier));
            }
        }

        private string _globalItemDecompositionTimeMultiplier;
        public string GlobalItemDecompositionTimeMultiplier
        {
            get { return _globalItemDecompositionTimeMultiplier; }
            set
            {
                _globalItemDecompositionTimeMultiplier = value;
                OnPropertyChanged(nameof(GlobalItemDecompositionTimeMultiplier));
            }
        }

        private string _globalCorpseDecompositionTimeMultiplier;
        public string GlobalCorpseDecompositionTimeMultiplier
        {
            get { return _globalCorpseDecompositionTimeMultiplier; }
            set
            {
                _globalCorpseDecompositionTimeMultiplier = value;
                OnPropertyChanged(nameof(GlobalCorpseDecompositionTimeMultiplier));
            }
        }

        private string _pvpZoneStructureDamageMultiplier;
        public string PvPZoneStructureDamageMultiplier
        {
            get { return _pvpZoneStructureDamageMultiplier; }
            set
            {
                _pvpZoneStructureDamageMultiplier = value;
                OnPropertyChanged(nameof(PvPZoneStructureDamageMultiplier));
            }
        }

        private string _structureDamageRepairCooldown;
        public string StructureDamageRepairCooldown
        {
            get { return _structureDamageRepairCooldown; }
            set
            {
                _structureDamageRepairCooldown = value;
                OnPropertyChanged(nameof(StructureDamageRepairCooldown));
            }
        }

        private string _increasePvPRespawnIntervalCheckPeriod;
        public string IncreasePvPRespawnIntervalCheckPeriod
        {
            get { return _increasePvPRespawnIntervalCheckPeriod; }
            set
            {
                _increasePvPRespawnIntervalCheckPeriod = value;
                OnPropertyChanged(nameof(IncreasePvPRespawnIntervalCheckPeriod));
            }
        }

        private string _increasePvPRespawnIntervalMultiplier;
        public string IncreasePvPRespawnIntervalMultiplier
        {
            get { return _increasePvPRespawnIntervalMultiplier; }
            set
            {
                _increasePvPRespawnIntervalMultiplier = value;
                OnPropertyChanged(nameof(IncreasePvPRespawnIntervalMultiplier));
            }
        }

        private string _increasePvPRespawnIntervalBaseAmount;
        public string IncreasePvPRespawnIntervalBaseAmount
        {
            get { return _increasePvPRespawnIntervalBaseAmount; }
            set
            {
                _increasePvPRespawnIntervalBaseAmount = value;
                OnPropertyChanged(nameof(IncreasePvPRespawnIntervalBaseAmount));
            }
        }

        private string _resourceNoReplenishRadiusPlayers;
        public string ResourceNoReplenishRadiusPlayers
        {
            get { return _resourceNoReplenishRadiusPlayers; }
            set
            {
                _resourceNoReplenishRadiusPlayers = value;
                OnPropertyChanged(nameof(ResourceNoReplenishRadiusPlayers));
            }
        }

        private string _cropGrowthSpeedMultiplier;
        public string CropGrowthSpeedMultiplier
        {
            get { return _cropGrowthSpeedMultiplier; }
            set
            {
                _cropGrowthSpeedMultiplier = value;
                OnPropertyChanged(nameof(CropGrowthSpeedMultiplier));
            }
        }

        private string _layEggIntervalMultiplier;
        public string LayEggIntervalMultiplier
        {
            get { return _layEggIntervalMultiplier; }
            set
            {
                _layEggIntervalMultiplier = value;
                OnPropertyChanged(nameof(LayEggIntervalMultiplier));
            }
        }

        private string _poopIntervalMultiplier;
        public string PoopIntervalMultiplier
        {
            get { return _poopIntervalMultiplier; }
            set
            {
                _poopIntervalMultiplier = value;
                OnPropertyChanged(nameof(PoopIntervalMultiplier));
            }
        }

        private string _cropDecaySpeedMultiplier;
        public string CropDecaySpeedMultiplier
        {
            get { return _cropDecaySpeedMultiplier; }
            set
            {
                _cropDecaySpeedMultiplier = value;
                OnPropertyChanged(nameof(CropDecaySpeedMultiplier));
            }
        }

        private string _matingIntervalMultiplier;
        public string MatingIntervalMultiplier
        {
            get { return _matingIntervalMultiplier; }
            set
            {
                _matingIntervalMultiplier = value;
                OnPropertyChanged(nameof(MatingIntervalMultiplier));
            }
        }

        private string _eggHatchSpeedMultiplier;
        public string EggHatchSpeedMultiplier
        {
            get { return _eggHatchSpeedMultiplier; }
            set
            {
                _eggHatchSpeedMultiplier = value;
                OnPropertyChanged(nameof(EggHatchSpeedMultiplier));
            }
        }

        private string _babyMatureSpeedMultiplier;
        public string BabyMatureSpeedMultiplier
        {
            get { return _babyMatureSpeedMultiplier; }
            set
            {
                _babyMatureSpeedMultiplier = value;
                OnPropertyChanged(nameof(BabyMatureSpeedMultiplier));
            }
        }

        private string _babyFoodConsumptionSpeedMultiplier;
        public string BabyFoodConsumptionSpeedMultiplier
        {
            get { return _babyFoodConsumptionSpeedMultiplier; }
            set
            {
                _babyFoodConsumptionSpeedMultiplier = value;
                OnPropertyChanged(nameof(BabyFoodConsumptionSpeedMultiplier));
            }
        }
        private string _dinoTurretDamageMultiplier;
        public string DinoTurretDamageMultiplier
        {
            get { return _dinoTurretDamageMultiplier; }
            set
            {
                _dinoTurretDamageMultiplier = value;
                OnPropertyChanged(nameof(DinoTurretDamageMultiplier));
            }
        }

        private string _dinoHarvestingDamageMultiplier;
        public string DinoHarvestingDamageMultiplier
        {
            get { return _dinoHarvestingDamageMultiplier; }
            set
            {
                _dinoHarvestingDamageMultiplier = value;
                OnPropertyChanged(nameof(DinoHarvestingDamageMultiplier));
            }
        }

        private string _playerHarvestingDamageMultiplier;
        public string PlayerHarvestingDamageMultiplier
        {
            get { return _playerHarvestingDamageMultiplier; }
            set
            {
                _playerHarvestingDamageMultiplier = value;
                OnPropertyChanged(nameof(PlayerHarvestingDamageMultiplier));
            }
        }

        private string _customRecipeEffectivenessMultiplier;
        public string CustomRecipeEffectivenessMultiplier
        {
            get { return _customRecipeEffectivenessMultiplier; }
            set
            {
                _customRecipeEffectivenessMultiplier = value;
                OnPropertyChanged(nameof(CustomRecipeEffectivenessMultiplier));
            }
        }

        private string _customRecipeSkillMultiplier;
        public string CustomRecipeSkillMultiplier
        {
            get { return _customRecipeSkillMultiplier; }
            set
            {
                _customRecipeSkillMultiplier = value;
                OnPropertyChanged(nameof(CustomRecipeSkillMultiplier));
            }
        }

        private string _autoPvEStartTimeSeconds;
        public string AutoPvEStartTimeSeconds
        {
            get { return _autoPvEStartTimeSeconds; }
            set
            {
                _autoPvEStartTimeSeconds = value;
                OnPropertyChanged(nameof(AutoPvEStartTimeSeconds));
            }
        }

        private string _autoPvEStopTimeSeconds;
        public string AutoPvEStopTimeSeconds
        {
            get { return _autoPvEStopTimeSeconds; }
            set
            {
                _autoPvEStopTimeSeconds = value;
                OnPropertyChanged(nameof(AutoPvEStopTimeSeconds));
            }
        }

        private string _killXPMultiplier;
        public string KillXPMultiplier
        {
            get { return _killXPMultiplier; }
            set
            {
                _killXPMultiplier = value;
                OnPropertyChanged(nameof(KillXPMultiplier));
            }
        }

        private string _harvestXPMultiplier;
        public string HarvestXPMultiplier
        {
            get { return _harvestXPMultiplier; }
            set
            {
                _harvestXPMultiplier = value;
                OnPropertyChanged(nameof(HarvestXPMultiplier));
            }
        }

        private string _craftXPMultiplier;
        public string CraftXPMultiplier
        {
            get { return _craftXPMultiplier; }
            set
            {
                _craftXPMultiplier = value;
                OnPropertyChanged(nameof(CraftXPMultiplier));
            }
        }

        private string _genericXPMultiplier;
        public string GenericXPMultiplier
        {
            get { return _genericXPMultiplier; }
            set
            {
                _genericXPMultiplier = value;
                OnPropertyChanged(nameof(GenericXPMultiplier));
            }
        }

        private string _specialXPMultiplier;
        public string SpecialXPMultiplier
        {
            get { return _specialXPMultiplier; }
            set
            {
                _specialXPMultiplier = value;
                OnPropertyChanged(nameof(SpecialXPMultiplier));
            }
        }

        private string _fuelConsumptionIntervalMultiplier;
        public string FuelConsumptionIntervalMultiplier
        {
            get { return _fuelConsumptionIntervalMultiplier; }
            set
            {
                _fuelConsumptionIntervalMultiplier = value;
                OnPropertyChanged(nameof(FuelConsumptionIntervalMultiplier));
            }
        }

        private string _photoModeRangeLimit;
        public string PhotoModeRangeLimit
        {
            get { return _photoModeRangeLimit; }
            set
            {
                _photoModeRangeLimit = value;
                OnPropertyChanged(nameof(PhotoModeRangeLimit));
            }
        }

        private bool _disablePhotoMode;
        public bool DisablePhotoMode
        {
            get { return _disablePhotoMode; }
            set
            {
                _disablePhotoMode = value;
                OnPropertyChanged(nameof(DisablePhotoMode));
            }
        }

        private bool _increasePvPRespawnInterval;
        public bool IncreasePvPRespawnInterval
        {
            get { return _increasePvPRespawnInterval; }
            set
            {
                _increasePvPRespawnInterval = value;
                OnPropertyChanged(nameof(IncreasePvPRespawnInterval));
            }
        }

        private bool _autoPvETimer;
        public bool AutoPvETimer
        {
            get { return _autoPvETimer; }
            set
            {
                _autoPvETimer = value;
                OnPropertyChanged(nameof(AutoPvETimer));
            }
        }

        private bool _autoPvEUseSystemTime;
        public bool AutoPvEUseSystemTime
        {
            get { return _autoPvEUseSystemTime; }
            set
            {
                _autoPvEUseSystemTime = value;
                OnPropertyChanged(nameof(AutoPvEUseSystemTime));
            }
        }

        private bool _disableFriendlyFire;
        public bool DisableFriendlyFire
        {
            get { return _disableFriendlyFire; }
            set
            {
                _disableFriendlyFire = value;
                OnPropertyChanged(nameof(DisableFriendlyFire));
            }
        }
        private bool _flyerPlatformAllowUnalignedDinoBasing;
        public bool FlyerPlatformAllowUnalignedDinoBasing
        {
            get { return _flyerPlatformAllowUnalignedDinoBasing; }
            set
            {
                _flyerPlatformAllowUnalignedDinoBasing = value;
                OnPropertyChanged(nameof(FlyerPlatformAllowUnalignedDinoBasing));
            }
        }

        private bool _disableLootCrates;
        public bool DisableLootCrates
        {
            get { return _disableLootCrates; }
            set
            {
                _disableLootCrates = value;
                OnPropertyChanged(nameof(DisableLootCrates));
            }
        }

        private bool _allowCustomRecipes;
        public bool AllowCustomRecipes
        {
            get { return _allowCustomRecipes; }
            set
            {
                _allowCustomRecipes = value;
                OnPropertyChanged(nameof(AllowCustomRecipes));
            }
        }


        private bool _pveAllowTribeWar;
        public bool PvEAllowTribeWar
        {
            get { return _pveAllowTribeWar; }
            set
            {
                _pveAllowTribeWar = value;
                OnPropertyChanged(nameof(PvEAllowTribeWar));
            }
        }

        private bool _pveAllowTribeWarCancel;
        public bool PvEAllowTribeWarCancel
        {
            get { return _pveAllowTribeWarCancel; }
            set
            {
                _pveAllowTribeWarCancel = value;
                OnPropertyChanged(nameof(PvEAllowTribeWarCancel));
            }
        }

        private bool _maxDifficulty;
        public bool MaxDifficulty
        {
            get { return _maxDifficulty; }
            set
            {
                _maxDifficulty = value;
                OnPropertyChanged(nameof(MaxDifficulty));
            }
        }

        private bool _useSingleplayerSettings;
        public bool UseSingleplayerSettings
        {
            get { return _useSingleplayerSettings; }
            set
            {
                _useSingleplayerSettings = value;
                OnPropertyChanged(nameof(UseSingleplayerSettings));
            }
        }

        private bool _useCorpseLocator;
        public bool UseCorpseLocator
        {
            get { return _useCorpseLocator; }
            set
            {
                _useCorpseLocator = value;
                OnPropertyChanged(nameof(UseCorpseLocator));
            }
        }

        private bool _showCreativeMode;
        public bool ShowCreativeMode
        {
            get { return _showCreativeMode; }
            set
            {
                _showCreativeMode = value;
                OnPropertyChanged(nameof(ShowCreativeMode));
            }
        }

        private bool _hardLimitTurretsInRange;
        public bool HardLimitTurretsInRange
        {
            get { return _hardLimitTurretsInRange; }
            set
            {
                _hardLimitTurretsInRange = value;
                OnPropertyChanged(nameof(HardLimitTurretsInRange));
            }
        }

        private bool _disableStructurePlacementCollision;
        public bool DisableStructurePlacementCollision
        {
            get { return _disableStructurePlacementCollision; }
            set
            {
                _disableStructurePlacementCollision = value;
                OnPropertyChanged(nameof(DisableStructurePlacementCollision));
            }
        }

        private bool _allowPlatformSaddleMultiFloors;
        public bool AllowPlatformSaddleMultiFloors
        {
            get { return _allowPlatformSaddleMultiFloors; }
            set
            {
                _allowPlatformSaddleMultiFloors = value;
                OnPropertyChanged(nameof(AllowPlatformSaddleMultiFloors));
            }
        }

        private bool _allowUnlimitedRespec;
        public bool AllowUnlimitedRespec
        {
            get { return _allowUnlimitedRespec; }
            set
            {
                _allowUnlimitedRespec = value;
                OnPropertyChanged(nameof(AllowUnlimitedRespec));
            }
        }

        private bool _disableDinoTaming;
        public bool DisableDinoTaming
        {
            get { return _disableDinoTaming; }
            set
            {
                _disableDinoTaming = value;
                OnPropertyChanged(nameof(DisableDinoTaming));
            }
        }
        private string _overrideMaxExperiencePointsDino;
        public string OverrideMaxExperiencePointsDino
        {
            get { return _overrideMaxExperiencePointsDino; }
            set
            {
                _overrideMaxExperiencePointsDino = value;
                OnPropertyChanged(nameof(OverrideMaxExperiencePointsDino));
            }
        }

        private string _maxNumberOfPlayersInTribe;
        public string MaxNumberOfPlayersInTribe
        {
            get { return _maxNumberOfPlayersInTribe; }
            set
            {
                _maxNumberOfPlayersInTribe = value;
                OnPropertyChanged(nameof(MaxNumberOfPlayersInTribe));
            }
        }

        private string _explorerNoteXPMultiplier;
        public string ExplorerNoteXPMultiplier
        {
            get { return _explorerNoteXPMultiplier; }
            set
            {
                _explorerNoteXPMultiplier = value;
                OnPropertyChanged(nameof(ExplorerNoteXPMultiplier));
            }
        }

        private string _bossKillXPMultiplier;
        public string BossKillXPMultiplier
        {
            get { return _bossKillXPMultiplier; }
            set
            {
                _bossKillXPMultiplier = value;
                OnPropertyChanged(nameof(BossKillXPMultiplier));
            }
        }

        private string _alphaKillXPMultiplier;
        public string AlphaKillXPMultiplier
        {
            get { return _alphaKillXPMultiplier; }
            set
            {
                _alphaKillXPMultiplier = value;
                OnPropertyChanged(nameof(AlphaKillXPMultiplier));
            }
        }

        private string _wildKillXPMultiplier;
        public string WildKillXPMultiplier
        {
            get { return _wildKillXPMultiplier; }
            set
            {
                _wildKillXPMultiplier = value;
                OnPropertyChanged(nameof(WildKillXPMultiplier));
            }
        }

        private string _caveKillXPMultiplier;
        public string CaveKillXPMultiplier
        {
            get { return _caveKillXPMultiplier; }
            set
            {
                _caveKillXPMultiplier = value;
                OnPropertyChanged(nameof(CaveKillXPMultiplier));
            }
        }

        private string _tamedKillXPMultiplier;
        public string TamedKillXPMultiplier
        {
            get { return _tamedKillXPMultiplier; }
            set
            {
                _tamedKillXPMultiplier = value;
                OnPropertyChanged(nameof(TamedKillXPMultiplier));
            }
        }

        private string _unclaimedKillXPMultiplier;
        public string UnclaimedKillXPMultiplier
        {
            get { return _unclaimedKillXPMultiplier; }
            set
            {
                _unclaimedKillXPMultiplier = value;
                OnPropertyChanged(nameof(UnclaimedKillXPMultiplier));
            }
        }

        private string _supplyCrateLootQualityMultiplier;
        public string SupplyCrateLootQualityMultiplier
        {
            get { return _supplyCrateLootQualityMultiplier; }
            set
            {
                _supplyCrateLootQualityMultiplier = value;
                OnPropertyChanged(nameof(SupplyCrateLootQualityMultiplier));
            }
        }

        private string _fishingLootQualityMultiplier;
        public string FishingLootQualityMultiplier
        {
            get { return _fishingLootQualityMultiplier; }
            set
            {
                _fishingLootQualityMultiplier = value;
                OnPropertyChanged(nameof(FishingLootQualityMultiplier));
            }
        }

        private string _craftingSkillBonusMultiplier;
        public string CraftingSkillBonusMultiplier
        {
            get { return _craftingSkillBonusMultiplier; }
            set
            {
                _craftingSkillBonusMultiplier = value;
                OnPropertyChanged(nameof(CraftingSkillBonusMultiplier));
            }
        }

        private bool _allowSpeedLeveling;
        public bool AllowSpeedLeveling
        {
            get { return _allowSpeedLeveling; }
            set
            {
                _allowSpeedLeveling = value;
                OnPropertyChanged(nameof(AllowSpeedLeveling));
            }
        }

        private bool _ballowFlyerSpeedLeveling;
        public bool AllowFlyerSpeedLeveling
        {
            get { return _ballowFlyerSpeedLeveling; }
            set
            {
                _ballowFlyerSpeedLeveling = value;
                OnPropertyChanged(nameof(AllowFlyerSpeedLeveling));
            }
        }
























    }

}