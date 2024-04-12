using CoreRCON;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Ark_Ascended_Manager.Services
{
    public class ArkRCONService
    {
        private RCON _rconClient;
        private readonly IPAddress _serverIP;
        private readonly ushort _serverPort;
        private readonly string _password;
        private bool _isConnected = false;

        public ArkRCONService(string ip, ushort port, string password)
        {
            _serverIP = IPAddress.Parse(ip);
            _serverPort = port;
            _password = password;
        }

        public async Task ConnectAsync()
        {
            if (_isConnected) return;

            _rconClient = new RCON(_serverIP, _serverPort, _password);
            try
            {
                await _rconClient.ConnectAsync();
                _isConnected = true;
            }
            catch (Exception ex)
            {
                // Handle connection errors
                _isConnected = false;
                throw new InvalidOperationException("Could not connect to RCON server.", ex);
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
                // If a command fails, assume the connection is broken and reset it
                _isConnected = false;
                throw;
            }
        }

        public async Task ShutdownServerAsync(int countdownMinutes, string reason)
        {
            if (!_isConnected) await ConnectAsync();

            for (int i = countdownMinutes; i > 0; i--)
            {
                await SendCommandAsync($"broadcast Server shutting down in {i} minute(s) due to {reason}.");
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            await SendCommandAsync("doexit");
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
