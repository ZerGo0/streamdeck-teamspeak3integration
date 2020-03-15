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
    [PluginActionId("com.zergo0.teamspeak3integration.toggleinputmutelocally")]
    public class TeamSpeak3InputMuteLocallyAction : PluginBase
    {
        public TeamSpeak3InputMuteLocallyAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            TeamSpeak3Telnet.Ts3Client?.Dispose();
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            try
            {
                if (TeamSpeak3Telnet.Ts3Client == null || !TeamSpeak3Telnet.Ts3Client.IsConnected)
                {
                    TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                    if (TeamSpeak3Telnet.Ts3Client == null) return;
                }

                if (payload.IsInMultiAction)
                    await ToggleMicMuteLocally((int) payload.UserDesiredState);
                else
                    await ToggleMicMuteLocally(payload.State == 1 ? 0 : 1);
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.Ts3Client?.Dispose();
                TeamSpeak3Telnet.Ts3Client = null;
                await SetInputStatusStateLocally();
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
            try
            {
                /*if (TeamSpeak3Telnet.Ts3Client == null || !TeamSpeak3Telnet.Ts3Client.IsConnected)
                {
                    TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                    if (TeamSpeak3Telnet.Ts3Client == null) return;
                }

                var clientId = TeamSpeak3Telnet.GetClientId();
                if (clientId == -1)
                {
                    TeamSpeak3Telnet.Ts3Client?.Dispose();
                    TeamSpeak3Telnet.Ts3Client = null;
                    return;
                }

                var inputMuteStatus = TeamSpeak3Telnet.GetInputMuteStatusLocally(clientId);
                if (inputMuteStatus == _savedSatus)
                {
                    await SetInputStatusStateLocally(inputMuteStatus);
                    return;
                }

                switch (inputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        await SetInputStatusStateLocally();
                        break;
                    case 1:
                        await SetInputStatusStateLocally(1);
                        break;
                }

                _savedSatus = inputMuteStatus;*/
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.Ts3Client?.Dispose();
                TeamSpeak3Telnet.Ts3Client = null;
                await SetInputStatusStateLocally();
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

        private async Task ToggleMicMuteLocally(int desiredState = -1)
        {
            try
            {
                var clientId = TeamSpeak3Telnet.GetClientId();
                if (clientId == -1)
                {
                    TeamSpeak3Telnet.Ts3Client?.Dispose();
                    TeamSpeak3Telnet.Ts3Client = null;
                    return;
                }

                int outputMuteStatus;
                if (desiredState == -1)
                    outputMuteStatus = TeamSpeak3Telnet.GetInputMuteStatusLocally(clientId);
                else
                    outputMuteStatus = desiredState == 1 ? 0 : 1;

                var setOutputMuteStatus = false;
                switch (outputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        setOutputMuteStatus = TeamSpeak3Telnet.SetInputMuteStatusLocally(1);
                        break;
                    case 1:
                        setOutputMuteStatus = TeamSpeak3Telnet.SetInputMuteStatusLocally(0);
                        break;
                }

                if (!setOutputMuteStatus) return;
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.Ts3Client?.Dispose();
                TeamSpeak3Telnet.Ts3Client = null;
                await SetInputStatusStateLocally();
            }
        }

        private async Task SetInputStatusStateLocally(int muted = 0)
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