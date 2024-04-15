using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Services
{
    internal class Logger
    {
        private static readonly string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "debug.log");
        private static string _webhookUrl;

        public static void Initialize()
        {
            LoadSettings();
        }

        private static void LoadSettings()
        {
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "botsettings.json");
            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                try
                {
                    var settings = JsonConvert.DeserializeObject<BotSettings>(json);
                    _webhookUrl = settings.LoggerWebhookUrl;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("Failed to deserialize settings: " + ex.Message);
                }
            }
        }

        public static void Log(string message)
        {
            string logEntry = $"{DateTime.Now}: {message}\n";
            try
            {
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error logging message: {ex.Message}");
            }
        }

        public static async Task LogToDiscord(string title, string description, string footerText, Discord.Color color)
        {
            if (!string.IsNullOrWhiteSpace(_webhookUrl))
            {
                var embed = new
                {
                    embeds = new[]
                    {
                new
                {
                    title = title,
                    description = description,
                    color = color.RawValue,
                    timestamp = DateTime.UtcNow,
                    footer = new { text = footerText }
                }
            }
                };

                var jsonPayload = JsonConvert.SerializeObject(embed);
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    try
                    {
                        var response = await httpClient.PostAsync(_webhookUrl, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Failed to send log to Discord webhook. Status: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending log to Discord: {ex.Message}");
                    }
                }
            }
        }



        private static async Task SendDiscordEmbed(string title, string description, string footerText, int color)
        {
            var embed = new
            {
                embeds = new[]
                {
            new
            {
                title = title,
                description = description,
                color = color,
                timestamp = DateTime.UtcNow,
                footer = new { text = footerText }
            }
        }
            };

            var jsonPayload = JsonConvert.SerializeObject(embed);
            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                try
                {
                    var response = await httpClient.PostAsync(_webhookUrl, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to send log to Discord webhook. Status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending log to Discord: {ex.Message}");
                }
            }
        }

        public static void LogInfoToDiscord(string message)
        {
            // Assuming 'message' is the description, provide a title and other required parameters
            string title = "Information";
            string footerText = "Log Footer";
            Discord.Color color = new Discord.Color(52, 152, 219); // A nice blue color in hexadecimal (you can change it to any color you like)

            // Now call LogToDiscord with all required parameters
            LogToDiscord(title, message, footerText, color);
        }



        // Define BotSettings class if not already defined elsewhere
        private class BotSettings
        {
            public string LoggerWebhookUrl { get; set; }
        }
    }
}
