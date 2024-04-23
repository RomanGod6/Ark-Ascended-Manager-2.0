using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Ark_Ascended_Manager.Services
{
    public class ServerMonitoringService : IHostedService, IDisposable
    {
        private Timer _timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Setup the timer to tick every 5 seconds
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var servers = LoadServerConfigs();
            bool anyServerStatusChanged = false;

            foreach (var serverConfig in servers)
            {
                int sanitizedChangeNumber = LoadSanitizedChangeNumber(serverConfig);
                bool isRunning = IsServerRunning(serverConfig);
                string newStatus = isRunning ? "Online" : "Offline";

                int serverChangeNumber = serverConfig.ChangeNumber;

                if (serverChangeNumber < sanitizedChangeNumber)
                {
                    serverConfig.ChangeNumberStatus = "Server is Not Up to Date";
                }
                else if (serverChangeNumber == sanitizedChangeNumber)
                {
                    serverConfig.ChangeNumberStatus = "Servers Up To Date";
                }

                if (serverConfig.ServerStatus != newStatus)
                {
                    serverConfig.ServerStatus = newStatus;
                    anyServerStatusChanged = true;
                }
            }

            if (anyServerStatusChanged)
            {
                SaveServerConfigs(servers);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private List<ServerConfig> LoadServerConfigs()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<ServerConfig>>(json) ?? new List<ServerConfig>();
            }
            return new List<ServerConfig>();
        }

        private int LoadSanitizedChangeNumber(ServerConfig serverConfig)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataFolder, "Ark Ascended Manager", serverConfig.AppId, "sanitizedsteamdata_" + serverConfig.AppId + ".json");
            try
            {
                string jsonData = File.ReadAllText(filePath);
                var jsonObject = JObject.Parse(jsonData);
                return jsonObject["ChangeNumber"].Value<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading or parsing the JSON file: " + ex.Message);
                return 0;
            }
        }

        private bool IsServerRunning(ServerConfig serverConfig)
        {
            string serverExeName = Path.GetFileNameWithoutExtension("ArkAscendedServer.exe");
            string serverExePath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe");

            var allProcesses = Process.GetProcesses();
            return allProcesses.Any(process => process.ProcessName.Equals(serverExeName, StringComparison.OrdinalIgnoreCase) &&
                                               process.MainModule.FileName.Equals(serverExePath, StringComparison.OrdinalIgnoreCase));
        }

        private void SaveServerConfigs(List<ServerConfig> servers)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");
            string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
