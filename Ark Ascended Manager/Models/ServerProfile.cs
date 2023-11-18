using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Ark_Ascended_Manager.Models;
namespace Ark_Ascended_Manager.Models
{
    public class ServerProfile
    {
        // Properties to represent the server profile details
        public string ProfileName { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        public string AppId { get; set; }
        public string ServerName { get; set; }
        public int ListenPort { get; set; }
        public int RCONPort { get; set; }
        public List<string> Mods { get; set; }
        public int MaxPlayerCount { get; set; }
        public string AdminPassword { get; set; }
        public string ServerPassword { get; set; }
        public bool UseBattlEye { get; set; }
        public bool ForceRespawnDinos { get; set; }
        public bool PreventSpawnAnimation { get; set; }

        // Property to hold scheduling data for this server profile
        public List<ScheduleEntry> SchedulingData { get; set; }

        // Method to load server profiles from JSON
        public static List<ServerProfile> LoadAllServerProfiles(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"The file {filePath} does not exist.");

                string jsonContent = File.ReadAllText(filePath);
                List<ServerProfile> serverProfiles = JsonConvert.DeserializeObject<List<ServerProfile>>(jsonContent);

                return serverProfiles ?? new List<ServerProfile>();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred while loading server profiles: {ex.Message}");
                return new List<ServerProfile>(); // Return an empty list or handle accordingly
            }
        }

        // Method to load scheduling data from JSON
        public static Dictionary<string, List<ScheduleEntry>> LoadAllSchedulingData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"The file {filePath} does not exist.");

                string jsonContent = File.ReadAllText(filePath);
                var schedulingData = JsonConvert.DeserializeObject<Dictionary<string, List<ScheduleEntry>>>(jsonContent);

                return schedulingData ?? new Dictionary<string, List<ScheduleEntry>>();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred while loading scheduling data: {ex.Message}");
                return new Dictionary<string, List<ScheduleEntry>>(); // Return an empty dictionary or handle accordingly
            }
        }
    }

    // ScheduleEntry class to represent each schedule item
    public class ScheduleEntry
    {
        public List<string> Days { get; set; }
        public string Time { get; set; }
        // Add other relevant properties as needed
    }
}
