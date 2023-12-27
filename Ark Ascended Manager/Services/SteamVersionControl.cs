using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Ark_Ascended_Manager.Services
{
    internal class SteamVersionControl
    {
        private Timer _timer;
        private List<int> _appIds = new List<int> { 2430930 }; // Add more App IDs to this list as needed

        public void StartUpdateTimer()
        {
            // Timer callback will call CheckForUpdates every 10 minutes
            _timer = new Timer(async _ =>
            {
                await CheckForUpdates(_appIds);
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private async Task CheckForUpdates(IEnumerable<int> appIds)
        {
            foreach (var appId in appIds)
            {
                string rawJson = await FetchAndSaveRawSteamData(appId);
                if (!string.IsNullOrEmpty(rawJson))
                {
                    var match = Regex.Match(rawJson, "\"_change_number\":\\s*(\\d+)");
                    if (match.Success)
                    {
                        int changeNumber = int.Parse(match.Groups[1].Value);
                        UpdateSanitizedData(appId.ToString(), changeNumber);
                    }
                }
            }
            await UpdateServersChangeNumberStatus();
        }

        public void StopUpdateTimer()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
        }

        private async Task<string> FetchAndSaveRawSteamData(int appId)
        {
            string content = string.Empty;
            try
            {
                using var client = new HttpClient();
                string url = $"https://api.steamcmd.net/v1/info/{appId}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    content = await response.Content.ReadAsStringAsync();
                    SaveContentToFile(content, $"unsanitizedsteamdata_{appId}.json", appId);
                }
                else
                {
                    Logger.Log($"Failed to fetch data for App {appId}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in FetchAndSaveRawSteamData for App {appId}: {ex.Message}");
            }
            return content;
        }

        private void SaveContentToFile(string content, string fileName, int appId)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", appId.ToString());
            Directory.CreateDirectory(folderPath); // Ensure the directory exists

            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                File.WriteAllText(filePath, content);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving file {fileName}: {ex.Message}");
            }
        }

        private void UpdateSanitizedData(string appId, int newChangeNumber)
        {
            string baseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            string folderPath = Path.Combine(baseFolderPath, appId);
            string filePath = Path.Combine(folderPath, $"sanitizedsteamdata_{appId}.json");

            Directory.CreateDirectory(folderPath); // Ensure the directory exists

            SanitizedSteamData currentData = new SanitizedSteamData();

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                currentData = JsonSerializer.Deserialize<SanitizedSteamData>(json) ?? new SanitizedSteamData();
            }

            currentData.AppId = appId;

            if (newChangeNumber > currentData.ChangeNumber)
            {
                currentData.ChangeNumber = newChangeNumber;
                currentData.LastUpdated = DateTime.UtcNow;

                string json = JsonSerializer.Serialize(currentData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                Logger.Log($"Updated Change Number for App {appId}: {newChangeNumber}");
            }
            else
            {
                Logger.Log($"No update required for App {appId}. Stored Change Number: {currentData.ChangeNumber}");
            }
        }
        private async Task UpdateServersChangeNumberStatus()
        {
            // Load server configurations from the servers.json file
            List<ServerConfig> serverConfigs = LoadServerConfigs();

            // Fetch the latest change number for each App ID
            foreach (int appId in _appIds)
            {
                int latestChangeNumber = await FetchLatestChangeNumber(appId);

                // Compare and update each server's change number status
                foreach (var serverConfig in serverConfigs)
                {
                    if (serverConfig.AppId == appId.ToString() && serverConfig.ChangeNumber != latestChangeNumber)
                    {
                        serverConfig.ChangeNumberStatus = "Server is Not Up to Date";
                        // Here you would also trigger any necessary notifications or UI updates
                    }
                }
            }

            // Save any updates back to the servers.json file
            SaveServerConfigs(serverConfigs);
        }
        private List<ServerConfig> LoadServerConfigs()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            // Check if the file exists before attempting to read
            if (!File.Exists(filePath))
            {
                // Log the absence of the file or handle accordingly
                Logger.Log($"The file {filePath} does not exist.");
                return new List<ServerConfig>(); // Return an empty list if the file doesn't exist
            }

            try
            {
                // Read the file content
                string jsonContent = File.ReadAllText(filePath);
                // Deserialize the JSON content to a List<ServerConfig>
                return JsonSerializer.Deserialize<List<ServerConfig>>(jsonContent) ?? new List<ServerConfig>();
            }
            catch (JsonException jsonEx)
            {
                // Log the exception or handle the deserialization error accordingly
                Logger.Log($"Error deserializing the file {filePath}: {jsonEx.Message}");
                return new List<ServerConfig>(); // Return an empty list in case of deserialization error
            }
            catch (Exception ex)
            {
                // Handle other exceptions that might occur
                Logger.Log($"An error occurred while reading the file {filePath}: {ex.Message}");
                return new List<ServerConfig>(); // Return an empty list in case of other errors
            }
        }
        private void SaveServerConfigs(List<ServerConfig> serverConfigs)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            try
            {
                // Serialize the list to JSON
                string jsonContent = JsonSerializer.Serialize(serverConfigs, new JsonSerializerOptions { WriteIndented = true });
                // Write the JSON to the file, creating it if it does not exist or overwriting it if it does
                File.WriteAllText(filePath, jsonContent);
            }
            catch (JsonException jsonEx)
            {
                // Log the exception or handle the serialization error accordingly
                Logger.Log($"Error serializing server configurations: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                // Handle other exceptions that might occur
                Logger.Log($"An error occurred while writing the server configurations to {filePath}: {ex.Message}");
            }
        }
        private async Task<int> FetchLatestChangeNumber(int appId)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sanitizedDataFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", appId.ToString(), $"sanitizedsteamdata_{appId}.json");

            try
            {
                // Asynchronously read the content of the file
                string jsonData = await File.ReadAllTextAsync(sanitizedDataFilePath);

                // Parse the JSON and extract the ChangeNumber
                using var jsonDocument = JsonDocument.Parse(jsonData);
                if (jsonDocument.RootElement.TryGetProperty("ChangeNumber", out JsonElement changeNumberElement) &&
                    changeNumberElement.TryGetInt32(out int changeNumber))
                {
                    return changeNumber;
                }
                else
                {
                    // Handle the case where ChangeNumber is not found or is not an integer
                    Logger.Log($"ChangeNumber not found or not an integer in {sanitizedDataFilePath}");
                }
            }
            catch (FileNotFoundException)
            {
                Logger.Log($"File not found: {sanitizedDataFilePath}");
            }
            catch (JsonException ex)
            {
                Logger.Log($"JSON parsing error in file {sanitizedDataFilePath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading the sanitized steam data file for App {appId}: {ex.Message}");
            }

            // If we reach this point, it means we couldn't fetch or parse the ChangeNumber
            return -1; // Return an invalid value or throw an exception
        }


        public class SanitizedSteamData
        {
            public string AppId { get; set; }
            public int ChangeNumber { get; set; }
            public DateTime LastUpdated { get; set; }
            public string ChangeNumberStatus { get; set; }
        }

        // Ensure to implement or define the Logger class according to your application's logging mechanism.
    }
}
