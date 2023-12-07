using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Ark_Ascended_Manager.Services.DiscordBotService;

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

        public DiscordBotService(IServiceProvider services, ulong guildId) // Accept Guild ID as a parameter
        {

            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _commands = new CommandService();
            _services = services;
            _guildId = guildId; // Store the Guild ID
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;


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
                    await guild.CreateApplicationCommandAsync(new SlashCommandBuilder()
                        .WithName("showservers")
                        .WithDescription("Show information about servers")
                        .Build());
                }
            };

            _client.InteractionCreated += HandleInteractionAsync;
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








        private async Task<(string, bool)> SendRconCommandAsync(string command, string adminPassword, int rconPort)
        {
            Debug.WriteLine($"{adminPassword}{rconPort}");
            string fullCommand = $"/C echo {command} | mcrcon 127.0.0.1 --password {adminPassword} -p {rconPort}";
            Debug.WriteLine("Executing RCON command: " + fullCommand);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = fullCommand,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                // Check if the specific error message is present in the output or error
                bool isServerOffline = output.Contains("The connection could not be made") || error.Contains("The connection could not be made");

                if (!string.IsNullOrEmpty(output) && !isServerOffline)
                {
                    Debug.WriteLine($"RCON command output: {output}");
                    return (output, true); // Successful output, server is online
                }

                // Specific error message or empty output indicates server is offline
                return (null, !isServerOffline);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception sending RCON command: {ex.Message}");
                return (null, false); // Exception occurred, server might be offline
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
                    Debug.WriteLine($"Error checking process: {ex.Message}");
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
            Debug.WriteLine($"[{severity}] {message}");
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
                    Debug.WriteLine("Slash command /showservers invoked. Updating embeds is now enabled.");

                    // Defer the interaction
                    await slashCommand.DeferAsync(ephemeral: false);
                    Debug.WriteLine("Interaction deferred.");

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
