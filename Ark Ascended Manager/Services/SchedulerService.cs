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
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string serversFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");
            string serversJson = File.ReadAllText(serversFilePath);
            servers = JsonConvert.DeserializeObject<List<Server>>(serversJson);
            Debug.WriteLine($"Loaded {servers.Count} servers.");
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;

            // Initialize a variable to track the next scheduled action time
            TimeSpan? nextActionTime = null;

            foreach (var schedule in schedules)
            {
                if (schedule.Days.Contains(now.DayOfWeek.ToString()))
                {
                    var scheduleTime = TimeSpan.Parse(schedule.Time);
                    if (now.TimeOfDay <= scheduleTime)
                    {
                        var timeUntilAction = scheduleTime - now.TimeOfDay;
                        if (!nextActionTime.HasValue || timeUntilAction < nextActionTime)
                        {
                            nextActionTime = timeUntilAction;
                        }
                    }

                    // Check if the current time is within a minute of the scheduled time
                    if (Math.Abs((now.TimeOfDay - scheduleTime).TotalMinutes) < 1)
                    {
                        Debug.WriteLine($"Executing schedule: {schedule.Nickname}");
                        Task.Run(() => ExecuteSchedule(schedule));
                    }
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



        private async Task ExecuteSchedule(Schedule schedule)
        {
            Debug.WriteLine($"Attempting to execute schedule: {schedule.Nickname}");
            var server = servers.FirstOrDefault(s => s.ProfileName == schedule.Server);
            if (server != null)
            {
                Debug.WriteLine($"Found server for schedule {schedule.Nickname}: {server.ProfileName}");
                switch (schedule.Action)
                {
                    case "Restart":
                        await RestartServer(server);
                        break;
                    case "Shutdown":
                        await ShutdownServer(server);
                        break;
                    case "CustomRCON": // Handle custom RCON commands
                        await ExecuteCustomRCONCommand(server, schedule.RconCommand);
                        break;
                        // Add other actions as needed
                }
            }
            else
            {
                Debug.WriteLine($"No matching server found for schedule {schedule.Nickname}");
            }
        }
        private async Task ExecuteCustomRCONCommand(Server server, string command)
        {
            try
            {
                Debug.WriteLine($"Sending custom RCON command to server {server.ProfileName}: {command}");
                var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                await rcon.ConnectAsync();
                var response = await rcon.SendCommandAsync(command);
                Debug.WriteLine($"Custom RCON command sent successfully. Response: {response}");
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
                // First, notify players of the impending shutdown via RCON
                var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                await rcon.ConnectAsync();

                // Send countdown messages
                for (int i = 10; i > 0; i--)
                {
                    await rcon.SendCommandAsync($"ServerChat Server will restart in {i} minute(s)...");
                    Debug.WriteLine($"Server restart notification sent: {i} minute(s) remaining.");
                    await Task.Delay(60000); // Wait for 1 minute between each notification
                }

                // Perform the shutdown
                await rcon.SendCommandAsync("doexit");
                Debug.WriteLine("RCON shutdown command sent successfully.");

                // Wait for the server to shut down fully - this delay may need to be adjusted
                await Task.Delay(30000); // 30 seconds delay to ensure the server has time to shut down

                // Start the server again
                string batFilePath = Path.Combine(server.ServerPath, "StartServer.bat");
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
                Debug.WriteLine($"Attempting to shutdown server: {server.ProfileName}");
                var rcon = new RCON(IPAddress.Parse("127.0.0.1"), (ushort)server.RCONPort, server.AdminPassword);
                await rcon.ConnectAsync();

                // Send countdown messages
                for (int i = 10; i > 0; i--)
                {
                    await rcon.SendCommandAsync($"ServerChat Server will shut down in {i} minute(s)...");
                    Debug.WriteLine($"Shutdown notification sent: {i} minute(s) remaining.");
                    await Task.Delay(60000); // Wait for 1 minute between each notification
                }

                // Shutdown command
                await rcon.SendCommandAsync("doexit");
                Debug.WriteLine("RCON shutdown command sent successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RCON shutdown: {ex.Message}");
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
        public string ProfileName { get; set; }
        public string ServerPath { get; set; }
        public int RCONPort { get; set; }
        public string AdminPassword { get; set; }
    }
}
