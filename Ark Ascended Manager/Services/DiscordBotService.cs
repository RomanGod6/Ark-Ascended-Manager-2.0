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
        private readonly BotSettings _settings;

        public List<DiscordServerConfig> ServerConfigs { get; private set; } = new List<DiscordServerConfig>();
        // Added properties
        public ulong GuildId { get; private set; }
        public string WebhookUrl { get; private set; }


        // Updated constructor to accept additional parameters
        public DiscordBotService(IServiceProvider services, ulong guildId, string webhookUrl, BotSettings settings)
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = services;
            _settings = settings;

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
            var user = (SocketGuildUser)component.User;
            if (!user.Roles.Any(role => _settings.AuthorizedRoleIds.Contains(role.Id)))
            {
                await component.RespondAsync("You do not have permission to do this.", ephemeral: true);
                return;
            }
            var parts = component.Data.CustomId.Split(':');
            if (parts.Length > 1)
            {
                string serverIdentifier = parts[1];  // This gets the server identifier part of the custom ID

                switch (parts[0])
                {
                    case "start_id":
                        await StartServer(component, serverIdentifier);
                        break;
                    case "save_world_id":
                        await ProcessSaveWorld(component, serverIdentifier);
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

        private async Task ProcessSaveWorld(SocketMessageComponent component, string serverName)
        {
            await component.DeferAsync(); // Acknowledge the interaction immediately

            var serverConfig = ServerConfigs.FirstOrDefault(s => s.ServerName == serverName);
            if (serverConfig == null)
            {
                await component.FollowupAsync($"Server configuration for {serverName} not found.");
                return;
            }

            try
            {
                var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword);
                await rconService.ConnectAsync();
                await rconService.SaveWorldAsync();
                await component.FollowupAsync($"World save initiated for {serverName}.");
            }
            catch (Exception ex)
            {
                await component.FollowupAsync($"Failed to initiate world save: {ex.Message}");
            }
        }


        private async Task StartServer(SocketMessageComponent component, string serverName)
        {
            // Immediately acknowledge the interaction.
            await component.DeferAsync();

          
            var discordServerConfig = ServerConfigs.FirstOrDefault(s => s.ServerName == serverName);

            if (discordServerConfig == null)
            {

                await component.FollowupAsync($"Server configuration for {serverName} not found.");
                return;
            }

            var serverConfig = ConvertToServerConfig(discordServerConfig);


            if (serverConfig.ServerPath == null || serverConfig.ServerPath == "")
            {
                await component.FollowupAsync("Server path is undefined or empty.");
                return;
            }

            if (IsServerRunning(serverConfig))
            {
                await component.FollowupAsync($"Server {serverName} is already running.");
                return;
            }

            string batchFilePath = Path.Combine(serverConfig.ServerPath, "LaunchServer.bat");

            if (!File.Exists(batchFilePath))
            {
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
                     ;
                    }
                    else
                    {
                
                        Logger.LogToDiscord("Server Start Failed", $"Could not start the server process for {serverName}.", "Error", new Discord.Color(255, 0, 0));
                    }
                }
                catch (Exception ex)
                {
         
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
                    Logger.LogInfoToDiscord($"Error checking process: {ex.Message}");
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

            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Error during bot start: {ex.Message}");
            }
        }


        public async Task StopAsync()
        {
      
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

                }
                else
                {
                    Logger.LogInfoToDiscord("Server configuration file not found.");
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogInfoToDiscord($"JSON deserialization failed: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"An error occurred when loading server configs: {ex.Message}");
            }

            return servers;
        }

        private Task LogAsync(LogMessage logMessage)
        {
            Logger.LogInfoToDiscord(logMessage.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {

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
                await PeriodicServerStatusCheck();
            }
            else
            {
                Logger.LogInfoToDiscord($"Guild ID was Null or undefined - please recheck the integrations tab for the correct Guild ID.");
            }
        }


        private async Task HandleSlashCommandExecutedAsync(SocketSlashCommand command)
        {
            if (command.Data.Name == "listservers")
            {
                var user = (SocketGuildUser)command.User;
                // Use _botSettings to access AuthorizedRoleIds
                if (user.Roles.Any(role => _settings.AuthorizedRoleIds.Contains(role.Id)))
                {
                    // User has an authorized role, proceed with the command
                    await command.DeferAsync();
                    await SendServerStatusEmbeds(command);
                }
                else
                {
                    // User does not have an authorized role, send a message
                    await command.RespondAsync("You do not have permission to use this command.", ephemeral: true);
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

        private Dictionary<string, ulong> serverMessageIds = new Dictionary<string, ulong>();
        private Dictionary<string, ulong> serverChannelIds = new Dictionary<string, ulong>();


        public async Task SendServerStatusEmbeds(SocketSlashCommand command)
        {
            var channelId = command.Channel.Id;
            foreach (var serverConfig in ServerConfigs)
            {
                serverChannelIds[serverConfig.ServerName] = channelId;
                try
                {

                    var isOnline = serverConfig.ServerStatus == "Online";
                    var embed = new EmbedBuilder()
                        .WithTitle($"{serverConfig.ServerName}")
                        .WithColor(isOnline ? Color.Green : Color.Red)
                        .AddField("Status", isOnline ? "Online" : "Offline", true)
                        .AddField("Map", serverConfig.MapName, true)
                        .AddField("Server IP", serverConfig.ServerIP)
                        /* .AddField("Current Players", $"{serverConfig.CurrentPlayerCount}", true) */
                        .AddField("Max Players", $"{serverConfig.MaxPlayerCount}", true)
                        .WithThumbnailUrl($"{serverConfig.ServerIcon}")
                        .Build();
                    var buttons = new ComponentBuilder()
                        .WithButton("Start", $"start_id:{serverConfig.ServerName}", ButtonStyle.Primary, disabled: false)
                        .WithButton("Shutdown", $"shutdown_id:{serverConfig.ServerName}", ButtonStyle.Danger)
                        .WithButton("Save World", $"save_world_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: false)
                        .WithButton("Update", $"update_id:{serverConfig.ServerName}", ButtonStyle.Success, disabled: true)
                        .WithButton("Manage", $"manage_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: true)
                        .Build();

                    await command.FollowupAsync(embed: embed, components: buttons);


                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Failed to send embed for server {serverConfig.ServerName}: {ex.Message}");
                }
            }


        }
        private async Task PeriodicServerStatusCheck()
        {
            while (true)
            {
                // Reload the server configurations before each check to get the latest data
                ServerConfigs = await LoadServerConfigsAsync();

                foreach (var serverConfig in ServerConfigs)
                {
                    try
                    {
                        // Now you have the latest server configuration for each tick
                        var embed = await CreateServerEmbed(serverConfig);
                        await UpdateServerEmbed(serverConfig, embed);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfoToDiscord($"Error in periodic check for {serverConfig.ServerName}: {ex.Message}");
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1)); // Or your preferred interval
            }
        }







        private bool CheckServerStatus(DiscordServerConfig serverConfig)
        {
            return serverConfig.ServerStatus == "Online";
        }

        public async Task ReloadServerConfigsAsync()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                List<DiscordServerConfig> updatedConfigs = JsonConvert.DeserializeObject<List<DiscordServerConfig>>(json) ?? new List<DiscordServerConfig>();

                if (ServerConfigs.Count != updatedConfigs.Count)
                {

                }

                ServerConfigs = updatedConfigs;
                
            }
            else
            {
                Logger.LogInfoToDiscord("Configuration file not found. Unable to reload configurations. Please ensure servers.json at appdata/roaming/arkascendedmanager/server.json excists.");
            }
        }


        private async Task UpdateServerEmbed(DiscordServerConfig serverConfig, Embed embed)
        {
            if (serverChannelIds.TryGetValue(serverConfig.ServerName, out ulong channelId))
            {
                var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
                if (channel != null)
                {
                    var messages = await channel.GetMessagesAsync().FlattenAsync();
                    var message = messages.FirstOrDefault(msg => msg.Embeds.Any(e => e.Title.Contains(serverConfig.ServerName)));
                    if (message != null)
                    {
                        Logger.LogInfoToDiscord($"Updating message for {serverConfig.ServerName}");
                        await (message as IUserMessage).ModifyAsync(msg => msg.Embed = embed);
                    }
                    else
                    {
                        Logger.LogInfoToDiscord($"No existing message found for {serverConfig.ServerName}. Sending new message.");
                        await channel.SendMessageAsync(embed: embed);
                    }
                }
            }
            else
            {
                Logger.LogInfoToDiscord($"Channel ID not found for {serverConfig.ServerName}");
            }
        }




        private async Task<string> GetCurrentPlayerCount(DiscordServerConfig serverConfig)
        {
            var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword);
            try
            {
                await rconService.ConnectAsync();
                var playerListResponse = await rconService.ListPlayersAsync();
                if (playerListResponse.Trim().Equals("No players connected", StringComparison.OrdinalIgnoreCase))
                {
                    return "0"; // No players connected
                }
                else
                {
                    var playerLines = playerListResponse.Split('\n');
                    int playerCount = playerLines.Count(line => !string.IsNullOrWhiteSpace(line));
                    return playerCount.ToString(); // Active players count
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Error getting player count for {serverConfig.ServerName}: {ex.Message}");

               
                serverConfig.IsRunning = false;
                serverConfig.ServerStatus = "Offline";
                return "Offline"; // Return a special string indicating that the server is offline
            }
            finally
            {
                rconService.Dispose();
            }
        }









        private async Task<Embed> CreateServerEmbed(DiscordServerConfig serverConfig)
        {
            try
            {
                var playerCount = await GetCurrentPlayerCount(serverConfig);
                var isOnline = playerCount != "Offline";
                Logger.LogInfoToDiscord($"Creating embed for {serverConfig.ServerName} - Status: {isOnline} - Players: {playerCount}");
                var embed = new EmbedBuilder()
                    .WithTitle(serverConfig.ServerName)
                    .WithColor(isOnline ? Color.Green : Color.Red)
                    .AddField("Status", isOnline ? "Online" : "Offline", true)
                    .AddField("Map", serverConfig.MapName, true)
                    .AddField("Server IP", serverConfig.ServerIP)
                    .AddField("Players", $"{playerCount}/{serverConfig.MaxPlayerCount}", true)
                    .WithThumbnailUrl($"{serverConfig.ServerIcon}")
                    .Build();
                return embed;
            }
            catch (Exception ex)
            {
                // If there is an RCON failure, we catch it here
                Logger.LogInfoToDiscord($"RCON failure when getting player count for {serverConfig.ServerName}: {ex.Message}");

                // Update the server status to offline due to RCON failure
                serverConfig.ServerStatus = "Offline";
                serverConfig.IsRunning = false;

                // Now create an embed reflecting the offline status
                var embed = new EmbedBuilder()
                    .WithTitle(serverConfig.ServerName)
                    .WithColor(Color.Red)
                    .AddField("Status", "Offline", true)
                    .AddField("Map", serverConfig.MapName, true)
                    .AddField("Server IP", serverConfig.ServerIP)
                    .AddField("Players", $"0/{serverConfig.MaxPlayerCount}", true)
                    .WithThumbnailUrl($"{serverConfig.ServerIcon}")
                    .Build();
                return embed;

            }
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
        public string ServerIcon { get; set; }
        public bool IsRunning { get; set; }
        public int ChangeNumber { get; set; }
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

    
    }
    public class BotSettings
    {
        public string Token { get; set; }
        public string GuildId { get; set; }
        public string WebhookUrl { get; set; }
        public string LoggerWebhookUrl { get; set; }
        public string[] IgnoredPatterns { get; set; }
        public List<ulong> AuthorizedRoleIds { get; set; }
        public bool VerboseLogging { get; set; }
    }
}
