using Ark_Ascended_Manager.Views.Pages;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ark_Ascended_Manager.Services
{
    public class DiscordBotService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        public List<DiscordServerConfig> ServerConfigs { get; private set; } = new List<DiscordServerConfig>();
        // Added properties
        public ulong GuildId { get; private set; }
        public string WebhookUrl { get; private set; }


        // Updated constructor to accept additional parameters
        public DiscordBotService(IServiceProvider services, ulong guildId, string webhookUrl)
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = services;

            // Initialize properties
            GuildId = guildId;
            WebhookUrl = webhookUrl;

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            // Other event subscriptions as needed
        }

        public async Task InitializeAsync(BotSettings settings)
        {
            await _client.LoginAsync(TokenType.Bot, settings.Token);
            await _client.StartAsync();

            // Load server configurations
         

            // Register commands, etc.
        }
        public async Task StartAsync(BotSettings settings)
        {
            try
            {
                await _client.LoginAsync(TokenType.Bot, settings.Token);
                await _client.StartAsync();
                Debug.WriteLine("Bot is now connected!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during bot start: {ex.Message}");
            }
        }


        public async Task StopAsync()
        {
            Console.WriteLine($"{_client.CurrentUser.Username} went to sleep");
            Logger.LogInfoToDiscord($"{_client.CurrentUser.Username} went to sleep");
            await _client.LogoutAsync();
            await _client.StopAsync();
           

      
            
        }
        public async Task<List<DiscordServerConfig>> LoadServerConfigsAsync()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");
            List<DiscordServerConfig> servers = new List<DiscordServerConfig>();

            try
            {
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    servers = JsonConvert.DeserializeObject<List<DiscordServerConfig>>(json) ?? new List<DiscordServerConfig>();
                    Debug.WriteLine($"Loaded server configs: {servers.Count} entries found.");
                }
                else
                {
                    Debug.WriteLine("Server configuration file not found.");
                }
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"JSON deserialization failed: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred when loading server configs: {ex.Message}");
            }

            return servers;
        }

        private Task LogAsync(LogMessage logMessage)
        {
            Console.WriteLine(logMessage.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine($"Connected as {_client.CurrentUser.Username}");
            Logger.LogInfoToDiscord($"Bot connected and ready as {_client.CurrentUser.Username}");
            ServerConfigs = await LoadServerConfigsAsync();
            _client.SlashCommandExecuted += HandleSlashCommandExecutedAsync;
            await RegisterCommands();
            
        }

        private async Task RegisterCommands()
        {
            var guild = _client.GetGuild(GuildId); // Make sure GuildId is set correctly
            if (guild != null)
            {
                await guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("listservers")
                    .WithDescription("List all servers with their status and information")
                    .Build());
            }
            else
            {
                Logger.LogInfoToDiscord($"Guild ID was Null or undefined - please recheck the integrations tab for the correct Guild ID.");
            }
        }



        private async Task HandleSlashCommandExecutedAsync(SocketSlashCommand command)
        {
            // Check if the executed command is /listservers
            if (command.Data.Name == "listservers")
            {
                try
                {
                    await command.DeferAsync();
                    await SendServerStatusEmbeds(command);
                }
                catch (Exception ex)
                {
                    // Log the exception to your logging system or print out for debugging
                    Logger.LogInfoToDiscord($"An error occurred: {ex.Message}");
                }
            }
        }

        // Method to send server status embeds as a response to /listservers command
        public async Task SendServerStatusEmbeds(SocketSlashCommand command)
        {
            Logger.LogInfoToDiscord($"Starting to send server status embeds for {ServerConfigs.Count} servers.");

            foreach (var serverConfig in ServerConfigs)
            {
                try
                {
                    Logger.LogInfoToDiscord($"Preparing embed for server: {serverConfig.ServerName}");
                    var isOnline = serverConfig.ServerStatus == "Online";
                    var embed = new EmbedBuilder()
                        .WithTitle($"{serverConfig.ServerName}")
                        .WithColor(isOnline ? Color.Green : Color.Red)
                        .AddField("Status", isOnline ? "Online" : "Offline", true)
                        .AddField("Map", serverConfig.MapName, true)
                        /* .AddField("Current Players", $"{serverConfig.CurrentPlayerCount}", true) */
                        .AddField("Max Players", $"{serverConfig.MaxPlayerCount}", true)
                        .WithThumbnailUrl("https://media.discordapp.net/attachments/1168388628657995836/1182037772224180308/AAM_Icon.png?ex=66295a76&is=6616e576&hm=f4fa28aa9f7952a0398c483b767c35d1e374bd20544c372bcf875d76a48666f7&=&format=webp&quality=lossless&width=222&height=230")
                        .Build();

                    await command.FollowupAsync(embed: embed);
                    Logger.LogInfoToDiscord($"Embed sent for server: {serverConfig.ServerName}");
                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Failed to send embed for server {serverConfig.ServerName}: {ex.Message}");
                }
            }

            Logger.LogInfoToDiscord("Finished sending server status embeds.");
        }



    }
    public class DiscordServerConfig
    {
        public string ChangeNumberStatus { get; set; }
        public bool IsMapNameOverridden { get; set; }
        public string ProfileName { get; set; }
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

        // ... other relevant details
    }
    public class BotSettings
    {
        public string Token { get; set; }
        public string GuildId { get; set; }
        public string WebhookUrl { get; set; }
        public string LoggerWebhookUrl { get; set; }
        public string[] IgnoredPatterns { get; set; }
        public List<ulong> AuthorizedRoleIds { get; set; }// Add this line
    }
}
