﻿using CoreRCON;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class RconPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        private ObservableCollection<ServerProfile> _serverProfiles;
        private ServerProfile _selectedServerProfile;
        private ObservableCollection<string> _connectedPlayers = new ObservableCollection<string>();
        private ObservableCollection<string> _serverChat = new ObservableCollection<string>();
        private string _rconStatus;
        private System.Timers.Timer _updateTimer;
        private System.Timers.Timer _chatUpdateTimer;
        private string _commandInput;
        private CoreRCON.RCON rcon;
        private System.Timers.Timer _connectionCheckTimer;
        public string CommandInput
        {
            get => _commandInput;
            set => SetProperty(ref _commandInput, value);
        }

        public ICommand SendCommand { get; }
        public ICommand CopyIdCommand { get; }
        public ICommand TestCommand { get; private set; }

        public RconPanelViewModel()
        {
            ServerProfiles = new ObservableCollection<ServerProfile>();
            LoadServerProfiles();
            SetupUpdateTimer();
            InitializeChatUpdateTimer();
            SendCommand = new RelayCommand(ExecuteSendCommand);
            CopyIdCommand = new RelayCommand<object>(CopySelectedPlayerIdToClipboard);
            _connectionCheckTimer = new System.Timers.Timer(10000); // Check every 10 seconds
            _connectionCheckTimer.Elapsed += OnConnectionCheckTimerElapsed;
            _connectionCheckTimer.AutoReset = true;
            _connectionCheckTimer.Enabled = true;

        }
        private async void OnConnectionCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Make sure to stop the timer while the connection check is ongoing
            _connectionCheckTimer.Stop();

            if (_selectedServerProfile != null)
            {
                await InitializeRconConnection(_selectedServerProfile);
            }

            // Restart the timer after the check is complete
            _connectionCheckTimer.Start();
        }
        public void OnNavigatedTo()
        {
            




            // Initialization logic specific to the servers page
        }
        public void OnNavigatedFrom()
        {
            if (_connectionCheckTimer != null)
            {
                _connectionCheckTimer.Stop();
                _connectionCheckTimer.Elapsed -= OnConnectionCheckTimerElapsed;
                _connectionCheckTimer.Dispose();
                _connectionCheckTimer = null;
            }


        }

        private void CopySelectedPlayerIdToClipboard(object parameter)
        {
            var playerInfo = parameter as string;
            if (!string.IsNullOrEmpty(playerInfo))
            {
                var playerId = ExtractPlayerId(playerInfo);
                if (!string.IsNullOrEmpty(playerId))
                {
                    Clipboard.SetText(playerId);
                }
            }
        }


        private string _selectedPlayerInfo;
        public string SelectedPlayerInfo
        {
            get => _selectedPlayerInfo;
            set
            {
                if (_selectedPlayerInfo != value)
                {
                    _selectedPlayerInfo = value;
                    OnPropertyChanged(nameof(SelectedPlayerInfo));
                }
            }
        }

        public ObservableCollection<ServerProfile> ServerProfiles
        {
            get => _serverProfiles;
            set => SetProperty(ref _serverProfiles, value);
        }
        public class CustomCommand
        {
            public string Label { get; set; }
            public string CommandText { get; set; }
            public ICommand Command { get; set; }
        }

        private async void ExecuteSendCommand()
        {
            if (!string.IsNullOrWhiteSpace(CommandInput) && _selectedServerProfile != null)
            {
                // Send the RCON command and get the response
                var commandResponse = await SendRconCommandAsync(_selectedServerProfile, CommandInput);

                // Clear the command input
                CommandInput = string.Empty;

                // Dispatch the action to the UI thread to update the ObservableCollection
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Here you can choose how to format the command and its output for display
                    ServerChat.Add($"Command: {CommandInput}");
                    foreach (var line in commandResponse)
                    {
                        ServerChat.Add(line);
                    }
                });
            }
        }
        private string _selectedPlayer;
        public string SelectedPlayer
        {
            get => _selectedPlayer;
            set => SetProperty(ref _selectedPlayer, value);
        }


        public ServerProfile SelectedServerProfile
        {
            get => _selectedServerProfile;
            set
            {
                if (SetProperty(ref _selectedServerProfile, value))
                {
                    UpdateTimersOnProfileSelection();
                    ServerChat.Clear();
                    Task.Run(async () =>
                    {
                        await InitializeRconConnection(value);
                        await UpdatePlayerList();
                    });
                    Task.Run(() => FetchChatAsync());
                }
            }
        }

        public ObservableCollection<string> ConnectedPlayers
        {
            get => _connectedPlayers;
            set => SetProperty(ref _connectedPlayers, value);
        }

        public ObservableCollection<string> ServerChat
        {
            get => _serverChat;
            set => SetProperty(ref _serverChat, value);
        }

        public string RconStatus
        {
            get => _rconStatus;
            set => SetProperty(ref _rconStatus, value);
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new System.Timers.Timer(15000); // 10 seconds interval
            _updateTimer.Elapsed += async (sender, e) => await UpdatePlayerList();
            _updateTimer.AutoReset = true;
        }

        private void InitializeChatUpdateTimer()
        {
            _chatUpdateTimer = new System.Timers.Timer(1000); // 2 seconds interval
            _updateTimer.Elapsed += async (sender, e) => await FetchChatAsync();
            _chatUpdateTimer.AutoReset = true;
        }

        private void UpdateTimersOnProfileSelection()
        {
            _updateTimer.Stop();
            _chatUpdateTimer.Stop();

            if (_selectedServerProfile != null)
            {
                _updateTimer.Start();
                _chatUpdateTimer.Start();
            }
        }

        private void LoadServerProfiles()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var serverProfiles = JsonSerializer.Deserialize<List<ServerProfile>>(json);
                if (serverProfiles != null)
                {
                    ServerProfiles = new ObservableCollection<ServerProfile>(serverProfiles);
                }
            }
        }
        private void CopyIdToClipboard(object parameter)
        {
            if (parameter is string playerInfo)
            {
                var playerId = ExtractPlayerId(playerInfo);
                Clipboard.SetText(playerId);
            }
        }

        private string ExtractPlayerId(string playerInfo)
        {
            // Splitting the string by ", " which is expected to be the separator between the player name and the ID.
            var parts = playerInfo.Split(new[] { ", " }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                // This will return the part after the comma, which is assumed to be the player ID.
                return parts[1].Trim();
            }
            else
            {
                // Handle the case where the string is not in the expected format.
                Debug.WriteLine("Unexpected player info format: " + playerInfo);
                return string.Empty; // or return null; depending on how you want to handle this case.
            }
        }


        private async Task UpdatePlayerList()
        {
            if (_selectedServerProfile == null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RconStatus = "Disconnected";
                    ConnectedPlayers.Clear();
                });
                _updateTimer.Stop();
                return;
            }

            try
            {
                var playersList = await SendRconCommandAsync(_selectedServerProfile, "ListPlayers");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Here, we assume that if the command is sent successfully, the server is online.
                    RconStatus = "Online"; // Set the status to Online by default.

                    if (playersList.Count == 0)
                    {
                        RconStatus = "No Players Connected"; // Now this indicates no players, but server is online.
                        ConnectedPlayers.Clear();
                    }
                    else
                    {
                        ConnectedPlayers = new ObservableCollection<string>(playersList);
                    }
                });
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RconStatus = "Offline";
                    ConnectedPlayers.Clear();
                });
            }
        }



        private async Task FetchChatAsync()
        {
            if (_selectedServerProfile == null)
            {
                Debug.WriteLine("No server profile selected. Chat update halted.");
                Application.Current.Dispatcher.Invoke(() => ServerChat.Clear());
                _chatUpdateTimer.Stop();
                return;
            }

            try
            {
                Debug.WriteLine($"Sending RCON command to fetch chat for server: {_selectedServerProfile.ServerName}");
                var chatMessages = await SendRconCommandAsync(_selectedServerProfile, "GetChat");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Check if there are any new messages and they are not just '>'
                    var newMessages = chatMessages.Where(msg => !string.IsNullOrWhiteSpace(msg) && msg.Trim() != ">").ToList();
                    if (newMessages.Count > 0)
                    {
                        foreach (var message in newMessages)
                        {
                            ServerChat.Add(message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception fetching chat: {ex}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ServerChat.Clear();
                    ServerChat.Add("Could not fetch chat messages.");
                });
            }
        }






        public void Dispose()
        {
            _updateTimer?.Dispose();
            _chatUpdateTimer?.Dispose();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Nested class for ServerProfile
        public class ServerProfile
        {
            public string ServerName { get; set; }
            public int RCONPort { get; set; }
            public string AdminPassword { get; set; }
            // Other properties...
        }
        private List<string> ParseChatMessages(string rconOutput)
        {
            var chatMessages = new List<string>();

            // Split the output into lines
            var lines = rconOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                // Check if the line contains an actual chat message and not the unwanted text
                if (!string.IsNullOrWhiteSpace(line) && !line.Contains("Server received, But no response!!"))
                {
                    // Add your regex matching and parsing logic here if needed
                    chatMessages.Add(line);
                }
            }

            return chatMessages;
        }



        private async Task InitializeRconConnection(ServerProfile profile)
        {
            try
            {
                rcon = new CoreRCON.RCON(IPAddress.Parse("127.0.0.1"), (ushort)profile.RCONPort, profile.AdminPassword);
                await rcon.ConnectAsync(); // Attempt to establish connection
                RconStatus = "Online"; // Set status to online only after a successful connection
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize RCON connection: {ex.Message}");
                RconStatus = "Offline"; // Set status to offline if connection fails
            }
        }



        private async Task<List<string>> SendRconCommandAsync(ServerProfile profile, string command)
        {
            List<string> result = new List<string>();

            try
            {
                Debug.WriteLine($"Attempting to send RCON command to server: {profile.ServerName}, Command: {command}");

                if (rcon == null)
                {
                    await InitializeRconConnection(profile);
                }

                string output = await rcon.SendCommandAsync(command);
                Debug.WriteLine($"RCON command sent. Output: {output}");
                // Process output as before...
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception sending RCON command: {ex}");
                result.Add("Error sending command: " + ex.Message);
            }

            return result;
        }





        private List<string> ParsePlayersList(string rconOutput)
        {
            var playersList = new List<string>();

            // Check for no players connected
            if (rconOutput.Contains("No Players Connected"))
            {
                return playersList; // Return an empty list if no players are connected
            }

            // Use regex to parse the output and extract player names and IDs
            var regex = new Regex(@"(\d+)\.\s(.+?),\s(\w+)");
            var matches = regex.Matches(rconOutput);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    // Assuming you want to store the player name and ID in the list
                    var playerName = match.Groups[2].Value.Trim();
                    var playerId = match.Groups[3].Value.Trim();
                    playersList.Add($"{playerName}, {playerId}");
                }
            }

            return playersList;
        }
    }
}
