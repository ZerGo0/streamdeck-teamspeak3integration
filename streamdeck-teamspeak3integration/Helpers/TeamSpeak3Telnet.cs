using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using PrimS.Telnet;

namespace ZerGo0.TeamSpeak3Integration.Helpers
{
    internal static class TeamSpeak3Telnet
    {
        public static async Task<Client> SetupTelnetClient(string apiKey)
        {
            Client client;
            try
            {
                client = new Client("127.0.0.1", 25639, new CancellationToken());
            }
            catch (SocketException)
            {
                return null;
            }

            if (!client.IsConnected) return null;

            var welcomeMessage = await client.ReadAsync();
            if (!welcomeMessage.Contains("TS3 Client")) return null;

            if (!await AuthenticateTelnet(client, apiKey)) return null;

            return client;
        }

        private static async Task<bool> AuthenticateTelnet(Client telnetClient, string apiKey)
        {
            if (!telnetClient.IsConnected) return false;

            await telnetClient.WriteLine($"auth apikey={apiKey}");
            var authResponse = await telnetClient.ReadAsync();

            return authResponse.Contains("msg=ok");
        }

        private static async Task<bool> SelectCurrentServer(Client telnetClient)
        {
            if (!telnetClient.IsConnected) return false;

            await telnetClient.WriteLine("use");
            var useResponse = await telnetClient.ReadAsync();

            return useResponse.Contains("msg=ok");
        }

        public static async Task<string> GetClientId(Client telnetClient)
        {
            if (!await SelectCurrentServer(telnetClient)) return null;

            if (!telnetClient.IsConnected) return null;
            await telnetClient.WriteLine("whoami");
            var whoAmIResponse = await telnetClient.ReadAsync();

            if (whoAmIResponse.Contains("msg=ok"))
                return whoAmIResponse.Split(new[] {"clid="}, StringSplitOptions.None)[1]
                    .Split(' ')[0]
                    .Trim();

            return null;
        }

#region Input Stuff

        public static async Task<int> GetInputMuteStatus(Client telnetClient, string clientId)
        {
            if (!telnetClient.IsConnected) return -1;
            await telnetClient.WriteLine($"clientvariable clid={clientId} client_input_muted");
            var inputMuteStatusIResponse = await telnetClient.ReadAsync();

            if (inputMuteStatusIResponse.Contains("msg=ok"))
                return int.Parse(
                    inputMuteStatusIResponse.Split(new[] {"client_input_muted="}, StringSplitOptions.None)[1]
                        .Split('\n')[0]
                        .Trim());

            return -1;
        }

        public static async Task<bool> SetInputMuteStatus(Client telnetClient, string inputMuteStatus)
        {
            if (!telnetClient.IsConnected) return false;
            await telnetClient.WriteLine($"clientupdate client_input_muted={inputMuteStatus}");
            var setInputMuteStatusResponse = await telnetClient.ReadAsync();

            return setInputMuteStatusResponse.Contains("msg=ok");
        }

#endregion

#region Output Stuff

        public static async Task<int> GetOutputMuteStatus(Client telnetClient, string clientId)
        {
            if (!telnetClient.IsConnected) return -1;
            await telnetClient.WriteLine($"clientvariable clid={clientId} client_output_muted");
            var inputMuteStatusIResponse = await telnetClient.ReadAsync();

            if (inputMuteStatusIResponse.Contains("msg=ok"))
                return int.Parse(
                    inputMuteStatusIResponse.Split(new[] {"client_output_muted="}, StringSplitOptions.None)[1]
                        .Split('\n')[0]
                        .Trim());

            return -1;
        }

        public static async Task<bool> SetOutputMuteStatus(Client telnetClient, string inputMuteStatus)
        {
            if (!telnetClient.IsConnected) return false;
            await telnetClient.WriteLine($"clientupdate client_output_muted={inputMuteStatus}");
            var setInputMuteStatusResponse = await telnetClient.ReadAsync();

            return setInputMuteStatusResponse.Contains("msg=ok");
        }

#endregion
    }
}