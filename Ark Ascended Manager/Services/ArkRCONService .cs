/*using CoreRCON;
using CoreRCON.Parsers.Standard;
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

        public ArkRCONService(string ip, ushort port, string password)
        {
            _serverIP = IPAddress.Parse(ip);
            _serverPort = port;
            _password = password;
        }

        public async Task ConnectAsync()
        {
            _rconClient = new RCON(_serverIP, _serverPort, _password);
            await _rconClient.ConnectAsync();
        }

        public async Task<string> SendCommandAsync(string command)
        {
            if (_rconClient == null || !_rconClient.IsConnected)
            {
                throw new InvalidOperationException("Not connected to RCON server.");
            }
            return await _rconClient.SendCommandAsync(command);
        }

        public async Task StartServerAsync()
        {
            // Command to start the server (replace "StartServerCommand" with the actual command)
            await SendCommandAsync("StartServerCommand");
        }

        public async Task StopServerAsync(int warningTimeInMinutes)
        {
            // Broadcast shutdown warning messages based on warningTimeInMinutes
            for (int i = warningTimeInMinutes; i > 0; i--)
            {
                await SendCommandAsync($"broadcast Server shutting down in {i} minute(s)");
                // Wait for 1 minute before sending the next message
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            // Command to stop the server (replace "StopServerCommand" with the actual command)
            await SendCommandAsync("StopServerCommand");
        }

        public async Task ScheduleCommandAsync(string command, TimeSpan delay)
        {
            // Delay the command execution by the specified delay
            await Task.Delay(delay);
            await SendCommandAsync(command);
        }

        // Additional methods for your specific functionalities, like Scheduled Commands, Cross Chat, etc.

        // Ensure to properly disconnect and clean up resources
        public void Dispose()
        {
            _rconClient?.Disconnect();
            _rconClient = null;
        }
    }
}
*/