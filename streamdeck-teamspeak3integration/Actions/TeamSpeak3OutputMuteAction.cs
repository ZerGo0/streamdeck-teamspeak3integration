using System;
using System.Threading.Tasks;

using BarRaider.SdTools;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PrimS.Telnet;

using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;

using ZerGo0.TeamSpeak3Integration.Helpers;

using KeyPayload = BarRaider.SdTools.KeyPayload;

namespace ZerGo0.TeamSpeak3Integration.Actions
{
    [PluginActionId("com.zergo0.teamspeak3integration.toggleoutputmute")]
    public class TeamSpeak3OutputMuteAction : PluginBase
    {
        public TeamSpeak3OutputMuteAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
                _settings = PluginSettings.CreateDefaultSettings();
            else
                _settings = payload.Settings.ToObject<PluginSettings>();
            connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;

            SaveSettings();
        }

        public override void Dispose()
        {
            _telnetclient?.Dispose();
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            try
            {
                if (_telnetclient == null || !_telnetclient.IsConnected)
                {
                    _telnetclient = await TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                    if (_telnetclient == null) return;
                }

                if (payload.IsInMultiAction)
                    await ToggleOutputMute(_telnetclient, (int) payload.UserDesiredState);
                else
                    await ToggleOutputMute(_telnetclient);
            }
            catch (Exception e)
            {
                _telnetclient?.Dispose();
                _telnetclient = null;
                await SetOutputStatusImage();
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
            try
            {
                if (_telnetclient == null || !_telnetclient.IsConnected)
                {
                    _telnetclient = await TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                    if (_telnetclient == null) return;
                }

                var clientId = await TeamSpeak3Telnet.GetClientId(_telnetclient);
                if (clientId == null) return;

                var outputMuteStatus = await TeamSpeak3Telnet.GetOutputMuteStatus(_telnetclient, clientId);
                if (outputMuteStatus == _savedSatus)
                {
                    await SetOutputStatusImage(outputMuteStatus);
                    return;
                }

                switch (outputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        await SetOutputStatusImage();
                        break;
                    case 1:
                        await SetOutputStatusImage(1);
                        break;
                }

                _savedSatus = outputMuteStatus;
            }
            catch (Exception)
            {
                _telnetclient?.Dispose();
                _telnetclient = null;
                await SetOutputStatusImage();
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        private class PluginSettings
        {
            [JsonProperty(PropertyName = "apiKey")]
            public string ApiKey { get; set; }

            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings
                {
                    ApiKey = string.Empty
                };

                return instance;
            }
        }

#region Private Members

        private readonly PluginSettings _settings;
        private int _savedSatus;
        private Client _telnetclient;

#endregion

#region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private async void StreamDeckConnection_OnSendToPlugin(object sender,
            StreamDeckEventReceivedEventArgs<SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context) return;
        }

        private async Task ToggleOutputMute(Client telnetClient, int desiredState = -1)
        {
            try
            {
                var clientId = await TeamSpeak3Telnet.GetClientId(telnetClient);
                if (clientId == null)
                {
                    telnetClient.Dispose();
                    return;
                }

                int outputMuteStatus;
                if (desiredState == -1)
                    outputMuteStatus = await TeamSpeak3Telnet.GetOutputMuteStatus(telnetClient, clientId);
                else
                    outputMuteStatus = desiredState == 1 ? 0 : 1;

                var setOutputMuteStatus = false;
                switch (outputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        setOutputMuteStatus = await TeamSpeak3Telnet.SetOutputMuteStatus(telnetClient, "1");
                        await SetOutputStatusImage(1);
                        break;
                    case 1:
                        setOutputMuteStatus = await TeamSpeak3Telnet.SetOutputMuteStatus(telnetClient, "0");
                        await SetOutputStatusImage();
                        break;
                }

                if (!setOutputMuteStatus) return;
            }
            catch (Exception)
            {
                _telnetclient?.Dispose();
                _telnetclient = null;
                await SetOutputStatusImage();
            }
        }

        private async Task SetOutputStatusImage(int muted = 0)
        {
            switch (muted)
            {
                case 0:
                    await Connection.SetStateAsync(0);
                    break;
                case 1:
                    await Connection.SetStateAsync(1);
                    break;
            }
        }

#endregion
    }
}