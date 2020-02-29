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
    [PluginActionId("com.zergo0.teamspeak3integration.changenickname")]
    public class TeamSpeak3ChangeNicknameAction : PluginBase
    {
        public TeamSpeak3ChangeNicknameAction(SDConnection connection, InitialPayload payload) : base(connection,
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
            TeamSpeak3Telnet.Ts3Client?.Dispose();
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            if (TeamSpeak3Telnet.Ts3Client == null || !TeamSpeak3Telnet.Ts3Client.IsConnected)
            {
                TeamSpeak3Telnet.SetupTelnetClient(_settings.ApiKey);
                if (TeamSpeak3Telnet.Ts3Client == null) return;
            }

            ChangeNickname();
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override void OnTick()
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

            [JsonProperty(PropertyName = "nickName")]
            public string NickName { get; set; }

            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings
                {
                    ApiKey = string.Empty,
                    NickName = string.Empty
                };

                return instance;
            }
        }

#region Private Members

        private readonly PluginSettings _settings;

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

        private void ChangeNickname()
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

                TeamSpeak3Telnet.ChangeNickname(_settings.NickName);
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.Ts3Client?.Dispose();
                TeamSpeak3Telnet.Ts3Client = null;
            }
        }

#endregion
    }
}