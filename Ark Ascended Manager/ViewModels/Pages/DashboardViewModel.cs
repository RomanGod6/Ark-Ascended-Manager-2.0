using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Management.Automation;
using Ark_Ascended_Manager.Resources;
using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Windows;

namespace Ark_Ascended_Manager.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        public Visibility AdminWarningVisibility { get; private set; }
        public Visibility AdminButtonVisibility { get; private set; }

        public ObservableCollection<ServerInfo> Servers { get; set; }

        public ICommand FetchServerInfoCommand { get; }

        public DashboardViewModel()
        {
            bool isAdmin = AppAdminChecker.IsRunningAsAdministrator();
            AdminWarningVisibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
            AdminButtonVisibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;

            Servers = new ObservableCollection<ServerInfo>();
            LoadServerConfigs();
            FetchServerInfoCommand = new RelayCommand(FetchServerInfo);
        }

        private void LoadServerConfigs()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            if (!File.Exists(path))
            {
                Debug.WriteLine($"Server config file not found at: {path}");
                return;
            }

            var json = File.ReadAllText(path);
            Debug.WriteLine($"Server config file content: {json}");

            List<DiscordServerConfig> serverConfigs = null;
            try
            {
                serverConfigs = JsonSerializer.Deserialize<List<DiscordServerConfig>>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deserializing JSON: {ex.Message}");
                return;
            }

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
                    RamUsage = -1,
                    CpuUsage = -1,
                    StorageSize = -1,
                    RconConnection = false
                });
            }

            Debug.WriteLine($"Loaded {Servers.Count} server configurations");
        }

        private void FetchServerInfo()
        {
            Debug.WriteLine("FetchServerInfo called");

            if (Servers == null || Servers.Count == 0)
            {
                Debug.WriteLine("No servers to process");
                return;
            }

            foreach (var server in Servers)
            {
                if (server.Config == null)
                {
                    Debug.WriteLine("Server config is null");
                    continue;
                }

                Debug.WriteLine($"Processing server: {server.Config.ServerName}");

                string baseServerPath = Path.GetFullPath(server.Config.ServerPath);
                string fullPathArk = Path.GetFullPath(Path.Combine(baseServerPath, "ShooterGame\\Binaries\\Win64\\ArkAscendedServer.exe"));
                string fullPathAsa = Path.GetFullPath(Path.Combine(baseServerPath, "ShooterGame\\Binaries\\Win64\\AsaApiLoader.exe"));

                Debug.WriteLine($"{fullPathArk}");
                Debug.WriteLine($"{fullPathAsa}");

                var processInfo = GetProcessInfoUsingPowerShell(fullPathArk) ?? GetProcessInfoUsingPowerShell(fullPathAsa);
                if (processInfo != null)
                {
                    server.CpuAffinity = processInfo.CpuAffinity;
                    server.RamUsage = processInfo.RamUsage;
                    server.CpuUsage = processInfo.CpuUsage;
                    server.StorageSize = GetStorageSize(baseServerPath);
                    server.RconConnection = CheckRconConnection(server.Config.RCONPort);

                    Debug.WriteLine($"Updated info for server: {server.Config.ServerName}");
                }
                else
                {
                    server.CpuAffinity = "Server process not found.";
                    server.RamUsage = -1;
                    server.CpuUsage = -1;
                    server.StorageSize = -1;
                    server.RconConnection = false;

                    Debug.WriteLine($"Process not found for server: {server.Config.ServerName}");
                }

                OnPropertyChanged(nameof(Servers));
            }
        }



        private string CleanUpPath(string path)
        {
            return path.Replace(@"\\", @"\");
        }


        private ProcessInfo GetProcessInfoUsingPowerShell(string executablePath)
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

                    var results = ps.Invoke();

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
                            CpuUsage = GetCpuUsageWMI(processId)
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







        private double GetCpuUsageWMI(int processId)
        {
            var process = Process.GetProcessById(processId);
            var processName = process.ProcessName.Replace(".exe", "");

            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName, true);
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            return cpuCounter.NextValue() / Environment.ProcessorCount;
        }

        private long GetStorageSize(string serverPath)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(serverPath));
                return driveInfo.TotalSize / 1024 / 1024; // Convert bytes to MB
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting storage size: {ex.Message}\nStackTrace: {ex.StackTrace}\nSource: {ex.Source}");
                return -1;
            }
        }

        private bool CheckRconConnection(int rconPort)
        {
            // Implement RCON connection check logic
            return false;
        }
    }

    public class ServerInfo : INotifyPropertyChanged
    {
        private string cpuAffinity;
        private long ramUsage;
        private double cpuUsage;
        private long storageSize;
        private bool rconConnection;

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

        public long RamUsage
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

        public long StorageSize
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
