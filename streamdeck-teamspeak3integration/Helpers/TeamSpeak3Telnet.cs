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

        public static async Task<int> GetClientId(Client telnetClient)
        {
            if (!await SelectCurrentServer(telnetClient)) return -1;

            if (!telnetClient.IsConnected) return -1;

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine("whoami");
                var whoAmIResponse = await telnetClient.ReadAsync();

                if (whoAmIResponse.Contains("msg=ok"))
                    return int.Parse(whoAmIResponse.Split(new[] {"clid="}, StringSplitOptions.None)[1]
                        .Split(' ')[0]
                        .Trim());

                retries++;
                await Task.Delay(10);
            }

            return -1;
        }

#region Nickname Stuff

        public static async Task<bool> ChangeNickname(Client telnetClient, string nickname)
        {
            if (!telnetClient.IsConnected) return false;

            await telnetClient.WriteLine($"clientupdate client_nickname={nickname}");
            var changeNicknameResponse = await telnetClient.ReadAsync();

            return changeNicknameResponse.Contains("msg=ok");
        }

#endregion

#region Input Stuff

        public static async Task<int> GetInputMuteStatus(Client telnetClient, int clientId)
        {
            if (!telnetClient.IsConnected) return -1;
            

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientvariable clid={clientId} client_input_muted");
                var inputMuteStatusIResponse = await telnetClient.ReadAsync();

                if (inputMuteStatusIResponse.Contains("msg=ok"))
                    return int.Parse(
                        inputMuteStatusIResponse.Split(new[] {"client_input_muted="}, StringSplitOptions.None)[1]
                            .Split('\n')[0]
                            .Trim());

                retries++;
                await Task.Delay(10);
            }

            return -1;
        }

        public static async Task<bool> SetInputMuteStatus(Client telnetClient, int inputMuteStatus)
        {
            if (!telnetClient.IsConnected) return false;

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientupdate client_input_muted={inputMuteStatus}");
                var setInputMuteStatusResponse = await telnetClient.ReadAsync();

                if (setInputMuteStatusResponse.Contains("msg=ok")) return true;

                retries++;
                await Task.Delay(10);
            }

            return false;
        }

#endregion

#region Output Stuff

        public static async Task<int> GetOutputMuteStatus(Client telnetClient, int clientId)
        {
            if (!telnetClient.IsConnected) return -1;

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientvariable clid={clientId} client_output_muted");
                var inputMuteStatusIResponse = await telnetClient.ReadAsync();

                if (inputMuteStatusIResponse.Contains("msg=ok"))
                    return int.Parse(
                        inputMuteStatusIResponse.Split(new[] {"client_output_muted="}, StringSplitOptions.None)[1]
                            .Split('\n')[0]
                            .Trim());
                
                retries++;
                await Task.Delay(10);
            }

            return -1;
        }

        public static async Task<bool> SetOutputMuteStatus(Client telnetClient, int outputMuteStatus)
        {
            if (!telnetClient.IsConnected) return false;

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientupdate client_output_muted={outputMuteStatus}");
                var setInputMuteStatusResponse = await telnetClient.ReadAsync();

                if (setInputMuteStatusResponse.Contains("msg=ok")) return true;

                retries++;
                await Task.Delay(10);
            }

            return false;
        }

#endregion

#region Away Stuff

        public static async Task<bool> SetAwayStatus(Client telnetClient, int status)
        {
            if (!telnetClient.IsConnected) return false;

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientupdate client_away={status}");
                var changeNicknameResponse = await telnetClient.ReadAsync();

                if (changeNicknameResponse.Contains("msg=ok")) return true;

                retries++;
                await Task.Delay(10);
            }

            return false;
        }

        public static async Task<int> GetAwayStatus(Client telnetClient, int clientId)
        {
            if (!telnetClient.IsConnected) return -1;

            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientvariable clid={clientId} client_away");
                var awayStatusResponse = await telnetClient.ReadAsync();

                if (awayStatusResponse.Contains("msg=ok"))
                    return int.Parse(
                        awayStatusResponse.Split(new[] {"client_away="}, StringSplitOptions.None)[1]
                            .Split('\n')[0]
                            .Trim());

                retries++;
                await Task.Delay(10);
                await Task.Delay(10);
            }

            return -1;
        }

        public static async Task<bool> SetAwayMessage(Client telnetClient, string statusMessage)
        {
            if (!telnetClient.IsConnected) return false;
            
            var retries = 0;
            while (retries < 10)
            {
                await telnetClient.WriteLine($"clientupdate client_away_message={statusMessage}");
                var changeNicknameResponse = await telnetClient.ReadAsync();

                if (changeNicknameResponse.Contains("msg=ok")) return true;

                retries++;
                await Task.Delay(10);
            }

            return false;
        }

#endregion
    }
}