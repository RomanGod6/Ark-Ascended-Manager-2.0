using Ark_Ascended_Manager.Views.Pages;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.ApplicationServices;
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
            _client.ButtonExecuted += HandleButtonExecutedAsync; 
            _client.ModalSubmitted += HandleModalSubmittedAsync;
            // Other event subscriptions as needed
        }

        private async Task HandleButtonExecutedAsync(SocketMessageComponent component)
        {
            var parts = component.Data.CustomId.Split(':');
            if (parts.Length > 1)
            {
                string serverIdentifier = parts[1];  // This gets the server identifier part of the custom ID

                switch (parts[0])
                {
                    case "start_id":
                        await StartServer(component, serverIdentifier);
                        break;
                    case "shutdown_id":
                        // Define and send the modal for shutdown
                        var modal = new ModalBuilder()
                            .WithTitle("Server Shutdown")
                            .WithCustomId("shutdown_modal:" + serverIdentifier)  // Pass server identifier to modal
                            .AddTextInput("Shutdown Time in Minutes", "time_input", placeholder: "Enter time in minutes", maxLength: 3)
                            .AddTextInput("Reason for Shutdown", "reason_input", placeholder: "Enter the reason")
                            .Build();
                        await component.RespondWithModalAsync(modal);
                        break;
                        // Add other cases as necessary
                }
            }
        }

        private async Task StartServer(SocketMessageComponent component, string serverName)
        {
            // Immediately acknowledge the interaction.
            await component.DeferAsync();

            Debug.WriteLine($"Attempting to start server: {serverName}");
            var discordServerConfig = ServerConfigs.FirstOrDefault(s => s.ServerName == serverName);

            if (discordServerConfig == null)
            {
                Debug.WriteLine("Server configuration not found for: " + serverName);
                // Followup since we used DeferAsync()
                await component.FollowupAsync($"Server configuration for {serverName} not found.");
                return;
            }

            var serverConfig = ConvertToServerConfig(discordServerConfig);
            Debug.WriteLine("Converted server configuration: " + JsonConvert.SerializeObject(serverConfig));

            if (serverConfig.ServerPath == null || serverConfig.ServerPath == "")
            {
                Debug.WriteLine("Server path is undefined or empty.");
                await component.FollowupAsync("Server path is undefined or empty.");
                return;
            }

            if (IsServerRunning(serverConfig))
            {
                Debug.WriteLine("Server already running: " + serverName);
                await component.FollowupAsync($"Server {serverName} is already running.");
                return;
            }

            string batchFilePath = Path.Combine(serverConfig.ServerPath, "LaunchServer.bat");
            Debug.WriteLine("Batch file path: " + batchFilePath);

            if (!File.Exists(batchFilePath))
            {
                Debug.WriteLine("Batch file does not exist: " + batchFilePath);
                await component.FollowupAsync("Launch batch file does not exist.");
                return;
            }

            // Run the server startup in a separate task to not block the thread
            _ = Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(batchFilePath)
                    {
                        WorkingDirectory = serverConfig.ServerPath,
                        UseShellExecute = true
                    };

                    Process process = Process.Start(startInfo);
                    if (process != null)
                    {
                        // Use the correct color format (Discord.Color)
                        Discord.Color greenColor = new Discord.Color(0, 255, 0);
                        string description = $"{serverName} server has been started.";
                        // Ensure Logger.LogToDiscord can be awaited
                        Logger.LogToDiscord("Server Started", description, "Notification", greenColor);
                        Debug.WriteLine($"Server {serverName} started.");
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to start the server process for {serverName}.");
                        Logger.LogToDiscord("Server Start Failed", $"Could not start the server process for {serverName}.", "Error", new Discord.Color(255, 0, 0));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception when trying to start server {serverName}: {ex}");
                    Logger.LogToDiscord("Server Start Failed", $"Failed to start server {serverName}: {ex.Message}", "Error", new Discord.Color(255, 0, 0));
                }
            });

            // Inform the user that the server is starting up
            await component.FollowupAsync($"Server {serverName} is starting up...");
        }






        private async Task HandleModalSubmittedAsync(SocketModal modal)
        {
            var parts = modal.Data.CustomId.Split(':');
            if (parts[0] == "shutdown_modal" && parts.Length > 1)
            {
                string serverIdentifier = parts[1];
                var discordServerConfig = ServerConfigs.FirstOrDefault(sc => sc.ServerName == serverIdentifier);
                var timeInput = modal.Data.Components.FirstOrDefault(x => x.CustomId == "time_input")?.Value;
                var reasonInput = modal.Data.Components.FirstOrDefault(x => x.CustomId == "reason_input")?.Value;

                // Provide an immediate response to acknowledge receipt of the shutdown request
                var ackResponse = $"Received shutdown request for server {serverIdentifier}. Processing...";
                await modal.RespondAsync(ackResponse);

                if (discordServerConfig != null && int.TryParse(timeInput, out int countdownMinutes) && !string.IsNullOrEmpty(reasonInput))
                {
                    SocketUser user = modal.User; // Get the user from the modal
                    await InitiateShutdownAsync(discordServerConfig, countdownMinutes, reasonInput, user); // Now pass the user
                }
                else
                {
                    // Follow up with the user to indicate the problem
                    await modal.FollowupAsync("Invalid input or server configuration could not be found.", ephemeral: true);
                }
            }
        }


        private async Task InitiateShutdownAsync(DiscordServerConfig discordServerConfig, int countdownMinutes, string reasonInput, SocketUser user)
        {
            Discord.Color greenColor = new Discord.Color(0, 255, 0); // Green
            Discord.Color redColor = new Discord.Color(255, 0, 0);   // Red
            Discord.Color darkRedColor = new Discord.Color(139, 0, 0); // Dark Red

            try
            {
                var serverConfig = ConvertToServerConfig(discordServerConfig);
                var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword);
                await rconService.ConnectAsync();

                // Inform about the initiation of the countdown
                string initialDescription = $"{user.Username} initiated a shutdown countdown for server {serverConfig.ServerName}. Shutdown in {countdownMinutes} minutes because: {reasonInput}.";
                await Logger.LogToDiscord("Server Shutdown Countdown Started", initialDescription, "Shutdown Logger", redColor);
                await rconService.SendServerChatAsync($"Server will shut down in {countdownMinutes} minutes due to {reasonInput}.");
                // Countdown loop
                while (countdownMinutes > 0)
                {
                    // Wait for one minute
                    await Task.Delay(TimeSpan.FromMinutes(1));

                    // Decrement countdown
                    countdownMinutes--;
                    await rconService.SendServerChatAsync($"Shutdown in {countdownMinutes} minutes.");
                    // Update the countdown status
                    string countdownDescription = $"{countdownMinutes} minutes remaining until shutdown of {serverConfig.ServerName}.";
                    await Logger.LogToDiscord("Server Shutdown Countdown Update", countdownDescription, "Countdown Update", redColor);
                }

                // Execute the shutdown
                await rconService.ShutdownServerAsync(0, reasonInput);  // Assumes there's a method to shutdown immediately
                rconService.Dispose();

                // Log the final shutdown
                await Logger.LogToDiscord("Server Shutdown Executed", $"Server {serverConfig.ServerName} has been shut down due to: {reasonInput}.", "Shutdown Logger", redColor);
            }
            catch (Exception ex)
            {
                await Logger.LogToDiscord("Server Shutdown Failed", $"An attempt to shut down server {discordServerConfig.ServerName} failed: {ex.Message}", "Shutdown Logger", darkRedColor);
                throw; // Rethrow the exception to be caught by the calling task
            }
        }




        public bool IsServerRunning(ServerConfig serverConfig)
        {
            bool isServerRunning = false; // Initialize the flag as false

            // Get the name of the server executable without the extension
            string serverExeName = Path.GetFileNameWithoutExtension("ArkAscendedServer.exe");
            string asaApiLoaderExeName = Path.GetFileNameWithoutExtension("AsaApiLoader.exe");

            // Get the full path to the server executable
            string serverExePath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe");
            string asaApiLoaderExePath = Path.Combine(serverConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "AsaApiLoader.exe");

            // Check if there's a process running from the server's executable path
            var allProcesses = Process.GetProcesses();
            foreach (var process in allProcesses)
            {
                try
                {
                    // Check if the process is a server process and if it's running from the expected path
                    if ((process.ProcessName.Equals(serverExeName, StringComparison.OrdinalIgnoreCase) && process.MainModule.FileName.Equals(serverExePath, StringComparison.OrdinalIgnoreCase)) ||
                        (process.ProcessName.Equals(asaApiLoaderExeName, StringComparison.OrdinalIgnoreCase) && process.MainModule.FileName.Equals(asaApiLoaderExePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        isServerRunning = true; // Set the flag to true if the server process is found
                        break; // No need to continue checking once we found a running server
                    }
                }
                catch (Exception ex)
                {
                    // This catch block can handle exceptions due to accessing process.MainModule which may require administrative privileges
                    Ark_Ascended_Manager.Services.Logger.Log($"Error checking process: {ex.Message}");
                }
            }

            return isServerRunning; // Return the flag indicating whether the server is running
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
        public ServerConfig ConvertToServerConfig(DiscordServerConfig discordConfig)
        {
            return new ServerConfig
            {
                ServerIP = discordConfig.ServerIP,
                RCONPort = discordConfig.RCONPort,
                AdminPassword = discordConfig.AdminPassword,
                ServerName = discordConfig.ServerName,
                ServerPath = discordConfig.ServerPath
                // Map other necessary properties
            };
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
                        .AddField("Server IP", serverConfig.ServerIP)
                        /* .AddField("Current Players", $"{serverConfig.CurrentPlayerCount}", true) */
                        .AddField("Max Players", $"{serverConfig.MaxPlayerCount}", true)
                        .WithThumbnailUrl("https://media.discordapp.net/attachments/1168388628657995836/1182037772224180308/AAM_Icon.png?ex=66295a76&is=6616e576&hm=f4fa28aa9f7952a0398c483b767c35d1e374bd20544c372bcf875d76a48666f7&=&format=webp&quality=lossless&width=222&height=230")
                        .Build();
                    var buttons = new ComponentBuilder()
                        .WithButton("Start", $"start_id:{serverConfig.ServerName}", ButtonStyle.Primary, disabled: false)
                        .WithButton("Shutdown", $"shutdown_id:{serverConfig.ServerName}", ButtonStyle.Danger)
                        .WithButton("Save World", $"save_world_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: true)
                        .WithButton("Update", $"update_id:{serverConfig.ServerName}", ButtonStyle.Success, disabled: true)
                        .WithButton("Manage", $"manage_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: true)
                        .Build();

                    await command.FollowupAsync(embed: embed, components: buttons);
                    Logger.LogInfoToDiscord($"Embed with buttons sent for server: {serverConfig.ServerName}");

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
        public string ServerIP { get; set; }
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
