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
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.ViewModels.Windows
{
    public class Player
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string SteamID { get; set; }
    }

    public class CommandMapping
    {
        public string Display { get; set; }
        public string Command { get; set; }
    }

    public class CommandConfig
    {
        public ObservableCollection<CommandMapping> Commands { get; set; }
    }

    public class RconViewModel : INotifyPropertyChanged
    {
        private string _serverPath;
        private string _adminPassword;
        private int _rconPort;
        private FileSystemWatcher _logFileWatcher;
        private FileSystemWatcher _debugLogWatcher;
        private string _serverIP;
        private string _serverName;
        private string _chatLog;
        private string _logFileContent;
        private string _debugLogContent;
        private string _commandInput;
        private string _newCommandDisplay;
        private string _newCommand;
        private ArkRCONService _rconService;
        private ObservableCollection<Player> _players;
        private System.Timers.Timer _chatTimer;
        private HashSet<string> _receivedMessages;
        private string _selectedLogFile;
        private ObservableCollection<string> _logFiles;

        private ObservableCollection<CommandMapping> _commands;
        private CommandMapping _selectedCommand;
        private static readonly string ConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "commands.json");

        public bool NoPlayersConnected => Players.Count == 0;

        public RconViewModel()
        {
            _players = new ObservableCollection<Player>();
            _receivedMessages = new HashSet<string>();
            _logFiles = new ObservableCollection<string>();
            _commands = new ObservableCollection<CommandMapping>();
            InitializeCommands();
            SendCommand = new RelayCommand(async () => await ExecuteSendCommand(), CanExecuteSendCommand);
            FetchPlayersCommand = new RelayCommand(async () => await FetchPlayers());
            AddCommand = new RelayCommand(AddNewCommand);
            SaveCommandsCommand = new RelayCommand(SaveCommands);
            DeleteCommand = new RelayCommand<CommandMapping>(DeleteSelectedCommand);
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

        public string DebugLogContent
        {
            get => _debugLogContent;
            set
            {
                _debugLogContent = value;
                OnPropertyChanged(nameof(DebugLogContent));
            }
        }

        public string ServerPath
        {
            get => _serverPath;
            set
            {
                _serverPath = value;
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
                OnPropertyChanged(nameof(ServerName));
            }
        }

        public string AdminPassword
        {
            get => _adminPassword;
            set
            {
                _adminPassword = value;
                OnPropertyChanged(nameof(AdminPassword));
            }
        }

        public int RCONPort
        {
            get => _rconPort;
            set
            {
                _rconPort = value;
                OnPropertyChanged(nameof(RCONPort));
            }
        }

        public string ServerIP
        {
            get => _serverIP;
            set
            {
                _serverIP = value;
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

        public string NewCommandDisplay
        {
            get => _newCommandDisplay;
            set
            {
                _newCommandDisplay = value;
                OnPropertyChanged(nameof(NewCommandDisplay));
            }
        }

        public string NewCommand
        {
            get => _newCommand;
            set
            {
                _newCommand = value;
                OnPropertyChanged(nameof(NewCommand));
            }
        }

        public ObservableCollection<CommandMapping> Commands
        {
            get => _commands;
            set
            {
                _commands = value;
                OnPropertyChanged(nameof(Commands));
            }
        }

        public CommandMapping SelectedCommand
        {
            get => _selectedCommand;
            set
            {
                _selectedCommand = value;
                OnPropertyChanged(nameof(SelectedCommand));
                OnSelectedCommandChanged();
            }
        }

        public ICommand SendCommand { get; }
        public ICommand FetchPlayersCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommandsCommand { get; }
        public ICommand DeleteCommand { get; }

        private bool CanExecuteSendCommand()
        {
            return !string.IsNullOrWhiteSpace(CommandInput);
        }

        private void LoadLogFiles()
        {
            if (string.IsNullOrEmpty(_serverPath))
            {
                return;
            }

            string logDirectory = Path.Combine(_serverPath, "ShooterGame", "Saved", "Logs");
            if (!Directory.Exists(logDirectory))
            {
                return;
            }

            var logFiles = Directory.GetFiles(logDirectory, "ServerGame*.log")
                .Concat(Directory.GetFiles(logDirectory, "ShooterGame*.log"))
                .ToList();

            LogFiles.Clear();
            foreach (var logFile in logFiles)
            {
                LogFiles.Add(logFile);
            }

            if (LogFiles.Count > 0)
            {
                SelectedLogFile = LogFiles.First();
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
                LogFileContent = File.ReadAllText(SelectedLogFile);
            }
            catch (Exception ex)
            {
                LogFileContent = $"Error loading log content: {ex.Message}";
            }
        }

        private async Task ExecuteSendCommand()
        {
            if (_rconService != null)
            {
                try
                {
                    var response = await _rconService.SendCommandAsync(CommandInput);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ChatLog += $"[{timestamp}] Command Sent: {CommandInput}" + Environment.NewLine;
                    ChatLog += $"[{timestamp}] Response: {response}" + Environment.NewLine;
                    CommandInput = string.Empty;
                }
                catch (Exception ex)
                {
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
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                }
            }
        }

        public void InitializeRconService()
        {
            if (string.IsNullOrEmpty(_serverPath))
            {
                throw new ArgumentNullException(nameof(ServerPath), "Server path cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(_serverIP))
            {
                throw new ArgumentNullException(nameof(ServerIP), "Server IP cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(_adminPassword))
            {
                throw new ArgumentNullException(nameof(AdminPassword), "Admin password cannot be null or empty.");
            }

            if (_rconPort <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(RCONPort), "RCON port must be greater than zero.");
            }

            try
            {
                _rconService = new ArkRCONService(_serverIP, (ushort)_rconPort, _adminPassword, _serverPath);
                StartChatTimer();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void StartChatTimer()
        {
            _chatTimer = new System.Timers.Timer(5000);
            _chatTimer.Elapsed += async (sender, e) => await FetchChatLog();
            _chatTimer.Start();
        }

        private async Task FetchChatLog()
        {
            try
            {
                if (_rconService == null)
                {
                    return;
                }

                var newChatLog = await _rconService.GetServerChatAsync();
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
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                if (ChatLog?.Contains("Attempting to connect to server... Please stand by.") == true)
                {
                    ChatLog = "Failed to connect to the server: The target machine actively refused the connection. Please check if the RCON server is running and the IP and port are correct.";
                }
            }
            catch (TimeoutException ex)
            {
                if (ChatLog?.Contains("Attempting to connect to server... Please stand by.") == true)
                {
                    ChatLog = "Failed to connect to the server: Timeout while waiting for authentication response. Please check the server and try again.";
                }
            }
            catch (InvalidOperationException ex)
            {
                if (ChatLog?.Contains("Attempting to connect to server... Please stand by.") == true)
                {
                    ChatLog = "Could not connect to the RCON server. Please ensure the server details are correct and the server is running.";
                }
            }
            catch (Exception ex)
            {
                if (ChatLog.Contains("Attempting to connect to server... Please stand by."))
                {
                    ChatLog = $"An unexpected error occurred: {ex.Message}";
                }
            }
        }

        private void OnSelectedCommandChanged()
        {
            if (SelectedCommand != null)
            {
                CommandInput = SelectedCommand.Command;
            }
        }

        private void InitializeCommands()
        {
            LoadCommands();
            if (Commands.Count == 0)
            {
                Commands.Add(new CommandMapping { Display = "/save", Command = "saveworld" });
                SaveCommands();
            }
        }

        private void LoadCommands()
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonConvert.DeserializeObject<CommandConfig>(json);
                if (config != null)
                {
                    Commands = new ObservableCollection<CommandMapping>(config.Commands);
                }
            }
            else
            {
                Commands = new ObservableCollection<CommandMapping>();
            }
        }

        private void SaveCommands()
        {
            var config = new CommandConfig { Commands = Commands };
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }

        private void AddNewCommand()
        {
            if (!string.IsNullOrWhiteSpace(NewCommandDisplay) && !string.IsNullOrWhiteSpace(NewCommand))
            {
                Commands.Add(new CommandMapping { Display = NewCommandDisplay, Command = NewCommand });
                SaveCommands();
                NewCommandDisplay = string.Empty;
                NewCommand = string.Empty;
            }
        }

        private void DeleteSelectedCommand(CommandMapping commandMapping)
        {
            if (commandMapping != null && Commands.Contains(commandMapping))
            {
                Commands.Remove(commandMapping);
                SaveCommands();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
