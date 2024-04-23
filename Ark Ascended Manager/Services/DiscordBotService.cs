using Ark_Ascended_Manager.Views.Pages;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;
using System.Net.Sockets;

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
                    case "update_id":
                        await InitiateUpdateProcess(component, serverIdentifier);
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

        private async Task InitiateUpdateProcess(SocketMessageComponent component, string serverIdentifier)
        {
            // Acknowledge the interaction immediately to avoid timeout
            await component.DeferAsync();
            Debug.WriteLine($"Initiating update process for server {serverIdentifier}.");

            Task.Run(async () =>
            {
                var discordServerConfig = ServerConfigs.FirstOrDefault(s => s.ServerName == serverIdentifier);
                if (discordServerConfig == null)
                {
                    await component.FollowupAsync("Server configuration not found.");
                    return;
                }

                ServerConfig serverConfig = ConvertToServerConfig(discordServerConfig);
                if (await IsServerOnlineUsingRCON(serverConfig))
                {
                    // Server is online, proceed with countdown and shutdown
                    await NotifyServerUpdateAndCheckOnline(serverConfig); // This will handle the 15-minute countdown
                    await StopServer(discordServerConfig);  // Stop the server
                    Debug.WriteLine("Server shutdown initiated. Proceeding with update after a brief delay.");
                    await Task.Delay(TimeSpan.FromSeconds(5));  // Wait for the server to properly shutdown
                }

                Debug.WriteLine("Server is offline or shutdown complete, proceeding with update.");
                bool updateSuccessful = await UpdateServerBasedOnJson(discordServerConfig, component);
                if (updateSuccessful)
                {
                    await ReloadServerConfigsAsync();  // Ensure server configs are reloaded after update
                   /* await UpdateServerEmbeds(discordServerConfig);*/
                    await component.FollowupAsync($"Update completed for {serverIdentifier}. Server will reboot when completed. Estimated time is 2-3 minutes.");
                }
                else
                {
                    await component.FollowupAsync($"Update failed for {serverIdentifier}. Please check logs for details.");
                }
            });
        }
        public async Task UpdateServerEmbeds(DiscordServerConfig serverConfig)
        {
            if (serverChannelIds.TryGetValue(serverConfig.ServerName, out ulong channelId))
            {
                var channel = _client.GetChannel(channelId) as IMessageChannel;
                if (channel != null)
                {
                    // Retrieve all messages and find the one with the existing embed
                    var messages = await channel.GetMessagesAsync().FlattenAsync();
                    var message = messages.FirstOrDefault(msg => msg.Embeds.Any(e => e.Title.Contains(serverConfig.ServerName)));

                    // Delete the old message if found
                    if (message != null)
                    {
                        await (message as IUserMessage).DeleteAsync();
                    }

                    var buttons = new ComponentBuilder()
                   .WithButton("Start", $"start_id:{serverConfig.ServerName}", ButtonStyle.Primary, disabled: false)
                   .WithButton("Shutdown", $"shutdown_id:{serverConfig.ServerName}", ButtonStyle.Danger)
                   .WithButton("Save World", $"save_world_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: false)
                   .WithButton("Update", $"update_id:{serverConfig.ServerName}", ButtonStyle.Success, disabled: false)
                   .WithButton("Manage", $"manage_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: true)
                   .Build();
                    // Create a new embed with updated information
                    var (embed, components) = await CreateServerEmbedAndButtons(serverConfig);

                    // Send the new embed message
                    // Send the new embed message with the buttons
                    await channel.SendMessageAsync(embed: embed, components: buttons);

                    Logger.LogInfoToDiscord($"Updated server status by creating a new embed for {serverConfig.ServerName}.");
                }
                else
                {
                    Logger.LogInfoToDiscord($"Channel not found for server {serverConfig.ServerName}.");
                }
            }
            else
            {
                Logger.LogInfoToDiscord($"Channel ID not found for {serverConfig.ServerName}. Ensure that channel IDs are correctly mapped.");
            }
        }




        public async Task<bool> NotifyServerUpdateAndCheckOnline(ServerConfig serverConfig)
        {
            var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword);
            try
            {
                await rconService.ConnectAsync();
                Logger.LogInfoToDiscord($"Connected to RCON for server {serverConfig.ServerName}");

                // Begin countdown of 15 minutes, sending an update notification every minute.
                for (int minutesLeft = 1; minutesLeft > 0; minutesLeft--)
                {
                    await rconService.SendServerChatAsync($"Server Update Required in {minutesLeft} minutes. Please prepare to log off.");
                    Logger.LogInfoToDiscord($"Update notification sent to server {serverConfig.ServerName}: {minutesLeft} minutes remaining.");
                    await Task.Delay(TimeSpan.FromMinutes(1)); // Wait for 1 minute before next notification.
                }

                // Finally, check if server is still responding before the update.
                await rconService.SaveWorldAsync();
                Logger.LogInfoToDiscord($"Successfully sent save world command to server {serverConfig.ServerName}");
                return true; // If command succeeds, server is considered online.
            }
            catch (SocketException ex)
            {
                Logger.LogInfoToDiscord($"SocketException on server {serverConfig.ServerName}: {ex.Message}");
                return false; // Assume server is offline on socket error
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Failed to send RCON command to server {serverConfig.ServerName}: {ex.Message}");
                return false; // If command fails, treat server as offline
            }
            finally
            {
                try
                {
                    rconService.Dispose(); // Attempt to properly close the connection
                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Error disposing RCON service for server {serverConfig.ServerName}: {ex.Message}");
                }
            }
        }


        public async Task<bool> UpdateServerBasedOnJson(DiscordServerConfig serverConfig, SocketMessageComponent component)
        {
            string steamCmdPath = FindSteamCmdPath();
            if (!string.IsNullOrEmpty(steamCmdPath) && !string.IsNullOrEmpty(serverConfig.AppId))
            {
                Debug.WriteLine($"Starting update for {serverConfig.ServerName} using SteamCMD.");
                string scriptPath = Path.Combine(Path.GetTempPath(), "steamcmd_update_script.txt");
                File.WriteAllLines(scriptPath, new string[]
                {
            $"force_install_dir \"{serverConfig.ServerPath}\"",
            "login anonymous",
            $"app_update {serverConfig.AppId} validate",
            "quit"
                });

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = steamCmdPath,
                    Arguments = $"+runscript \"{scriptPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                using (Process process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    process.WaitForExit();
                    Debug.WriteLine($"SteamCMD exited with code {process.ExitCode}.");

                    if (process.ExitCode == 0)
                    {
                        await component.FollowupAsync($"Update completed successfully for {serverConfig.ServerName}.");
                        // Call to start the server if update succeeds
                        return await StartServerAfterUpdate(serverConfig, component);
                    }
                    else
                    {
                        await component.FollowupAsync($"Update failed for {serverConfig.ServerName}. Check logs for details.");
                        return false;
                    }
                }
            }
            else
            {
                Debug.WriteLine("SteamCMD path not found or App ID is empty, update cannot proceed.");
                await component.FollowupAsync("Update failed: SteamCMD path not found or App ID is empty.");
                return false;
            }
        }

        private async Task<bool> StartServerAfterUpdate(DiscordServerConfig serverConfig, SocketMessageComponent component)
        {
            string batchFilePath = Path.Combine(serverConfig.ServerPath, "LaunchServer.bat");
            if (File.Exists(batchFilePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(batchFilePath)
                {
                    WorkingDirectory = serverConfig.ServerPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process serverProcess = Process.Start(startInfo);
                if (serverProcess != null)
                {
                    Debug.WriteLine($"Server {serverConfig.ServerName} is starting.");
                    await component.FollowupAsync($"Server {serverConfig.ServerName} is restarting. ");
                    return true;
                }
                else
                {
                    await component.FollowupAsync("Failed to start the server. Process could not be initiated.");
                    return false;
                }
            }
            else
            {
                await component.FollowupAsync("Launch batch file does not exist. Server start failed.");
                return false;
            }
        }




        private string FindSteamCmdPath()
        {
            // Define the JSON file path in the app data folder
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            string jsonFilePath = Path.Combine(appDataPath, "SteamCmdPath.json");

            // Try to read the path from the JSON file
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                dynamic pathData = JsonConvert.DeserializeObject<dynamic>(json);
                string savedPath = pathData?.SteamCmdPath;
                if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                {
                    return savedPath;
                }
            }


            return null; // or handle this case appropriately
        }

        private async Task StopServer(DiscordServerConfig serverConfig)
        {
            var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword);
            try
            {
                await rconService.ConnectAsync();
                await rconService.SendCommandAsync("doexit"); 
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Error stopping server {serverConfig.ServerName}: {ex.Message}");
            }
            finally
            {
                rconService.Dispose();
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
            try
            {
                Debug.WriteLine("Start Server Called intially");
                await component.DeferAsync();
                Debug.WriteLine("Start Server Called");
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

                string batchFilePath = Path.Combine(serverConfig.ServerPath, "LaunchServer.bat");
                if (!File.Exists(batchFilePath))
                {
                    await component.FollowupAsync("Launch batch file does not exist.");
                    return;
                }

                Debug.WriteLine($"Attempting to start server {serverName} using batch file at {batchFilePath}.");

                ProcessStartInfo startInfo = new ProcessStartInfo(batchFilePath)
                {
                    WorkingDirectory = serverConfig.ServerPath,
                    UseShellExecute = true,  // Change this to true to show the command window
                    CreateNoWindow = false   // This will now function correctly to show the window
                };

                Process process = Process.Start(startInfo);
                await Task.Delay(5000); // Wait for a few seconds to assume starting is initiated
                Debug.WriteLine($"Process launched for server {serverName}, check server directly for more details.");
                await component.FollowupAsync($"Server {serverName} is starting up. Please check directly for operation status.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in StartServer: {ex}");
                Logger.LogInfoToDiscord($"Exception when trying to start server {serverName}: {ex.Message}");
                await component.FollowupAsync($"Exception occurred while starting {serverName}. Please check server logs.");
            }
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




        public async Task<bool> IsServerOnlineUsingRCON(ServerConfig serverConfig)
        {
            var rconService = new ArkRCONService(serverConfig.ServerIP, (ushort)serverConfig.RCONPort, serverConfig.AdminPassword);
            try
            {
                await rconService.ConnectAsync();
                await rconService.SaveWorldAsync();
                Logger.LogInfoToDiscord($"Successfully sent RCON command to server {serverConfig.ServerName}");
                return true; // If command succeeds, server is online
            }
            catch (SocketException ex)
            {
                Logger.LogInfoToDiscord($"SocketException on server {serverConfig.ServerName}: {ex.Message}");
                return false; // Assume server is offline on socket error
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Failed to send RCON command to server {serverConfig.ServerName}: {ex.Message}");
                return false; // If command fails, treat server as offline
            }
            finally
            {
                try
                {
                    rconService.Dispose(); // Attempt to properly close the connection
                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Error disposing RCON service for server {serverConfig.ServerName}: {ex.Message}");
                }
            }
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
                        .WithButton("Update", $"update_id:{serverConfig.ServerName}", ButtonStyle.Success, disabled: false)
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
                        var (embed, components) = await CreateServerEmbedAndButtons(serverConfig);
                        await UpdateServerEmbed(serverConfig, embed);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInfoToDiscord($"Error in periodic check for {serverConfig.ServerName}: {ex.Message}");
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(5)); // Or your preferred interval
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
            // If the server is known to be offline, return "Offline" immediately.
            if (serverConfig.ServerStatus == "Offline")
            {
                return "Offline";
            }

            // If the status is not known, try to connect and get the player count.
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
                // If an exception occurs, assume the server is offline.
                // Update the server's status accordingly.
                serverConfig.ServerStatus = "Offline";
                Logger.LogInfoToDiscord($"Exception when trying to retrieve player count: {ex.Message}");
                return "Offline";
            }
            finally
            {
                rconService.Dispose();
            }
        }





        private Process GetServerProcess(string serverPath, string primaryExecutableName, string secondaryExecutableName)
        {
            Logger.LogInfoToDiscord($"Looking for server process with primary name: {primaryExecutableName} or secondary name: {secondaryExecutableName}");

            Process process = null;

            // Attempt to find the primary process
            process = FindProcessByExecutableName(primaryExecutableName);
            if (process != null)
            {
                return process;
            }

            // If not found, attempt to find the secondary process
            process = FindProcessByExecutableName(secondaryExecutableName);
            if (process != null)
            {
                return process;
            }

            Logger.LogInfoToDiscord("No matching server process found.");
            return null;
        }

        private Process FindProcessByExecutableName(string executableName)
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (string.Equals(Path.GetFileName(process.MainModule.FileName), executableName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInfoToDiscord($"Found process for server: {process.ProcessName}");
                        return process;
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
                {
                    if (process.ProcessName.Equals(executableName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInfoToDiscord($"Found process by name (without path due to access restrictions): {process.ProcessName}");
                        return process;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Exception when accessing process {process.ProcessName}: {ex.Message}");
                }
            }

            return null; 
        }


        private long GetProcessMemoryUsage(Process process)
        {
            if (process != null)
            {
                try
                {
                    process.Refresh(); // Refresh to get the latest info
                    return process.WorkingSet64 / 1024 / 1024; // Convert bytes to MB
                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Error getting memory usage: {ex.Message}");
                }
            }
            return -1;
        }

        private double GetCpuUsageForProcess(Process process)
        {
            try
            {
                if (process == null)
                    return 0;

                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;
                Thread.Sleep(500); // Wait time to measure CPU usage over

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;
                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;

                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100; // Convert to percentage
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Error calculating CPU usage: {ex.Message}");
                return 0;
            }
        }











        private async Task<(Embed, ComponentBuilder)> CreateServerEmbedAndButtons(DiscordServerConfig serverConfig)
        {
            try
            {
                if (string.IsNullOrEmpty(serverConfig.ServerName))
                    throw new Exception("Server name is not defined.");
                if (string.IsNullOrEmpty(serverConfig.ServerIP))
                    throw new Exception("Server IP is not defined.");

                Process serverProcess = GetServerProcess(serverConfig.ServerPath, "ArkAscendedServer.exe", "AsaApiLoader.exe");
                var playerCount = await GetCurrentPlayerCount(serverConfig);
                bool isOnline = playerCount != "Offline" && serverProcess != null;
                long ramUsageMB = isOnline ? GetProcessMemoryUsage(serverProcess) : 0;
                double ramUsageGB = ramUsageMB / 1024.0; // Convert MB to GB
                ulong totalRamMB = GetTotalPhysicalMemory();
                double totalRamGB = totalRamMB / 1024.0 / 1024.0; // Convert bytes to GB
                double cpuUsage = isOnline ? GetCpuUsageForProcess(serverProcess) : 0;

                var embedBuilder = new EmbedBuilder()
                    .WithTitle(serverConfig.ServerName)
                    .WithColor(isOnline ? Color.Green : Color.Red)
                    .AddField("Status", isOnline ? "Online" : "Offline", true)
                    .AddField("Map", serverConfig.MapName ?? "Unknown", true)
                    .AddField("Server IP", serverConfig.ServerIP)
                    .AddField("Players", playerCount, true)
                    .AddField("RAM Usage", isOnline ? $"{ramUsageGB:N2} GB / {totalRamGB:N2} GB" : "N/A", true)
                    .AddField("CPU Usage", isOnline ? $"{cpuUsage:N2}%" : "N/A", true)
                    .WithThumbnailUrl(serverConfig.ServerIcon ?? "default_icon_url_here");

                var embed = embedBuilder.Build();

                var buttons = new ComponentBuilder()
                    .WithButton("Start", $"start_id:{serverConfig.ServerName}", ButtonStyle.Primary, disabled: !isOnline)
                    .WithButton("Shutdown", $"shutdown_id:{serverConfig.ServerName}", ButtonStyle.Danger, disabled: !isOnline)
                    .WithButton("Save World", $"save_world_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: !isOnline)
                    .WithButton("Update", $"update_id:{serverConfig.ServerName}", ButtonStyle.Success, disabled: !isOnline)
                    .WithButton("Manage", $"manage_id:{serverConfig.ServerName}", ButtonStyle.Secondary, disabled: true);

                return (embed, buttons);
            }
            catch (Exception ex)
            {
                Logger.LogInfoToDiscord($"Error creating server embed for {serverConfig.ServerName}: {ex.Message}");
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription($"Failed to create embed for {serverConfig.ServerName}. Exception: {ex.Message}")
                    .WithColor(Color.Red)
                    .Build();

                return (errorEmbed, new ComponentBuilder()); // Return the error embed and an empty ComponentBuilder
            }
        }





        private static ulong GetTotalPhysicalMemory()
                {
                    ComputerInfo CI = new ComputerInfo();
                    ulong totalPhysicalMemory = CI.TotalPhysicalMemory;
                    return totalPhysicalMemory;
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
