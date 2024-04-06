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
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Microsoft.Win32;
using CoreRCON;
using System.Net;
using System.Windows.Forms;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Ark_Ascended_Manager.Helpers;
using System.Text;
using static Ark_Ascended_Manager.ViewModels.Pages.ConfigPageViewModel;
using static Ark_Ascended_Manager.Views.Pages.ConfigPage;
using Logger = Ark_Ascended_Manager.Services.Logger;


namespace Ark_Ascended_Manager.ViewModels.Pages
{

    public class ConfigPageViewModel : ObservableObject
    {
       

        private ObservableCollection<ScheduleTask> _scheduleTasks;
        private string _currentServer;
        private FileSystemWatcher _fileWatcher;
       
        public ObservableCollection<EngramConfig> EngramConfigs { get; set; } = new ObservableCollection<EngramConfig>();

        public ObservableCollection<StackSizeOverride> StackSizeOverrides { get; } = new ObservableCollection<StackSizeOverride>();
        public ICommand DeleteScheduleCommand { get; private set; }
        public ObservableCollection<string> PluginNames { get; set; }
        
        public Dictionary<string, string> OptionsList { get; set; }


        private readonly INavigationService _navigationService;
        public ServerConfig CurrentServerConfig { get; private set; }
       
        public ICommand SaveGameIniSettingsCommand { get; private set; }
        public ICommand LoadLaunchServerSettingsCommand { get; private set; }
        public ICommand StartServerCommand { get; private set; }
        public ICommand UpdateServerCommand { get; private set; }
        public ICommand ToggleTextBoxVisibilityCommand { get; private set; }
        public ICommand StopServerCommand { get; private set; }
        public ICommand SaveGameIniCommand { get; }
        public ICommand SaveGAMEIniFileCommand { get; private set; }
        public ICommand DeleteServerCommand { get; }
        public ICommand WipeServerCommand { get; }
        public ICommand LoadJsonCommand { get; private set; }

