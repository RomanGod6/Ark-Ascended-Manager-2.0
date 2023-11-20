using Wpf.Ui.Controls;
using System.Collections.Generic; // If using collections
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Windows.Input;
using Ark_Ascended_Manager.Views.Pages;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class RconPanelViewModel : ObservableObject
    {
        private ObservableCollection<string> _serverNames = new ObservableCollection<string>();
        private string _selectedServerName;
        private ICommand _selectServerCommand;
        private ServerConfig _currentServerConfig;

        public ObservableCollection<string> ServerNames
        {
            get => _serverNames;
            set => SetProperty(ref _serverNames, value);
        }
        public ServerConfig CurrentServerConfig
        {
            get => _currentServerConfig;
            set => SetProperty(ref _currentServerConfig, value);
        }

        public string SelectedServerName
        {
            get => _selectedServerName;
            set
            {
                if (SetProperty(ref _selectedServerName, value))
                {
                    // Logic to execute when a server is selected
                    // For example: LoadServerDetails(_selectedServerName);
                }
            }
        }

        public ICommand SelectServerCommand
        {
            get => _selectServerCommand;
            set => SetProperty(ref _selectServerCommand, value);
        }

        public RconPanelViewModel()
        {
            LoadServers();
            SelectServerCommand = new RelayCommand<object>(OnServerSelected);
        }


        private void LoadServers()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var servers = JsonSerializer.Deserialize<List<ServerConfig>>(json);
                    if (servers != null)
                    {
                        ServerNames = new ObservableCollection<string>(servers.Select(s => s.ServerName));
                        OnPropertyChanged(nameof(ServerNames));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load servers: {ex.Message}");
                // Handle the exception, for example, by logging or displaying an error message
            }
        }
        private async Task<List<string>> SendRconCommandAsync(string command)
        {
            var playersList = new List<string>();

            // Ensure that the current server configuration is loaded
            if (CurrentServerConfig == null)
            {
                Debug.WriteLine("Current server configuration is not loaded.");
                return playersList;
            }

            // Prepare the RCON command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C echo {command} | mcrcon 127.0.0.1 --password {CurrentServerConfig.AdminPassword} -p {CurrentServerConfig.RCONPort}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                // Start the process and wait for it to exit
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                // Process the output to extract player names
                playersList = ParsePlayersList(output);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception sending RCON command: {ex}");
            }

            return playersList;
        }
        private List<string> ParsePlayersList(string rconOutput)
        {
            var playersList = new List<string>();

            // Check for no players connected
            if (rconOutput.Contains("No Players Connected"))
            {
                return playersList;
            }

            // Use regex to parse the output and extract player names and IDs
            var regex = new Regex(@"(\d+)\.\s(.+?),\s(\w+)");
            var matches = regex.Matches(rconOutput);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var playerName = match.Groups[2].Value.Trim();
                    var playerId = match.Groups[3].Value.Trim();
                    playersList.Add($"{playerName}, {playerId}");
                }
            }

            return playersList;
        }






        private async void OnServerSelected(object commandParameter)
        {
            if (commandParameter is string serverName)
            {
                SelectedServerName = serverName;

                // Load the current server configuration based on the selected server
                LoadCurrentServerConfig(serverName);

                // Once the server is selected and its configuration is loaded, send the RCON command
                var playersList = await SendRconCommandAsync("ListPlayers");
                // Handle the players list, e.g., update a property bound to a ListBox
            }
        }

        // INotifyPropertyChanged implementation and SetProperty method
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void LoadCurrentServerConfig(string serverName)
        {
            CurrentServerConfig = GetServerConfigByName(serverName);
        }

        private ServerConfig GetServerConfigByName(string serverName)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            if (!File.Exists(filePath))
            {
                Debug.WriteLine("Server configuration file not found.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var servers = JsonSerializer.Deserialize<List<ServerConfig>>(json);
                return servers?.FirstOrDefault(s => s.ServerName == serverName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception while reading server configurations: {ex.Message}");
                return null;
            }
        }


        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
public class ServerConfig
{
    public string ProfileName { get; set; }
    public string ServerPath { get; set; }
    public string MapName { get; set; }
    public string AppId { get; set; }
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

    // Add other properties as needed
}