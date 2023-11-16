using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ark_Ascended_Manager.Models
{
    public class ServerProfile
    {
        public string ProfileName { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        public string AppId { get; set; }
        public string ServerName { get; set; }
        public int ListenPort { get; set; }
        public int RCONPort { get; set; }
        public List<string> Mods { get; set; } // If Mods is a list of strings in the JSON
        public int MaxPlayerCount { get; set; }
        public string AdminPassword { get; set; }
        public string ServerPassword { get; set; }
        public bool UseBattlEye { get; set; }
        public bool ForceRespawnDinos { get; set; }
        public bool PreventSpawnAnimation { get; set; }
        public static List<ServerProfile> LoadAllServerProfiles()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string fileName = "allServersSchedulingData.json"; // Ensure the file name is correct and includes the .json extension
            string fullPath = Path.Combine(folderPath, fileName);

            try
            {
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"The file {fullPath} does not exist.");

                string jsonContent = File.ReadAllText(fullPath);
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

    }
}
