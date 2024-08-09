using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Management.Automation;
using Ark_Ascended_Manager.Resources;
using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Windows;
using YourNamespace.Helpers;

namespace Ark_Ascended_Manager.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly string _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "serverMetrics.db");

        public Visibility AdminWarningVisibility { get; private set; }
        public Visibility AdminButtonVisibility { get; private set; }

        public ObservableCollection<ServerInfo> Servers { get; set; }

        private ServerInfo _selectedServer;
        public ServerInfo SelectedServer
        {
            get => _selectedServer;
            set
            {
                if (_selectedServer != value)
                {
                    _selectedServer = value;
                    OnPropertyChanged(nameof(SelectedServer));
                    // Fetch additional server info when a new server is selected
                    FetchAdditionalServerInfoAsync();
                }
            }
        }

        public ICommand FetchServerInfoCommand { get; }

        public DashboardViewModel()
        {
            bool isAdmin = AppAdminChecker.IsRunningAsAdministrator();
            AdminWarningVisibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
            AdminButtonVisibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;

            Servers = new ObservableCollection<ServerInfo>();
            LoadServerConfigs();
            FetchServerInfoCommand = new RelayCommand(async () => await FetchServerInfoAsync());

            InitializeDatabase();

            // Auto-fetch server info after loading configurations
            StartServerCycleAsync();
        }

        private void LoadServerConfigs()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

                if (!File.Exists(path))
                {
                    Debug.WriteLine($"Server config file not found at: {path}");
                    return;
                }

                // Read the JSON file using JsonHelper
                List<DiscordServerConfig> serverConfigs = JsonHelper.ReadJsonFile<List<DiscordServerConfig>>(path);

                if (serverConfigs == null || serverConfigs.Count == 0)
                {
                    Debug.WriteLine("No server configurations found in the JSON file");
                    return;
                }

                foreach (var config in serverConfigs)
                {
                    Servers.Add(new ServerInfo
                    {
                        Config = config,
                        CpuAffinity = "Not fetched",
                        RamUsage = "N/A",
                        CpuUsage = -1,
                        StorageSize = "N/A",
                        RconConnection = false
                    });
                }

                Debug.WriteLine($"Loaded {Servers.Count} server configurations");

                // Automatically select the first server
                if (Servers.Count > 0)
                {
                    SelectedServer = Servers[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading server configurations: {ex.Message}");
            }
        }


        private async Task FetchServerInfoAsync()
        {
            Debug.WriteLine("FetchServerInfo called");

            if (Servers == null || Servers.Count == 0)
            {
                Debug.WriteLine("No servers to process");
                return;
            }

            var tasks = Servers.Select(async server =>
            {
                if (server.Config == null)
                {
                    Debug.WriteLine("Server config is null");
                    return;
                }

                Debug.WriteLine($"Processing server: {server.Config.ServerName}");

                string baseServerPath = Path.GetFullPath(server.Config.ServerPath);
                string fullPathArk = Path.GetFullPath(Path.Combine(baseServerPath, "ShooterGame\\Binaries\\Win64\\ArkAscendedServer.exe"));
                string fullPathAsa = Path.GetFullPath(Path.Combine(baseServerPath, "ShooterGame\\Binaries\\Win64\\AsaApiLoader.exe"));

                Debug.WriteLine($"{fullPathArk}");
                Debug.WriteLine($"{fullPathAsa}");

                var processInfo = await Task.Run(() => GetProcessInfoUsingPowerShell(fullPathArk)) ?? await Task.Run(() => GetProcessInfoUsingPowerShell(fullPathAsa));
                if (processInfo != null)
                {
                    server.CpuAffinity = ConvertCpuAffinityToReadable(processInfo.CpuAffinity);
                    server.RamUsage = FormatRamUsage(processInfo.RamUsage);
                    server.CpuUsage = Math.Round(processInfo.CpuUsage, 2);
                    server.StorageSize = await Task.Run(() => GetFormattedStorageSize(baseServerPath));
                    server.RconConnection = await Task.Run(() => CheckRconConnection(server.Config.ServerIP, server.Config.RCONPort, server.Config.AdminPassword, baseServerPath));

                    Debug.WriteLine($"Updated info for server: {server.Config.ServerName}");

                    // Log metrics to database
                    LogMetricsToDatabase(server);
                }
                else
                {
                    server.CpuAffinity = "Server process not found.";
                    server.RamUsage = "N/A";
                    server.CpuUsage = -1;
                    server.StorageSize = "N/A";
                    server.RconConnection = false;

                    Debug.WriteLine($"Process not found for server: {server.Config.ServerName}");
                }

                OnPropertyChanged(nameof(Servers));
            });

            await Task.WhenAll(tasks);
        }

        private async Task StartServerCycleAsync()
        {
            while (true)
            {
                await FetchServerInfoAsync();
                await Task.Delay(TimeSpan.FromMinutes(5)); // Adjust the interval as needed
            }
        }

        private string CleanUpPath(string path)
        {
            return path.Replace(@"\\", @"\");
        }

        private async Task<ProcessInfo> GetProcessInfoUsingPowerShell(string executablePath)
        {
            try
            {
                Debug.WriteLine($"Searching for process with executable path: {executablePath}");

                using (PowerShell ps = PowerShell.Create())
                {
                    // Normalize path for PowerShell (single backslashes)
                    string normalizedPath = executablePath.Replace("\\", "\\");

                    string script = $@"
                    Get-Process | 
                    Where-Object {{ $_.Path -eq '{normalizedPath}' }} | 
                    Select-Object Id, Path, WorkingSet, ProcessorAffinity
                ";
                    ps.AddScript(script);

                    var results = await Task.Run(() => ps.Invoke());

                    if (ps.Streams.Error.Count > 0)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Debug.WriteLine($"PowerShell Error: {error}");
                        }
                    }

                    foreach (PSObject result in results)
                    {
                        var processId = Convert.ToInt32(result.Properties["Id"].Value);
                        var ramUsage = Convert.ToInt64(result.Properties["WorkingSet"].Value) / 1024 / 1024; // Convert bytes to MB
                        var affinityMask = (long)(IntPtr)result.Properties["ProcessorAffinity"].Value;
                        var coresUsed = Convert.ToString(affinityMask, 2).PadLeft(Environment.ProcessorCount, '0');

                        return new ProcessInfo
                        {
                            CpuAffinity = coresUsed,
                            RamUsage = ramUsage,
                            CpuUsage = await Task.Run(() => GetCpuUsageWMI(processId))
                        };
                    }
                }

                Debug.WriteLine("No matching process found.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching process info: {ex.Message}");
            }

            return null;
        }

        private async Task<double> GetCpuUsageWMI(int processId)
        {
            var process = Process.GetProcessById(processId);
            var processName = process.ProcessName.Replace(".exe", "");

            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName, true);
            cpuCounter.NextValue();
            await Task.Delay(1000);
            return cpuCounter.NextValue() / Environment.ProcessorCount;
        }

        private async Task<string> GetFormattedStorageSize(string serverPath)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(serverPath));
                var totalSizeGB = driveInfo.TotalSize / 1024.0 / 1024 / 1024;
                var folderSize = await Task.Run(() => GetDirectorySize(serverPath)) / 1024.0 / 1024 / 1024;
                return $"{Math.Round(folderSize, 2)} GB / {Math.Round(totalSizeGB, 2)} GB";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting storage size: {ex.Message}\nStackTrace: {ex.StackTrace}\nSource: {ex.Source}");
                return "N/A";
            }
        }

        private long GetDirectorySize(string folderPath)
        {
            long size = 0;
            try
            {
                var dirInfo = new DirectoryInfo(folderPath);
                size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting directory size: {ex.Message}");
            }
            return size;
        }

        private async Task<bool> CheckRconConnection(string serverIP, int rconPort, string password, string serverPath)
        {
            try
            {
                string ip = string.IsNullOrEmpty(serverIP) ? "127.0.0.1" : serverIP;
                using (var arkRconService = new ArkRCONService(ip, (ushort)rconPort, password, serverPath))
                {
                    await arkRconService.ConnectAsync();
                    var result = await arkRconService.SendCommandAsync("listplayers");
                    return !string.IsNullOrEmpty(result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking RCON connection: {ex.Message}");
                return false;
            }
        }

        private async Task FetchAdditionalServerInfoAsync()
        {
            if (SelectedServer == null || SelectedServer.Config == null)
            {
                Debug.WriteLine("No selected server or server config to fetch additional info");
                return;
            }

            try
            {
                string ip = string.IsNullOrEmpty(SelectedServer.Config.ServerIP) ? "127.0.0.1" : SelectedServer.Config.ServerIP;
                using (var arkRconService = new ArkRCONService(ip, (ushort)SelectedServer.Config.RCONPort, SelectedServer.Config.AdminPassword, SelectedServer.Config.ServerPath))
                {
                    await arkRconService.ConnectAsync();
                    SelectedServer.RconConnection = true;
                    SelectedServer.PlayerInfo = await arkRconService.ListPlayersAsync();
                    SelectedServer.ChatHistory = await arkRconService.GetServerChatAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching additional server info: {ex.Message}");
                SelectedServer.RconConnection = false;
                SelectedServer.PlayerInfo = "Unable to fetch player info";
                SelectedServer.ChatHistory = "Unable to fetch chat history";
            }

            OnPropertyChanged(nameof(SelectedServer));
        }

        private string ConvertCpuAffinityToReadable(string cpuAffinity)
        {
            var cores = new List<int>();
            for (int i = 0; i < cpuAffinity.Length; i++)
            {
                if (cpuAffinity[i] == '1')
                {
                    cores.Add(i);
                }
            }
            return string.Join(", ", cores);
        }

        private string FormatRamUsage(long ramUsage)
        {
            if (ramUsage < 1024)
            {
                return $"{ramUsage} MB";
            }
            else
            {
                var ramInGB = ramUsage / 1024.0;
                return $"{Math.Round(ramInGB, 2)} GB";
            }
        }

        private void InitializeDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS ServerMetrics (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ServerName TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        CpuAffinity TEXT,
                        RamUsage TEXT,
                        CpuUsage REAL,
                        StorageSize TEXT,
                        RconConnection INTEGER
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void LogMetricsToDatabase(ServerInfo server)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string insertQuery = @"
                    INSERT INTO ServerMetrics (ServerName, CpuAffinity, RamUsage, CpuUsage, StorageSize, RconConnection)
                    VALUES (@ServerName, @CpuAffinity, @RamUsage, @CpuUsage, @StorageSize, @RconConnection)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@ServerName", server.Config.ServerName);
                    command.Parameters.AddWithValue("@CpuAffinity", server.CpuAffinity);
                    command.Parameters.AddWithValue("@RamUsage", server.RamUsage);
                    command.Parameters.AddWithValue("@CpuUsage", server.CpuUsage);
                    command.Parameters.AddWithValue("@StorageSize", server.StorageSize);
                    command.Parameters.AddWithValue("@RconConnection", server.RconConnection ? 1 : 0);
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    public class ServerInfo : INotifyPropertyChanged
    {
        private string cpuAffinity;
        private string ramUsage;
        private double cpuUsage;
        private string storageSize;
        private bool rconConnection;
        private string playerInfo;
        private string chatHistory;

        public DiscordServerConfig Config { get; set; }

        public string CpuAffinity
        {
            get => cpuAffinity;
            set
            {
                cpuAffinity = value;
                OnPropertyChanged(nameof(CpuAffinity));
            }
        }

        public string RamUsage
        {
            get => ramUsage;
            set
            {
                ramUsage = value;
                OnPropertyChanged(nameof(RamUsage));
            }
        }

        public double CpuUsage
        {
            get => cpuUsage;
            set
            {
                cpuUsage = value;
                OnPropertyChanged(nameof(CpuUsage));
            }
        }

        public string StorageSize
        {
            get => storageSize;
            set
            {
                storageSize = value;
                OnPropertyChanged(nameof(StorageSize));
            }
        }

        public bool RconConnection
        {
            get => rconConnection;
            set
            {
                rconConnection = value;
                OnPropertyChanged(nameof(RconConnection));
            }
        }

        public string PlayerInfo
        {
            get => playerInfo;
            set
            {
                playerInfo = value;
                OnPropertyChanged(nameof(PlayerInfo));
            }
        }

        public string ChatHistory
        {
            get => chatHistory;
            set
            {
                chatHistory = value;
                OnPropertyChanged(nameof(ChatHistory));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProcessInfo
    {
        public string CpuAffinity { get; set; }
        public long RamUsage { get; set; }
        public double CpuUsage { get; set; }
    }
}
