using System.Diagnostics;
using System.Timers;
using Newtonsoft.Json;
using System.IO;
using System.Timers;
namespace Ark_Ascended_Manager.Services
{
    public class CrashDetection
    {
        private System.Timers.Timer checkTimer;
        private readonly string jsonFilePath;
        private readonly object fileLock = new object();

        public CrashDetection()
        {
            jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "crashdetection.json");
            checkTimer = new System.Timers.Timer(30000); // Check every minute
            checkTimer.Elapsed += OnTimedEvent;
            checkTimer.Start();
            Debug.WriteLine("CrashDetection Timer started.");
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Timer ticked, checking server status.");
            var monitoringInfos = LoadMonitoringInfos();
            foreach (var info in monitoringInfos)
            {
                if (!IsServerRunning(info.ServerDirectory))
                {
                    RestartServer(info);
                }

            }
        }

        private List<MonitoringInfo> LoadMonitoringInfos()
        {
            lock (fileLock)
            {
                if (!File.Exists(jsonFilePath))
                {
                    // Create an empty JSON file if it doesn't exist
                    File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(new List<MonitoringInfo>(), Formatting.Indented));
                }

                string json = File.ReadAllText(jsonFilePath);
                return JsonConvert.DeserializeObject<List<MonitoringInfo>>(json) ?? new List<MonitoringInfo>();
            }
        }


        private bool IsServerRunning(string serverDirectory)
        {
            string arkServerExePath = Path.Combine(serverDirectory, @"ShooterGame\Binaries\Win64\ArkAscendedServer.exe");
            string asaApiLoaderExePath = Path.Combine(serverDirectory, @"ShooterGame\Binaries\Win64\AsaApiLoader.exe");


            // Get all running processes
            Process[] allProcesses = Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    // Get the full path of the running process
                    string processPath = process.MainModule.FileName;

                    // Check if it matches either of the server executables
                    if (processPath.Equals(arkServerExePath, StringComparison.OrdinalIgnoreCase) ||
                        processPath.Equals(asaApiLoaderExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore any process for which we cannot retrieve the path
                }
            }

            // No matching process found
            return false;
        }



        private void RestartServer(MonitoringInfo info)
        {
            try
            {
                string batchFilePath = Path.Combine(info.ServerDirectory, "LaunchServer.bat");
                if (File.Exists(batchFilePath))
                {
                    var process = Process.Start(batchFilePath);
                    UpdateMonitoringInfo(info, process.Id);  // Update with new PID
                    Thread.Sleep(5000);
                }
                else
                {
                    Debug.WriteLine($"Batch file not found at {batchFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restart server: {ex.Message}");
            }
        }



        private void UpdateMonitoringInfo(MonitoringInfo info, int newPid)
        {
            lock (fileLock)
            {
                var monitoringInfos = LoadMonitoringInfos();
                var serverInfo = monitoringInfos.FirstOrDefault(m => m.ServerDirectory == info.ServerDirectory);
                if (serverInfo != null)
                {
                    serverInfo.Pid = newPid;
                    string json = JsonConvert.SerializeObject(monitoringInfos, Formatting.Indented);
                    File.WriteAllText(jsonFilePath, json);
                }
            }
        }
    }

    public class MonitoringInfo
    {
        public string ServerDirectory { get; set; }
        public int Pid { get; set; }
    }
}
