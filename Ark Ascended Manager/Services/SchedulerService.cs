using CoreRCON;
using CoreRCON.Parsers.Standard;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Management;

namespace Ark_Ascended_Manager.Services
{
    internal class SchedulerService
    {
        private List<Schedule> schedules;
        private List<Server> servers;
        private System.Timers.Timer actionTimer;
        private System.Timers.Timer reloadTimer;
        private const string DatabaseFileName = "schedules.db";
        private bool isServerStarting = false;

        public SchedulerService()
        {
            InitializeDatabase();
            LoadSchedules();
            LoadServers();
            InitializeSchedulesWatcher();

            // Timer for checking actions
            actionTimer = new System.Timers.Timer(5000);
            actionTimer.Elapsed += Timer_Elapsed;
            actionTimer.AutoReset = true;
            actionTimer.Start();
            Debug.WriteLine("Scheduler action timer started.");

            // Timer for reloading schedules
            reloadTimer = new System.Timers.Timer(10000); // Reload schedules every 10 seconds
            reloadTimer.Elapsed += ReloadTimer_Elapsed;
            reloadTimer.AutoReset = true;
            reloadTimer.Start();
            Debug.WriteLine("Scheduler reload timer started.");
        }

        private void InitializeDatabase()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            string dbPath = Path.Combine(folderPath, DatabaseFileName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Schedules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nickname TEXT,
                    Action TEXT,
                    RconCommand TEXT,
                    Times TEXT,
                    Days TEXT,
                    ReoccurrenceIntervalType TEXT,
                    ReoccurrenceInterval INTEGER,
                    Server TEXT
                )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void LoadSchedules()
        {
            schedules = new List<Schedule>();
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            string dbPath = Path.Combine(folderPath, DatabaseFileName);
            Debug.WriteLine($"Database path: {dbPath}");

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string selectQuery = "SELECT * FROM Schedules";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var schedule = new Schedule
                        {
                            Nickname = reader["Nickname"].ToString(),
                            Action = reader["Action"].ToString(),
                            RconCommand = reader["RconCommand"].ToString(),
                            Times = JsonConvert.DeserializeObject<List<TimeSpan>>(reader["Times"].ToString()),
                            Days = JsonConvert.DeserializeObject<List<string>>(reader["Days"].ToString()),
                            ReoccurrenceIntervalType = reader["ReoccurrenceIntervalType"].ToString(),
                            ReoccurrenceInterval = Convert.ToInt32(reader["ReoccurrenceInterval"]),
                            Server = reader["Server"].ToString()
                        };
                        Debug.WriteLine($"Loaded schedule: {schedule.Nickname} with times: {string.Join(", ", schedule.Times)}");
                        schedules.Add(schedule);
                    }
                }
            }
            Debug.WriteLine($"Total schedules loaded: {schedules.Count}");
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
                Debug.WriteLine($"servers.json content: {serversJson}");

                servers = JsonConvert.DeserializeObject<List<Server>>(serversJson);

                if (servers == null || !servers.Any())
                {
                    Debug.WriteLine("No servers loaded or failed to deserialize servers.json.");
                    return;
                }

                Debug.WriteLine($"Loaded {servers.Count} servers.");

                foreach (var server in servers)
                {
                    Debug.WriteLine($"Loaded server with ProfileName: '{server?.ProfileName ?? "null"}', ServerPath: '{server?.ServerPath ?? "null"}'");

                    if (server == null)
                    {
                        Debug.WriteLine("Deserialization produced a null Server object.");
                    }
                    else if (server.ServerPath == null || server.ProfileName == null)
                    {
                        Debug.WriteLine("ServerPath or ProfileName is null. Full server object:");
                        Debug.WriteLine(JsonConvert.SerializeObject(server, Formatting.Indented));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading servers: {ex.Message}");
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            Debug.WriteLine($"Timer elapsed at {now}");
            TimeSpan? nextActionTime = null;

            foreach (var schedule in schedules)
            {
                if (schedule.Days.Contains(now.DayOfWeek.ToString()))
                {
                    Debug.WriteLine($"Schedule {schedule.Nickname} is scheduled for today.");
                    foreach (var scheduleTime in schedule.Times.ToList())
                    {
                        TimeSpan timeUntilAction = scheduleTime - now.TimeOfDay;
                        Debug.WriteLine($"Checking schedule time: {scheduleTime}, current time: {now.TimeOfDay}, time until action: {timeUntilAction.TotalMinutes} minutes");

                        if (Math.Abs(timeUntilAction.TotalSeconds) < 5)  // Adjust the time precision if necessary
                        {
                            Debug.WriteLine($"Executing scheduled task: {schedule.Nickname} at {now}");
                            Task.Run(() => ExecuteSchedule(schedule));

                            if (schedule.ReoccurrenceInterval > 0)
                            {
                                TimeSpan newTime;
                                switch (schedule.ReoccurrenceIntervalType)
                                {
                                    case "Minutes":
                                        newTime = scheduleTime.Add(new TimeSpan(0, schedule.ReoccurrenceInterval, 0));
                                        break;
                                    case "Hours":
                                        newTime = scheduleTime.Add(new TimeSpan(schedule.ReoccurrenceInterval, 0, 0));
                                        break;
                                    default:
                                        throw new InvalidOperationException("Invalid recurrence interval type.");
                                }
                                if (newTime.TotalDays < 1)
                                {
                                    schedule.Times.Add(newTime);
                                    Debug.WriteLine($"Scheduled next occurrence of task '{schedule.Nickname}' at {newTime}");
                                }
                            }
                            schedule.Times.Remove(scheduleTime);
                        }

                        if (timeUntilAction >= TimeSpan.Zero && (!nextActionTime.HasValue || timeUntilAction < nextActionTime))
                        {
                            nextActionTime = timeUntilAction;
                        }
                    }
                }
            }

            if (nextActionTime.HasValue)
            {
                Debug.WriteLine($"Next action in {nextActionTime.Value.TotalMinutes} minutes.");
            }
            else
            {
                Debug.WriteLine("No upcoming actions today.");
            }
        }

        private void ReloadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Reloading schedules from the database...");
            LoadSchedules();
        }

        private async Task ExecuteSchedule(Schedule schedule)
        {
            Debug.WriteLine($"[INFO] Attempting to execute schedule: {schedule.Nickname} at {DateTime.Now}");

            if (servers == null || !servers.Any())
            {
                Debug.WriteLine("[ERROR] No servers loaded. Ensure servers.json is properly loaded and contains server entries.");
                return;
            }

            string trimmedScheduleServer = schedule.Server?.Trim() ?? string.Empty;
            Debug.WriteLine($"[INFO] Looking for server with profile name '{trimmedScheduleServer}' in the list of servers.");

            var server = servers.FirstOrDefault(s => s.ProfileName?.Trim().Equals(trimmedScheduleServer, StringComparison.OrdinalIgnoreCase) == true);

            if (server != null)
            {
                Debug.WriteLine($"[INFO] Found server for schedule '{schedule.Nickname}': {server.ProfileName}");

                server.IsRunning = IsServerRunning(server); // Checking server status here
                Debug.WriteLine($"[INFO] Server '{server.ProfileName}' running status: {server.IsRunning}");

                if (!isServerStarting && !server.IsRunning)
                {
                    isServerStarting = true;
                    Debug.WriteLine("[INFO] Server is offline. Starting the server...");

                    string batFilePath = Path.Combine(server.ServerPath, "LaunchServer.bat");
                    Process.Start(batFilePath);
                    Debug.WriteLine($"[INFO] Server start process initiated for: {batFilePath}");

                    // Check the server status every 5 seconds for a total duration of 5 minutes
                    int maxRetries = 60; // 5 minutes (60 retries at 5 seconds each)
                    bool serverStarted = false;

                    for (int i = 0; i < maxRetries; i++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        server.IsRunning = IsServerRunning(server);

                        if (server.IsRunning)
                        {
                            serverStarted = true;
                            break;
                        }

                        Debug.WriteLine($"[INFO] Checking server status... Attempt {i + 1}/{maxRetries}, running: {server.IsRunning}");
                    }

                    isServerStarting = false;

                    if (serverStarted)
                    {
                        Debug.WriteLine($"[INFO] Server '{server.ProfileName}' is now running.");
                    }
                    else
                    {
                        Debug.WriteLine($"[ERROR] Server '{server.ProfileName}' failed to start after multiple attempts.");
                        return; // Exit if server failed to start
                    }
                }
                else if (isServerStarting)
                {
                    Debug.WriteLine($"Server '{server.ProfileName}' is already in the process of starting.");
                    return;
                }
                else if (server.IsRunning)
                {
                    Debug.WriteLine($"Server '{server.ProfileName}' is already running.");
                }

                var arkRCONService = new ArkRCONService(server.ServerIP, (ushort)server.RCONPort, server.AdminPassword, server.ServerPath);

                switch (schedule.Action)
                {
                    case "Restart":
                        await RestartServer(server, arkRCONService);
                        break;
                    case "Shutdown":
                        await ShutdownServer(server, arkRCONService);
                        break;
                    case "Custom RCON Command":
                        await ExecuteCustomRCONCommand(arkRCONService, schedule.RconCommand);
                        break;
                }

                // Handle recurring schedules
                if (schedule.ReoccurrenceInterval > 0)
                {
                    foreach (var time in schedule.Times.ToList())
                    {
                        TimeSpan newTime;

                        switch (schedule.ReoccurrenceIntervalType)
                        {
                            case "Minutes":
                                newTime = time.Add(new TimeSpan(0, schedule.ReoccurrenceInterval, 0));
                                break;
                            case "Hours":
                                newTime = time.Add(new TimeSpan(schedule.ReoccurrenceInterval, 0, 0));
                                break;
                            default:
                                throw new InvalidOperationException("Invalid recurrence interval type.");
                        }

                        if (newTime.TotalDays < 1)
                        {
                            schedule.Times.Add(newTime);
                            Debug.WriteLine($"Scheduled next occurrence of task '{schedule.Nickname}' at {newTime}");
                        }
                    }
                }

                SaveSchedulesToDatabase();
            }
            else
            {
                Debug.WriteLine($"No matching server found for schedule '{schedule.Nickname}'. Available servers are:");
                foreach (var srv in servers)
                {
                    Debug.WriteLine($"Server ProfileName: '{srv?.ProfileName ?? "null"}', ServerPath: '{srv?.ServerPath?.Trim() ?? "null"}'");
                }
            }
        }




        private bool IsServerRunning(Server server)
        {
            try
            {
                string serverExePath = Path.Combine(server.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe");
                string serverProcessName = Path.GetFileNameWithoutExtension(serverExePath);

                string query = $"SELECT ExecutablePath FROM Win32_Process WHERE Name = '{serverProcessName}.exe'";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        string executablePath = result["ExecutablePath"] as string;
                        if (string.Equals(executablePath, serverExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error: {ex.Message}");
                return false;
            }
        }


        private async Task ExecuteCustomRCONCommand(ArkRCONService arkRCONService, string command)
        {
            try
            {
                Debug.WriteLine($"Sending custom RCON command '{command}'");

                // Ensure RCON connection
                await arkRCONService.ConnectAsync();

                // Send the command and get the response
                var response = await arkRCONService.SendCommandAsync(command);

                // Log the response
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
            LoadSchedules();
        }

        private async Task RestartServer(Server server, ArkRCONService arkRCONService)
        {
            try
            {
                server.IsRunning = IsServerRunning(server);

                if (!server.IsRunning)
                {
                    Debug.WriteLine("Server is offline. Starting the server...");
                    string batFilePath = Path.Combine(server.ServerPath, "LaunchServer.bat");
                    Process.Start(batFilePath);
                    Debug.WriteLine($"Server start process initiated for: {batFilePath}");

                    await Task.Delay(60000);
                }

                await arkRCONService.ConnectAsync();

                for (int i = 1; i > 0; i--)
                {
                    await arkRCONService.SendServerChatAsync($"Server will restart in {i} minute(s)...");
                    Debug.WriteLine($"Server restart notification sent: {i} minute(s) remaining.");
                    await Task.Delay(60000);
                }

                await arkRCONService.SendCommandAsync("doexit");
                Debug.WriteLine("RCON shutdown command sent successfully.");

                await Task.Delay(30000);

                await UpdateServer(server);

                string restartBatFilePath = Path.Combine(server.ServerPath, "LaunchServer.bat");
                Process.Start(restartBatFilePath);
                Debug.WriteLine($"Server restart process started for: {restartBatFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restarting server: {ex.Message}");
            }
        }

        private async Task ShutdownServer(Server server, ArkRCONService arkRCONService)
        {
            try
            {
                RemoveServerFromMonitoring(server.ServerPath);
                Debug.WriteLine($"Attempting to shutdown server: {server.ProfileName}");

                await arkRCONService.ConnectAsync();

                for (int i = 10; i > 0; i--)
                {
                    Debug.WriteLine($"Sending shutdown countdown message: {i} minute(s) remaining.");
                    await arkRCONService.SendServerChatAsync($"Server will shut down in {i} minute(s)...");
                    await Task.Delay(60000);
                }

                Debug.WriteLine("Sending RCON shutdown command.");
                await arkRCONService.SendCommandAsync("doexit");
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
                ProfileName = server.ProfileName,
                ServerPath = server.ServerPath,
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

            var serverForDebug = servers.FirstOrDefault(s => s.ProfileName == "YourServerProfileName");
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

            var serverToUpdate = servers.FirstOrDefault(s => s.ProfileName == selectedServer.ProfileName);

            if (serverToUpdate != null && serverToUpdate.IsServerRunning)
            {
                System.Windows.MessageBox.Show("The server is currently running. Please stop the server before updating.");
                return;
            }

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
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appDataPath, "SteamCmdPath.json");

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
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSteamCmdPath(openFileDialog.FileName, jsonFilePath);
                return openFileDialog.FileName;
            }

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
                    server.ChangeNumber = json.ChangeNumber;

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
            var index = allServers.FindIndex(s => s.AppId == updatedServer.AppId && s.ProfileName.Equals(updatedServer.ProfileName, StringComparison.OrdinalIgnoreCase));

            if (index != -1)
            {
                Debug.WriteLine($"SaveUpdatedServer: Current ChangeNumber for server '{allServers[index].ProfileName}' is {allServers[index].ChangeNumber}");

                allServers[index].ChangeNumber = updatedServer.ChangeNumber;

                Debug.WriteLine($"SaveUpdatedServer: New ChangeNumber to be set for server '{allServers[index].ProfileName}' is {updatedServer.ChangeNumber}");

                WriteAllServers(allServers);
            }
            else
            {
                Debug.WriteLine("SaveUpdatedServer: Server not found.");
            }
        }

        private void SaveSchedulesToDatabase()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            string dbPath = Path.Combine(folderPath, DatabaseFileName);

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    string deleteQuery = "DELETE FROM Schedules";
                    using (var command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string insertQuery = @"
                    INSERT INTO Schedules (
                        Nickname, Action, RconCommand, Times, Days, ReoccurrenceIntervalType, ReoccurrenceInterval, Server
                    ) VALUES (
                        @Nickname, @Action, @RconCommand, @Times, @Days, @ReoccurrenceIntervalType, @ReoccurrenceInterval, @Server
                    )";
                    foreach (var schedule in schedules)
                    {
                        using (var command = new SQLiteCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Nickname", schedule.Nickname);
                            command.Parameters.AddWithValue("@Action", schedule.Action);
                            command.Parameters.AddWithValue("@RconCommand", schedule.RconCommand);
                            command.Parameters.AddWithValue("@Times", JsonConvert.SerializeObject(schedule.Times));
                            command.Parameters.AddWithValue("@Days", JsonConvert.SerializeObject(schedule.Days));
                            command.Parameters.AddWithValue("@ReoccurrenceIntervalType", schedule.ReoccurrenceIntervalType);
                            command.Parameters.AddWithValue("@ReoccurrenceInterval", schedule.ReoccurrenceInterval);
                            command.Parameters.AddWithValue("@Server", schedule.Server);
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }
    }

    internal class Schedule
    {
        public string Nickname { get; set; }
        public string Action { get; set; }
        public string RconCommand { get; set; }
        public List<TimeSpan> Times { get; set; } = new List<TimeSpan>();
        public List<string> Days { get; set; }
        public string Server { get; set; }
        public string ReoccurrenceIntervalType { get; set; }
        public int ReoccurrenceInterval { get; set; }
    }
}
