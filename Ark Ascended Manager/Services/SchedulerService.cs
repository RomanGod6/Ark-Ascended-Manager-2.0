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
        private const string JsonFilePath = @"C:\Users\Dillon Griffey\AppData\Roaming\Ark Ascended Manager\allServersSchedulingData.json";
        private Dictionary<string, List<ScheduleItem>> _restartShutdownSchedule;
        private Dictionary<string, List<ScheduleItem>> _saveWorldSchedule;
        private List<Timer> _timers = new List<Timer>();
        private List<ServerProfile> _serverProfiles;
        public SchedulerService()
        {
            LoadSchedules();
            InitializeSchedulers();
            LoadServerProfiles();
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

            foreach (var day in days)
            {
                if (Enum.TryParse(day, true, out DayOfWeek dayOfWeek))
                {
                    var daysUntilNextOccurrence = ((int)dayOfWeek - (int)now.DayOfWeek + 7) % 7;
                    if (daysUntilNextOccurrence == 0 && now.TimeOfDay > scheduledTime)
                    {
                        daysUntilNextOccurrence = 7;
                    }
                    var nextDay = now.AddDays(daysUntilNextOccurrence).Date.Add(scheduledTime);
                    if (nextDay < nextOccurrence)
                    {
                        nextOccurrence = nextDay;
                    }
                }
            }

            return nextOccurrence - now;
        }

        private async Task ExecuteScheduledAction(string serverName, ScheduleItem item, string actionType)
        {
            if (item.IsSelected)
            {
                string command = DetermineRconCommand(item, actionType);
                ServerProfile profile = GetServerProfile(serverName);
                await SendRconCommandAsync(profile, command);
            }
        }


        private string DetermineRconCommand(ScheduleItem item, string actionType)
        {
            // Determine the appropriate RCON command based on the schedule item and action type
            // Placeholder logic
            return "command"; // Example
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

            Debug.WriteLine($"Sending RCON command to server: {profile.Name}, Command: {command}");
            Debug.WriteLine($"Full Command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            try
            {
                await Task.Run(() =>
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    Debug.WriteLine($"RCON Output: {output}");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending RCON command: {ex.Message}");
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
