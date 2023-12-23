using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Ark_Ascended_Manager.Services.DiscordBotService;
using CoreRCON;
using System.Net;
using System.Net.Http;
using System.Text;
using CoreRCON.Parsers.Standard;

namespace Ark_Ascended_Manager.Services
{

    public class DiscordBotService
    {
        private ServerInfo currentServerInfo;
        private Timer _playerCountTimer;
        private int _playerCount;
        private bool _isServerOnline;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly ulong _guildId; // Store the Guild ID
        private Dictionary<ulong, string> userServerSelections = new Dictionary<ulong, string>();
        private Timer _chatFetchTimer;
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private readonly DiscordWebhookService _webhookService;




        public DiscordBotService(IServiceProvider services, ulong guildId, string webhookUrl) // Accept Guild ID as a parameter
        {

            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _commands = new CommandService();
            _services = services;
            _guildId = guildId; // Store the Guild ID
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _httpClient = new HttpClient();
            _webhookUrl = webhookUrl ?? throw new ArgumentNullException(nameof(webhookUrl));
            _webhookService = new DiscordWebhookService();
            _client.SelectMenuExecuted += OnSelectMenuExecuted;

            RegisterCommands();
        }

        // Method to start fetching player count




        private void RegisterCommands()
        {
            _client.Ready += async () =>
            {
                var guild = _client.GetGuild(_guildId);
                if (guild != null)
                {
                    // Register showservers command
                    await guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                        .WithName("showservers")
                        .WithDescription("Show information about servers")
                        .Build());

                    // Register listplayers command
                    await guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                        .WithName("listplayers")
                        .WithDescription("List all players on the server")
                        .Build());
                }
            };

