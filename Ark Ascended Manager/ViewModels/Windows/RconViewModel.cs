using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Ark_Ascended_Manager.Services;
using CommunityToolkit.Mvvm.Input;

namespace Ark_Ascended_Manager.ViewModels.Windows
{
    public class Player
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string SteamID { get; set; }
    }

    public class RconViewModel : INotifyPropertyChanged
    {
        private string _serverPath;
        private string _adminPassword;
        private int _rconPort;
        private FileSystemWatcher _logFileWatcher;
        private string _serverIP;
        private string _serverName;
        private string _chatLog;
        private string _logFileContent;
        private string _commandInput;
        private ArkRCONService _rconService;
        private ObservableCollection<Player> _players;
        private System.Timers.Timer _chatTimer;
        private HashSet<string> _receivedMessages;
        private string _selectedLogFile;
        private ObservableCollection<string> _logFiles;

        public bool NoPlayersConnected => Players.Count == 0;

        public RconViewModel()
        {
            _players = new ObservableCollection<Player>();
            _receivedMessages = new HashSet<string>();
            _logFiles = new ObservableCollection<string>();
            SendCommand = new RelayCommand(async () => await ExecuteSendCommand(), CanExecuteSendCommand);
            FetchPlayersCommand = new RelayCommand(async () => await FetchPlayers());
            DebugLog("RconViewModel instantiated.");
        }

        public ObservableCollection<string> LogFiles
        {
            get => _logFiles;
            set
            {
                _logFiles = value;
                OnPropertyChanged(nameof(LogFiles));
            }
        }

        public string SelectedLogFile
        {
            get => _selectedLogFile;
            set
            {
                _selectedLogFile = value;
                OnPropertyChanged(nameof(SelectedLogFile));
                LoadSelectedLogFileContent();
            }
        }

        public string LogFileContent
        {
            get => _logFileContent;
            set
            {
                _logFileContent = value;
                OnPropertyChanged(nameof(LogFileContent));
            }
        }

        public string ServerPath
        {
            get => _serverPath;
            set
            {
                _serverPath = value;
                DebugLog($"ServerPath set to: {_serverPath}");
                OnPropertyChanged(nameof(ServerPath));
                LoadLogFiles();
            }
        }

        public ObservableCollection<Player> Players
        {
            get => _players;
            set
            {
                _players = value;
                OnPropertyChanged(nameof(Players));
                OnPropertyChanged(nameof(NoPlayersConnected)); // Notify that NoPlayersConnected has changed
            }
        }

        public string ServerName
        {
            get => _serverName;
            set
            {
                _serverName = value;
                DebugLog($"ServerName set to: {_serverName}");
                OnPropertyChanged(nameof(ServerName));
            }
        }

        public string AdminPassword
        {
            get => _adminPassword;
            set
            {
                _adminPassword = value;
                DebugLog($"AdminPassword set to: {new string('*', _adminPassword.Length)}");
                OnPropertyChanged(nameof(AdminPassword));
            }
        }

        public int RCONPort
        {
            get => _rconPort;
            set
            {
                _rconPort = value;
                DebugLog($"RCONPort set to: {_rconPort}");
                OnPropertyChanged(nameof(RCONPort));
            }
        }

        public string ServerIP
        {
            get => _serverIP;
            set
            {
                _serverIP = value;
                DebugLog($"ServerIP set to: {_serverIP}");
                OnPropertyChanged(nameof(ServerIP));
            }
        }

        public string ChatLog
        {
            get => _chatLog;
            set
            {
                _chatLog = value;
                OnPropertyChanged(nameof(ChatLog));
            }
        }

        public string CommandInput
        {
            get => _commandInput;
            set
            {
                _commandInput = value;
                OnPropertyChanged(nameof(CommandInput));
                ((RelayCommand)SendCommand).NotifyCanExecuteChanged();
            }
        }

        public ICommand SendCommand { get; }
        public ICommand FetchPlayersCommand { get; }

        private bool CanExecuteSendCommand()
        {
            return !string.IsNullOrWhiteSpace(CommandInput);
        }

        private void LoadLogFiles()
        {
            if (string.IsNullOrEmpty(_serverPath))
            {
                DebugLog("Server path is not set. Cannot load log files.");
                return;
            }

            string logDirectory = Path.Combine(_serverPath, "ShooterGame", "Saved", "Logs");
            if (!Directory.Exists(logDirectory))
            {
                DebugLog($"Log directory not found at: {logDirectory}");
                return;
            }

            DebugLog($"Loading log files from directory: {logDirectory}");
            var allFiles = Directory.GetFiles(logDirectory);
            DebugLog($"All files in directory: {string.Join(", ", allFiles)}");

            var logFiles = Directory.GetFiles(logDirectory, "ServerGame*.log")
                .Concat(Directory.GetFiles(logDirectory, "ShooterGame*.log"))
                .ToList();
            DebugLog($"Matched log files: {string.Join(", ", logFiles)}");

            LogFiles.Clear();
            foreach (var logFile in logFiles)
            {
                DebugLog($"Adding log file to collection: {logFile}");
                LogFiles.Add(logFile);
            }

            if (LogFiles.Count > 0)
            {
                DebugLog($"Setting selected log file to the first one found: {LogFiles.First()}");
                SelectedLogFile = LogFiles.First();
            }
            else
            {
                DebugLog("No log files found.");
            }
        }

        private void LoadSelectedLogFileContent()
        {
            if (string.IsNullOrEmpty(SelectedLogFile))
            {
                LogFileContent = string.Empty;
                return;
            }

            try
            {
                DebugLog($"Loading log content from: {SelectedLogFile}");
                LogFileContent = File.ReadAllText(SelectedLogFile);
                DebugLog("Log content loaded.");
            }
            catch (Exception ex)
            {
                DebugLog($"Exception during loading log content: {ex.Message}");
                LogFileContent = $"Error loading log content: {ex.Message}";
            }
        }

        private async Task ExecuteSendCommand()
        {
            if (_rconService != null)
            {
                try
                {
                    DebugLog($"Sending command: {CommandInput}");
                    var response = await _rconService.SendCommandAsync(CommandInput);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ChatLog += $"[{timestamp}] Command Sent: {CommandInput}" + Environment.NewLine;
                    ChatLog += $"[{timestamp}] Response: {response}" + Environment.NewLine;
                    CommandInput = string.Empty;
                }
                catch (Exception ex)
                {
                    DebugLog($"Exception during sending command: {ex.Message}");
                    ChatLog += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}" + Environment.NewLine;
                }
            }
        }

        public async Task FetchPlayers()
        {
            if (_rconService != null)
            {
                try
                {
                    DebugLog("Fetching players...");
                    var response = await _rconService.SendCommandAsync("listplayers");
                    var lines = response.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    Players.Clear();
                    foreach (var line in lines.Skip(1))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            Players.Add(new Player
                            {
                                ID = int.Parse(parts[0]),
                                Name = parts[1],
                                SteamID = parts[2]
                            });
                        }
                    }
                    OnPropertyChanged(nameof(NoPlayersConnected)); // Notify that NoPlayersConnected has changed
                    DebugLog("Players fetched and parsed.");
                }
                catch (Exception ex)
                {
                    DebugLog($"Exception during fetching players: {ex.Message}");
                }
            }
        }

        public void InitializeRconService()
        {
            DebugLog("Initializing RconService...");

            if (string.IsNullOrEmpty(_serverPath))
            {
                DebugLog("Server path is null or empty.");
                throw new ArgumentNullException(nameof(ServerPath), "Server path cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(_serverIP))
            {
                DebugLog("Server IP is null or empty.");
                throw new ArgumentNullException(nameof(ServerIP), "Server IP cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(_adminPassword))
            {
                DebugLog("Admin password is null or empty.");
                throw new ArgumentNullException(nameof(AdminPassword), "Admin password cannot be null or empty.");
            }

            if (_rconPort <= 0)
            {
                DebugLog("RCON port is less than or equal to zero.");
                throw new ArgumentOutOfRangeException(nameof(RCONPort), "RCON port must be greater than zero.");
            }

            try
            {
                DebugLog($"Attempting to connect with ServerIP: {_serverIP}, RCONPort: {_rconPort}, AdminPassword: {new string('*', _adminPassword.Length)}");

                _rconService = new ArkRCONService(_serverIP, (ushort)_rconPort, _adminPassword, _serverPath);
                DebugLog("RconService initialized successfully.");
                StartChatTimer();
            }
            catch (Exception ex)
            {
                DebugLog($"Exception during RconService initialization: {ex.Message}");
                throw;
            }
        }

        private void StartChatTimer()
        {
            _chatTimer = new System.Timers.Timer(5000);
            _chatTimer.Elapsed += async (sender, e) => await FetchChatLog();
            _chatTimer.Start();
            DebugLog("Chat timer started.");
        }

        private async Task FetchChatLog()
        {
            try
            {
                if (_rconService == null)
                {
                    DebugLog("RconService is null.");
                    return;
                }

                DebugLog("Fetching chat log...");
                var newChatLog = await _rconService.GetServerChatAsync();
                DebugLog("Chat log fetched.");

                var newMessages = newChatLog?.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Where(msg => !_receivedMessages.Contains(msg) && (msg.Contains("SERVER") || msg.Contains("USER")));

                if (newMessages != null)
                {
                    foreach (var message in newMessages)
                    {
                        _receivedMessages.Add(message);
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        ChatLog += $"[{timestamp}] {message}" + Environment.NewLine;
                    }
                }
                DebugLog("Chat log updated.");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                DebugLog($"SocketException during chat log fetch: {ex.Message}");
                if (ChatLog?.Contains("Attempting to connect to server... Please stand by.") == true)
                {
                    ChatLog = "Failed to connect to the server: The target machine actively refused the connection. Please check if the RCON server is running and the IP and port are correct.";
                }
            }
            catch (TimeoutException ex)
            {
                DebugLog($"TimeoutException during chat log fetch: {ex.Message}");
                if (ChatLog?.Contains("Attempting to connect to server... Please stand by.") == true)
                {
                    ChatLog = "Failed to connect to the server: Timeout while waiting for authentication response. Please check the server and try again.";
                }
            }
            catch (InvalidOperationException ex)
            {
                DebugLog($"InvalidOperationException during chat log fetch: {ex.Message}");
                if (ChatLog?.Contains("Attempting to connect to server... Please stand by.") == true)
                {
                    ChatLog = "Could not connect to the RCON server. Please ensure the server details are correct and the server is running.";
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Exception during chat log fetch: {ex.Message}");
                if (ChatLog.Contains("Attempting to connect to server... Please stand by."))
                {
                    ChatLog = $"An unexpected error occurred: {ex.Message}";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DebugLog(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[RconViewModel] {message}");
        }
    }
}
