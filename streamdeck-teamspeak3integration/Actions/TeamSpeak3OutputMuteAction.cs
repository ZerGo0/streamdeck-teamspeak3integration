﻿using System;
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
                    await ToggleOutputMute((int) payload.UserDesiredState);
                else
                    await ToggleOutputMute();
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
                await SetOutputStatusState();
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

                var outputMuteStatus = TeamSpeak3Telnet.GetOutputMuteStatus(clientId);
                if (outputMuteStatus == _savedSatus)
                {
                    await SetOutputStatusState(outputMuteStatus);
                    return;
                }

                switch (outputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        await SetOutputStatusState();
                        break;
                    case 1:
                        await SetOutputStatusState(1);
                        break;
                }

                _savedSatus = outputMuteStatus;
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
                await SetOutputStatusState();
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

        private async Task ToggleOutputMute(int desiredState = -1)
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

                int outputMuteStatus;
                if (desiredState == -1)
                    outputMuteStatus = TeamSpeak3Telnet.GetOutputMuteStatus(clientId);
                else
                    outputMuteStatus = desiredState == 1 ? 0 : 1;

                var setOutputMuteStatus = false;
                switch (outputMuteStatus)
                {
                    case -1:
                        return;
                    case 0:
                        setOutputMuteStatus = TeamSpeak3Telnet.SetOutputMuteStatus(1);
                        break;
                    case 1:
                        setOutputMuteStatus = TeamSpeak3Telnet.SetOutputMuteStatus(0);
                        break;
                }

                if (!setOutputMuteStatus) return;
            }
            catch (Exception)
            {
                TeamSpeak3Telnet.TS3_CLIENT?.Dispose();
                TeamSpeak3Telnet.TS3_CLIENT = null;
                await SetOutputStatusState();
            }
        }

        private async Task SetOutputStatusState(int muted = 0)
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