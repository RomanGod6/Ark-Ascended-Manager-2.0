using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ark_Ascended_Manager.Models;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Services
{
    internal class SchedulerService
    {
        private string JsonFilePath;
        private Dictionary<string, List<ScheduleItem>> _restartShutdownSchedule;
        private Dictionary<string, List<ScheduleItem>> _saveWorldSchedule;
        private List<Timer> _timers = new List<Timer>();
        private List<ServerProfile> _serverProfiles;
        private Timer _countdownTimer;
        public SchedulerService()
        {
            SetJsonFilePath();
            LoadSchedules();
            InitializeSchedulers();
            LoadServerProfiles();
            _countdownTimer = new Timer(LogNextScheduledAction, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }
        private void SetJsonFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            JsonFilePath = Path.Combine(appDataPath, "Ark Ascended Manager", "allServersSchedulingData.json");
        }
        private void LogNextScheduledAction(object state)
        {
            var nextAction = GetNextScheduledAction();
            if (nextAction != null)
            {
                TimeSpan timeRemaining = nextAction.NextRunTime - DateTime.Now;
                Debug.WriteLine($"Next scheduled action: {nextAction.Action} for server {nextAction.ServerName} in {timeRemaining}");
            }
            else
            {
                Debug.WriteLine("No upcoming actions scheduled.");
            }
        }

        private ScheduledTaskInfo GetNextScheduledAction()
        {
            // Assuming GetScheduledTasks() returns a list of all scheduled tasks
            var allTasks = GetScheduledTasks();
            var upcomingTasks = allTasks.OrderBy(t => t.NextRunTime).FirstOrDefault();

            return upcomingTasks;
        }

        private void LoadSchedules()
        {
            try
            {
                string jsonContent = File.ReadAllText(JsonFilePath);
                var schedules = JsonConvert.DeserializeObject<Schedules>(jsonContent);

                _restartShutdownSchedule = schedules.RestartShutdown;
                _saveWorldSchedule = schedules.SaveWorld;

                Debug.WriteLine("Schedules loaded successfully.");

                // Debug output to check the loaded schedules
                foreach (var schedule in _restartShutdownSchedule)
                {
                    Debug.WriteLine($"RestartShutdown - Server: {schedule.Key}");
                    foreach (var item in schedule.Value)
                    {
                        Debug.WriteLine($"  Time: {item.Time}, Days: {item.DaysAsString}");
                    }
                }

                foreach (var schedule in _saveWorldSchedule)
                {
                    Debug.WriteLine($"SaveWorld - Server: {schedule.Key}");
                    foreach (var item in schedule.Value)
                    {
                        Debug.WriteLine($"  Time: {item.Time}, Days: {item.DaysAsString}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading schedules: {ex.Message}");
            }
        }

        private void InitializeSchedulers()
        {
            // Initialize schedulers for RestartShutdown
            foreach (var schedule in _restartShutdownSchedule)
            {
                foreach (var item in schedule.Value)
                {
                    TimeSpan timeToAction = ConvertToTimeSpan(item.Time, item.Days);
                    var timer = new Timer(async _ =>
                    {
                        await ExecuteScheduledAction(schedule.Key, item, "Restart/Shutdown");
                    }, null, timeToAction, Timeout.InfiniteTimeSpan);

                    _timers.Add(timer);
                }
            }

            // Initialize schedulers for SaveWorld
            foreach (var schedule in _saveWorldSchedule)
            {
                foreach (var item in schedule.Value)
                {
                    TimeSpan timeToAction = ConvertToTimeSpan(item.Time, item.Days);
                    var timer = new Timer(async _ =>
                    {
                        await ExecuteScheduledAction(schedule.Key, item, "Save World");
                    }, null, timeToAction, Timeout.InfiniteTimeSpan);

                    _timers.Add(timer);
                }
            }
        }


        private TimeSpan ConvertToTimeSpan(string time, List<string> days)
        {
            if (!TimeSpan.TryParse(time, out TimeSpan scheduledTime))
            {
                throw new FormatException("Invalid time format");
            }

            var now = DateTime.Now;
            var nextOccurrence = DateTime.MaxValue;

            // Calculate the next occurrence of the scheduled action
            foreach (var day in days)
            {
                if (Enum.TryParse(day, true, out DayOfWeek dayOfWeek))
                {
                    var daysUntilNextOccurrence = ((int)dayOfWeek - (int)now.DayOfWeek + 7) % 7;

                    // If the scheduled time is today but has already passed, set the next occurrence to the next week
                    if (daysUntilNextOccurrence == 0 && now.TimeOfDay > scheduledTime)
                    {
                        daysUntilNextOccurrence = 7;
                    }

                    // Calculate the datetime for the next occurrence of the action
                    var nextDay = now.AddDays(daysUntilNextOccurrence).Date.Add(scheduledTime);

                    // If this occurrence is sooner than the previously calculated one, use this one instead
                    if (nextDay < nextOccurrence)
                    {
                        nextOccurrence = nextDay;
                    }
                }
            }

            // Calculate the TimeSpan until the next occurrence
            TimeSpan timeToAction = nextOccurrence - now;

            // Output the debug information with the countdown
            Debug.WriteLine($"Next action for time '{time}' on days [{string.Join(", ", days)}] is in {timeToAction.TotalHours} hours ({timeToAction}).");

            return timeToAction;
        }


        private async Task ExecuteScheduledAction(string serverName, ScheduleItem item, string actionType)
        {
            // Assuming if it's in the schedule, it should be executed
            ServerProfile profile = GetServerProfile(serverName);
            Debug.WriteLine("Intiating Execute Schedules");

            // Send server chat command for all actions
            await SendServerChatCommandAsync(profile, $"Action {actionType} will start shortly. Please be prepared.");

            if (actionType == "Shutdown" || actionType == "Restart/Shutdown")
            {
                // Send server chat command every minute for 9 minutes
                for (int i = 1; i <= 9; i++)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await SendServerChatCommandAsync(profile, $"{actionType} in {10 - i} minutes.");
                }

                // Perform save world before shutdown or restart
                await SendRconCommandAsync(profile, "saveworld");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            // Execute the main action
            string command = DetermineRconCommand(item, actionType);
            await SendRconCommandAsync(profile, command);

            // If it's a restart, start the batch file after shutdown
            if (actionType == "Restart/Shutdown")
            {
                StartBatchFileForRestart(serverName);
            }
        }




        private async Task SendServerChatCommandAsync(ServerProfile profile, string message)
        {
            // Implement the logic to send a server chat command
            string chatCommand = $"ServerChat {message}";
            await SendRconCommandAsync(profile, chatCommand);
        }

        private void StartBatchFileForRestart(string serverName)
        {
            // Retrieve the server profile
            ServerProfile profile = GetServerProfile(serverName);
            if (profile == null)
            {
                Debug.WriteLine($"Server profile for '{serverName}' not found. Cannot start batch file.");
                return;
            }

            // Construct the path to the batch file
            string batchFilePath = Path.Combine(profile.ServerPath, "LaunchServer.bat");

            // Check if the batch file exists
            if (!File.Exists(batchFilePath))
            {
                Debug.WriteLine($"Batch file '{batchFilePath}' not found.");
                return;
            }

            try
            {
                // Start the batch file
                Process.Start(batchFilePath);
                Debug.WriteLine($"Started batch file: {batchFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting batch file: {ex.Message}");
            }
        }




        private string DetermineRconCommand(ScheduleItem item, string actionType)
        {
            switch (actionType)
            {
                case "Save World":
                    return "saveworld";
                case "Shutdown":
                                    return "doexit";
                case "Restart":
                case "Restart/Shutdown":
                    return "doexit"; // For restart, we'll handle the batch file execution separately
                default:
                    throw new InvalidOperationException($"Action type '{actionType}' is not recognized.");
            }
        }


        private void LoadServerProfiles()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string serverProfilesPath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            try
            {
                string jsonContent = File.ReadAllText(serverProfilesPath);
                _serverProfiles = JsonConvert.DeserializeObject<List<ServerProfile>>(jsonContent) ?? new List<ServerProfile>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading server profiles: {ex.Message}");
                _serverProfiles = new List<ServerProfile>();
            }
        }


        private ServerProfile GetServerProfile(string serverName)
        {
            return _serverProfiles.FirstOrDefault(profile => profile.ServerName == serverName);
        }



        private async Task SendRconCommandAsync(ServerProfile profile, string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C echo {command} | mcrcon 127.0.0.1 --password {profile.AdminPassword} -p {profile.RCONPort}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                Debug.WriteLine($"Attempting to send RCON command to server: {profile.Name}, Command: {command}");
                Debug.WriteLine($"Full Command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Debug.WriteLine($"RCON command sent. Output: {output}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception sending RCON command: {ex}");
            }
        }


        // Method to get the scheduled tasks
        public List<ScheduledTaskInfo> GetScheduledTasks()
        {
            var tasks = new List<ScheduledTaskInfo>();

            // Process RestartShutdown Schedules
            foreach (var schedule in _restartShutdownSchedule)
            {
                foreach (var item in schedule.Value)
                {
                    TimeSpan timeToAction = ConvertToTimeSpan(item.Time, item.Days);
                    tasks.Add(new ScheduledTaskInfo
                    {
                        ServerName = schedule.Key,
                        Action = "Restart/Shutdown",
                        NextRunTime = DateTime.Now + timeToAction
                    });
                }
            }

            // Process SaveWorld Schedules
            foreach (var schedule in _saveWorldSchedule)
            {
                foreach (var item in schedule.Value)
                {
                    TimeSpan timeToAction = ConvertToTimeSpan(item.Time, item.Days);
                    tasks.Add(new ScheduledTaskInfo
                    {
                        ServerName = schedule.Key,
                        Action = "Save World",
                        NextRunTime = DateTime.Now + timeToAction
                    });
                }
            }

            return tasks;
        }

    }

    // ScheduledTaskInfo class
    public class ScheduledTaskInfo
    {
        public string ServerName { get; set; }
        public string Action { get; set; }
        public DateTime NextRunTime { get; set; }
    }

    public class Schedules
    {
        public Dictionary<string, List<ScheduleItem>> RestartShutdown { get; set; }
        public Dictionary<string, List<ScheduleItem>> SaveWorld { get; set; }
    }

    public class ScheduleItem
    {
        public List<string> Days { get; set; }
        public string Time { get; set; }
        public string DaysAsString { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ServerProfile
    {
        public string Name { get; set; }
        public string AdminPassword { get; set; }
        public int RCONPort { get; set; }
        public string ProfileName { get; set; }
        public string ServerPath { get; set; }
        public string ServerName { get; set; }
    }
}
