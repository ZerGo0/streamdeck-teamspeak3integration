using System;
using System.Threading.Tasks;

using BarRaider.SdTools;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;

using ZerGo0.TeamSpeak3Integration.Helpers;

using KeyPayload = BarRaider.SdTools.KeyPayload;

namespace ZerGo0.TeamSpeak3Integration.Actions
{
    [PluginActionId("com.zergo0.teamspeak3integration.toggleafkstatus")]
    public class TeamSpeak3AfkStatusAction : PluginBase
    {
        public TeamSpeak3AfkStatusAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            try
            {
                if (TeamSpeak3Telnet.TS3_CLIENT == null || !TeamSpeak3Telnet.TS3_CLIENT.IsConnected)
                {
                    TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                    if (TeamSpeak3Telnet.TS3_CLIENT == null) return;
                }

                if (payload.IsInMultiAction)
                    await ToggleAwayStatus((int) payload.UserDesiredState);
                else
                    await ToggleAwayStatus();
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
                await SetAwayStatusState();
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
            try
            {
                if (TeamSpeak3Telnet.TS3_CLIENT == null || !TeamSpeak3Telnet.TS3_CLIENT.IsConnected)
                {
                    TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                    if (TeamSpeak3Telnet.TS3_CLIENT == null) return;
                }

                var clientId = TeamSpeak3Telnet.GetClientId();
                if (clientId == -1)
                {
                    TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                    TeamSpeak3Telnet.TS3_CLIENT = null;
                    return;
                }

                var awayStatus = TeamSpeak3Telnet.GetAwayStatus(clientId);
                if (awayStatus == _savedSatus)
                {
                    await SetAwayStatusState(awayStatus);
                    return;
                }

                switch (awayStatus)
                {
                    case -1:
                        return;
                    case 0:
                        await SetAwayStatusState();
                        break;
                    case 1:
                        await SetAwayStatusState(1);
                        break;
                }

                _savedSatus = awayStatus;
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
                await SetAwayStatusState();
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

            [JsonProperty(PropertyName = "awayStatusMessage")]
            public string AwayStatusMessage { get; set; }

            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings
                {
                    ApiKey = string.Empty,
                    AwayStatusMessage = string.Empty
                };

                return instance;
            }
        }

#region Private Members

        private readonly PluginSettings _settings;
        private int _savedSatus;

#endregion

#region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private void StreamDeckConnection_OnSendToPlugin(object sender,
            StreamDeckEventReceivedEventArgs<SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context) return;
        }

        private async Task ToggleAwayStatus(int desiredState = -1)
        {
            try
            {
                var clientId = TeamSpeak3Telnet.GetClientId();
                if (clientId == -1)
                {
                    TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                    TeamSpeak3Telnet.TS3_CLIENT = null;
                    return;
                }

                int awayStatus;
                if (desiredState == -1)
                    awayStatus = TeamSpeak3Telnet.GetAwayStatus(clientId);
                else
                    awayStatus = desiredState == 1 ? 0 : 1;

                var setAwayStatus = false;
                switch (awayStatus)
                {
                    case -1:
                        return;
                    case 0:
                        TeamSpeak3Telnet.SetInputMuteStatus(1);
                        TeamSpeak3Telnet.SetOutputMuteStatus(1);
                        setAwayStatus = TeamSpeak3Telnet.SetAwayStatus(1);
                        if (_settings.AwayStatusMessage.Length > 0)
                            TeamSpeak3Telnet.SetAwayMessage(_settings.AwayStatusMessage);
                        break;
                    case 1:
                        TeamSpeak3Telnet.SetInputMuteStatus(0);
                        TeamSpeak3Telnet.SetOutputMuteStatus(0);
                        setAwayStatus = TeamSpeak3Telnet.SetAwayStatus(0);
                        break;
                }

                if (!setAwayStatus) return;
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
                await SetAwayStatusState();
            }
        }

        private async Task SetAwayStatusState(int muted = 0)
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