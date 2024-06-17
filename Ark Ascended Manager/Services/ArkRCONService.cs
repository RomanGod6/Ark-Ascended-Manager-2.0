using CoreRCON;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Ark_Ascended_Manager.Services
{
    public class ArkRCONService : IDisposable
    {
        private RCON _rconClient;
        private readonly IPAddress _serverIP;
        private readonly ushort _serverPort;
        private readonly string _password;
        private readonly string _batFilePath;  // Store the full path to LaunchServer.bat
        private bool _isConnected = false;

        public ArkRCONService(string ip, ushort port, string password, string serverConfigPath)
        {
            _serverPort = port;
            _password = password;
            _batFilePath = Path.Combine(serverConfigPath, "LaunchServer.bat");

            // Determine the IP to use here in the constructor
            string multiHomeIP = GetMultiHomeIP();
            _serverIP = multiHomeIP != null ? IPAddress.Parse(multiHomeIP) : IPAddress.Parse("127.0.0.1");

            Logger.Log($"Initializing RCON service with IP: {_serverIP}, Port: {_serverPort}");
            if (multiHomeIP != null)
            {
                Logger.Log("MultiHome is configured. Using specified MultiHome IP.");
            }
            else
            {
                Logger.Log("MultiHome not configured. Using default IP or provided IP.");
            }
        }

        private string GetMultiHomeIP()
        {
            if (File.Exists(_batFilePath))
            {
                string batFileContent = File.ReadAllText(_batFilePath);
                var multiHomeLine = batFileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .FirstOrDefault(line => line.Contains("set MultiHome="));
                if (multiHomeLine != null)
                {
                    var parts = multiHomeLine.Split('=');
                    if (parts.Length > 1 && IPAddress.TryParse(parts[1].Trim(), out IPAddress multiHomeIP))
                    {
                        return multiHomeIP.ToString();
                    }
                }
            }
            return null;  // Return null if no MultiHome IP is set
        }

        public async Task ConnectAsync()
        {
            if (_isConnected) return;

            Logger.Log($"Attempting RCON connection to {_serverIP}:{_serverPort}");

            // Ensure _serverIP is used directly if RCON constructor expects an IPAddress
            _rconClient = new RCON(_serverIP, _serverPort, _password);
            try
            {
                await _rconClient.ConnectAsync();
                _isConnected = true;
                Logger.Log($"Successfully connected to RCON on {_serverIP}:{_serverPort}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to {_serverIP}:{_serverPort} - {ex.GetType().Name}: {ex.Message}");
                _rconClient?.Dispose();
                _isConnected = false;
                throw new InvalidOperationException("Could not connect to RCON server.");
            }
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
                    _isConnected = false;
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
                    Debug.WriteLine($"Broadcasting shutdown message: {i} minute(s) remaining.");
                    await SendCommandAsync($"ServerChat Server shutting down in {i} minute(s) due to {reason}.");
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }

                Debug.WriteLine("Sending shutdown command to RCON server.");
                await SendCommandAsync("doexit");
            }
            catch (Exception ex)
            {
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
