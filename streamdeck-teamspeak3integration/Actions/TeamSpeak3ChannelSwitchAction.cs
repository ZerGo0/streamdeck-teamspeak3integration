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
    [PluginActionId("com.zergo0.teamspeak3integration.channelswitch")]
    public class TeamSpeak3ChannelSwitchAction : PluginBase
    {
        #region Private Members

        private readonly PluginSettings _settings;

        #endregion

        public TeamSpeak3ChannelSwitchAction(SDConnection connection, InitialPayload payload) : base(connection,
            payload)
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

                ChannelSwitch();
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
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

            [JsonProperty(PropertyName = "channelName")]
            public string ChannelName { get; set; }

            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings
                {
                    ApiKey = string.Empty,
                    ChannelName = string.Empty
                };

                return instance;
            }
        }

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

        private void ChannelSwitch()
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

                TeamSpeak3Telnet.ChannelSwitch(_settings.ChannelName, clientId);
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
            }
        }

        #endregion
    }
}