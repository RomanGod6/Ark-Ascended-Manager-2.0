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
                Console.WriteLine($"Error logging message: {ex.Message}");
            }
        }

        public static void LogToDiscord(string message)
        {
            if (!string.IsNullOrWhiteSpace(_webhookUrl))
            {
                Task.Run(() => SendDiscordEmbed(message)).ConfigureAwait(false);
            }
        }

        private static async Task SendDiscordEmbed(string message)
        {
            var embed = new
            {
                embeds = new[]
                {
            new
            {
                description = message,
                title = "Important Notification",
                color = 3447003, // Light blue color, change as needed
                timestamp = DateTime.UtcNow,
                footer = new { text = "Notification Logger" }
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
            LogToDiscord(message);
        }


        // Define BotSettings class if not already defined elsewhere
        private class BotSettings
        {
            public string LoggerWebhookUrl { get; set; }
        }
    }
}