        private string _iniContent;
        private CoreRCON.RCON rcon;
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
        private string _selectedOption;
        public string SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (_selectedOption != value)
                {
                    Logger.Log($"Changing SelectedOption from {_selectedOption} to {value}");
                    _selectedOption = value;
                    OnPropertyChanged();

                    // Check if CurrentServerConfig is null before trying to access it
                    if (CurrentServerConfig != null)
                    {
                        CurrentServerConfig.MapName = value;
                        Logger.Log($"CurrentServerConfig.MapName set to {value}");

                        // Save changes to servers.json
                        SaveServerConfigToJson();
                    }
                    else
                    {
                        Logger.Log("CurrentServerConfig is null. Cannot set MapName.");
                    }
                }
                else
                {
                    Logger.Log($"SelectedOption set with same value: {value}. No changes made.");
                }
            }
        }


        private ObservableCollection<EngramOverride> _engramOverrides;
        public ObservableCollection<EngramOverride> EngramOverrides
        {
            get
            {
                if (_engramOverrides == null)
                {
                    _engramOverrides = new ObservableCollection<EngramOverride>();
                }
                return _engramOverrides;
            }
            set
            {
                _engramOverrides = value;
                OnPropertyChanged(nameof(EngramOverrides));
            }
        }




        public ObservableCollection<ScheduleTask> ScheduleTasks
        {
            get => _scheduleTasks;
            set
            {
                _scheduleTasks = value;
                OnPropertyChanged(nameof(ScheduleTasks));
            }
        }



        public ICommand SaveIniFileCommand { get; private set; }
        


        public ConfigPageViewModel(INavigationService navigationService)
        {
            {
                _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            }
            OptionsList = new Dictionary<string, string>
    {
        { "TheIsland_WP", "The Island" },
        { "ScorchedEarth_WP", "Scorched Earth" },
        // ... Add other maps as needed
    };
            LoadServerProfile();
            
            InitializeFileWatcher();
            LoadAndDisplaySchedules();
            SaveGameIniSettingsCommand = new RelayCommand(SaveAllSettings);
            LoadLaunchServerSettingsCommand = new RelayCommand(UpdateLaunchParameters);
            StartServerCommand = new RelayCommand(StartServer);
            UpdateServerCommand = new RelayCommand(UpdateServerBasedOnJson);
            StopServerCommand = new RelayCommand(async () => await InitiateServerShutdownAsync());
            LoadCustomGUSIniFile();
            SaveIniFileCommand = new RelayCommand(SaveCustomGUSIniFile);
            LoadCustomGAMEIniFile();
            SaveGAMEIniFileCommand = new RelayCommand(SaveCustomGAMEIniFile);
            DeleteServerCommand = new RelayCommand(DeleteServer);
            WipeServerCommand = new RelayCommand(WipeServer);
            DeleteScheduleCommand = new RelayCommand<ScheduleTask>(DeleteSchedule);
            LoadPlugins();
            _overrideEnabled = true;
            LoadJsonCommand = new RelayCommand(ExecuteLoadJson);
            StackSizeOverrides = new ObservableCollection<StackSizeOverride>();
            OnPropertyChanged(nameof(OptionsList));

            if (CurrentServerConfig != null)
            {
                Logger.Log($"CurrentServerConfig.MapName: {CurrentServerConfig.MapName}");
                if (OptionsList.ContainsKey(CurrentServerConfig.MapName))
                {
                    SelectedOption = CurrentServerConfig.MapName;
                }
                else
                {
                    Logger.Log("Map name not found in OptionsList. Available maps are:");
                    foreach (var map in OptionsList.Keys)
                    {
                        Logger.Log($"Available map: {map}");
                    }
                }
            }
            else
            {
                Logger.Log("CurrentServerConfig is null.");
            }

            OnPropertyChanged(nameof(SelectedOption));




        }
        public void LoadStackSizeOverrides(string iniFilePath)
        {
            // Read the Game.ini file
            var iniLines = File.ReadAllLines(iniFilePath).ToList();
            // Define a regex pattern to match the stack size override lines
            var pattern = @"ConfigOverrideItemMaxQuantity=\(ItemClassString=""(.+?)"",Quantity=\(MaxItemQuantity=(\d+), bIgnoreMultiplier=(.+?)\)\)";
            var regex = new Regex(pattern);

            foreach (var line in iniLines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    var itemClassString = match.Groups[1].Value;
                    var maxItemQuantity = int.Parse(match.Groups[2].Value);
                    var ignoreMultiplier = bool.Parse(match.Groups[3].Value);

                    var stackSizeOverride = new StackSizeOverride
                    {
                        ItemClassString = itemClassString,
                        MaxItemQuantity = maxItemQuantity,
                        IgnoreMultiplier = ignoreMultiplier
                    };

                    StackSizeOverrides.Add(stackSizeOverride);
                }
            }
        }







        private void DeleteSchedule(ScheduleTask scheduleToDelete)
        {
            if (scheduleToDelete == null) return;

            // Remove the schedule from the ObservableCollection
            ScheduleTasks.Remove(scheduleToDelete);

            // Update the JSON file
            UpdateJsonFile();
        }
        private void UpdateJsonFile()
        {
            try
            {
                string appDataRoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string fullPath = Path.Combine(appDataRoamingPath, "Ark Ascended Manager", "schedules.json");

                // Serialize the updated list to JSON
                string updatedJson = JsonConvert.SerializeObject(ScheduleTasks.ToList());

                // Write the updated JSON back to the file
                File.WriteAllText(fullPath, updatedJson);
            }
            catch (Exception ex)
            {
                // Handle or log the error
                Debug.WriteLine("Error updating JSON file: " + ex.Message);
            }
        }
        private void LoadAndDisplaySchedules()
        {
            var schedules = ReadAndParseJson(@"\AppData\Roaming\Ark Ascended Manager\schedules.json");
            _currentServer = CurrentServerConfig.ProfileName; // Implement this method
            ScheduleTasks = new ObservableCollection<ScheduleTask>(
                schedules.Where(s => s.Server == _currentServer));

        }
        
        private void InitializeFileWatcher()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fullPath = Path.Combine(appDataPath, "Ark Ascended Manager", "schedules.json");

            if (!File.Exists(fullPath))
            {
                return;
            }

            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath));
            _fileWatcher.Changed += (sender, e) => LoadAndDisplaySchedules();
            _fileWatcher.EnableRaisingEvents = true;
        }


        private List<ScheduleTask> ReadAndParseJson(string fileName)
        {
            // Get the full path to the AppData\Roaming directory for the current user
            string appDataRoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fullPath = Path.Combine(appDataRoamingPath, "Ark Ascended Manager", "schedules.json");

            // Log the full path for debugging purposes
            Debug.WriteLine($"Attempting to read from: {fullPath}");

            // Check if the file exists to avoid a FileNotFoundException
            if (!File.Exists(fullPath))
            {
                Debug.WriteLine($"The file at path {fullPath} does not exist.");
                // Return an empty list since the file doesn't exist
                return new List<ScheduleTask>();
            }

            // The file exists, read the file's content and deserialize it into objects
            var fileContent = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<List<ScheduleTask>>(fileContent) ?? new List<ScheduleTask>();
        }







        public class ScheduleTask
        {
            public string Nickname { get; set; }
            public string Action { get; set; }
            public string RconCommand { get; set; }
            public string Time { get; set; }
            public List<string> Days { get; set; }
            public string ReoccurrenceIntervalType { get; set; }
            public int ReoccurrenceInterval { get; set; }
            public string Server { get; set; }
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


        public void LoadJsonForSelectedPlugin(string selectedPlugin)
        {
            string jsonFilePath = Path.Combine(CurrentServerConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins", selectedPlugin, "config.json");

            if (File.Exists(jsonFilePath))
            {
                SelectedPluginConfig = File.ReadAllText(jsonFilePath);
            }
            else
            {
                SelectedPluginConfig = "File not found.";
            }
        }
        private void SaveAllSettings()
        {
            SaveGameIniSettings();
            SaveGameUserSettings();
            UpdateCurrentServerAdminPassword();
            // Display a message box to inform the user that the settings have been saved
            System.Windows.MessageBox.Show("Settings have been saved successfully.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateCurrentServerAdminPassword()
        {
            try
            {
                if (CurrentServerConfig != null)
            {
                CurrentServerConfig.AdminPassword = ServerAdminPassword;
                CurrentServerConfig.ServerName = SessionName;
                SaveServerConfig(CurrentServerConfig.ProfileName);
            }
            else
            {
                // Handle the case where no current server is selected
            }
        }
            catch (Exception ex)
            {
                // Log the error
                Logger.Log($"An error occurred while saving game settings: {ex.Message}");
                // Optionally, log the stack trace or other details
                Logger.Log(ex.StackTrace);
            }
        }
        private void SaveServerConfig(string profileName)
        {
            Debug.WriteLine($"SaveServerConfig: Called for ProfileName '{profileName}' with new AdminPassword and ServerName.");

            var allServerConfigs = ReadAllServerConfigs();
            var serverToUpdate = allServerConfigs.FirstOrDefault(s => s.ProfileName == profileName);

            if (serverToUpdate != null)
            {
                Debug.WriteLine($"SaveServerConfig: Found server configuration. Current AdminPassword: '{serverToUpdate.AdminPassword}', Current ServerName: '{serverToUpdate.ServerName}'");

                // Update the AdminPassword and ServerName for the matched server configuration
                serverToUpdate.AdminPassword = ServerAdminPassword;
                serverToUpdate.ServerName = SessionName;

                Debug.WriteLine($"SaveServerConfig: Updated AdminPassword to: '{ServerAdminPassword}' and ServerName to: '{SessionName}'");

                // Save the updated list back to servers.json
                WriteAllServerConfigs(allServerConfigs);
                Debug.WriteLine("SaveServerConfig: Updated server configurations written back to servers.json");
            }
            else
            {
                Debug.WriteLine("SaveServerConfig: Server configuration not found for the given ProfileName.");
            }
        }








        public void LoadPlugins()
        {
            var pluginsPath = Path.Combine(CurrentServerConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins");

            PluginNames = new ObservableCollection<string>();

            // Check if the directory exists
            if (Directory.Exists(pluginsPath))
            {
                foreach (var dir in Directory.GetDirectories(pluginsPath))
                {
                    var dirName = Path.GetFileName(dir);
                    PluginNames.Add(dirName);
                }
            }
            else
            {
                // Handle the case where the directory doesn't exist
            }

            // Notify UI to update the plugin list
            OnPropertyChanged(nameof(PluginNames));
        }
        private string _selectedPlugin;
        public string SelectedPlugin
        {
            get => _selectedPlugin;
            set
            {
                if (_selectedPlugin != value)
                {
                    _selectedPlugin = value;
                    OnPropertyChanged(nameof(SelectedPlugin));
                    LoadSelectedPluginConfig(value);
                    // This should trigger the breakpoint
                }
            }
        }
        private string _selectedPluginConfig;
        public string SelectedPluginConfig
        {
            get => _selectedPluginConfig;
            set
            {
                if (_selectedPluginConfig != value)
                {
                    _selectedPluginConfig = value;
                    OnPropertyChanged(nameof(SelectedPluginConfig));
                }
            }
        }



        public void UpdateSelectedPluginConfig(string jsonContent)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Ark_Ascended_Manager.Services.Logger.Log("Entering UpdateSelectedPluginConfig method.");
                try
                {
                    Ark_Ascended_Manager.Services.Logger.Log("Original JSON content: " + jsonContent);
                    // Format the JSON to be indented
                    dynamic parsedJson = JsonConvert.DeserializeObject(jsonContent);
                    string formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                    Ark_Ascended_Manager.Services.Logger.Log("Formatted JSON content: " + formattedJson);
                    SelectedPluginConfig = formattedJson;

                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    // Handle JSON formatting error or set raw text
                    Ark_Ascended_Manager.Services.Logger.Log("JSON formatting error: " + ex.ToString());
                    SelectedPluginConfig = jsonContent;
                }

                OnPropertyChanged(nameof(SelectedPluginConfig)); // Explicitly notify UI to update
                Ark_Ascended_Manager.Services.Logger.Log("SelectedPluginConfig property updated.");
            });
        }


        public void LoadSelectedPluginConfig(string selectedPlugin)
        {
            var configFilePath = Path.Combine(CurrentServerConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins", selectedPlugin, "config.json");

            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                Ark_Ascended_Manager.Services.Logger.Log("JSON Content: " + json); // This will print the content to the Output window
                UpdateSelectedPluginConfig(json);
            }
            else
            {
                // Handle the case where the config file doesn't exist
                Ark_Ascended_Manager.Services.Logger.Log("Config file does not exist.");
            }
        }

        private void ExecuteLoadJson()
        {
            
            LoadSelectedPluginConfig(SelectedPlugin);
        }

        public void SaveSelectedPluginConfig(string selectedPlugin, string configContent)
        {
            var configFilePath = Path.Combine(CurrentServerConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins", selectedPlugin, "config.json");

            try
            {
                File.WriteAllText(configFilePath, configContent);
                // Handle post-save operations (e.g., notify user of success)
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., write access issues)
            }
        }


        private void WipeServer()
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to wipe the server? This action cannot be undone.", "Confirm Wipe", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string savedArksPath = Path.Combine(CurrentServerConfig.ServerPath, "ShooterGame", "Saved", "SavedArks");
                    if (Directory.Exists(savedArksPath))
                    {
                        Directory.Delete(savedArksPath, true);
                        System.Windows.MessageBox.Show("The server has been wiped successfully.", "Wipe Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                        // Navigate away or refresh the view as needed
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("The server save directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred while wiping the server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void OnNavigatedTo(object parameter)
        {
            LoadAndDisplaySchedules();
            InitializeFileWatcher();
        }
        private void SaveCustomGAMEIniFile()
        {
            // Save the IniContent back to the file
            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string filePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            File.WriteAllText(filePath, GameIniContent);
            System.Windows.MessageBox.Show("GAME INI file saved successfully.");

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
            System.Windows.MessageBox.Show("GUS file saved successfully.");

        }
        private void DeleteServer()
        {
            // Show confirmation dialog and get the user input
            string userInput = ShowConfirmationDialog("Please type the server name to confirm deletion:");

            if (userInput.Equals(SessionName, StringComparison.OrdinalIgnoreCase))
            {
                // Logic to remove server from servers.json and delete the folder
                _navigationService.Navigate(typeof(DashboardPage));
                RemoveServerAndFolder(SessionName);


            }
            else
            {
                // Notify the user that the names didn't match
                System.Windows.MessageBox.Show("Server name does not match. Deletion cancelled.");
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
                System.Windows.MessageBox.Show("Server not found in the configuration. No action taken.");
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
        // Ensure this method is in the ConfigPageViewModel if that's where it's being called
        

      

        private async Task SendRconCommandAsync(string command)
        {
            if (rcon == null || CurrentServerConfig == null)
            {
                Ark_Ascended_Manager.Services.Logger.Log("RCON connection is not established or server profile is not selected.");
                return;
            }

            try
            {
                // Send the command using the CoreRCON library's method
                string response = await rcon.SendCommandAsync(command);
                Ark_Ascended_Manager.Services.Logger.Log($"RCON command response: {response}");
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception sending RCON command: {ex.Message}");
            }
        }
        private void RemoveServerFromMonitoring(string serverDirectory)
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

        public async Task InitiateServerShutdownAsync()
        {
            Ark_Ascended_Manager.Services.Logger.Log("Shutdown Clicked");

            if (CurrentServerConfig == null)
            {
                System.Windows.MessageBox.Show("Current server configuration is not loaded.");
                return;
            }
            RemoveServerFromMonitoring(CurrentServerConfig.ServerPath);
            // Prompt the user for the countdown time
            string timeInput = Interaction.InputBox(
                "Enter the countdown time in minutes for shutdown:",
                "Shutdown Timer",
                "10"
            );

            if (!int.TryParse(timeInput, out int countdownMinutes) || countdownMinutes <= 0)
            {
                System.Windows.MessageBox.Show("Invalid input for countdown timer.");
                return;
            }

            // Prompt the user for the reason for shutdown
            string reason = Interaction.InputBox(
                "Enter the reason for the shutdown:",
                "Shutdown Reason",
                "Maintenance"
            );

            if (string.IsNullOrEmpty(reason))
            {
                System.Windows.MessageBox.Show("No reason for shutdown provided.");
                return;
            }

            // Ensure RCON is connected before initiating shutdown
            await InitializeRconConnection(CurrentServerConfig);

            // Countdown logic
            for (int minute = countdownMinutes; minute > 0; minute--)
            {
                // Send a warning message to server
                await SendRconCommandAsync($"ServerChat Shutdown in {minute} minutes for {reason}.");
                // Wait for 1 minute
                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            // Save world before shutdown
            await SendRconCommandAsync("saveworld");
            // Shutdown command
            await SendRconCommandAsync("doexit");
        }

        private async Task InitializeRconConnection(ServerConfig profile)
        {
            try
            {
                rcon = new CoreRCON.RCON(IPAddress.Parse("127.0.0.1"), (ushort)profile.RCONPort, profile.AdminPassword);
                await rcon.ConnectAsync(); // Attempt to establish connection
                
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Failed to initialize RCON connection: {ex.Message}");
                
            }
        }






        // Call this method when user clicks the stop button and enters the countdown time
        public async Task OnStopServerClicked()
        {
            
            await InitiateServerShutdownAsync();
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
                        Ark_Ascended_Manager.Services.Logger.Log($"App ID for {mapName} is {serverConfig.AppId}");
                        return serverConfig.AppId;
                    }
                }
            }

            Ark_Ascended_Manager.Services.Logger.Log($"Map name {mapName} not found in servers.json");
            return null; // Or your default app ID
        }


        public void UpdateServerBasedOnJson()
        {
            LoadServerProfile(); // Ensure that CurrentServerConfig is loaded

            ServerConfig currentServerConfig = CurrentServerConfig; // Assuming CurrentServerConfig is a property or field

            Debug.WriteLine($"UpdateServerBasedOnJson: Current Server Config - ProfileName: {currentServerConfig?.ProfileName}, AppId: {currentServerConfig?.AppId}");

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
                UpdateChangeNumberFromJson(currentServerConfig);
            }
            else
            {
                Ark_Ascended_Manager.Services.Logger.Log("Could not update the server, App ID not found or Server Config is null");
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
        private void UpdateChangeNumberFromJson(ServerConfig currentServerConfig)
        {
            Debug.WriteLine("UpdateChangeNumberFromJson: Method called.");

            if (currentServerConfig == null || string.IsNullOrEmpty(currentServerConfig.AppId))
            {
                Debug.WriteLine("UpdateChangeNumberFromJson: CurrentServerConfig is null or AppId is empty.");
                return;
            }

            string jsonFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ark Ascended Manager",
                currentServerConfig.AppId,
                $"sanitizedsteamdata_{currentServerConfig.AppId}.json");

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
                    currentServerConfig.ChangeNumber = json.ChangeNumber;
                    SaveUpdatedServerConfig(currentServerConfig);
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
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == true)
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
                var process = Process.Start(batchFilePath); 
                int pid = process.Id;
                SaveMonitoringInfo(new MonitoringInfo { ServerDirectory = serverDirectory, Pid = pid });
            }
            else
            {
             
          
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



        public string ServerPlatform { get; set; }

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
                                case "Port":
                                    ListenPort = value;
                                    break;
                                case "RconPort":
                                    RconPort = value;
                                    break;
                                case "MaxPlayers":
                                    MaxPlayerCount = value;
                                    break;
                                case "MultiHome":
                                    MultihomeIP = value;
                                    break;
                                case "mods":
                                    Mods = value;
                                    break;
                                case "passivemod":
                                    OverridePassiveMod = value;
                                    break;
                                case "customparameters":
                                    CustomLaunchOptions = value;
                                    break;




                                    // Add cases for any other 'set' values you need to parse
                            }
                        }
                    }
                    else if (line.Trim().StartsWith("start ", StringComparison.OrdinalIgnoreCase))
                    {
                        // Existing settings extraction
                        UseBattleye = line.Contains("-UseBattleye");
                        UseOldConsole = line.Contains("-oldconsole");
                        DisableCustomCosmetics = line.Contains("-DisableCustomCosmetics");
                        AutoDestroyStructures = line.Contains("-AutoDestroyStructures");
                        NotifyAdminCommandsInChat = line.Contains("-NotifyAdminCommandsInChat");
                        ForceRespawnDinos = line.Contains("-ForceRespawnDinos");
                        ServerPlatformSetting = ExtractParameterValue(line, "-ServerPlatform");
                        ServerIP = ExtractParameterValue(line, "-ServerIP");
                        PluginsEnabled = line.Contains("AsaApiLoader.exe");
                        ClusterID = ExtractParameterValue(line, "-clusterid");
                        ClusterDirOverride = ExtractParameterValue(line, "-ClusterDirOverride");

                        // Map name and other parameters extraction from the command
                        var commandParts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (commandParts.Length > 1)
                        {
                            var args = commandParts[2].Split('?');
                            if (args.Length > 0)
                            {
                                var mapNameWithParams = args[0];

                                // Check if mapNameWithParams is an official map
                                if (mapNameWithParams.EndsWith("_WP") && OptionsList.ContainsKey(mapNameWithParams))
                                {
                                    // It's an official map, so no override is needed
                                    OverrideMapName = null;
                                    OverrideEnabled = false;
                                }
                                else
                                {
                                    // It's not an official map, or doesn't follow the naming convention
                                    OverrideMapName = mapNameWithParams;
                                    OverrideEnabled = true;
                                }
                            }
                        }
                    }

                }
            }

            // Trigger UI updates if using data binding
            OnPropertyChanged(nameof(ListenPort));
            OnPropertyChanged(nameof(RconPort));
            OnPropertyChanged(nameof(MaxPlayerCount));
            OnPropertyChanged(nameof(UseBattleye));
            OnPropertyChanged(nameof(UseOldConsole));
            OnPropertyChanged(nameof(DisableCustomCosmetics));
            OnPropertyChanged(nameof(AutoDestroyStructures));
            OnPropertyChanged(nameof(NotifyAdminCommandsInChat));
            OnPropertyChanged(nameof(ForceRespawnDinos));
            OnPropertyChanged(nameof(ServerPlatformSetting));
            OnPropertyChanged(nameof(Mods));
            OnPropertyChanged(nameof(MultihomeIP));
            OnPropertyChanged(nameof(ServerIP));
            OnPropertyChanged(nameof(PluginsEnabled));
            OnPropertyChanged(nameof(ClusterID));
            OnPropertyChanged(nameof(ClusterDirOverride));
        }

        private string ExtractParameterValue(string line, string parameterName)
        {
            // This pattern should capture the value after the parameter name and equals sign, handling quotes if present
            var match = Regex.Match(line, $@"{parameterName}=""?([^""\s]+)""?");
            if (!match.Success)
            {
                // Try another pattern if the parameter can be without quotes
                match = Regex.Match(line, $@"{parameterName}=([^""\s]+)");
            }
            return match.Success ? match.Groups[1].Value : string.Empty;
        }





        private string _serverPlatformSetting;
        public string ServerPlatformSetting
        {
            get { return _serverPlatformSetting; }
            set
            {
                if (_serverPlatformSetting != value)
                {
                    _serverPlatformSetting = value;
                    OnPropertyChanged(nameof(ServerPlatformSetting));
                    // You can also include any validation or transformation logic here
                }
            }
        }

        public class ServerConfigs
        {
            public string ChangeNumberStatus { get; set; }
            public bool IsMapNameOverridden { get; set; }
            public string ProfileName { get; set; }
            public int? Pid { get; set; }
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

            // ... other relevant details
        }




        private void UpdateLaunchParameters()
        {
            string servermap = CurrentServerConfig.MapName;
         
            string serverPath = CurrentServerConfig.ServerPath;
            string batFilePath = Path.Combine(serverPath, "LaunchServer.bat");
            string modsSetting = string.IsNullOrEmpty(Mods) ? "" : $"-mods={Mods}";
            string booleanSettings = ConstructBooleanSettings();
            Ark_Ascended_Manager.Services.Logger.Log("Server Platform Setting before save: " + ServerPlatformSetting);
            string serverPlatformSetting = ServerPlatformSetting;
            Ark_Ascended_Manager.Services.Logger.Log("Server Platform Setting after save: " + ServerPlatformSetting);
            // Determine the executable based on whether plugins are enabled
            string executable = PluginsEnabled ? "AsaApiLoader.exe" : "ArkAscendedServer.exe";
            string mapName = OverrideEnabled && !string.IsNullOrEmpty(OverrideMapName) ? OverrideMapName : servermap;
            string passiveMod = OverridePassiveMod;



            // Construct the batch file content
            string newBatchFileContent = ConstructBatchFileContent(serverPath, executable, modsSetting, booleanSettings, serverPlatformSetting, ServerIP, mapName, passiveMod, CustomLaunchOptions);


            // Write the updated content to the batch file
            File.WriteAllText(batFilePath, newBatchFileContent);
            UpdateServerConfigFromBatch(serverPath);
            SaveServerConfigToJson();
            EnsureClusterDirectoryExists(batFilePath);

            Console.WriteLine("Launch parameters have been updated.");
            System.Windows.MessageBox.Show("Settings have been saved successfully.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EnsureClusterDirectoryExists(string batFilePath)
        {
            string clusterDirOverride = ExtractClusterDirOverride(batFilePath);

            if (!string.IsNullOrWhiteSpace(clusterDirOverride))
            {
                // Check if the directory exists
                if (!Directory.Exists(clusterDirOverride))
                {
                    try
                    {
                        // Attempt to create the directory
                        Directory.CreateDirectory(clusterDirOverride);
                        Ark_Ascended_Manager.Services.Logger.Log($"Cluster directory created: {clusterDirOverride}");
                    }
                    catch (Exception ex)
                    {
                        // Log the exception if directory creation fails
                        Ark_Ascended_Manager.Services.Logger.Log($"Failed to create cluster directory: {ex.Message}");
                    }
                }
                else
                {
                    Ark_Ascended_Manager.Services.Logger.Log($"Cluster directory already exists: {clusterDirOverride}");
                }
            }
        }

        private string ExtractClusterDirOverride(string batFilePath)
        {
            string[] batFileLines = File.ReadAllLines(batFilePath);
            foreach (var line in batFileLines)
            {
                if (line.Contains("-ClusterDirOverride"))
                {
                    var match = Regex.Match(line, @"-ClusterDirOverride=""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return null;
        }

        private void SaveServerConfigToJson()
        {
            // Define the path to the servers.json file
            string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            // Load the existing server configurations
            List<ServerConfig> serverConfigs = new List<ServerConfig>();
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(json) ?? new List<ServerConfig>();
            }

            // Find the existing server config by ProfileName or create a new one if it doesn't exist
            var serverConfig = serverConfigs.FirstOrDefault(sc => sc.ProfileName == CurrentServerConfig.ProfileName);
            if (serverConfig == null)
            {
                serverConfig = new ServerConfig();
                serverConfigs.Add(serverConfig);
            }

            // Update the properties of the server config
            serverConfig.ProfileName = CurrentServerConfig.ProfileName;
            serverConfig.ServerPath = CurrentServerConfig.ServerPath;
            serverConfig.MapName = CurrentServerConfig.MapName; // If you have this value
            serverConfig.AppId = CurrentServerConfig.AppId; // If you have this value
            serverConfig.IsRunning = CurrentServerConfig.IsRunning; // If you have this value
            serverConfig.ListenPort = CurrentServerConfig.ListenPort;
            serverConfig.RCONPort = CurrentServerConfig.RCONPort;
            serverConfig.Mods = CurrentServerConfig.Mods; // If you have this value
            serverConfig.MaxPlayerCount = CurrentServerConfig.MaxPlayerCount;
            serverConfig.UseBattlEye = CurrentServerConfig.UseBattlEye; // If you have this value
            serverConfig.ForceRespawnDinos = CurrentServerConfig.ForceRespawnDinos; // If you have this value
                                                                                            // ... Add any other properties you need to update ...

            // Serialize the updated list of server configs to JSON
            string updatedJson = JsonConvert.SerializeObject(serverConfigs, Formatting.Indented);

            // Write the updated JSON to the servers.json file
            File.WriteAllText(jsonFilePath, updatedJson);

            Ark_Ascended_Manager.Services.Logger.Log("servers.json has been updated with the latest server configuration.");
        }

        private void UpdateServerConfigFromBatch(string serverPath)
        {
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
                                case "Port":
                                    CurrentServerConfig.ListenPort = int.Parse(value);
                                    break;
                                case "RconPort":
                                    CurrentServerConfig.RCONPort = int.Parse(value);
                                    break;
                                case "MaxPlayers":
                                    CurrentServerConfig.MaxPlayerCount = int.Parse(value);
                                    break;
                                case "mods":
                                    CurrentServerConfig.Mods = value.Split(',').ToList();
                                    break;


                                    // Add cases for any other 'set' values you need to parse
                            }
                        }
                    }
                }
            }
        }

        // Then you call SaveServerConfigToJson to save the updated ServerConfig to servers.json
        



        private string ConstructBooleanSettings()
        {
            // Combine all boolean settings into a single string
            string booleanSettings = "";
            booleanSettings += UseBattleye ? " -UseBattleye" : " -NoBattlEye";
            if (UseOldConsole) booleanSettings += " -oldconsole";
            if (DisableCustomCosmetics) booleanSettings += " -DisableCustomCosmetics";
            if (AutoDestroyStructures) booleanSettings += " -AutoDestroyStructures";
            if (NotifyAdminCommandsInChat) booleanSettings += " -NotifyAdminCommandsInChat";
            if (ForceRespawnDinos) booleanSettings += " -ForceRespawnDinos";
            
            return booleanSettings;
        }

        private string ConstructBatchFileContent(string serverPath, string executable, string modsSetting, string booleanSettings, string serverPlatformSetting, string serverIP, string mapName, string passiveMod, string customLaunchOptions)
        {
            string serverIPArgument = !string.IsNullOrWhiteSpace(serverIP) ? $" -ServerIP={serverIP}" : "";
            string serverPlatformArgument = !string.IsNullOrWhiteSpace(serverPlatformSetting) ? $" -ServerPlatform={serverPlatformSetting}" : "";
            string clusterArguments = !string.IsNullOrWhiteSpace(ClusterID) ? $" -clusterid={ClusterID}" : "";
            string clusterDirOverrideArgument = !string.IsNullOrWhiteSpace(ClusterDirOverride) ? $" -ClusterDirOverride=\"{ClusterDirOverride}\"" : "";
            string customLaunchOptionsArgument = !string.IsNullOrWhiteSpace(customLaunchOptions) ? $" {customLaunchOptions}" : "";

            string batchFileContent = $@"
cd /d ""{serverPath}\\ShooterGame\\Binaries\\Win64""
set ServerName={SessionName}
set Port={ListenPort}
set RconPort={RconPort}
set MaxPlayers={MaxPlayerCount}
set mods={Mods}
set MultiHome={MultihomeIP}
set customparameters={customLaunchOptionsArgument}
set passivemod={passiveMod}
set AdditionalSettings=-WinLiveMaxPlayers=%MaxPlayers% -SecureSendArKPayload -ActiveEvent=none -NoTransferFromFiltering -servergamelog -ServerRCONOutputTribeLogs -noundermeshkilling -nosteamclient -game -server -log -mods=%mods% -passivemod=%passivemod%

start {executable} {mapName}?listen?RCONEnabled=True?Port=%Port%?RCONPort=%RconPort%?MultiHome=%MultiHome%{booleanSettings}{serverIPArgument}{serverPlatformArgument}{clusterArguments}{clusterDirOverrideArgument}{customLaunchOptionsArgument} %AdditionalSettings%
".Trim();
            BatchFilePreview = batchFileContent;
            return batchFileContent;
        }


        private string _batchFilePreview;
        public string BatchFilePreview
        {
            get { return _batchFilePreview; }
            set
            {
                if (_batchFilePreview != value)
                {
                    _batchFilePreview = value;
                    OnPropertyChanged(nameof(BatchFilePreview)); 
                }
            }
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

        public string ClusterID { get; set; }
        public string ClusterDirOverride { get; set; }



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
        private bool _useOldConsole;
        public bool UseOldConsole
        {
            get => _useOldConsole;
            set => SetProperty(ref _useOldConsole, value);
        }
        private bool _disableCustomCosmetics;
        public bool DisableCustomCosmetics
        {
            get => _disableCustomCosmetics;
            set => SetProperty(ref _disableCustomCosmetics, value);
        }
        private bool _autoDestroyStructures;
        public bool AutoDestroyStructures
        {
            get => _autoDestroyStructures;
            set => SetProperty(ref _autoDestroyStructures, value);
        }
        private bool _notifyAdminCommandsInChat;
        public bool NotifyAdminCommandsInChat
        {
            get => _notifyAdminCommandsInChat;
            set => SetProperty(ref _notifyAdminCommandsInChat, value);
        }

        private bool _forceRespawnDinos;
        public bool ForceRespawnDinos
        {
            get => _forceRespawnDinos;
            set => SetProperty(ref _forceRespawnDinos, value);
        }


        private bool _disableCrossPlatform;
        public bool DisableCrossPlatform
        {
            get => _disableCrossPlatform;
            set => SetProperty(ref _disableCrossPlatform, value);
        }
        private bool _overrideEnabled = false; // Default value

        public bool OverrideEnabled
        {
            get { return _overrideEnabled; }
            set
            {
                _overrideEnabled = value;
                OnPropertyChanged(nameof(OverrideEnabled));
            }
        }
        private string _overrideMapName;
        public string OverrideMapName
        {
            get { return _overrideMapName; }
            set
            {
                _overrideMapName = value;
                OnPropertyChanged(nameof(OverrideMapName));
                
            }
        }
        private string _overridePassiveMod;
        public string OverridePassiveMod
        {
            get { return _overridePassiveMod; }
            set
            {
                _overridePassiveMod = value;
                OnPropertyChanged(nameof(OverridePassiveMod));

            }
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
                        case "ResourcesRespawnPeriodMultiplier":
                            ResourcesRespawnPeriodMultiplier = value;
                            break;
                        case "DinoCountMultiplier":
                            DinoCountMultiplier = value;
                            break;
                        case "HairGrowthSpeedMultiplier":
                            HairGrowthSpeedMultiplier = value;
                            break;
                        case "BaseTemperatureMultiplier":
                            BaseTemperatureMultiplier = value;
                            break;
                        case "HarvestHealthMultiplier":
                            HarvestHealthMultiplier = value;
                            break;
                        case "PlayerCharacterFoodDrainMultiplier":
                            PlayerCharacterFoodDrainMultiplier = value;
                            break;
                        case "AllowThirdPersonPlayer":
                            AllowThirdPersonPlayer = ConvertToBoolean(value);
                            break;
                        case "AllowCaveBuildingPvE":
                            AllowCaveBuildingPvE = ConvertToBoolean(value);
                            break;
                        case "AllowCaveBuildingPvP":
                            AllowCaveBuildingPvP = ConvertToBoolean(value);
                            break;
                        case "AlwaysNotifyPlayerJoined":
                            AlwaysNotifyPlayerJoined = ConvertToBoolean(value);
                            break;
                        case "AlwaysNotifyPlayerLeft":
                            AlwaysNotifyPlayerLeft = ConvertToBoolean(value);
                            break;
                        case "AllowFlyerCarryPvE":
                            AllowFlyerCarryPvE = ConvertToBoolean(value);
                            break;
                        case "DisableStructureDecayPvE":
                            DisableStructureDecayPvE = ConvertToBoolean(value);
                            break;
                        case "GlobalVoiceChat":
                            GlobalVoiceChat = ConvertToBoolean(value);
                            break;
                        case "MaxStructuresInRange":
                            MaxStructuresInRange = value;
                            break;
                        case "NoTributeDownloads":
                            NoTributeDownloads = ConvertToBoolean(value);
                            break;
                        case "PreventDownloadSurvivors":
                            PreventDownloadSurvivors = ConvertToBoolean(value);
                            break;
                        case "PreventDownloadItems":
                            PreventDownloadItems = ConvertToBoolean(value);
                            break;
                        case "PreventDownloadDinos":
                            PreventDownloadDinos = ConvertToBoolean(value);
                            break;
                        case "TributeItemExpirationSeconds":
                            TributeItemExpirationSeconds = value;
                            break;
                        case "TributeDinoExpirationSeconds":
                            TributeDinoExpirationSeconds = value;
                            break;
                        case "TributeCharacterExpirationSeconds":
                            TributeCharacterExpirationSeconds = value;
                            break;
                        case "ProximityChat":
                            ProximityChat = ConvertToBoolean(value);
                            break;
                        case "ResourceNoReplenishRadiusStructures":
                            ResourceNoReplenishRadiusStructures = value;
                            break;
                        case "ServerAdminPassword":
                            ServerAdminPassword = value;
                            break;
                        case "SessionName":
                            SessionName = value;
                            break;
                        case "ServerCrosshair":
                            ServerCrosshair = ConvertToBoolean(value);
                            break;
                        case "ServerForceNoHud":
                            ServerForceNoHud = ConvertToBoolean(value);
                            break;
                        case "ServerHardcore":
                            ServerHardcore = ConvertToBoolean(value);
                            break;
                        case "ServerPvE":
                            ServerPvE = ConvertToBoolean(value);
                            break;
                        case "ShowMapPlayerLocation":
                            ShowMapPlayerLocation = ConvertToBoolean(value);
                            break;
                        case "TamedDinoDamageMultiplier":
                            TamedDinoDamageMultiplier = value;
                            break;
                        case "DinoResistanceMultiplier":
                            DinoResistanceMultiplier = value;
                            break;
                        case "DinoDamageMultiplier":
                            DinoDamageMultiplier = value;
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
                            EnablePVPGamma = ConvertToBoolean(value);
                            break;
                        case "EnablePVEGamma":
                            EnablePVEGamma = ConvertToBoolean(value);
                            break;
                        case "SpectatorPassword":
                            SpectatorPassword = value;
                            break;
                        case "ServerPassword":
                            ServerPassword = value;
                            break;
                        case "AdminPassword":
                            AdminPassword = value;
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
                        case "CosmeticWhitelistOverride":
                            CosmeticWhitelistOverride = value;
                            break;
                        case "Message":
                            MOTD = value;
                            break;
                        case "Duration":
                            Duration = value;
                            break;
                        case "DisableDinoDecayPvE":
                            DisableDinoDecayPvE = ConvertToBoolean(value);
                            break;
                        case "PvEDinoDecayPeriodMultiplier":
                            PvEDinoDecayPeriodMultiplier = value;
                            break;
                        case "AdminLogging":
                            AdminLogging = ConvertToBoolean(value);
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
                            ForceAllStructureLocking = ConvertToBoolean(value);
                            break;
                        case "AutoDestroyOldStructuresMultiplier":
                            AutoDestroyOldStructuresMultiplier = value;
                            break;
                        case "StructureDamageMultiplier":
                            StructureDamageMultiplier = value;
                            break;
                        case "StructureResistanceMultiplier":
                            StructureResistanceMultiplier = value;
                            break;
                        case "AutoDestroyStructures":
                            AutoDestroyStructures = ConvertToBoolean(value);
                            break;
                        case "UseVSync":
                            UseVSync = ConvertToBoolean(value);
                            break;
                        case "MaxPlatformSaddleStructureLimit":
                            MaxPlatformSaddleStructureLimit = value;
                            break;
                        case "PassiveDefensesDamageRiderlessDinos":
                            PassiveDefensesDamageRiderlessDinos = ConvertToBoolean(value);
                            break;
                        case "AutoSavePeriodMinutes":
                            AutoSavePeriodMinutes = value;
                            break;
                        case "RCONServerGameLogBuffer":
                            RCONServerGameLogBuffer = value;
                            break;
                        case "OverrideStructurePlatformPrevention":
                            OverrideStructurePlatformPrevention = ConvertToBoolean(value);
                            break;
                        case "PreventOfflinePvPInterval":
                            PreventOfflinePvPInterval = value;
                            break;
                        case "bPvPDinoDecay":
                            BPvPDinoDecay = ConvertToBoolean(value);
                            break;
                        case "PreventOfflinePvP":
                            PreventOfflinePvP = ConvertToBoolean(value);
                            break;
                        case "bPvPStructureDecay":
                            BPvPStructureDecay = ConvertToBoolean(value);
                            break;
                        case "DisableImprintDinoBuff":
                            DisableImprintDinoBuff = ConvertToBoolean(value);
                            break;
                        case "AllowAnyoneBabyImprintCuddle":
                            AllowAnyoneBabyImprintCuddle = ConvertToBoolean(value);
                            break;
                        case "EnableExtraStructurePreventionVolumes":
                            EnableExtraStructurePreventionVolumes = ConvertToBoolean(value);
                            break;
                        case "ShowFloatingDamageText":
                            ShowFloatingDamageText = ConvertToBoolean(value);
                            break;
                        case "DestroyUnconnectedWaterPipes":
                            DestroyUnconnectedWaterPipes = ConvertToBoolean(value);
                            break;
                        case "OverrideOfficialDifficulty":
                            OverrideOfficialDifficulty = value;
                            break;
                        case "TheMaxStructuresInRange":
                            TheMaxStructuresInRange = value;
                            break;
                        case "MinimumDinoReuploadInterval":
                            MinimumDinoReuploadInterval = value;
                            break;
                        case "PvEAllowStructuresAtSupplyDrops":
                            PvEAllowStructuresAtSupplyDrops = ConvertToBoolean(value);
                            break;
                        case "NPCNetworkStasisRangeScalePlayerCountStart":
                            NPCNetworkStasisRangeScalePlayerCountStart = value;
                            break;
                        case "MaxTamedDinos_SoftTameLimit":
                            MaxTamedDinosSoftTameLimit = value;
                            break;
                        case "MaxTamedDinos_SoftTameLimit_CountdownForDeletionDuration":
                            MaxTamedDinosSoftTameLimitCountdownForDeletionDuration = value;
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
                        case "DayCycleSpeedScale":
                            DayCycleSpeedScale = value;
                            break;
                        case "DayTimeSpeedScale":
                            DayTimeSpeedScale = value;
                            break;
                        case "NightTimeSpeedScale":
                            NightTimeSpeedScale = value;
                            break;
                        case "AutoDestroyDecayedDinos":
                            AutoDestroyDecayedDinos = ConvertToBoolean(value);
                            break;
                        case "PlayerCharacterWaterDrainMultiplier":
                            PlayerCharacterWaterDrainMultiplier = value;
                            break;
                        case "ClampItemSpoilingTimes":
                            ClampItemSpoilingTimes = ConvertToBoolean(value);
                            break;
                        case "UseOptimizedHarvestingHealth":
                            UseOptimizedHarvestingHealth = ConvertToBoolean(value);
                            break;
                        case "ClampResourceHarvestDamage":
                            ClampResourceHarvestDamage = ConvertToBoolean(value);
                            break;
                        case "AllowCrateSpawnsOnTopOfStructures":
                            AllowCrateSpawnsOnTopOfStructures = ConvertToBoolean(value);
                            break;
                        case "ForceFlyerExplosives":
                            ForceFlyerExplosives = ConvertToBoolean(value);
                            break;
                        case "AllowFlyingStaminaRecovery":
                            AllowFlyingStaminaRecovery = ConvertToBoolean(value);
                            break;
                        case "DinoCharacterStaminaDrainMultiplier":
                            DinoCharacterStaminaDrainMultiplier = value;
                            break;
                        case "DinoCharacterHealthRecoveryMultiplier":
                            DinoCharacterHealthRecoveryMultiplier = value;
                            break;
                        case "OxygenSwimSpeedStatMultiplier":
                            OxygenSwimSpeedStatMultiplier = value;
                            break;
                        case "bPvEDisableFriendlyFire":
                            BPvEDisableFriendlyFire = ConvertToBoolean(value);
                            break;
                        case "ServerAutoForceRespawnWildDinosInterval":
                            ServerAutoForceRespawnWildDinosInterval = value;
                            break;
                        case "DisableWeatherFog":
                            DisableWeatherFog = ConvertToBoolean(value);
                            break;
                        case "RandomSupplyCratePoints":
                            RandomSupplyCratePoints = ConvertToBoolean(value);
                            break;
                        case "CrossARKAllowForeignDinoDownloads":
                            CrossARKAllowForeignDinoDownloads = ConvertToBoolean(value);
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
                        case "TribeSlotReuseCooldown":
                            TribeSlotReuseCooldown = value;
                            break;
                        case "PlatformSaddleBuildAreaBoundsMultiplier":
                            PlatformSaddleBuildAreaBoundsMultiplier = value;
                            break;
                        case "AlwaysAllowStructurePickup":
                            AlwaysAllowStructurePickup = ConvertToBoolean(value);
                            break;
                        case "StructurePickupTimeAfterPlacement":
                            StructurePickupTimeAfterPlacement = value;
                            break;
                        case "StructurePickupHoldDuration":
                            StructurePickupHoldDuration = value;
                            break;
                        case "AllowHideDamageSourceFromLogs":
                            AllowHideDamageSourceFromLogs = ConvertToBoolean(value);
                            break;
                        case "RaidDinoCharacterFoodDrainMultiplier":
                            RaidDinoCharacterFoodDrainMultiplier = value;
                            break;
                        case "ItemStackSizeMultiplier":
                            ItemStackSizeMultiplier = value;
                            break;
                        case "AllowHitMarkers":
                            AllowHitMarkers = ConvertToBoolean(value);
                            break;
                        case "PreventSpawnAnimations":
                            PreventSpawnAnimations = ConvertToBoolean(value);
                            break;
                        case "AllowMultipleAttachedC4":
                            AllowMultipleAttachedC4 = ConvertToBoolean(value);
                            break;





                    }
                }
            }
        }
        bool ConvertToBoolean(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            value = value.Trim();

            // Handle "True" and "False" values
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            // Handle "0" as false, "1" as true
            if (value == "0")
            {
                return false;
            }
            else if (value == "1")
            {
                return true;
            }

            // If none of the above conditions are met, the input is invalid
            throw new FormatException($"String '{value}' was not recognized as a valid Boolean.");
        }


        private void SaveGameUserSettings()
        {
            try
            {
                string serverPath = CurrentServerConfig.ServerPath;
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");

            // Read all lines
            var lines = File.ReadAllLines(iniFilePath).ToList();

            // Update specific lines
            UpdateLine(ref lines, "ServerSettings", "HarvestAmountMultiplier", HarvestAmountMultiplier);
            UpdateLine(ref lines, "ServerSettings", "ResourcesRespawnPeriodMultiplier", ResourcesRespawnPeriodMultiplier);
            UpdateLine(ref lines, "ServerSettings", "DayTimeSpeedScale", DayTimeSpeedScale);
            UpdateLine(ref lines, "ServerSettings", "DayCycleSpeedScale", DayCycleSpeedScale);
            UpdateLine(ref lines, "ServerSettings", "NightTimeSpeedScale", NightTimeSpeedScale);
            UpdateLine(ref lines, "ServerSettings", "DinoCountMultiplier", DinoCountMultiplier);
            UpdateLine(ref lines, "ServerSettings", "HairGrowthSpeedMultiplier", HairGrowthSpeedMultiplier);
            UpdateLine(ref lines, "ServerSettings", "BaseTemperatureMultiplier", BaseTemperatureMultiplier);
            UpdateLine(ref lines, "ServerSettings", "HarvestHealthMultiplier", HarvestHealthMultiplier);
            UpdateLine(ref lines, "ServerSettings", "AllowThirdPersonPlayer", AllowThirdPersonPlayer.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowCaveBuildingPvE", AllowCaveBuildingPvE.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowCaveBuildingPvP", AllowCaveBuildingPvP.ToString());
            UpdateLine(ref lines, "ServerSettings", "AlwaysNotifyPlayerJoined", AlwaysNotifyPlayerJoined.ToString());
            UpdateLine(ref lines, "ServerSettings", "AlwaysNotifyPlayerLeft", AlwaysNotifyPlayerLeft.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowFlyerCarryPvE", AllowFlyerCarryPvE.ToString());
            UpdateLine(ref lines, "ServerSettings", "DisableStructureDecayPvE", DisableStructureDecayPvE.ToString());
            UpdateLine(ref lines, "ServerSettings", "GlobalVoiceChat", GlobalVoiceChat.ToString());
            UpdateLine(ref lines, "ServerSettings", "MaxStructuresInRange", MaxStructuresInRange);
            UpdateLine(ref lines, "ServerSettings", "NoTributeDownloads", NoTributeDownloads.ToString());
            UpdateLine(ref lines, "ServerSettings", "PreventDownloadSurvivors", PreventDownloadSurvivors.ToString());
            UpdateLine(ref lines, "ServerSettings", "PreventDownloadItems", PreventDownloadItems.ToString());
            UpdateLine(ref lines, "ServerSettings", "PreventDownloadDinos", PreventDownloadDinos.ToString());
            UpdateLine(ref lines, "ServerSettings", "TributeItemExpirationSeconds", TributeItemExpirationSeconds?.ToString() ?? "86400");
            UpdateLine(ref lines, "ServerSettings", "TributeDinoExpirationSeconds", TributeDinoExpirationSeconds.ToString() ?? "86400");
            UpdateLine(ref lines, "ServerSettings", "TributeCharacterExpirationSeconds", TributeCharacterExpirationSeconds.ToString() ?? "86400");
            UpdateLine(ref lines, "ServerSettings", "ProximityChat", ProximityChat.ToString());
            UpdateLine(ref lines, "ServerSettings", "ResourceNoReplenishRadiusStructures", ResourceNoReplenishRadiusStructures);
            UpdateLine(ref lines, "ServerSettings", "ServerAdminPassword", ServerAdminPassword);
            UpdateLine(ref lines, "SessionSettings", "SessionName", SessionName);
            UpdateLine(ref lines, "ServerSettings", "PlayerCharacterFoodDrainMultiplier", PlayerCharacterFoodDrainMultiplier);
            UpdateLine(ref lines, "ServerSettings", "PlayerCharacterWaterDrainMultiplier", PlayerCharacterWaterDrainMultiplier);
            UpdateLine(ref lines, "ServerSettings", "ServerCrosshair", ServerCrosshair.ToString());
            UpdateLine(ref lines, "ServerSettings", "ServerForceNoHud", ServerForceNoHud.ToString());
            UpdateLine(ref lines, "ServerSettings", "ServerHardcore", ServerHardcore.ToString());
            UpdateLine(ref lines, "ServerSettings", "ServerPvE", ServerPvE.ToString());
            UpdateLine(ref lines, "ServerSettings", "ShowMapPlayerLocation", ShowMapPlayerLocation.ToString());
            UpdateLine(ref lines, "ServerSettings", "TamedDinoDamageMultiplier", TamedDinoDamageMultiplier);
            UpdateLine(ref lines, "ServerSettings", "DinoResistanceMultiplier", DinoResistanceMultiplier);
            UpdateLine(ref lines, "ServerSettings", "DinoDamageMultiplier", DinoDamageMultiplier);
            UpdateLine(ref lines, "ServerSettings", "TamedDinoResistanceMultiplier", TamedDinoResistanceMultiplier);
            UpdateLine(ref lines, "ServerSettings", "TamingSpeedMultiplier", TamingSpeedMultiplier);
            UpdateLine(ref lines, "ServerSettings", "DinoCharacterStaminaDrainMultiplier", DinoCharacterStaminaDrainMultiplier);
            UpdateLine(ref lines, "ServerSettings", "XPMultiplier", XPMultiplier);
            UpdateLine(ref lines, "ServerSettings", "DinoCharacterHealthRecoveryMultiplier", DinoCharacterHealthRecoveryMultiplier);
            UpdateLine(ref lines, "ServerSettings", "EnablePVPGamma", EnablePVPGamma.ToString());
            UpdateLine(ref lines, "ServerSettings", "EnablePVEGamma", EnablePVEGamma.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowFlyingStaminaRecovery", AllowFlyingStaminaRecovery.ToString());
            UpdateLine(ref lines, "ServerSettings", "SpectatorPassword", SpectatorPassword);
            UpdateLine(ref lines, "ServerSettings", "ServerPassword", ServerPassword);
            UpdateLine(ref lines, "ServerSettings", "AdminPassword", AdminPassword);
            UpdateLine(ref lines, "ServerSettings", "DifficultyOffset", DifficultyOffset);
            UpdateLine(ref lines, "ServerSettings", "PvEStructureDecayDestructionPeriod", PvEStructureDecayDestructionPeriod);
            UpdateLine(ref lines, "ServerSettings", "Banlist", Banlist);
            UpdateLine(ref lines, "ServerSettings", "CosmeticWhitelistOverride", CosmeticWhitelistOverride);
            UpdateLine(ref lines, "MessageOfTheDay", "Message", MOTD);
            UpdateLine(ref lines, "MessageOfTheDay", "Duration", Duration);
            UpdateLine(ref lines, "ServerSettings", "ServerAutoForceRespawnWildDinosInterval", ServerAutoForceRespawnWildDinosInterval);
            UpdateLine(ref lines, "ServerSettings", "DisableDinoDecayPvE", DisableDinoDecayPvE.ToString());
            UpdateLine(ref lines, "ServerSettings", "PvEDinoDecayPeriodMultiplier", PvEDinoDecayPeriodMultiplier);
            UpdateLine(ref lines, "ServerSettings", "AdminLogging", AdminLogging.ToString());
            UpdateLine(ref lines, "ServerSettings", "MaxTamedDinos", MaxTamedDinos);
            UpdateLine(ref lines, "ServerSettings", "MaxNumbersofPlayersInTribe", MaxNumbersofPlayersInTribe);
            UpdateLine(ref lines, "ServerSettings", "BattleNumOfTribestoStartGame", BattleNumOfTribestoStartGame);
            UpdateLine(ref lines, "ServerSettings", "TimeToCollapseROD", TimeToCollapseROD);
            UpdateLine(ref lines, "ServerSettings", "BattleAutoStartGameInterval", BattleAutoStartGameInterval);
            UpdateLine(ref lines, "ServerSettings", "BattleSuddenDeathInterval", BattleSuddenDeathInterval);
            UpdateLine(ref lines, "ServerSettings", "KickIdlePlayersPeriod", KickIdlePlayersPeriod);
            UpdateLine(ref lines, "ServerSettings", "PerPlatformMaxStructuresMultiplier", PerPlatformMaxStructuresMultiplier);
            UpdateLine(ref lines, "ServerSettings", "ForceAllStructureLocking", ForceAllStructureLocking.ToString());
            UpdateLine(ref lines, "ServerSettings", "AutoDestroyOldStructuresMultiplier", AutoDestroyOldStructuresMultiplier);
            UpdateLine(ref lines, "ServerSettings", "StructureDamageMultiplier", StructureDamageMultiplier);
            UpdateLine(ref lines, "ServerSettings", "StructureResistanceMultiplier", StructureResistanceMultiplier);
            UpdateLine(ref lines, "ServerSettings", "AutoDestroyStructures", AutoDestroyStructures.ToString());
            UpdateLine(ref lines, "ServerSettings", "UseVSync", UseVSync.ToString());
            UpdateLine(ref lines, "ServerSettings", "PreventSpawnAnimations", PreventSpawnAnimations.ToString());
            UpdateLine(ref lines, "ServerSettings", "MaxPlatformSaddleStructureLimit", MaxPlatformSaddleStructureLimit);
            UpdateLine(ref lines, "ServerSettings", "PassiveDefensesDamageRiderlessDinos", PassiveDefensesDamageRiderlessDinos.ToString());
            UpdateLine(ref lines, "ServerSettings", "bPvEDisableFriendlyFire", BPvEDisableFriendlyFire.ToString());
            UpdateLine(ref lines, "ServerSettings", "AutoSavePeriodMinutes", AutoSavePeriodMinutes);
            UpdateLine(ref lines, "ServerSettings", "RCONServerGameLogBuffer", RCONServerGameLogBuffer);
            UpdateLine(ref lines, "ServerSettings", "OverrideStructurePlatformPrevention", OverrideStructurePlatformPrevention.ToString());
            UpdateLine(ref lines, "ServerSettings", "bPvPDinoDecay", BPvPDinoDecay.ToString());
            UpdateLine(ref lines, "ServerSettings", "bPvPStructureDecay", BPvPStructureDecay.ToString());
            UpdateLine(ref lines, "ServerSettings", "DisableImprintDinoBuff", DisableImprintDinoBuff.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowAnyoneBabyImprintCuddle", AllowAnyoneBabyImprintCuddle.ToString());
            UpdateLine(ref lines, "ServerSettings", "EnableExtraStructurePreventionVolumes", EnableExtraStructurePreventionVolumes.ToString());
            UpdateLine(ref lines, "ServerSettings", "ShowFloatingDamageText", ShowFloatingDamageText.ToString());
            UpdateLine(ref lines, "ServerSettings", "DestroyUnconnectedWaterPipes", DestroyUnconnectedWaterPipes.ToString());
            UpdateLine(ref lines, "ServerSettings", "OverrideOfficialDifficulty", OverrideOfficialDifficulty);
            UpdateLine(ref lines, "ServerSettings", "TheMaxStructuresInRange", TheMaxStructuresInRange);
            UpdateLine(ref lines, "ServerSettings", "MinimumDinoReuploadInterval", MinimumDinoReuploadInterval.ToString());
            UpdateLine(ref lines, "ServerSettings", "PvEAllowStructuresAtSupplyDrops", PvEAllowStructuresAtSupplyDrops.ToString());
            UpdateLine(ref lines, "ServerSettings", "NPCNetworkStasisRangeScalePlayerCountStart", NPCNetworkStasisRangeScalePlayerCountStart);
            UpdateLine(ref lines, "ServerSettings", "MaxTamedDinos_SoftTameLimit", MaxTamedDinosSoftTameLimit);
            UpdateLine(ref lines, "ServerSettings", "MaxTamedDinos_SoftTameLimit_CountdownForDeletionDuration", MaxTamedDinosSoftTameLimitCountdownForDeletionDuration);
            UpdateLine(ref lines, "ServerSettings", "NPCNetworkStasisRangeScalePlayerCountEnd", NPCNetworkStasisRangeScalePlayerCountEnd);
            UpdateLine(ref lines, "ServerSettings", "NPCNetworkStasisRangeScalePercentEnd", NPCNetworkStasisRangeScalePercentEnd);
            UpdateLine(ref lines, "ServerSettings", "MaxPersonalTamedDinos", MaxPersonalTamedDinos);
            UpdateLine(ref lines, "ServerSettings", "PreventOfflinePvPInterval", PreventOfflinePvPInterval);
            UpdateLine(ref lines, "ServerSettings", "PreventOfflinePvP", PreventOfflinePvP.ToString());
            UpdateLine(ref lines, "ServerSettings", "AutoDestroyDecayedDinos", AutoDestroyDecayedDinos.ToString());
            UpdateLine(ref lines, "ServerSettings", "ClampItemSpoilingTimes", ClampItemSpoilingTimes.ToString());
            UpdateLine(ref lines, "ServerSettings", "UseOptimizedHarvestingHealth", UseOptimizedHarvestingHealth.ToString());
            UpdateLine(ref lines, "ServerSettings", "ClampResourceHarvestDamage", ClampResourceHarvestDamage.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowCrateSpawnsOnTopOfStructures", AllowCrateSpawnsOnTopOfStructures.ToString());
            UpdateLine(ref lines, "ServerSettings", "ForceFlyerExplosives", ForceFlyerExplosives.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowMultipleAttachedC4", AllowMultipleAttachedC4.ToString());
            UpdateLine(ref lines, "ServerSettings", "DisableWeatherFog", DisableWeatherFog.ToString());
            UpdateLine(ref lines, "ServerSettings", "RandomSupplyCratePoints", RandomSupplyCratePoints.ToString());
            UpdateLine(ref lines, "ServerSettings", "CrossARKAllowForeignDinoDownloads", CrossARKAllowForeignDinoDownloads.ToString());
            UpdateLine(ref lines, "ServerSettings", "AlwaysAllowStructurePickup", AlwaysAllowStructurePickup.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowHideDamageSourceFromLogs", AllowHideDamageSourceFromLogs.ToString());
            UpdateLine(ref lines, "ServerSettings", "AllowHitMarkers", AllowHitMarkers.ToString());
            UpdateLine(ref lines, "ServerSettings", "OxygenSwimSpeedStatMultiplier", OxygenSwimSpeedStatMultiplier);
            UpdateLine(ref lines, "ServerSettings", "PersonalTamedDinosSaddleStructureCost", PersonalTamedDinosSaddleStructureCost);
            UpdateLine(ref lines, "ServerSettings", "StructurePreventResourceRadiusMultiplier", StructurePreventResourceRadiusMultiplier);
            UpdateLine(ref lines, "ServerSettings", "TribeNameChangeCooldown", TribeNameChangeCooldown);
            UpdateLine(ref lines, "ServerSettings", "TribeSlotReuseCooldown", TribeSlotReuseCooldown);
            UpdateLine(ref lines, "ServerSettings", "PlatformSaddleBuildAreaBoundsMultiplier", PlatformSaddleBuildAreaBoundsMultiplier);
            UpdateLine(ref lines, "ServerSettings", "StructurePickupHoldDuration", StructurePickupHoldDuration);
            UpdateLine(ref lines, "ServerSettings", "StructurePickupTimeAfterPlacement", StructurePickupTimeAfterPlacement);
            UpdateLine(ref lines, "ServerSettings", "RaidDinoCharacterFoodDrainMultiplier", RaidDinoCharacterFoodDrainMultiplier);

            // ... Repeat for other properties ...

            // Write the updated lines back to the file
            File.WriteAllLines(iniFilePath, lines);
        }
            catch (Exception ex)
            {
                // Log the error
                Logger.Log($"An error occurred while saving game settings: {ex.Message}");
                // Optionally, log the stack trace or other details
                Logger.Log(ex.StackTrace);
            }
        }

        private void UpdateLine(ref List<string> lines, string header, string key, string newValue)
        {
            // Null checks to prevent NullReferenceException
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (header == null) throw new ArgumentNullException(nameof(header));
            if (key == null) throw new ArgumentNullException(nameof(key));

            // Ensure that the new value is not null, if it is null replace with empty string
            newValue ??= "";

            // Ensure the header is formatted correctly
            string formattedHeader = $"[{header}]";

            // Find the index of the header, if it doesn't exist, add it
            int headerIndex = lines.FindIndex(line => line.Trim().Equals(formattedHeader, StringComparison.OrdinalIgnoreCase));
            if (headerIndex == -1)
            {
                // Append the new section at the end of the file
                lines.Add(formattedHeader);
                lines.Add($"{key}={newValue}");
                return;
            }

            // Calculate the range of lines under this header
            int sectionStart = headerIndex + 1;
            int sectionEnd = lines.FindIndex(sectionStart, line => line.Trim().StartsWith("[") && line.Trim().EndsWith("]"));
            sectionEnd = (sectionEnd == -1) ? lines.Count : sectionEnd;

            // Find the index of the key within the section
            int keyIndex = lines.FindIndex(sectionStart, sectionEnd - sectionStart, line => line.Trim().StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase));

            if (keyIndex != -1)
            {
                // If the key exists, update it
                lines[keyIndex] = $"{key}={newValue}";
            }
            else
            {
                // If the key doesn't exist, add it at the end of the section
                lines.Insert(sectionEnd, $"{key}={newValue}");
            }
        }







        private string _harvestHealthMultiplier;
        public string HarvestHealthMultiplier
        {
            get { return _harvestHealthMultiplier; }
            set
            {
                _harvestHealthMultiplier = value;
                OnPropertyChanged(nameof(HarvestHealthMultiplier)); // Notify the UI of the change
            }
        }
        private string _dinoCountMultiplier;
        public string DinoCountMultiplier
        {
            get { return _dinoCountMultiplier; }
            set
            {
                _dinoCountMultiplier = value;
                OnPropertyChanged(nameof(DinoCountMultiplier)); // Notify the UI of the change
            }
        }
        private string _dayTimeSpeedScale;
        public string DayTimeSpeedScale
        {
            get { return _dayTimeSpeedScale; }
            set
            {
                _dayTimeSpeedScale = value;
                OnPropertyChanged(nameof(DayTimeSpeedScale)); // Notify the UI of the change
            }
        }
        private string _dayCycleSpeedScale;
        public string DayCycleSpeedScale
        {
            get { return _dayCycleSpeedScale; }
            set
            {
                _dayCycleSpeedScale = value;
                OnPropertyChanged(nameof(DayCycleSpeedScale)); // Notify the UI of the change
            }
        }
        private string _nightTimeSpeedScale;
        public string NightTimeSpeedScale
        {
            get { return _nightTimeSpeedScale; }
            set
            {
                _nightTimeSpeedScale = value;
                OnPropertyChanged(nameof(NightTimeSpeedScale)); // Notify the UI of the change
            }
        }
        private string _hairGrowthSpeedMultiplier;
        public string HairGrowthSpeedMultiplier
        {
            get { return _hairGrowthSpeedMultiplier; }
            set
            {
                _hairGrowthSpeedMultiplier = value;
                OnPropertyChanged(nameof(HairGrowthSpeedMultiplier)); // Notify the UI of the change
            }
        }
        private string _baseTemperatureMultiplier;
        public string BaseTemperatureMultiplier
        {
            get { return _baseTemperatureMultiplier; }
            set
            {
                _baseTemperatureMultiplier = value;
                OnPropertyChanged(nameof(BaseTemperatureMultiplier)); // Notify the UI of the change
            }
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
        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }
        private string _resourcesRespawnPeriodMultiplier;
        public string ResourcesRespawnPeriodMultiplier
        {
            get { return _resourcesRespawnPeriodMultiplier; }
            set
            {
                _resourcesRespawnPeriodMultiplier = value;
                OnPropertyChanged(nameof(ResourcesRespawnPeriodMultiplier)); // Notify the UI of the change
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
        private bool _allowCaveBuildingPvP;
        public bool AllowCaveBuildingPvP
        {
            get { return _allowCaveBuildingPvP; }
            set
            {
                _allowCaveBuildingPvP = value;
                OnPropertyChanged(nameof(AllowCaveBuildingPvP)); // Notify the UI of the change
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
        private string _tributeItemExpirationSeconds;
        public string TributeItemExpirationSeconds
        {
            get { return _tributeItemExpirationSeconds; }
            set
            {
                _tributeItemExpirationSeconds = value;
                OnPropertyChanged(nameof(TributeItemExpirationSeconds)); // Notify the UI of the change
            }
        }
        private string _tributeDinoExpirationSeconds;
        public string TributeDinoExpirationSeconds
        {
            get { return _tributeDinoExpirationSeconds; }
            set
            {
                _tributeDinoExpirationSeconds = value;
                OnPropertyChanged(nameof(TributeDinoExpirationSeconds)); // Notify the UI of the change
            }
        }
        private string _tributeCharacterExpirationSeconds;
        public string TributeCharacterExpirationSeconds
        {
            get { return _tributeCharacterExpirationSeconds; }
            set
            {
                _tributeCharacterExpirationSeconds = value;
                OnPropertyChanged(nameof(TributeCharacterExpirationSeconds)); // Notify the UI of the change
            }
        }
        private bool _clampResourceHarvestDamage;
        public bool ClampResourceHarvestDamage
        {
            get { return _clampResourceHarvestDamage; }
            set
            {
                _clampResourceHarvestDamage = value;
                OnPropertyChanged(nameof(ClampResourceHarvestDamage)); // Notify the UI of the change
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
        private string _sessionName;
        public string SessionName
        {
            get { return _sessionName; }
            set
            {
                _sessionName = value;
                OnPropertyChanged(nameof(SessionName)); // Notify the UI of the change
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
        private string _dinoResistanceMultiplier;
        public string DinoResistanceMultiplier
        {
            get { return _dinoResistanceMultiplier; }
            set
            {
                _dinoResistanceMultiplier = value;
                OnPropertyChanged(nameof(DinoResistanceMultiplier)); // Notify the UI of the change
            }
        }
        private string _dinoDamageMultiplier;
        public string DinoDamageMultiplier
        {
            get { return _dinoDamageMultiplier; }
            set
            {
                _dinoDamageMultiplier = value;
                OnPropertyChanged(nameof(DinoDamageMultiplier)); // Notify the UI of the change
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

        private string _cosmeticWhitelistOverride;
        public string CosmeticWhitelistOverride
        {
            get { return _cosmeticWhitelistOverride; }
            set
            {
                _cosmeticWhitelistOverride = value;
                OnPropertyChanged(nameof(CosmeticWhitelistOverride)); // Notify the UI of the change
            }
        }

        private string _mOTD;
        public string MOTD
        {
            get { return _mOTD; }
            set
            {
                _mOTD = value;
                OnPropertyChanged(nameof(MOTD));
                
            }
        }
        private string _duration;
        public string Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                OnPropertyChanged(nameof(Duration));

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
        private string _structureDamageMultiplier;
        public string StructureDamageMultiplier
        {
            get { return _structureDamageMultiplier; }
            set
            {
                _structureDamageMultiplier = value;
                OnPropertyChanged(nameof(StructureDamageMultiplier)); // Notify the UI of the change
            }
        }
        private string _structureResistanceMultiplier;
        public string StructureResistanceMultiplier
        {
            get { return _structureResistanceMultiplier; }
            set
            {
                _structureResistanceMultiplier = value;
                OnPropertyChanged(nameof(StructureResistanceMultiplier)); // Notify the UI of the change
            }
        }
        private string _limitTurretsNum;
        public string LimitTurretsNum
        {
            get { return _limitTurretsNum; }
            set
            {
                _limitTurretsNum = value;
                OnPropertyChanged(nameof(LimitTurretsNum)); // Notify the UI of the change
            }
        }
        private string _limitTurretsRange;
        public string LimitTurretsRange
        {
            get { return _limitTurretsRange; }
            set
            {
                _limitTurretsRange = value;
                OnPropertyChanged(nameof(LimitTurretsRange)); // Notify the UI of the change
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
        private bool _preventOfflinePvP;
        public bool PreventOfflinePvP
        {
            get { return _preventOfflinePvP; }
            set
            {
                _preventOfflinePvP = value;
                OnPropertyChanged(nameof(PreventOfflinePvP)); // Notify the UI of the change
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

        private string _minimumDinoReuploadInterval;
        public string MinimumDinoReuploadInterval
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
        private string _maxTamedDinosSoftTameLimitCountdownForDeletionDuration;
        public string MaxTamedDinosSoftTameLimitCountdownForDeletionDuration
        {
            get { return _maxTamedDinosSoftTameLimitCountdownForDeletionDuration; }
            set
            {
                _maxTamedDinosSoftTameLimitCountdownForDeletionDuration = value;
                OnPropertyChanged(nameof(MaxTamedDinosSoftTameLimitCountdownForDeletionDuration)); // Notify the UI of the change
            }
        }
        private string _maxTamedDinosSoftTameLimit;
        public string MaxTamedDinosSoftTameLimit
        {
            get { return _maxTamedDinosSoftTameLimit; }
            set
            {
                _maxTamedDinosSoftTameLimit = value;
                OnPropertyChanged(nameof(MaxTamedDinosSoftTameLimit)); // Notify the UI of the change
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
        private string _tribeSlotReuseCooldown;
        public string TribeSlotReuseCooldown
        {
            get { return _tribeSlotReuseCooldown; }
            set
            {
                _tribeSlotReuseCooldown = value;
                OnPropertyChanged(nameof(TribeSlotReuseCooldown)); // Notify the UI of the change
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
                        case "PerLevelStatsMultiplier_DinoTamed_Add[7]":
                            PerLevelStatsMultiplier_DinoTamed_Add_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[8]":
                            PerLevelStatsMultiplier_DinoTamed_Add_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Add[9]":
                            PerLevelStatsMultiplier_DinoTamed_Add_9 = value;
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
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[7]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[8]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoTamed_Affinity[9]":
                            PerLevelStatsMultiplier_DinoTamed_Affinity_9 = value;
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
                        case "PerLevelStatsMultiplier_DinoWild[7]":
                            PerLevelStatsMultiplier_DinoWild_7 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[8]":
                            PerLevelStatsMultiplier_DinoWild_8 = value;
                            break;
                        case "PerLevelStatsMultiplier_DinoWild[9]":
                            PerLevelStatsMultiplier_DinoWild_9 = value;
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
                        case "PerLevelStatsMultiplier_Player[11]":
                            PerLevelStatsMultiplier_Player_11 = value;
                            break;
                        case "PlayerBaseStatMultipliers[0]":
                            PlayerBaseStatMultipliers_0 = value;
                            break;
                        case "PlayerBaseStatMultipliers[1]":
                            PlayerBaseStatMultipliers_1 = value;
                            break;
                        case "PlayerBaseStatMultipliers[2]":
                            PlayerBaseStatMultipliers_2 = value;
                            break;
                        case "PlayerBaseStatMultipliers[3]":
                            PlayerBaseStatMultipliers_3 = value;
                            break;
                        case "PlayerBaseStatMultipliers[4]":
                            PlayerBaseStatMultipliers_4 = value;
                            break;
                        case "PlayerBaseStatMultipliers[5]":
                            PlayerBaseStatMultipliers_5 = value;
                            break;
                        case "PlayerBaseStatMultipliers[6]":
                            PlayerBaseStatMultipliers_6 = value;
                            break;
                        case "PlayerBaseStatMultipliers[7]":
                            PlayerBaseStatMultipliers_7 = value;
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
                        case "MaxAlliancesPerTribe":
                            MaxAlliancesPerTribe = value;
                            break;
                        case "MaxTribesPerAlliance":
                            MaxTribesPerAlliance = value;
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
                        case "BabyImprintAmountMultiplier":
                            BabyImprintAmountMultiplier = value;
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
                        case "PlayerDamageMultiplier":
                            PlayerDamageMultiplier = value;
                            break;
                        case "MaxFallSpeedMultiplier":
                            MaxFallSpeedMultiplier = value;
                            break;
                        case "PlayerCharacterHealthRecoveryMultiplier":
                            PlayerCharacterHealthRecoveryMultiplier = value;
                            break;
                        case "PlayerCharacterStaminaDrainMultiplier":
                            PlayerCharacterStaminaDrainMultiplier = value;
                            break;
                        case "PassiveTameIntervalMultiplier":
                            PassiveTameIntervalMultiplier = value;
                            break;
                        case "WildDinoTorporDrainMultiplier":
                            WildDinoTorporDrainMultiplier = value;
                            break;
                        case "TamedDinoTorporDrainMultiplier":
                            TamedDinoTorporDrainMultiplier = value;
                            break;
                        case "TamedDinoCharacterFoodDrainMultiplier":
                            TamedDinoCharacterFoodDrainMultiplier = value;
                            break;
                        case "WildDinoCharacterFoodDrainMultiplier":
                            WildDinoCharacterFoodDrainMultiplier = value;
                            break;
                        case "PlayerResistanceMultiplier":
                            PlayerResistanceMultiplier = value;
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
                            DisablePhotoMode = ConvertToBoolean(value);
                            break;
                        case "DestroyTamesOverTheSoftTameLimit":
                            DestroyTamesOverTheSoftTameLimit = ConvertToBoolean(value);
                            break;
                        case "AllowCryoFridgeOnSaddle":
                            AllowCryoFridgeOnSaddle = ConvertToBoolean(value);
                            break;
                        case "DisableCryopodFridgeRequirement":
                            DisableCryopodFridgeRequirement = ConvertToBoolean(value);
                            break;
                        case "DisableCryopodEnemyCheck":
                            DisableCryopodEnemyCheck = ConvertToBoolean(value);
                            break;
                        case "IncreasePvPRespawnInterval":
                            IncreasePvPRespawnInterval = ConvertToBoolean(value);
                            break;
                        case "AutoPvETimer":
                            AutoPvETimer = ConvertToBoolean(value);
                            break;
                        case "AutoPvEUseSystemTime":
                            AutoPvEUseSystemTime = Convert.ToBoolean(value);
                            break;
                        case "bPvPDisableFriendlyFire":
                            BPvPDisableFriendlyFire = ConvertToBoolean(value);
                            break;
                        case "FlyerPlatformAllowUnalignedDinoBasing":
                            FlyerPlatformAllowUnalignedDinoBasing = ConvertToBoolean(value);
                            break;
                        case "DisableLootCrates":
                            DisableLootCrates = ConvertToBoolean(value);
                            break;
                        case "AllowCustomRecipes":
                            AllowCustomRecipes = ConvertToBoolean(value);
                            break;
                        case "PassiveDefensesDamageRiderlessDinos":
                            PassiveDefensesDamageRiderlessDinos = ConvertToBoolean(value);
                            break;
                        case "PvEAllowTribeWar":
                            PvEAllowTribeWar = ConvertToBoolean(value);
                            break;
                        case "PvEAllowTribeWarCancel":
                            PvEAllowTribeWarCancel = ConvertToBoolean(value);
                            break;
                        case "MaxDifficulty":
                            MaxDifficulty = value;
                            break;
                        case "UseSingleplayerSettings":
                            UseSingleplayerSettings = ConvertToBoolean(value);
                            break;
                        case "UseCorpseLocator":
                            UseCorpseLocator = ConvertToBoolean(value);
                            break;
                        case "ShowCreativeMode":
                            ShowCreativeMode = ConvertToBoolean(value);
                            break;
                        case "PreventDiseases":
                            PreventDiseases = ConvertToBoolean(value);
                            break;
                        case "NonPermanentDiseases":
                            NonPermanentDiseases = ConvertToBoolean(value);
                            break;
                        case "HardLimitTurretsInRange":
                            HardLimitTurretsInRange = ConvertToBoolean(value);
                            break;
                        case "DisableStructurePlacementCollision":
                            DisableStructurePlacementCollision = ConvertToBoolean(value);
                            break;
                        case "AllowPlatformSaddleMultiFloors":
                            AllowPlatformSaddleMultiFloors = ConvertToBoolean(value);
                            break;
                        case "AllowUnlimitedRespec":
                            AllowUnlimitedRespec = ConvertToBoolean(value);
                            break;
                        case "DisableDinoTaming":
                            DisableDinoTaming = ConvertToBoolean(value);
                            break;
                        case "bDisableDinoBreeding":
                            DisableDinoBreeding = ConvertToBoolean(value);
                            break;
                        case "bDisableDinoRiding":
                            DisableDinoRiding = ConvertToBoolean(value);
                            break;
                        case "bAllowUnclaimDinos":
                            AllowUnclaimDinos = ConvertToBoolean(value);
                            break;
                        case "PreventMateBoost":
                            PreventMateBoost = ConvertToBoolean(value);
                            break;
                        case "ForceAllowCaveFlyers":
                            ForceAllowCaveFlyers = ConvertToBoolean(value);
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
                        case "MatingSpeedMultiplier":
                            MatingSpeedMultiplier = value;
                            break;
                        case "IncreasePvPRespawnIntervalBaseAmount":
                            IncreasePvPRespawnIntervalBaseAmount = value;
                            break;
                        case "FishingLootQualityMultiplier":
                            FishingLootQualityMultiplier = value;
                            break;
                        case "CraftingSkillBonusMultiplier":
                            CraftingSkillBonusMultiplier = value;
                            break;
                        case "LimitTurretsNum":
                            LimitTurretsNum = value;
                            break;
                        case "LimitTurretsRange":
                            LimitTurretsRange = value;
                            break;
                        case "AllowSpeedLeveling":
                            AllowSpeedLeveling = ConvertToBoolean(value);
                            break;
                        case "AllowFlyerSpeedLeveling":
                            AllowFlyerSpeedLeveling = ConvertToBoolean(value);
                            break;
                            // Add cases for all other settings
                            // ...
                    }
                }
            }
        }
        private void SaveGameIniSettings()
        {
            try
            {
                string serverPath = CurrentServerConfig.ServerPath;
                string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");

                // Read all lines
                var lines = File.ReadAllLines(iniFilePath).ToList();

                // Update specific lines
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyImprintingStatScaleMultiplier", BabyImprintingStatScaleMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyCuddleIntervalMultiplier", BabyCuddleIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyCuddleGracePeriodMultiplier", BabyCuddleGracePeriodMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyCuddleLoseImprintQualitySpeedMultiplier", BabyCuddleLoseImprintQualitySpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[0]", PerLevelStatsMultiplier_DinoTamed_0);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[1]", PerLevelStatsMultiplier_DinoTamed_1);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[2]", PerLevelStatsMultiplier_DinoTamed_2);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[3]", PerLevelStatsMultiplier_DinoTamed_3);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[4]", PerLevelStatsMultiplier_DinoTamed_4);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[7]", PerLevelStatsMultiplier_DinoTamed_7);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[8]", PerLevelStatsMultiplier_DinoTamed_8);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed[9]", PerLevelStatsMultiplier_DinoTamed_9);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[0]", PerLevelStatsMultiplier_DinoTamed_Add_0);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[1]", PerLevelStatsMultiplier_DinoTamed_Add_1);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[2]", PerLevelStatsMultiplier_DinoTamed_Add_2);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[3]", PerLevelStatsMultiplier_DinoTamed_Add_3);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[4]", PerLevelStatsMultiplier_DinoTamed_Add_4);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[7]", PerLevelStatsMultiplier_DinoTamed_Add_7);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[8]", PerLevelStatsMultiplier_DinoTamed_Add_8);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Add[9]", PerLevelStatsMultiplier_DinoTamed_Add_9);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[0]", PerLevelStatsMultiplier_DinoTamed_Affinity_0);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[1]", PerLevelStatsMultiplier_DinoTamed_Affinity_1);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[2]", PerLevelStatsMultiplier_DinoTamed_Affinity_2);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[3]", PerLevelStatsMultiplier_DinoTamed_Affinity_3);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[4]", PerLevelStatsMultiplier_DinoTamed_Affinity_4);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[7]", PerLevelStatsMultiplier_DinoTamed_Affinity_7);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[8]", PerLevelStatsMultiplier_DinoTamed_Affinity_8);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoTamed_Affinity[9]", PerLevelStatsMultiplier_DinoTamed_Affinity_9);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[0]", PerLevelStatsMultiplier_DinoWild_0);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[1]", PerLevelStatsMultiplier_DinoWild_1);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[2]", PerLevelStatsMultiplier_DinoWild_2);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[3]", PerLevelStatsMultiplier_DinoWild_3);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[4]", PerLevelStatsMultiplier_DinoWild_4);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[7]", PerLevelStatsMultiplier_DinoWild_7);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[8]", PerLevelStatsMultiplier_DinoWild_8);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_DinoWild[9]", PerLevelStatsMultiplier_DinoWild_9);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[0]", PerLevelStatsMultiplier_Player_0);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[1]", PerLevelStatsMultiplier_Player_1);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[2]", PerLevelStatsMultiplier_Player_2);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[3]", PerLevelStatsMultiplier_Player_3);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[4]", PerLevelStatsMultiplier_Player_4);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[5]", PerLevelStatsMultiplier_Player_5);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[6]", PerLevelStatsMultiplier_Player_6);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[7]", PerLevelStatsMultiplier_Player_7);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[8]", PerLevelStatsMultiplier_Player_8);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[9]", PerLevelStatsMultiplier_Player_9);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[10]", PerLevelStatsMultiplier_Player_10);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PerLevelStatsMultiplier_Player[11]", PerLevelStatsMultiplier_Player_11);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[0]", PlayerBaseStatMultipliers_0);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[1]", PlayerBaseStatMultipliers_1);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[2]", PlayerBaseStatMultipliers_2);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[3]", PlayerBaseStatMultipliers_3);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[4]", PlayerBaseStatMultipliers_4);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[5]", PlayerBaseStatMultipliers_5);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[6]", PlayerBaseStatMultipliers_6);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerBaseStatMultipliers[7]", PlayerBaseStatMultipliers_7);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "GlobalSpoilingTimeMultiplier", GlobalSpoilingTimeMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "GlobalItemDecompositionTimeMultiplier", GlobalItemDecompositionTimeMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "GlobalCorpseDecompositionTimeMultiplier", GlobalCorpseDecompositionTimeMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PvPZoneStructureDamageMultiplier", PvPZoneStructureDamageMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "StructureDamageRepairCooldown", StructureDamageRepairCooldown);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "IncreasePvPRespawnIntervalCheckPeriod", IncreasePvPRespawnIntervalCheckPeriod);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "IncreasePvPRespawnIntervalMultiplier", IncreasePvPRespawnIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "IncreasePvPRespawnIntervalBaseAmount", IncreasePvPRespawnIntervalBaseAmount);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MaxAlliancesPerTribe", MaxAlliancesPerTribe);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MaxTribesPerAlliance", MaxTribesPerAlliance);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "ResourceNoReplenishRadiusPlayers", ResourceNoReplenishRadiusPlayers);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CropGrowthSpeedMultiplier", CropGrowthSpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "LayEggIntervalMultiplier", LayEggIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PoopIntervalMultiplier", PoopIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CropDecaySpeedMultiplier", CropDecaySpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MatingIntervalMultiplier", MatingIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyImprintAmountMultiplier", BabyImprintAmountMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "EggHatchSpeedMultiplier", EggHatchSpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MatingSpeedMultiplier", MatingSpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyMatureSpeedMultiplier", BabyMatureSpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BabyFoodConsumptionSpeedMultiplier", BabyFoodConsumptionSpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DinoTurretDamageMultiplier", DinoTurretDamageMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DinoHarvestingDamageMultiplier", DinoHarvestingDamageMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "LimitTurretsNum", LimitTurretsNum);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "LimitTurretsRange", LimitTurretsRange);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerHarvestingDamageMultiplier", PlayerHarvestingDamageMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CustomRecipeEffectivenessMultiplier", CustomRecipeEffectivenessMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CustomRecipeSkillMultiplier", CustomRecipeSkillMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AutoPvEStartTimeSeconds", AutoPvEStartTimeSeconds);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AutoPvEStopTimeSeconds", AutoPvEStopTimeSeconds);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "KillXPMultiplier", KillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "HarvestXPMultiplier", HarvestXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CraftXPMultiplier", CraftXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "GenericXPMultiplier", GenericXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerDamageMultiplier", PlayerDamageMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MaxFallSpeedMultiplier", MaxFallSpeedMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerCharacterHealthRecoveryMultiplier", PlayerCharacterHealthRecoveryMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerCharacterStaminaDrainMultiplier", PlayerCharacterStaminaDrainMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PassiveTameIntervalMultiplier", PassiveTameIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "WildDinoTorporDrainMultiplier", WildDinoTorporDrainMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "TamedDinoTorporDrainMultiplier", TamedDinoTorporDrainMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "TamedDinoCharacterFoodDrainMultiplier", TamedDinoCharacterFoodDrainMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "WildDinoCharacterFoodDrainMultiplier", WildDinoCharacterFoodDrainMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PlayerResistanceMultiplier", PlayerResistanceMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "SpecialXPMultiplier", SpecialXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "FuelConsumptionIntervalMultiplier", FuelConsumptionIntervalMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PhotoModeRangeLimit", PhotoModeRangeLimit);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DisablePhotoMode", DisablePhotoMode.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AllowCryoFridgeOnSaddle", AllowCryoFridgeOnSaddle.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DestroyTamesOverTheSoftTameLimit", DestroyTamesOverTheSoftTameLimit.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DisableCryopodFridgeRequirement", DisableCryopodFridgeRequirement.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DisableCryopodEnemyCheck", DisableCryopodEnemyCheck.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "IncreasePvPRespawnInterval", IncreasePvPRespawnInterval.ToString(CultureInfo.InvariantCulture));
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "bAutoPvETimer", AutoPvETimer.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "bAutoPvEUseSystemTime", AutoPvEUseSystemTime.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "bPvPDisableFriendlyFire", BPvPDisableFriendlyFire.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "FlyerPlatformAllowUnalignedDinoBasing", FlyerPlatformAllowUnalignedDinoBasing.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DisableLootCrates", DisableLootCrates.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AllowCustomRecipes", AllowCustomRecipes.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PassiveDefensesDamageRiderlessDinos", PassiveDefensesDamageRiderlessDinos.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PvEAllowTribeWar", PvEAllowTribeWar.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PvEAllowTribeWarCancel", PvEAllowTribeWarCancel.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MaxDifficulty", MaxDifficulty.ToString(CultureInfo.InvariantCulture) ?? "");
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "UseSingleplayerSettings", UseSingleplayerSettings.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "UseCorpseLocator", UseCorpseLocator.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "ShowCreativeMode", ShowCreativeMode.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "NonPermanentDiseases", NonPermanentDiseases.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PreventDiseases", PreventDiseases.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "HardLimitTurretsInRange", HardLimitTurretsInRange.ToString(CultureInfo.InvariantCulture) ?? "");
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DisableStructurePlacementCollision", DisableStructurePlacementCollision.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AllowPlatformSaddleMultiFloors", AllowPlatformSaddleMultiFloors.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AllowUnlimitedRespec", AllowUnlimitedRespec.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "DisableDinoTaming", DisableDinoTaming.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "bAllowUnclaimDinos", AllowUnclaimDinos.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "bDisableDinoBreeding", DisableDinoBreeding.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "bDisableDinoRiding", DisableDinoRiding.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "ForceAllowCaveFlyers", ForceAllowCaveFlyers.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "PreventMateBoost", PreventMateBoost.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "OverrideMaxExperiencePointsDino", OverrideMaxExperiencePointsDino);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "MaxNumberOfPlayersInTribe", MaxNumberOfPlayersInTribe);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "ExplorerNoteXPMultiplier", ExplorerNoteXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "BossKillXPMultiplier", BossKillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AlphaKillXPMultiplier", AlphaKillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "WildKillXPMultiplier", WildKillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CaveKillXPMultiplier", CaveKillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "TamedKillXPMultiplier", TamedKillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "UnclaimedKillXPMultiplier", UnclaimedKillXPMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "SupplyCrateLootQualityMultiplier", SupplyCrateLootQualityMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "FishingLootQualityMultiplier", FishingLootQualityMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "CraftingSkillBonusMultiplier", CraftingSkillBonusMultiplier);
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AllowSpeedLeveling", AllowSpeedLeveling.ToString());
                UpdateLine(ref lines, "/Script/ShooterGame.ShooterGameMode", "AllowFlyerSpeedLeveling", AllowFlyerSpeedLeveling.ToString());

                // ... Continue for all other properties

                // Write the updated lines back to the file
                File.WriteAllLines(iniFilePath, lines);
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.Log($"An error occurred while saving game settings: {ex.Message}");
                // Optionally, log the stack trace or other details
                Logger.Log(ex.StackTrace);
            }
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
        private string _perLevelStatsMultiplier_Player_11;
        public string PerLevelStatsMultiplier_Player_11
        {
            get { return _perLevelStatsMultiplier_Player_11; }
            set
            {
                _perLevelStatsMultiplier_Player_11= value;
                OnPropertyChanged(nameof(PerLevelStatsMultiplier_Player_11));
            }
        }
        private string _PlayerBaseStatMultipliers_0;
        public string PlayerBaseStatMultipliers_0
        {
            get { return _PlayerBaseStatMultipliers_0; }
            set
            {
                _PlayerBaseStatMultipliers_0 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_0));
            }
        }
        private string _PlayerBaseStatMultipliers_1;
        public string PlayerBaseStatMultipliers_1
        {
            get { return _PlayerBaseStatMultipliers_1; }
            set
            {
                _PlayerBaseStatMultipliers_1 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_1));
            }
        }
        private string _PlayerBaseStatMultipliers_2;
        public string PlayerBaseStatMultipliers_2
        {
            get { return _PlayerBaseStatMultipliers_2; }
            set
            {
                _PlayerBaseStatMultipliers_2 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_2));
            }
        }
        private string _PlayerBaseStatMultipliers_3;
        public string PlayerBaseStatMultipliers_3
        {
            get { return _PlayerBaseStatMultipliers_3; }
            set
            {
                _PlayerBaseStatMultipliers_3 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_3));
            }
        }
        private string _PlayerBaseStatMultipliers_4;
        public string PlayerBaseStatMultipliers_4
        {
            get { return _PlayerBaseStatMultipliers_4; }
            set
            {
                _PlayerBaseStatMultipliers_4 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_4));
            }
        }
        private string _PlayerBaseStatMultipliers_5;
        public string PlayerBaseStatMultipliers_5
        {
            get { return _PlayerBaseStatMultipliers_5; }
            set
            {
                _PlayerBaseStatMultipliers_5 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_5));
            }
        }
        private string _PlayerBaseStatMultipliers_6;
        public string PlayerBaseStatMultipliers_6
        {
            get { return _PlayerBaseStatMultipliers_6; }
            set
            {
                _PlayerBaseStatMultipliers_6 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_6));
            }
        }
        private string _PlayerBaseStatMultipliers_7;
        public string PlayerBaseStatMultipliers_7
        {
            get { return _PlayerBaseStatMultipliers_7; }
            set
            {
                _PlayerBaseStatMultipliers_7 = value;
                OnPropertyChanged(nameof(PlayerBaseStatMultipliers_7));
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
        private string _maxAlliancesPerTribe;
        public string MaxAlliancesPerTribe
        {
            get { return _maxAlliancesPerTribe; }
            set
            {
                _maxAlliancesPerTribe = value;
                OnPropertyChanged(nameof(MaxAlliancesPerTribe));
            }
        }
        private string _maxTribesPerAlliance;
        public string MaxTribesPerAlliance
        {
            get { return _maxTribesPerAlliance; }
            set
            {
                _maxTribesPerAlliance = value;
                OnPropertyChanged(nameof(MaxTribesPerAlliance));
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
        private string _matingSpeedMultiplier;
        public string MatingSpeedMultiplier
        {
            get { return _matingSpeedMultiplier; }
            set
            {
                _matingSpeedMultiplier = value;
                OnPropertyChanged(nameof(MatingSpeedMultiplier));
            }
        }
        private string _babyImprintAmountMultiplier;
        public string BabyImprintAmountMultiplier
        {
            get { return _babyImprintAmountMultiplier; }
            set
            {
                _babyImprintAmountMultiplier = value;
                OnPropertyChanged(nameof(BabyImprintAmountMultiplier));
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


        private string _playerCharacterFoodDrainMultiplier;
        public string PlayerCharacterFoodDrainMultiplier
        {
            get { return _playerCharacterFoodDrainMultiplier; }
            set
            {
                _playerCharacterFoodDrainMultiplier = value;
                OnPropertyChanged(nameof(PlayerCharacterFoodDrainMultiplier));
            }
        }
        private string _playerCharacterWaterDrainMultiplier;
        public string PlayerCharacterWaterDrainMultiplier
        {
            get { return _playerCharacterWaterDrainMultiplier; }
            set
            {
                _playerCharacterWaterDrainMultiplier = value;
                OnPropertyChanged(nameof(PlayerCharacterWaterDrainMultiplier));
            }
        }
        private string _maxFallSpeedMultiplier;
        public string MaxFallSpeedMultiplier
        {
            get { return _maxFallSpeedMultiplier; }
            set
            {
                _maxFallSpeedMultiplier = value;
                OnPropertyChanged(nameof(MaxFallSpeedMultiplier));
            }
        }
        private string _playerCharacterHealthRecoveryMultiplier;
        public string PlayerCharacterHealthRecoveryMultiplier
        {
            get { return _playerCharacterHealthRecoveryMultiplier; }
            set
            {
                _playerCharacterHealthRecoveryMultiplier = value;
                OnPropertyChanged(nameof(PlayerCharacterHealthRecoveryMultiplier));
            }
        }
        private string _tamedDinoCharacterFoodDrainMultiplier;
        public string TamedDinoCharacterFoodDrainMultiplier
        {
            get { return _tamedDinoCharacterFoodDrainMultiplier; }
            set
            {
                _tamedDinoCharacterFoodDrainMultiplier = value;
                OnPropertyChanged(nameof(TamedDinoCharacterFoodDrainMultiplier));
            }
        }
        private string _wildDinoCharacterFoodDrainMultiplier;
        public string WildDinoCharacterFoodDrainMultiplier
        {
            get { return _wildDinoCharacterFoodDrainMultiplier; }
            set
            {
                _wildDinoCharacterFoodDrainMultiplier = value;
                OnPropertyChanged(nameof(WildDinoCharacterFoodDrainMultiplier));
            }
        }
        private string _playerCharacterStaminaDrainMultiplier;
        public string PlayerCharacterStaminaDrainMultiplier
        {
            get { return _playerCharacterStaminaDrainMultiplier; }
            set
            {
                _playerCharacterStaminaDrainMultiplier = value;
                OnPropertyChanged(nameof(PlayerCharacterStaminaDrainMultiplier));
            }
        }
        private string _dinoCharacterStaminaDrainMultiplier;
        public string DinoCharacterStaminaDrainMultiplier
        {
            get { return _dinoCharacterStaminaDrainMultiplier; }
            set
            {
                _dinoCharacterStaminaDrainMultiplier = value;
                OnPropertyChanged(nameof(DinoCharacterStaminaDrainMultiplier));
            }
        }
        private string _dinoCharacterHealthRecoveryMultiplier;
        public string DinoCharacterHealthRecoveryMultiplier
        {
            get { return _dinoCharacterHealthRecoveryMultiplier; }
            set
            {
                _dinoCharacterHealthRecoveryMultiplier = value;
                OnPropertyChanged(nameof(DinoCharacterHealthRecoveryMultiplier));
            }
        }
        private string _passiveTameIntervalMultiplier;
        public string PassiveTameIntervalMultiplier
        {
            get { return _passiveTameIntervalMultiplier; }
            set
            {
                _passiveTameIntervalMultiplier = value;
                OnPropertyChanged(nameof(PassiveTameIntervalMultiplier));
            }
        }
        private string _wildDinoTorporDrainMultiplier;
        public string WildDinoTorporDrainMultiplier
        {
            get { return _wildDinoTorporDrainMultiplier; }
            set
            {
                _wildDinoTorporDrainMultiplier = value;
                OnPropertyChanged(nameof(WildDinoTorporDrainMultiplier));
            }
        }
        private string _tamedDinoTorporDrainMultiplier;
        public string TamedDinoTorporDrainMultiplier
        {
            get { return _tamedDinoTorporDrainMultiplier; }
            set
            {
                _tamedDinoTorporDrainMultiplier = value;
                OnPropertyChanged(nameof(TamedDinoTorporDrainMultiplier));
            }
        }
        private string _playerDamageMultiplier;
        public string PlayerDamageMultiplier
        {
            get { return _playerDamageMultiplier; }
            set
            {
                _playerDamageMultiplier = value;
                OnPropertyChanged(nameof(PlayerDamageMultiplier));
            }
        }
        private string _playerResistanceMultiplier;
        public string PlayerResistanceMultiplier
        {
            get { return _playerResistanceMultiplier; }
            set
            {
                _playerResistanceMultiplier = value;
                OnPropertyChanged(nameof(PlayerResistanceMultiplier));
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
        private bool _destroyTamesOverTheSoftTameLimit;
        public bool DestroyTamesOverTheSoftTameLimit
        {
            get { return _destroyTamesOverTheSoftTameLimit; }
            set
            {
                _destroyTamesOverTheSoftTameLimit = value;
                OnPropertyChanged(nameof(DestroyTamesOverTheSoftTameLimit));
            }
        }
        private bool _allowCryoFridgeOnSaddle;
        public bool AllowCryoFridgeOnSaddle
        {
            get { return _allowCryoFridgeOnSaddle; }
            set
            {
                _allowCryoFridgeOnSaddle = value;
                OnPropertyChanged(nameof(AllowCryoFridgeOnSaddle));
            }
        }
        private bool _disableCryopodFridgeRequirement;
        public bool DisableCryopodFridgeRequirement
        {
            get { return _disableCryopodFridgeRequirement; }
            set
            {
                _disableCryopodFridgeRequirement = value;
                OnPropertyChanged(nameof(DisableCryopodFridgeRequirement));
            }
        }
        private bool _disableCryopodEnemyCheck;
        public bool DisableCryopodEnemyCheck
        {
            get { return _disableCryopodEnemyCheck; }
            set
            {
                _disableCryopodEnemyCheck = value;
                OnPropertyChanged(nameof(DisableCryopodEnemyCheck));
            }
        }
        private bool _preventSpawnAnimations;
        public bool PreventSpawnAnimations
        {
            get { return _preventSpawnAnimations; }
            set
            {
                _preventSpawnAnimations = value;
                OnPropertyChanged(nameof(PreventSpawnAnimations));
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

        private bool _bPvPdisableFriendlyFire;
        public bool BPvPDisableFriendlyFire
        {
            get { return _bPvPdisableFriendlyFire; }
            set
            {
                _bPvPdisableFriendlyFire = value;
                OnPropertyChanged(nameof(BPvPDisableFriendlyFire));
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

        private string _maxDifficulty;
        public string MaxDifficulty
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

        private bool _preventDiseases;
        public bool PreventDiseases
        {
            get { return _preventDiseases; }
            set
            {
                _preventDiseases = value;
                OnPropertyChanged(nameof(PreventDiseases));
            }
        }
        private bool _nonPermanentDiseases;
        public bool NonPermanentDiseases
        {
            get { return _nonPermanentDiseases; }
            set
            {
                _nonPermanentDiseases = value;
                OnPropertyChanged(nameof(NonPermanentDiseases));
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
        private string _customLaunchOptions;
        public string CustomLaunchOptions
        {
            get { return _customLaunchOptions; }
            set
            {
                if (_customLaunchOptions != value)
                {
                    _customLaunchOptions = value;
                    OnPropertyChanged(nameof(CustomLaunchOptions));
                }
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

        private bool _forceAllowCaveFlyers;
        public bool ForceAllowCaveFlyers
        {
            get { return _forceAllowCaveFlyers; }
            set
            {
                _forceAllowCaveFlyers = value;
                OnPropertyChanged(nameof(ForceAllowCaveFlyers));
            }
        }
        private bool _allowUnclaimDinos;
        public bool AllowUnclaimDinos
        {
            get { return _allowUnclaimDinos; }
            set
            {
                _allowUnclaimDinos = value;
                OnPropertyChanged(nameof(AllowUnclaimDinos));
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
        private bool _disableDinoBreeding;
        public bool DisableDinoBreeding
        {
            get { return _disableDinoBreeding; }
            set
            {
                _disableDinoBreeding = value;
                OnPropertyChanged(nameof(DisableDinoBreeding));
            }
        }
        private bool _disableDinoRiding;
        public bool DisableDinoRiding
        {
            get { return _disableDinoRiding; }
            set
            {
                _disableDinoRiding = value;
                OnPropertyChanged(nameof(DisableDinoRiding));
            }
        }
        private bool _preventMateBoost;
        public bool PreventMateBoost
        {
            get { return _preventMateBoost; }
            set
            {
                _preventMateBoost = value;
                OnPropertyChanged(nameof(PreventMateBoost));
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