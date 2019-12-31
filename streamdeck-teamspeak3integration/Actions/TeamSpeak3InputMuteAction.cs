using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

using BarRaider.SdTools;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PrimS.Telnet;

namespace ZerGo0.TeamSpeak3Integration.Actions
{
    [PluginActionId("com.zergo0.teamspeak3integration.toggleInputMute")]
    public class TeamSpeak3InputMuteAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    ApiKey = string.Empty
                };

                return instance;
            }

            [JsonProperty(PropertyName = "apiKey")]
            public string ApiKey { get; set; }
        }

        #region Private Members

        private readonly PluginSettings _settings;
        private readonly Stopwatch _stopwatch;
        private int _savedSatus;
        private Client _telnetclient;

        #endregion
        public TeamSpeak3InputMuteAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this._settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this._settings = payload.Settings.ToObject<PluginSettings>();
            }
            connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;
            
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            
            SaveSettings();
        }

        public override void Dispose()
        {
            _telnetclient?.Dispose();
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
            
            if (_telnetclient == null || !_telnetclient.IsConnected)
            {
                _telnetclient = await SetupTelnetClient();
                if (_telnetclient == null) return;
            }
            
            await ToggleMicMute(_telnetclient);
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (_stopwatch.ElapsedMilliseconds <= 50) return;
            _stopwatch.Restart();

            try
            {
                if (_telnetclient == null || !_telnetclient.IsConnected)
                {
                    _telnetclient = await SetupTelnetClient();
                    if (_telnetclient == null) return;
                }

                var clientId = await GetClientId(_telnetclient);
                if (clientId == null) return;

                var inputMuteStatus = await GetInputMuteStatus(_telnetclient, clientId);
                if (inputMuteStatus == _savedSatus)
                {
                    await SetInputStatusImage(inputMuteStatus);
                    return;
                }
                switch (inputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        await SetInputStatusImage(0);
                        break;
                    case 1:
                        await SetInputStatusImage(1);
                        break;
                }
            
                _savedSatus = inputMuteStatus;
            }
            catch (Exception)
            {
                _telnetclient?.Dispose();
                //await SetInputStatusImage(0);
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods


        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private async void StreamDeckConnection_OnSendToPlugin(object sender, streamdeck_client_csharp.StreamDeckEventReceivedEventArgs<streamdeck_client_csharp.Events.SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context)
            {
                return;
            }
        }

        private async Task<Client> SetupTelnetClient()
        {
            Client client;
            try
            {
                client = new Client("127.0.0.1", 25639, new System.Threading.CancellationToken());
            }
            catch (SocketException)
            {
                return null;
            }
            if (!client.IsConnected) return null;

            var welcomeMessage = await client.ReadAsync();
            if (!welcomeMessage.Contains("TS3 Client")) return null;

            if (!await AuthenticateTelnet(client)) return null;

            return client;
        }

        private async Task ToggleMicMute(Client telnetClient)
        {
            try
            {
                var clientId = await GetClientId(telnetClient);
                if (clientId == null)
                {
                    telnetClient.Dispose();
                    return;
                }

                var outputMuteStatus = await GetInputMuteStatus(telnetClient, clientId);
                var setOutputMuteStatus = false;
                switch (outputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        setOutputMuteStatus = await SetInputMuteStatus(telnetClient, "1");
                        await SetInputStatusImage(1);
                        break;
                    case 1:
                        setOutputMuteStatus = await SetInputMuteStatus(telnetClient, "0");
                        await SetInputStatusImage(0);
                        break;
                }
            
                if (!setOutputMuteStatus) return;
            }
            catch (Exception)
            {
                _telnetclient?.Dispose();
                //await SetInputStatusImage(0);
            }
        }

        private async Task<bool> AuthenticateTelnet(Client telnetClient)
        {
            if (!telnetClient.IsConnected) return false;
            await telnetClient.WriteLine($"auth apikey={_settings.ApiKey}");
            var authResponse = await telnetClient.ReadAsync();
            
            return authResponse.Contains("msg=ok");
        }

        private async Task<string> GetClientId(Client telnetClient)
        {
            if (!telnetClient.IsConnected) return null;
            await telnetClient.WriteLine("whoami");
            var whoAmIResponse = await telnetClient.ReadAsync();
            
            if (whoAmIResponse.Contains("msg=ok"))
                return whoAmIResponse.Split(new string[] {"clid="},StringSplitOptions.None)[1]
                    .Split(' ')[0]
                    .Trim();
            
            return null;
        }

        private async Task<int> GetInputMuteStatus(Client telnetClient, string clientId)
        {
            if (!telnetClient.IsConnected) return -1;
            await telnetClient.WriteLine($"clientvariable clid={clientId} client_input_muted");
            var inputMuteStatusIResponse = await telnetClient.ReadAsync();
            
            if (inputMuteStatusIResponse.Contains("msg=ok"))
                return int.Parse(inputMuteStatusIResponse.Split(new string[] {"client_input_muted="},StringSplitOptions.None)[1]
                    .Split('\n')[0]
                    .Trim());
            
            return -1;
        }

        private async Task<bool> SetInputMuteStatus(Client telnetClient, string inputMuteStatus)
        {
            if (!telnetClient.IsConnected) return false;
            await telnetClient.WriteLine($"clientupdate client_input_muted={inputMuteStatus}");
            var setInputMuteStatusResponse = await telnetClient.ReadAsync();

            return setInputMuteStatusResponse.Contains("msg=ok");
        }

        private async Task SetInputStatusImage(int muted = 0)
        {
            switch (muted)
            {
                case 0:
                    await Connection.StreamDeckConnection.SetStateAsync(0, Connection.ContextId);
                    //await Connection.SetStateAsync(0);
                    break;
                case 1:
                    await Connection.StreamDeckConnection.SetStateAsync(1, Connection.ContextId);
                    //await Connection.SetStateAsync(1);
                    break;
            }
        }

        #endregion
    }
}