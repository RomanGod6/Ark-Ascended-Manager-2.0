using CoreRCON;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
namespace Ark_Ascended_Manager.Services
{
    public class ArkRCONService
    {
        private RCON _rconClient;
        private readonly IPAddress _serverIP;
        private readonly ushort _serverPort;
        private readonly string _password;
        private bool _isConnected = false;
        private readonly bool _tryLocalhostFirst;

        public ArkRCONService(string ip, ushort port, string password, bool tryLocalhostFirst = true)
        {
            _serverIP = IPAddress.Parse(ip);
            _serverPort = port;
            _password = password;
            _tryLocalhostFirst = tryLocalhostFirst;
        }

        public async Task ConnectAsync()
        {
            if (_isConnected) return;

            IPAddress[] addressesToTry = _tryLocalhostFirst
                ? new[] { IPAddress.Parse("127.0.0.1"), _serverIP }
                : new[] { _serverIP, IPAddress.Parse("127.0.0.1") };

            foreach (var address in addressesToTry)
            {
                Logger.LogInfoToDiscord($"Attempting RCON connection to {address}:{_serverPort}");

                _rconClient = new RCON(address, _serverPort, _password);
                try
                {
                    await _rconClient.ConnectAsync();
                    await _rconClient.SendCommandAsync("listplayers");
                    _isConnected = true;
                    Logger.LogInfoToDiscord($"Successfully connected to RCON on {address}:{_serverPort}");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogInfoToDiscord($"Failed to connect to {address}:{_serverPort} - {ex.GetType().Name}: {ex.Message}");
                    _rconClient?.Dispose(); // Dispose before the next attempt
                }
            }

            _isConnected = false;
            Logger.LogInfoToDiscord("Could not connect to RCON server on any address.");
            throw new InvalidOperationException("Could not connect to RCON server on any address.");
        }


        public async Task<string> SendCommandAsync(string command)
        {
            if (!_isConnected)
                await ConnectAsync();

            try
            {
                return await _rconClient.SendCommandAsync(command);
            }
            catch (Exception)
            {
                // Consider retrying the command a certain number of times before failing
                _isConnected = false;
                throw;
            }
        }

        public async Task SendServerChatAsync(string message)
        {
            if (_isConnected)
            {
                try
                {
                    await _rconClient.SendCommandAsync($"serverchat {message}");
                }
                catch (Exception ex)
                {
                    _isConnected = false; // Assume connection might be lost on error
                    throw new InvalidOperationException("Failed to send RCON command: " + ex.Message);
                }
            }
            else
            {
                throw new InvalidOperationException("Not connected to RCON server.");
            }
        }

        public async Task ShutdownServerAsync(int countdownMinutes, string reason)
        {
            try
            {
                if (!_isConnected)
                {
                    await ConnectAsync();
                    Debug.WriteLine("Connected to RCON server.");
                }

                for (int i = countdownMinutes; i > 0; i--)
                {
                    // Log the attempt to send the broadcast message
                    Debug.WriteLine($"Broadcasting shutdown message: {i} minute(s) remaining.");
                    await SendCommandAsync($"ServerChat Server shutting down in {i} minute(s) due to {reason}.");
                    await Task.Delay(TimeSpan.FromMinutes(1)); // Consider using a CancellationToken here
                }

                // Log the attempt to send the shutdown command
                Debug.WriteLine("Sending shutdown command to RCON server.");
                await SendCommandAsync("doexit");
            }
            catch (Exception ex)
            {
                // Log the exception and throw it to be handled further up the call stack if necessary
                Debug.WriteLine($"An error occurred during shutdown: {ex.Message}");
                throw;
            }
        }


        public async Task SaveWorldAsync()
        {
            if (!_isConnected) await ConnectAsync();
            await SendCommandAsync("saveworld");
        }

        public async Task<string> ListPlayersAsync()
        {
            if (!_isConnected) await ConnectAsync();
            return await SendCommandAsync("listplayers");
        }

        public async Task<string> GetServerChatAsync()
        {
            if (!_isConnected) await ConnectAsync();
            return await SendCommandAsync("getchat");
        }

        public void Dispose()
        {
            _rconClient?.Dispose();
            _isConnected = false;
        }
    }
}
