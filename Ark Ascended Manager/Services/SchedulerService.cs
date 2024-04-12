using CoreRCON;
using CoreRCON.Parsers.Standard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers; // This is for System.Timers.Timer
using System.Windows.Forms;

namespace Ark_Ascended_Manager.Services
{
    internal class SchedulerService
    {
        private List<Schedule> schedules;
        private List<Server> servers;
        private System.Timers.Timer timer; 

        public SchedulerService()
        {
            
            LoadSchedules();
            LoadServers();
            InitializeSchedulesWatcher();
            // Initialize and configure the System.Timers.Timer
            timer = new System.Timers.Timer(60000); // Set interval to 60,000 milliseconds (1 minute)
            timer.Elapsed += Timer_Elapsed; // Subscribe to the Elapsed event
            timer.AutoReset = true; // Enable AutoReset to continuously raise the event
            timer.Start(); // Start the timer

        }

        private void LoadSchedules()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string schedulesFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "schedules.json");

            // Check if the file exists before trying to read it
            if (File.Exists(schedulesFilePath))
            {
                string schedulesJson = File.ReadAllText(schedulesFilePath);
                schedules = JsonConvert.DeserializeObject<List<Schedule>>(schedulesJson);
                Debug.WriteLine($"Loaded {schedules.Count} schedules.");
            }
            else
            {
                // If the file doesn't exist, you could either create a new list or handle the case appropriately
                schedules = new List<Schedule>();
                Debug.WriteLine("No schedules.json file found. Loaded 0 schedules.");
            }
        }


        private void LoadServers()
        {
            try
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string serversFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");

                if (!File.Exists(serversFilePath))
                {
                    Debug.WriteLine("servers.json file not found.");
                    return;
                }

                string serversJson = File.ReadAllText(serversFilePath);
                servers = JsonConvert.DeserializeObject<List<Server>>(serversJson);
                Debug.WriteLine($"Loaded {servers.Count} servers.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading servers: {ex.Message}");
            }
        }



        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            TimeSpan? nextActionTime = null;

            // First, handle any regular scheduled tasks.
            foreach (var schedule in schedules)
            {
                if (schedule.Days.Contains(now.DayOfWeek.ToString()))
                {
                    var scheduleTime = TimeSpan.Parse(schedule.Time);
                    var timeUntilAction = scheduleTime - now.TimeOfDay;

                    if (now.TimeOfDay <= scheduleTime &&
                        (!nextActionTime.HasValue || timeUntilAction < nextActionTime))
                    {
                        nextActionTime = timeUntilAction;
                    }

                    // Check if the current time is within a minute of the scheduled time
                    if (Math.Abs(timeUntilAction.TotalMinutes) < 1)
                    {
                        Debug.WriteLine($"Executing scheduled task: {schedule.Nickname}");
                        Task.Run(() => ExecuteSchedule(schedule));
                    }
                }
            }

            // Next, check if there are any update-on-restart actions to be performed.
            foreach (var server in servers)
            {
                if (server.UpdateOnRestart && !server.IsServerRunning)
                {
                    // You need to define the "IsServerOnline" method if not already present.
                    Debug.WriteLine($"Server {server.ProfileName} is scheduled to be updated on restart.");
                    Task.Run(() => UpdateServerIfNecessary(server));
                }
            }

            // Log the countdown to the next action
            if (nextActionTime.HasValue)
            {
                Debug.WriteLine($"Next action in {nextActionTime.Value.TotalMinutes} minutes.");
            }
            else
            {
                Debug.WriteLine("No upcoming actions today.");
            }
        }
        private async Task UpdateServerIfNecessary(Server server)
        {
            if (!server.IsServerRunning)
            {
                // Perform update logic here
                await UpdateServer(server);
            }
            else
            {
                Debug.WriteLine("Server is currently running. Consider scheduling the update for later.");
            }
        }


        private async Task ExecuteSchedule(Schedule schedule)
        {
            Debug.WriteLine($"Attempting to execute schedule: {schedule.Nickname}");

            // Additional debug information to log the server names being compared
            Debug.WriteLine($"Looking for server named '{schedule.Server.Trim()}' in the list of servers.");

            var server = servers.FirstOrDefault(s => s.ProfileName.Trim() == schedule.Server.Trim());

            if (server != null)
            {
                Debug.WriteLine($"Found server for schedule '{schedule.Nickname}': {server.ProfileName}");
                switch (schedule.Action)
                {
                    case "Restart":
                        await RestartServer(server);
                        break;
                    case "Shutdown":
                        await ShutdownServer(server);
                        break;
                    case "Custom RCON Command":
                        await ExecuteCustomRCONCommand(server, schedule.RconCommand);
                        break;
                        // Add other actions as needed
                }
            }
            else
            {
                Debug.WriteLine($"No matching server found for schedule '{schedule.Nickname}'. Available servers are:");
                foreach (var srv in servers)
                {
                    Debug.WriteLine($"Server ProfileName: '{srv.ProfileName.Trim()}'");
                }
            }
        }

        private async Task ExecuteCustomRCONCommand(Server server, string command)
        {
            try
            {
                Debug.WriteLine($"Sending custom RCON command '{command}' to server {server.ProfileName}");
                var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                await rcon.ConnectAsync();
                var response = await rcon.SendCommandAsync(command);
                Debug.WriteLine($"Custom RCON command sent. Response: {response ?? "No response"}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending custom RCON command: {ex.Message}");
            }
        }





        private FileSystemWatcher schedulesWatcher;

        private void InitializeSchedulesWatcher()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string schedulesFolderPath = Path.Combine(appDataFolder, "Ark Ascended Manager");

            // Ensure the directory exists
            Directory.CreateDirectory(schedulesFolderPath);

            schedulesWatcher = new FileSystemWatcher(schedulesFolderPath, "schedules.json")
            {
                NotifyFilter = NotifyFilters.LastWrite
            };

            schedulesWatcher.Changed += OnSchedulesFileChanged;
            schedulesWatcher.EnableRaisingEvents = true;
        }


        private void OnSchedulesFileChanged(object source, FileSystemEventArgs e)
        {
            // Debounce or delay might be needed to handle multiple events
            LoadSchedules();
        }
        private async Task RestartServer(Server server)
        {
            try
            {
                // Notify players of the impending shutdown via RCON
                var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                await rcon.ConnectAsync();

                // Send countdown messages
                for (int i = 1; i > 0; i--)
                {
                    await rcon.SendCommandAsync($"ServerChat Server will restart in {i} minute(s)...");
                    Debug.WriteLine($"Server restart notification sent: {i} minute(s) remaining.");
                    await Task.Delay(60000); // Wait for 1 minute between each notification
                }

                // Perform the shutdown
                await rcon.SendCommandAsync("doexit");
                Debug.WriteLine("RCON shutdown command sent successfully.");

                // Wait for the server to shut down fully
                await Task.Delay(30000); // 30 seconds delay to ensure the server has time to shut down

                // Update the server before restarting
                await UpdateServer(server);

                // Start the server again
                string batFilePath = Path.Combine(server.ServerPath, "LaunchServer.bat");
                Process.Start(batFilePath);
                Debug.WriteLine($"Server restart process started for: {batFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restarting server: {ex.Message}");
            }
        }


        private async Task ShutdownServer(Server server)
        {
            try
            {
                RemoveServerFromMonitoring(server.ServerPath);
                Debug.WriteLine($"Attempting to shutdown server: {server.ProfileName}");
                Debug.WriteLine($"Connecting to RCON at IP: 127.0.0.1, Port: {server.RCONPort}");

                var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                await rcon.ConnectAsync();

                // Send countdown messages
                for (int i = 10; i > 0; i--)
                {
                    Debug.WriteLine($"Sending shutdown countdown message: {i} minute(s) remaining.");
                    await rcon.SendCommandAsync($"ServerChat Server will shut down in {i} minute(s)...");
                    await Task.Delay(60000); // Wait for 1 minute between each notification
                }

                // Shutdown command
                Debug.WriteLine("Sending RCON shutdown command.");
                await rcon.SendCommandAsync("doexit");
                Debug.WriteLine("RCON shutdown command sent successfully.");

                ServerConfig serverConfig = ConvertToServerConfig(server);
                UpdateServer(server);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RCON shutdown: {ex.Message}");
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

        private ServerConfig ConvertToServerConfig(Server server)
        {
            return new ServerConfig
            {
                // Map properties from Server to ServerConfig
                ProfileName = server.ProfileName,
                ServerPath = server.ServerPath,
                // ... other properties ...
            };
        }

        private List<Server> ReadAllServers()
        {
            Debug.WriteLine("ReadAllServers: Method called.");

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");
            Debug.WriteLine($"ReadAllServers: File path - {filePath}");

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Debug.WriteLine("ReadAllServers: JSON content read successfully.");
                    List<Server> servers = JsonConvert.DeserializeObject<List<Server>>(json);
                    return servers ?? new List<Server>();
                }
                else
                {
                    Debug.WriteLine("ReadAllServers: JSON file does not exist.");
                    return new List<Server>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReadAllServers: Exception occurred - {ex.Message}");
                return new List<Server>();
            }
        }


        private void WriteAllServers(List<Server> servers)
        {
            Debug.WriteLine("WriteAllServers: Method called.");

            // Optional: Debug print for verification
            var serverForDebug = servers.FirstOrDefault(s => s.ProfileName == "YourServerProfileName"); // Replace with the actual profile name
            if (serverForDebug != null)
            {
                Debug.WriteLine($"WriteAllServers: Details for server '{serverForDebug.ProfileName}' will be written.");
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");
            Debug.WriteLine($"WriteAllServers: File path - {filePath}");

            try
            {
                string json = JsonConvert.SerializeObject(servers, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Debug.WriteLine("WriteAllServers: JSON file written successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WriteAllServers: Exception occurred - {ex.Message}");
            }
        }

        private async Task UpdateServer(Server selectedServer)
        {
            if (selectedServer == null)
            {
                System.Windows.MessageBox.Show("Server configuration is not provided.");
                return;
            }

            // Using 'selectedServer' to avoid shadowing
            var serverToUpdate = servers.FirstOrDefault(s => s.ProfileName == selectedServer.ProfileName);

            // Check if the server is running before updating
            if (serverToUpdate != null && serverToUpdate.IsServerRunning)
            {
                System.Windows.MessageBox.Show("The server is currently running. Please stop the server before updating.");
                return;
            }

            // Call the update method with the current server configuration
            if (serverToUpdate != null)
            {
                UpdateServerBasedOnJson(serverToUpdate);
            }
            else
            {
                System.Windows.MessageBox.Show("Server not found.");
            }
        }



        public void UpdateServerBasedOnJson(Server server)
        {
            if (server != null && !string.IsNullOrEmpty(server.AppId))
            {
                string scriptPath = Path.Combine(Path.GetTempPath(), "steamcmd_update_script.txt");
                File.WriteAllLines(scriptPath, new string[]
                {
            $"force_install_dir \"{server.ServerPath}\"",
            "login anonymous",
            $"app_update {server.AppId} validate",
            "quit"
                });

                RunSteamCMD(scriptPath);

                // After running SteamCMD, update the change number
                UpdateChangeNumberFromJson(server);
            }
            else
            {
                Ark_Ascended_Manager.Services.Logger.Log("Could not update the server, App ID not found or server is null");
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
        private string FindSteamCmdPath()
        {
            // Define the JSON file path in the app data directory
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appDataPath, "SteamCmdPath.json");

            // Try to read the path from the JSON file
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string json = File.ReadAllText(jsonFilePath);
                    dynamic pathData = JsonConvert.DeserializeObject<dynamic>(json);
                    string savedPath = pathData?.SteamCmdPath;
                    if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                    {
                        return savedPath;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur during reading and deserializing the JSON file
                    // For example, log the exception and proceed to prompt the user
                }
            }

            // Prompt the user to locate steamcmd.exe if the path is not found or not valid
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Save the selected path to the JSON file for future use
                SaveSteamCmdPath(openFileDialog.FileName, jsonFilePath);
                return openFileDialog.FileName;
            }

            // Return null if the path could not be found or the user cancelled the dialog
            return null;
        }

        private void SaveSteamCmdPath(string path, string jsonFilePath)
        {
            var pathData = new { SteamCmdPath = path };
            string json = JsonConvert.SerializeObject(pathData, Formatting.Indented);
            File.WriteAllText(jsonFilePath, json);
        }
        private void UpdateChangeNumberFromJson(Server server)
        {
            Debug.WriteLine("UpdateChangeNumberFromJson: Method called.");

            if (server == null || string.IsNullOrEmpty(server.AppId))
            {
                Debug.WriteLine("UpdateChangeNumberFromJson: Server is null or AppId is empty.");
                return;
            }

            string jsonFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ark Ascended Manager",
                server.AppId,
                $"sanitizedsteamdata_{server.AppId}.json");

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
                    server.ChangeNumber = json.ChangeNumber; // Update only the ChangeNumber

                    // After updating the ChangeNumber, save the updated server info back to the file
                    SaveUpdatedServer(server);
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

        private void SaveUpdatedServer(Server updatedServer)
        {
            Debug.WriteLine("SaveUpdatedServer: Method called.");

            var allServers = ReadAllServers();
            // Find the index of the server to update
            var index = allServers.FindIndex(s => s.AppId == updatedServer.AppId && s.ProfileName.Equals(updatedServer.ProfileName, StringComparison.OrdinalIgnoreCase));

            if (index != -1) // Check if the server is found
            {
                Debug.WriteLine($"SaveUpdatedServer: Current ChangeNumber for server '{allServers[index].ProfileName}' is {allServers[index].ChangeNumber}");

                // Update the ChangeNumber of the server
                allServers[index].ChangeNumber = updatedServer.ChangeNumber;

                Debug.WriteLine($"SaveUpdatedServer: New ChangeNumber to be set for server '{allServers[index].ProfileName}' is {updatedServer.ChangeNumber}");

                // Write all servers back to storage
                WriteAllServers(allServers);
            }
            else
            {
                Debug.WriteLine("SaveUpdatedServer: Server not found.");
            }
        }
        }



    }


    internal class Schedule
    {
        public string Nickname { get; set; }
        public string Action { get; set; }
        public string RconCommand { get; set; }
        public string Time { get; set; }
        public List<string> Days { get; set; }
        public string Server { get; set; }
    }

    internal class Server
    {
    public string ChangeNumberStatus { get; set; }
    public bool IsMapNameOverridden { get; set; }
    public string ProfileName { get; set; }
    public string ServerIP { get; set; }
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
    public bool PreventSpawnAnimation { get; set; } // Use bool for checkboxes
    public bool IsServerRunning { get; set; }
    public bool UpdateOnRestart { get; set; }
}

