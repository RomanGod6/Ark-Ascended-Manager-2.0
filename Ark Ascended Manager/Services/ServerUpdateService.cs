using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace Ark_Ascended_Manager.Services
{
    internal class ServerUpdateService
    {
        private readonly string _serverConfigPath;

        public ServerUpdateService(string serverConfigPath)
        {
            _serverConfigPath = serverConfigPath;
        }

        public async Task CheckAndUpdateServersOnStartup()
        {
            var servers = LoadServerProfiles();
            foreach (var server in servers)
            {
                var sanitizedData = LoadSanitizedData(server.AppId);
                if (sanitizedData == null || server.ChangeNumber != sanitizedData.ChangeNumber)
                {
                    await UpdateServer(server);
                    SetChangeNumberForServer(server, sanitizedData?.ChangeNumber ?? 0);
                }
            }
        }

        private List<ServerConfig> LoadServerProfiles()
        {
            string json = File.ReadAllText(_serverConfigPath);
            return JsonConvert.DeserializeObject<List<ServerConfig>>(json);

        }

        private async Task UpdateServer(ServerConfig server)
        {
            // Check if the ChangeNumber is set or matches the latest one from sanitized data
            var sanitizedData = LoadSanitizedData(server.AppId);
            if (sanitizedData == null || server.ChangeNumber != sanitizedData.ChangeNumber)
            {
                // Create the script for SteamCMD to update the server
                string scriptPath = CreateSteamCMDScript(server);

                // Run the SteamCMD script
                RunSteamCMD(scriptPath);

                // If needed, update the ChangeNumber after a successful update
                SetChangeNumberForServer(server, sanitizedData?.ChangeNumber ?? 0);

                // Save the updated server profile back to the servers.json file or wherever appropriate
                SaveServerProfile(server);

                // Optional: Delete the script file if you no longer need it
                // File.Delete(scriptPath);
            }
        }
        private void SaveServerProfile(ServerConfig server)
        {
            // Load the current list of server profiles
            var servers = LoadServerProfiles();

            // Update the specific server profile
            var serverToUpdate = servers.FirstOrDefault(s => s.AppId == server.AppId);
            if (serverToUpdate != null)
            {
                serverToUpdate.ChangeNumber = server.ChangeNumber;
                // ... update other properties as needed
            }

            // Save the updated list back to the JSON file
            string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
            File.WriteAllText(_serverConfigPath, json);
        }
        




        private void SetChangeNumberForServer(ServerConfig server, int newChangeNumber)
        {
            server.ChangeNumber = newChangeNumber;
            // Save the updated server profile back to the JSON file or wherever appropriate
        }

        private SanitizedSteamData LoadSanitizedData(string appId)
        {
            // Construct the path for the sanitized data file
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", appId, $"sanitizedsteamdata_{appId}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<SanitizedSteamData>(json);

            }
            return null;
        }
        private async Task UpdateServerWithSteamCMD(ServerConfig server)
        {
            string scriptPath = CreateSteamCMDScript(server);
            RunSteamCMD(scriptPath);
            // After the server is updated, you can delete the script file if needed.
        }

        private string CreateSteamCMDScript(ServerConfig server)
        {
            string scriptContent = @$"
        force_install_dir ""{server.ServerPath}""
        login anonymous
        app_update {server.AppId} validate
        quit
    ";

            string scriptPath = Path.Combine(Path.GetTempPath(), $"steamcmd_script_{server.AppId}.txt");
            File.WriteAllText(scriptPath, scriptContent);
            return scriptPath;
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
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)

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

    }

    // Assuming you have classes defined something like this:
    public class ServerConfig
    {
        public string ProfileName { get; set; }
        public string ServerStatus { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        public string AppId { get; set; }
        public string ServerIP { get; set; }
        public bool IsRunning { get; set; }
        public string ServerName { get; set; }
        public int ListenPort { get; set; }
        public int RCONPort { get; set; }
        public List<string> Mods { get; set; }
        public int MaxPlayerCount { get; set; }
        public string AdminPassword { get; set; }
        public string ServerIcon { get; set; }
        public string ServerPassword { get; set; }
        public bool UseBattlEye { get; set; }
        public bool ForceRespawnDinos { get; set; }
        public bool PreventSpawnAnimation { get; set; }
        public int ChangeNumber { get; set; }
        public string ChangeNumberStatus { get; set; }
    }

    public class SanitizedSteamData
    {
        public string AppId { get; set; }
        public int ChangeNumber { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
