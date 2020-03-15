using System;
using System.Net.Sockets;
using System.Threading;

using PrimS.Telnet;

namespace ZerGo0.TeamSpeak3Integration.Helpers
{
    internal static class TeamSpeak3Telnet
    {
        public static Client Ts3Client;
        private static readonly object _TS3_CLIENT_LOCK_OBJ = new object();
        public static void SetupTelnetClient(string apiKey)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (Ts3Client != null && Ts3Client.IsConnected) return;
                
                try
                {
                    Ts3Client = new Client("127.0.0.1", 25639, new CancellationToken());
                }
                catch (SocketException)
                {
                    Ts3Client = null;
                    return;
                }

                if (!Ts3Client.IsConnected)
                {
                    Ts3Client = null;
                    return;
                }

                var welcomeMessage = Ts3Client.ReadAsync().Result;
                if (!welcomeMessage.Contains("TS3 Client")) 
                {
                    Ts3Client = null;
                    return;
                }

                if (!AuthenticateTelnet(apiKey)) Ts3Client = null;
            }
        }

        private static bool AuthenticateTelnet(string apiKey)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;

                Ts3Client.WriteLine($"auth apikey={apiKey}");
                var authResponse = Ts3Client.ReadAsync().Result;

                return authResponse.Contains("msg=ok");
            }
        }

        private static bool SelectCurrentServer()
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;

                Ts3Client.WriteLine("use");
                var useResponse = Ts3Client.ReadAsync().Result;

                return useResponse.Contains("msg=ok");
            }
        }

        public static int GetClientId()
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!SelectCurrentServer()) return -1;

                if (!Ts3Client.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine("whoami");
                    var whoAmIResponse = Ts3Client.ReadAsync().Result;

                    if (whoAmIResponse.Contains("msg=ok"))
                        return int.Parse(whoAmIResponse.Split(new[] {"clid="}, StringSplitOptions.None)[1]
                            .Split(' ')[0]
                            .Trim());

                    retries++;
                }

                return -1;
            }
        }

#region Nickname Stuff

        public static bool ChangeNickname(string nickname)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;
                
                if (!string.IsNullOrWhiteSpace(nickname))
                    nickname = nickname.Replace(" ", "\\s");

                Ts3Client.WriteLine($"clientupdate client_nickname={nickname}");
                var changeNicknameResponse = Ts3Client.ReadAsync().Result;

                return changeNicknameResponse.Contains("msg=ok");
            }
        }

#endregion

#region Input Stuff

        public static int GetInputMuteStatus(int clientId)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return -1;
                
                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientvariable clid={clientId} client_input_muted");
                    var inputMuteStatusIResponse = Ts3Client.ReadAsync().Result;

                    if (inputMuteStatusIResponse.Contains("msg=ok"))
                        return int.Parse(
                            inputMuteStatusIResponse.Split(new[] {"client_input_muted="}, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetInputMuteStatus(int inputMuteStatus)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientupdate client_input_muted={inputMuteStatus}");
                    var setInputMuteStatusResponse = Ts3Client.ReadAsync().Result;

                    if (setInputMuteStatusResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

#endregion

#region Output Stuff

        public static int GetOutputMuteStatus(int clientId)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientvariable clid={clientId} client_output_muted");
                    var inputMuteStatusIResponse = Ts3Client.ReadAsync().Result;

                    if (inputMuteStatusIResponse.Contains("msg=ok"))
                        return int.Parse(
                            inputMuteStatusIResponse.Split(new[] {"client_output_muted="}, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());
                
                    retries++;
                }

                return -1;
            }
        }

        public static bool SetOutputMuteStatus(int outputMuteStatus)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientupdate client_output_muted={outputMuteStatus}");
                    var setInputMuteStatusResponse = Ts3Client.ReadAsync().Result;

                    if (setInputMuteStatusResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

#endregion

#region Away Stuff

        public static bool SetAwayStatus(int status)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientupdate client_away={status}");
                    var changeNicknameResponse = Ts3Client.ReadAsync().Result;

                    if (changeNicknameResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

        public static int GetAwayStatus(int clientId)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientvariable clid={clientId} client_away");
                    var awayStatusResponse = Ts3Client.ReadAsync().Result;

                    if (awayStatusResponse.Contains("msg=ok"))
                        return int.Parse(
                            awayStatusResponse.Split(new[] {"client_away="}, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetAwayMessage(string statusMessage)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;
                

                if (!string.IsNullOrWhiteSpace(statusMessage))
                    statusMessage = statusMessage.Replace(" ", "\\s");
            
                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientupdate client_away_message={statusMessage}");
                    var changeNicknameResponse = Ts3Client.ReadAsync().Result;

                    if (changeNicknameResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

#endregion

#region Input Local Stuff

        //TODO: Check how to actually call this, no public information found sadly, doesn't work currently
        public static int GetInputMuteStatusLocally(int clientId)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return -1;
                
                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientvariable clid={clientId} client_input_deactivated");
                    var inputMuteStatusIResponse = Ts3Client.ReadAsync().Result;

                    if (inputMuteStatusIResponse.Contains("msg=ok"))
                        return int.Parse(
                            inputMuteStatusIResponse.Split(new[] {"client_input_deactivated="}, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetInputMuteStatusLocally(int inputMuteStatus)
        {
            lock (_TS3_CLIENT_LOCK_OBJ)
            {
                if (!Ts3Client.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    Ts3Client.WriteLine($"clientupdate client_input_deactivated={inputMuteStatus}");
                    var setInputMuteStatusResponse = Ts3Client.ReadAsync().Result;

                    if (setInputMuteStatusResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

#endregion
    }
}