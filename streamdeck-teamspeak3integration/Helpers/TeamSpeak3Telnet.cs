using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using PrimS.Telnet;

namespace ZerGo0.TeamSpeak3Integration.Helpers
{
    internal static class TeamSpeak3Telnet
    {
        public static Client TS3_CLIENT;
        private static readonly object ro_TS3_CLIENT_LOCK_OBJ = new object();

        public static void SetupTelnetClient(string apiKey)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (TS3_CLIENT != null && TS3_CLIENT.IsConnected) return;

                try
                {
                    TS3_CLIENT = new Client("127.0.0.1", 25639, new CancellationToken());
                }
                catch (SocketException)
                {
                    TS3_CLIENT = null;
                    return;
                }

                if (!TS3_CLIENT.IsConnected)
                {
                    TS3_CLIENT = null;
                    return;
                }

                var welcomeMessage = TS3_CLIENT.ReadAsync().Result;
                if (!welcomeMessage.Contains("TS3 Client"))
                {
                    TS3_CLIENT = null;
                    return;
                }

                if (!AuthenticateTelnet(apiKey)) TS3_CLIENT = null;
            }
        }

        private static bool AuthenticateTelnet(string apiKey)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                TS3_CLIENT.WriteLine($"auth apikey={apiKey}");
                var authResponse = TS3_CLIENT.ReadAsync().Result;

                return authResponse.Contains("msg=ok");
            }
        }

        private static bool SelectCurrentServer()
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                TS3_CLIENT.WriteLine("use");
                var useResponse = TS3_CLIENT.ReadAsync().Result;

                return useResponse.Contains("msg=ok");
            }
        }

        public static int GetClientId()
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!SelectCurrentServer()) return -1;

                if (!TS3_CLIENT.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine("whoami");
                    var whoAmIResponse = TS3_CLIENT.ReadAsync().Result;

                    if (whoAmIResponse.Contains("msg=ok"))
                        return int.Parse(whoAmIResponse.Split(new[] { "clid=" }, StringSplitOptions.None)[1]
                            .Split(' ')[0]
                            .Trim());

                    retries++;
                }

                return -1;
            }
        }

        private static string Substring(this string @this, string from = null, string until = null,
            StringComparison comparison = StringComparison.InvariantCulture)
        {
            var fromLength = (from ?? string.Empty).Length;
            var startIndex = !string.IsNullOrEmpty(from)
                ? @this.IndexOf(from, comparison) + fromLength
                : 0;

            if (startIndex < fromLength)
                throw new ArgumentException("from: Failed to find an instance of the first anchor");

            var endIndex = !string.IsNullOrEmpty(until)
                ? @this.IndexOf(until, startIndex, comparison)
                : @this.Length;

            if (endIndex < 0) throw new ArgumentException("until: Failed to find an instance of the last anchor");

            var subString = @this.Substring(startIndex, endIndex - startIndex);
            return subString;
        }

        private static string FixTs3SpecificChars(this string @this)
        {
            var fixedString = @this
                .Replace(" ", "\\s")
                .Replace("|", "\\p");

            return fixedString;
        }

        #region Nickname Stuff

        public static bool ChangeNickname(string nickname)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                if (string.IsNullOrWhiteSpace(nickname)) return false;

                nickname = nickname.FixTs3SpecificChars();

                TS3_CLIENT.WriteLine($"clientupdate client_nickname={nickname}");
                var changeNicknameResponse = TS3_CLIENT.ReadAsync().Result;

                return changeNicknameResponse.Contains("msg=ok");
            }
        }

        #endregion

        #region Channel Stuff

        public static bool ChannelSwitch(string channelName, int clientId)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                if (string.IsNullOrWhiteSpace(channelName)) return false;

                channelName = channelName.FixTs3SpecificChars();

                var channelList = GetChannelList();

                if (string.IsNullOrWhiteSpace(channelName)) return false;

                var channelListArray = channelList.Split('|');

                var channel = channelListArray.FirstOrDefault(tempChannel =>
                    tempChannel.Substring("channel_name=", " channel")
                        .FixTs3SpecificChars().Contains(channelName) ||
                    tempChannel.Substring("cid=", " pid")
                        .FixTs3SpecificChars().Contains(channelName));

                if (string.IsNullOrWhiteSpace(channel)) return false;

                var channelId = channel.Substring("cid=", " pid=");

                if (string.IsNullOrWhiteSpace(channelId)) return false;

                TS3_CLIENT.WriteLine($"clientmove cid={channelId} clid={clientId}");
                var channelSwitchResponse = TS3_CLIENT.ReadAsync().Result;

                return channelSwitchResponse.Contains("msg=ok");
            }
        }

        private static string GetChannelList()
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return null;

                TS3_CLIENT.WriteLine("channellist");
                var channeListResponse = TS3_CLIENT.ReadAsync().Result;

                return channeListResponse.Contains("msg=ok") ? channeListResponse : null;
            }
        }

        #endregion

        #region Input Stuff

        public static int GetInputMuteStatus(int clientId)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientvariable clid={clientId} client_input_muted");
                    var inputMuteStatusIResponse = TS3_CLIENT.ReadAsync().Result;

                    if (inputMuteStatusIResponse.Contains("msg=ok"))
                        return int.Parse(
                            inputMuteStatusIResponse.Split(new[] { "client_input_muted=" }, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetInputMuteStatus(int inputMuteStatus)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientupdate client_input_muted={inputMuteStatus}");
                    var setInputMuteStatusResponse = TS3_CLIENT.ReadAsync().Result;

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
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientvariable clid={clientId} client_output_muted");
                    var inputMuteStatusIResponse = TS3_CLIENT.ReadAsync().Result;

                    if (inputMuteStatusIResponse.Contains("msg=ok"))
                        return int.Parse(
                            inputMuteStatusIResponse.Split(new[] { "client_output_muted=" }, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetOutputMuteStatus(int outputMuteStatus)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientupdate client_output_muted={outputMuteStatus}");
                    var setInputMuteStatusResponse = TS3_CLIENT.ReadAsync().Result;

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
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientupdate client_away={status}");
                    var changeNicknameResponse = TS3_CLIENT.ReadAsync().Result;

                    if (changeNicknameResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

        public static bool SetGlobalAwayStatus(int status)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    bool run_succesfull = true;
                    TS3_CLIENT.WriteLine($"serverconnectionhandlerlist");
                    var connectionHanderList = TS3_CLIENT.ReadAsync().Result.Split('\n')[0];

                    if(connectionHanderList.Contains('|')) {
                        foreach (var connectionHander in connectionHanderList.Split('|'))
                        {
                            TS3_CLIENT.WriteLine($"use {connectionHandler}");
                            var changeConnectionHandlerResponse = TS3_CLIENT.ReadAsync().Result;
                            if (!changeConnectionHandlerResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                                break;
                            }
                            TS3_CLIENT.WriteLine($"clientupdate client_away={status}");
                            var changeAwayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                            if (!changeAwayStatusResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                                break;
                            }
                        }
                    }else {
                            TS3_CLIENT.WriteLine($"clientupdate client_away={status}");
                            var changeAwayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                            if (!changeAwayStatusResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                            }
                    }

                    if(run_succesfull)
                        return true;
                    retries++;
                }

                return false;
            }
        }

        public static bool SetGlobalAwayMessage(string statusMessage)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                if (!string.IsNullOrWhiteSpace(statusMessage))
                    statusMessage = statusMessage.FixTs3SpecificChars();

                var retries = 0;
                while (retries < 10)
                {
                    bool run_succesfull = true;
                    TS3_CLIENT.WriteLine($"serverconnectionhandlerlist");
                    var connectionHanderList = TS3_CLIENT.ReadAsync().Result.Split('\n')[0];

                    if(connectionHanderList.Contains('|')) {
                        foreach (var connectionHander in connectionHanderList.Split('|'))
                        {
                            TS3_CLIENT.WriteLine($"use {connectionHandler}");
                            var changeConnectionHandlerResponse = TS3_CLIENT.ReadAsync().Result;
                            if (!changeConnectionHandlerResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                                break;
                            }
                            TS3_CLIENT.WriteLine($"client_away_message client_away_message={statusMessage}");
                            var changeAwayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                            if (!changeAwayStatusResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                                break;
                            }
                        }
                    }else {
                            TS3_CLIENT.WriteLine($"client_away_message client_away_message={statusMessage}");
                            var changeAwayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                            if (!changeAwayStatusResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                            }
                    }

                    if(run_succesfull)
                        return true;
                    retries++;
                }

                return false;
            }
        }

        public static int GetGlobalAwayStatus(int clientId)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    bool run_succesfull = true;
                    TS3_CLIENT.WriteLine($"serverconnectionhandlerlist");
                    var connectionHanderList = TS3_CLIENT.ReadAsync().Result.Split('\n')[0];

                    if(connectionHanderList.Contains('|')) {
                        foreach (var connectionHander in connectionHanderList.Split('|'))
                        {
                            TS3_CLIENT.WriteLine($"use {connectionHandler}");
                            var changeConnectionHandlerResponse = TS3_CLIENT.ReadAsync().Result;
                            if (!changeConnectionHandlerResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                                break;
                            }
                            TS3_CLIENT.WriteLine($"clientvariable clid={clientId} client_away");
                            var awayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                            if (!awayStatusResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                                break;
                            }

                            var status = int.Parse(
                                awayStatusResponse.Split(new[] { "client_away=" }, StringSplitOptions.None)[1]
                                    .Split('\n')[0]
                                    .Trim());
                            
                            if(status == 0){
                                return status;
                            }
                        }
                    }else {
                            TS3_CLIENT.WriteLine($"clientvariable clid={clientId} client_away");
                            var awayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                            if (!awayStatusResponse.Contains("msg=ok")) {
                                run_succesfull = false;
                            }else {
                                var status = int.Parse(
                                    awayStatusResponse.Split(new[] { "client_away=" }, StringSplitOptions.None)[1]
                                        .Split('\n')[0]
                                        .Trim());
                                
                                return status;
                            }
                    }

                    if(run_succesfull)
                        return 1;
                    retries++;
                }

                return -1;
            }
        }

        public static int GetAwayStatus(int clientId)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientvariable clid={clientId} client_away");
                    var awayStatusResponse = TS3_CLIENT.ReadAsync().Result;

                    if (awayStatusResponse.Contains("msg=ok"))
                        return int.Parse(
                            awayStatusResponse.Split(new[] { "client_away=" }, StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetAwayMessage(string statusMessage)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                if (!string.IsNullOrWhiteSpace(statusMessage))
                    statusMessage = statusMessage.FixTs3SpecificChars();

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientupdate client_away_message={statusMessage}");
                    var changeNicknameResponse = TS3_CLIENT.ReadAsync().Result;

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
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return -1;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientvariable clid={clientId} client_input_deactivated");
                    var inputMuteStatusIResponse = TS3_CLIENT.ReadAsync().Result;

                    if (inputMuteStatusIResponse.Contains("msg=ok"))
                        return int.Parse(
                            inputMuteStatusIResponse.Split(new[] { "client_input_deactivated=" },
                                    StringSplitOptions.None)[1]
                                .Split('\n')[0]
                                .Trim());

                    retries++;
                }

                return -1;
            }
        }

        public static bool SetInputMuteStatusLocally(int inputMuteStatus)
        {
            lock (ro_TS3_CLIENT_LOCK_OBJ)
            {
                if (!TS3_CLIENT.IsConnected) return false;

                var retries = 0;
                while (retries < 10)
                {
                    TS3_CLIENT.WriteLine($"clientupdate client_input_deactivated={inputMuteStatus}");
                    var setInputMuteStatusResponse = TS3_CLIENT.ReadAsync().Result;

                    if (setInputMuteStatusResponse.Contains("msg=ok")) return true;

                    retries++;
                }

                return false;
            }
        }

        #endregion
    }
}