            _client.InteractionCreated += HandleInteractionAsync;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.ButtonExecuted += ButtonHandler;
        }
        private void TimerFetchPlayerCount(object state)
        {
            var server = state as ServerInfo;
            if (server != null)
            {
                // Call the asynchronous method using Task.Run to handle it in a non-blocking way
                Task.Run(async () => await FetchPlayerCount(server));
            }
        }

        // FetchPlayerCount remains an async Task method for asynchronous operation
        private async Task FetchPlayerCount(ServerInfo server)
        {
            if (server == null)
            {
                _isServerOnline = false;
                _playerCount = 0;
            }
            else
            {
                var (playerList, isServerOnline) = await SendRconCommandAsync("listplayers", server.AdminPassword, server.RCONPort);
                _isServerOnline = isServerOnline;

                if (!string.IsNullOrEmpty(playerList))
                {
                    _playerCount = ProcessPlayerList(playerList);
                }
                else
                {
                    _playerCount = 0;
                }
            }

            // Update the embed with the latest status
            await UpdateEmbedWithPlayerCount(server);
            await FetchAndSendServerChat(server);
            
        }
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.CommandName)
            {
                case "showservers":
                    // Handle showservers command...
                    break;
                case "listplayers":
                    await HandleListPlayersCommand(command);
                    break;
            }
        }
        private async Task HandleListPlayersCommand(SocketSlashCommand command)
        {
            await command.DeferAsync(ephemeral: true); // Acknowledge the command

            var serverProfiles = await LoadServerProfilesAsync();

            // Loop through server profiles to create a dropdown for each.
            foreach (var serverProfile in serverProfiles)
            {
                var players = await GetPlayersFromServerProfile(serverProfile);

                // Create a new SelectMenuBuilder
                var menuBuilder = new SelectMenuBuilder()
                    .WithCustomId($"select_player_{serverProfile.ServerName}")
                    .WithPlaceholder("Select a player");

                // Add each player as an option in the select menu
                foreach (var player in players)
                {
                    menuBuilder.AddOption(player.Name, player.EOSID); // Use AddOption here
                }

                // Create the message component with the select menu
                var componentBuilder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"Players on {serverProfile.ServerName}")
                    .WithDescription("Select a player from the dropdown to manage:");

                await command.FollowupAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
            }
        }


        private async Task<List<ServerProfile>> LoadServerProfilesAsync()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonConvert.DeserializeObject<List<ServerProfile>>(json);
            }
            return new List<ServerProfile>();
        }




        private async Task ButtonHandler(SocketMessageComponent component)
        {
            try
            {
                var customId = component.Data.CustomId;
                var parts = customId.Split('_');
                if (parts.Length < 3)
                {
                    Ark_Ascended_Manager.Services.Logger.Log($"Error: Custom ID '{customId}' format is incorrect. Expected format 'action_eosid_serverName'.");
                    await component.RespondAsync("Error: Custom ID format is incorrect.");
                    return;
                }

                var action = parts[0];
                var eosid = parts[1];
                var serverName = string.Join("_", parts.Skip(2));

                Ark_Ascended_Manager.Services.Logger.Log($"Action: {action}, EOSID: {eosid}, Server Name: {serverName}");

                var serverProfile = await GetServerProfileAsync(serverName);
                if (serverProfile == null)
                {
                    Ark_Ascended_Manager.Services.Logger.Log($"Server profile not found for server name: {serverName}");
                    await component.RespondAsync("Server profile not found.");
                    return;
                }

                Ark_Ascended_Manager.Services.Logger.Log($"Found Server Profile - Server Name: {serverProfile.ServerName}, AdminPassword: {serverProfile.AdminPassword}, RCONPort: {serverProfile.RCONPort}");

                switch (action)
                {
                    case "kick":
                        Ark_Ascended_Manager.Services.Logger.Log($"Attempting to kick player with EOSID: {eosid}");
                        var kickResponse = await SendRconCommandAsync($"KickPlayer {eosid}", serverProfile.AdminPassword, serverProfile.RCONPort);
                        Ark_Ascended_Manager.Services.Logger.Log($"Kick Response: {kickResponse}");
                        await component.RespondAsync($"Player with EOSID {eosid} has been kicked from server {serverProfile.ServerName}.");
                        break;
                    case "getEOSID":
                        // Handle the case where the action is to get the EOSID
                        Ark_Ascended_Manager.Services.Logger.Log($"Retrieving EOSID: {eosid}");
                        await component.RespondAsync($"EOSID: {eosid}");
                        break;
                    default:
                        Ark_Ascended_Manager.Services.Logger.Log($"Unknown action: {action}");
                        await component.RespondAsync("Unknown action.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception caught in ButtonHandler: {ex}");
                await component.RespondAsync("An error occurred while processing your request.");
            }
        }
        public async Task SendPlayerDropdown(SocketSlashCommand command, List<Player> players)
        {
            // Create a list of SelectMenuOptionBuilder, each representing a player.
            var options = players.Select(player => new SelectMenuOptionBuilder()
                .WithLabel(player.Name)
                .WithValue(player.EOSID))
                .ToList();

            // Create the SelectMenuBuilder with the options.
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId("player_select_menu")
                .WithPlaceholder("Select a player")
                .WithOptions(options) // Pass the list of SelectMenuOptionBuilder objects here directly
                .WithMinValues(1)
                .WithMaxValues(1);

            // Create the message component, which includes our SelectMenuBuilder.
            var componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            // Send the message with the dropdown menu.
            await command.FollowupAsync("Select a player to manage:", components: componentBuilder.Build());
        }



        // Handling the interaction when an admin selects an option.
        public async Task OnSelectMenuExecuted(SocketMessageComponent component)
        {
            Ark_Ascended_Manager.Services.Logger.Log($"CustomId: {component.Data.CustomId}");
            Ark_Ascended_Manager.Services.Logger.Log($"Selected Value(s): {string.Join(", ", component.Data.Values)}");


            var parts = component.Data.CustomId.Split('_');
            var action = parts[0];
            var serverName = parts.Length > 1 ? string.Join("_", parts.Skip(2)) : null; // Join the rest of the parts to form the serverName
            Ark_Ascended_Manager.Services.Logger.Log($"Server Name: {serverName}");
            var selectedEOSID = component.Data.Values.FirstOrDefault();

            if (action == "select_player" && serverName != null)
            {
                var serverProfile = await GetServerProfileAsync(serverName);
                if (serverProfile != null && serverProfile.Players != null)
                {
                    var selectedPlayer = serverProfile.Players.FirstOrDefault(p => p.EOSID == selectedEOSID);

                    if (selectedPlayer != null)
                    {
                        var buttonComponents = new ComponentBuilder()
                            .WithButton("Kick Player", $"kick_{selectedPlayer.EOSID}", ButtonStyle.Danger)
                            .WithButton("Get EOSID", $"eosid_{selectedPlayer.EOSID}", ButtonStyle.Primary)
                            .Build();

                        // Use RespondAsync instead of FollowupAsync
                        await component.RespondAsync($"Selected player: {selectedPlayer.Name}", components: buttonComponents);
                    }
                    else
                    {
                        await component.RespondAsync("Player not found.");
                    }
                }
                else
                {
                    await component.RespondAsync("Server profile not found or players list is uninitialized.");
                }
            }
            else
            {
                await component.RespondAsync("Action not recognized or server name missing.");
            }
        }






        private async Task<ServerProfile> GetServerProfileAsync(string serverName)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                var serverProfiles = JsonConvert.DeserializeObject<List<ServerProfile>>(json);
                return serverProfiles.FirstOrDefault(profile => profile.ServerName == serverName);
            }

            return null; // or throw an exception if you prefer
        }
        public class ServerProfile
        {
            public string ProfileName { get; set; }
            public string ServerStatus { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }
            public bool IsRunning { get; set; }
            public string ServerName { get; set; }
            public List<Player> Players { get; set; }
            public int ListenPort { get; set; }
            public int RCONPort { get; set; }
            public string Mods { get; set; }
            public int MaxPlayerCount { get; set; }
            public string AdminPassword { get; set; }
            public string ServerPassword { get; set; }
            public bool UseBattlEye { get; set; }
            public bool ForceRespawnDinos { get; set; }
            public bool PreventSpawnAnimation { get; set; }
            // Add other properties as needed...
        }
        private async Task<List<Player>> GetPlayersFromServerProfile(ServerProfile serverProfile)
        {
            var (response, success) = await SendRconCommandAsync("listplayers", serverProfile.AdminPassword, serverProfile.RCONPort);
            var players = new List<Player>();

            if (success && !string.IsNullOrWhiteSpace(response))
            {
                var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    // Here we need to parse the line to extract player name and EOSID
                    // The exact parsing logic will depend on the format of the line
                    // Assuming the format is "0. PlayerName, EOSID"
                    var parts = line.Split(new[] { ". ", ", " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3) // Make sure we have at least the index, player name, and EOSID
                    {
                        var player = new Player
                        {
                            Id = parts[0], // The player index, may not be needed
                            Name = parts[1],
                            EOSID = parts[2] // The EOSID
                        };
                        players.Add(player);
                    }
                }
            }

            return players;
        }

        public class Player
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string EOSID { get; set; }
        }




        private async Task FetchAndSendServerChat(ServerInfo serverInfo)
        {
            if (serverInfo == null)
                return;

            var (chatMessages, _) = await SendRconCommandAsync("getchat", serverInfo.AdminPassword, serverInfo.RCONPort);

            // Remove unwanted message and trim to avoid whitespace issues
            chatMessages = RemoveUnwantedMessages(chatMessages).Trim();

            // Only send the message if it's not empty after filtering and trimming
            if (!string.IsNullOrEmpty(chatMessages))
            {
                // Format the message with the server name
                string formattedMessage = $"[{serverInfo.ServerName}]: {chatMessages}";

                // Use the webhook service to send the message
                await _webhookService.SendWebhookMessageAsync(_webhookUrl, formattedMessage);
            }
        }

        private string RemoveUnwantedMessages(string chatMessages)
        {
            var lines = chatMessages.Split('\n');
            var filteredLines = lines.Where(line => !line.Contains("Server received, But no response!!")).ToArray();
            return string.Join("\n", filteredLines);
        }



        // StartFetchingPlayerCount method
        public void StartFetchingPlayerCount()
        {
            if (_playerCountTimer == null)
            {
                // Convert TimeSpan to milliseconds
                int dueTime = (int)TimeSpan.FromSeconds(10).TotalMilliseconds; // Delay before the timer starts
                int period = (int)TimeSpan.FromSeconds(5).TotalMilliseconds; // Interval between invocations

                _playerCountTimer = new Timer(TimerFetchPlayerCount, currentServerInfo, dueTime, period);
                Console.WriteLine("Player count fetching timer started.");
            }
        }
        public class DiscordWebhookService
        {
            private readonly HttpClient _httpClient = new HttpClient();

            public DiscordWebhookService()
            {
                // Initialization (if needed)
            }

            public async Task SendWebhookMessageAsync(string webhookUrl, string message)
            {
                var payload = new
                {
                    content = message
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                }
            }
        }
        







        private async Task<(string, bool)> SendRconCommandAsync(string command, string adminPassword, int rconPort)
        {
            var serverIp = IPAddress.Parse("127.0.0.1");
            var rcon = new CoreRCON.RCON(serverIp, (ushort)rconPort, adminPassword);

            try
            {
                await rcon.ConnectAsync();
                string response = await rcon.SendCommandAsync(command);
                Ark_Ascended_Manager.Services.Logger.Log($"RCON command response: {response}");

                // Assuming there's no Disconnect method, we don't call it.
                // Instead, we'll rely on the using statement to ensure that
                // the connection is closed properly.
                return (response, true);
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception sending RCON command: {ex.Message}");
                return (null, false);
            }
            finally
            {
                // If the RCON class implements IDisposable, we dispose of it here.
                // Otherwise, we might need to do something else to ensure the connection is closed.
                (rcon as IDisposable)?.Dispose();
            }
        }
        







        private ulong? _lastEmbedMessageId; // Nullable ulong to store the message ID
        private bool _shouldUpdateEmbed = false;









        public async Task StartAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("Error: Bot token is empty or invalid.");
                    return;
                }

                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting the bot: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            await _client.StopAsync();
            await _client.LogoutAsync();

            // Stop the timer when the bot stops
            if (_playerCountTimer != null)
            {
                _playerCountTimer.Dispose();
                _playerCountTimer = null;
                Console.WriteLine("Player count fetching timer stopped.");
            }
        }


        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            // Bot is online and ready
            Console.WriteLine("Bot is online and ready.");
            // Set a custom status message
            _client.SetGameAsync("Standing by for player count:", null, ActivityType.Playing);

            return Task.CompletedTask;
        }
        private int ProcessPlayerList(string playerList)
        {
            if (string.IsNullOrEmpty(playerList))
            {
                return 0; // No players online or no data returned
            }

            // Split the string by new lines
            var playerEntries = playerList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Use LINQ to count only lines that start with a number followed by a period, indicating a player entry
            int playerCount = playerEntries.Count(line => Regex.IsMatch(line.Trim(), @"^\d+\.\s"));

            return playerCount;
        }
        private bool IsServerRunning(ServerInfo serverInfo)
        {
            // Get the name of the server executable without the extension
            string serverExeName = Path.GetFileNameWithoutExtension("ArkAscendedServer.exe");
            string asaApiLoaderExeName = Path.GetFileNameWithoutExtension("AsaApiLoader.exe");

            // Get the full path to the server executable
            string serverExePath = Path.Combine(serverInfo.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe");
            string asaApiLoaderExePath = Path.Combine(serverInfo.ServerPath, "ShooterGame", "Binaries", "Win64", "AsaApiLoader.exe");

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
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // This catch block can handle exceptions due to accessing process.MainModule which may require administrative privileges
                    Ark_Ascended_Manager.Services.Logger.Log($"Error checking process: {ex.Message}");
                }
            }

            return false;
        }
        private async Task StartServerAsync(ServerInfo serverInfo)
        {
            if (IsServerRunning(serverInfo))
            {
                Log("The server is already running.", LogSeverity.Info);
                // Respond back to the user if you have a way to send a response here
                return;
            }

            // Starting the server...
            string serverDirectory = serverInfo.ServerPath;
            if (!string.IsNullOrEmpty(serverDirectory) && Directory.Exists(serverDirectory))
            {
                string batchFilePath = Path.Combine(serverDirectory, "LaunchServer.bat");
                if (File.Exists(batchFilePath))
                {
                    Process.Start(batchFilePath);
                    // Wait for a short period to allow the server to start
                    await Task.Delay(TimeSpan.FromSeconds(30)); // Adjust the delay as needed

                    // Check if the server has started
                    if (IsServerRunning(serverInfo))
                    {
                        Log("Server started successfully.", LogSeverity.Info);
                        // Send response that the server has started
                    }
                    else
                    {
                        Log("Failed to start the server.", LogSeverity.Error);
                        // Send response that the server failed to start
                    }
                }
                else
                {
                    Log("Batch file not found.", LogSeverity.Error);
                    // Send response that the batch file was not found
                }
            }
            else
            {
                Log("Invalid server directory.", LogSeverity.Error);
                // Send response that the server directory is invalid
            }
        }

        // Logging method
        private void Log(string message, LogSeverity severity)
        {
            Ark_Ascended_Manager.Services.Logger.Log($"[{severity}] {message}");
            // Extend this method to log to other sources if needed
        }








        private async Task<(int rconPort, string adminPassword)> ExtractRconDetailsFromBatchFile(string serverPath)
        {
            string batchFilePath = Path.Combine(serverPath, "LaunchServer.bat");
            if (!File.Exists(batchFilePath))
            {
                Log("Batch file not found.", LogSeverity.Error);
                return (0, null); // Return default values indicating failure to find the batch file
            }

            string batchFileContent = await File.ReadAllTextAsync(batchFilePath);
            Regex rconPortRegex = new Regex(@"set RconPort=(\d+)");
            Regex adminPasswordRegex = new Regex(@"set AdminPassword=([^ \r\n]+)");

            var rconPortMatch = rconPortRegex.Match(batchFileContent);
            var adminPasswordMatch = adminPasswordRegex.Match(batchFileContent);

            if (!rconPortMatch.Success || !adminPasswordMatch.Success)
            {
                Log("Failed to extract RCON details from batch file.", LogSeverity.Error);
                return (0, null); // Return default values indicating failure to extract details
            }

            int rconPort = int.Parse(rconPortMatch.Groups[1].Value);
            string adminPassword = adminPasswordMatch.Groups[1].Value;

            return (rconPort, adminPassword);
        }

        private async Task StopServerAsync(ServerInfo serverInfo, int countdownMinutes, string reason)
        {
            if (!IsServerRunning(serverInfo))
            {
                Log("The server is not currently running.", LogSeverity.Info);
                return;
            }

            // Extract RCON details from batch file
            var (rconPort, adminPassword) = await ExtractRconDetailsFromBatchFile(serverInfo.ServerPath);

            if (rconPort == 0 || string.IsNullOrEmpty(adminPassword))
            {
                Log("Failed to extract RCON details from batch file.", LogSeverity.Error);
                return;
            }

            // Debugging: log the values before sending the command
            Log($"RCON Port: {rconPort}, Admin Password: {adminPassword}", LogSeverity.Debug);

            // Countdown logic with messages at each minute
            for (int minute = countdownMinutes; minute > 0; minute--)
            {
                string shutdownMessage = $"ServerChat Shutdown in {minute} minutes due to {reason}.";
                Log($"Sending RCON command: {shutdownMessage}", LogSeverity.Debug);
                await SendRconCommandAsync(shutdownMessage, adminPassword, rconPort);
                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            string shutdownNowMessage = $"ServerChat Server is shutting down NOW due to {reason}.";
            Log($"Sending RCON command: {shutdownNowMessage}", LogSeverity.Debug);
            await SendRconCommandAsync(shutdownNowMessage, adminPassword, rconPort);

            Log("Sending RCON command: saveworld", LogSeverity.Debug);
            await SendRconCommandAsync("saveworld", adminPassword, rconPort);

            Log("Sending RCON command: doexit", LogSeverity.Debug);
            await SendRconCommandAsync("doexit", adminPassword, rconPort);

            Log("Server shutdown process initiated.", LogSeverity.Info);
        }










        private async Task SendServerInfoAsync(SocketSlashCommand slashCommand)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string serversFilePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            if (File.Exists(serversFilePath))
            {
                string serversJson = File.ReadAllText(serversFilePath);
                var servers = JsonConvert.DeserializeObject<List<ServerInfo>>(serversJson);

                foreach (var server in servers)
                {
                    await FetchPlayerCount(server);

                    var embedBuilder = new EmbedBuilder
                    {
                        Title = "Server Information",
                        Description = $"Server Name: {server.ServerName}\nMap Name: {server.MapName}\nListen Port: {server.ListenPort}",
                        Color = Color.Blue
                    };
                    embedBuilder.AddField("Player Count", $"{_playerCount}/{server.MaxPlayerCount}", true);

                    // Create a button
                    var buttons = new ComponentBuilder()
                    .WithButton("Start Server", $"start_server_{server.ServerPath}", ButtonStyle.Primary)
                    .WithButton("Stop Server", $"stop_server_{server.ServerPath}", ButtonStyle.Danger)
                    .Build();



                    if (_lastEmbedMessageId.HasValue)
                    {
                        var channel = slashCommand.Channel as SocketTextChannel;
                        var message = await channel.GetMessageAsync(_lastEmbedMessageId.Value) as IUserMessage;
                        if (message != null)
                        {
                            await message.ModifyAsync(msg =>
                            {
                                msg.Embed = embedBuilder.Build();
                                msg.Components = buttons;
                            });
                        }
                        else
                        {
                            _lastEmbedMessageId = null;
                            var sentMessage = await channel.SendMessageAsync(
                                embed: embedBuilder.Build(),
                                components: buttons
                            );
                            _lastEmbedMessageId = sentMessage.Id;
                        }
                    }
                    else
                    {
                        var sentMessage = await slashCommand.FollowupAsync(
                            embed: embedBuilder.Build(),
                            components: buttons
                        );
                        _lastEmbedMessageId = sentMessage.Id;
                    }
                }

                // Start the timer after sending all initial embeds
                if (_playerCountTimer == null)
                {
                    currentServerInfo = servers.First(); // Assuming tracking the first server
                    int dueTime = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
                    int period = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

                    _playerCountTimer = new Timer(TimerFetchPlayerCount, currentServerInfo, dueTime, period);
                    Console.WriteLine("Player count fetching timer started.");
                    StartFetchingPlayerCount();
                }
            }
            else
            {
                await slashCommand.FollowupAsync("Server information not found.");
            }
        }
        private async Task UpdateEmbedWithPlayerCount(ServerInfo server)
        {
            if (_client.LoginState != LoginState.LoggedIn || !_shouldUpdateEmbed)
            {
                return;
            }

            ulong channelId = 1168332523483439125; // Example channel ID
            var channel = _client.GetChannel(channelId) as SocketTextChannel;
            if (channel != null && _lastEmbedMessageId.HasValue)
            {
                var message = await channel.GetMessageAsync(_lastEmbedMessageId.Value) as IUserMessage;
                if (message != null)
                {
                    // Update existing embed with new information
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Server Information")
                        .WithDescription($"Server Name: {server.ServerName}\nMap Name: {server.MapName}\nListen Port: {server.ListenPort}")
                        .AddField("Status", _isServerOnline ? "Online" : "Offline", true)
                        .AddField("Player Count", $"{_playerCount}/{server.MaxPlayerCount}", true)
                        .WithColor(Color.Blue); // Set the color

                    await message.ModifyAsync(msg => msg.Embed = embedBuilder.Build());
                }
                else
                {
                    // Send new embed if the original message was not found
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Server Information")
                        .WithDescription($"Server Name: {server.ServerName}\nMap Name: {server.MapName}\nListen Port: {server.ListenPort}")
                        .AddField("Status", _isServerOnline ? "Online" : "Offline", true)
                        .AddField("Player Count", $"{_playerCount}/{server.MaxPlayerCount}", true)
                        .WithColor(Color.Blue); // Set the color

                    var sentMessage = await channel.SendMessageAsync(embed: embedBuilder.Build());
                    _lastEmbedMessageId = sentMessage.Id;
                }
            }
        }
        public class ServerInfo
        {
            public string ServerName { get; set; }
            public string MapName { get; set; }
            public int ListenPort { get; set; }
            public string AdminPassword { get; set; }
            public int RCONPort { get; set; }
            public int MaxPlayerCount { get; set; }

            public string ServerPath { get; set; }
            // Include other properties as needed
        }









        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            if (interaction is SocketSlashCommand slashCommand)
            {
                if (slashCommand.CommandName == "showservers")
                {
                    _shouldUpdateEmbed = true; // Set flag to true to allow embed updates
                    Ark_Ascended_Manager.Services.Logger.Log("Slash command /showservers invoked. Updating embeds is now enabled.");

                    // Defer the interaction
                    await slashCommand.DeferAsync(ephemeral: false);
                    Ark_Ascended_Manager.Services.Logger.Log("Interaction deferred.");

                    // Send the server info as a follow-up message
                    await SendServerInfoAsync(slashCommand);
                }
            }
            else if (interaction is SocketMessageComponent component)
            {
                string customId = component.Data.CustomId;

                if (customId.StartsWith("start_server_"))
                {
                    string serverPath = customId.Substring("start_server_".Length);
                    ServerInfo serverInfo = new ServerInfo { ServerPath = serverPath };
                    await StartServerAsync(serverInfo);

                    // Start the player count fetching timer after the server starts
                    if (!IsServerRunning(serverInfo))
                    {
                        currentServerInfo = serverInfo; // Update the current server info
                        StartFetchingPlayerCount(); // Start the timer
                    }

                    await component.RespondAsync("Server Started!");
                }
                else if (component.Data.CustomId.StartsWith("stop_server_"))
                {
                    string serverPath = component.Data.CustomId.Substring("stop_server_".Length);
                    ServerInfo serverInfo = new ServerInfo { ServerPath = serverPath };

                    // Predefined shutdown parameters
                    int countdownMinutes = 10; // You can customize this
                    string reason = "Maintenance"; // You can customize this

                    await StopServerAsync(serverInfo, countdownMinutes, reason);
                    await component.RespondAsync("Server stopping...");
                }
            }











        }
    }
}
