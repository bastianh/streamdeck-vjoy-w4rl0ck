using System.Threading.Tasks;
using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using dev.w4rl0ck.streamdeck.vjoy.libs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dev.w4rl0ck.streamdeck.vjoy.Actions
{
    [PluginActionId("dev.w4rl0ck.streamdeck.vjoy.togglebutton")]
    public class ToggleButtonAction : KeypadBase
    {
        public ToggleButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
            Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
            
            SimpleVJoyInterface.UpdateButtonSignal += SimpleVJoyInterface_OnUpdateButtonSignal;
            SimpleVJoyInterface.VJoyStatusSignal += SimpleVJoyInterface_OnVJoyStatusSignal;
            
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
                GlobalSettingsManager.Instance.RequestGlobalSettings();
            }
            else
            {
                settings = payload.Settings.ToObject<PluginSettings>();
            }
            
#pragma warning disable 4014
            InitializeSettings();
#pragma warning restore 4014
        }
        
        public override void Dispose()
        {
            Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
            Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
            SimpleVJoyInterface.UpdateButtonSignal -= SimpleVJoyInterface_OnUpdateButtonSignal;
        }

        private void Connection_OnPropertyInspectorDidAppear(object sender,
            SDEventReceivedEventArgs<PropertyInspectorDidAppear> e)
        {
            SendPropertyInspectorData();
            _propertyInspectorIsOpen = true;
        }
        private void Connection_OnPropertyInspectorDidDisappear(object sender, SDEventReceivedEventArgs<PropertyInspectorDidDisappear> e)
        {
            _propertyInspectorIsOpen = false;
        }

        private void SimpleVJoyInterface_OnVJoyStatusSignal(SimpleVJoyInterface.VJoyStatus status)
        {
            if (_propertyInspectorIsOpen) 
                SendPropertyInspectorData();
        }
        
        private void SimpleVJoyInterface_OnUpdateButtonSignal(uint buttonId, bool state)
        {
            if (buttonId != _buttonId) return;
            _buttonState = state;
            Connection.SetStateAsync(_buttonState ? 1u : 0u);
        }

        private async void SendPropertyInspectorData()
        {
            
            var deviceList = SimpleVJoyInterface.Instance.CheckAvailableDevices();
            var devices = JArray.Parse(JsonConvert.SerializeObject(deviceList));

            var data = new JObject
            {
                ["device"] = SimpleVJoyInterface.Instance.CurrentVJoyId,
                ["status"] = SimpleVJoyInterface.Instance.Status.ToString(),
                ["devices"] = devices
            };
            await Connection.SendToPropertyInspectorAsync(data);
        }        
        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed ("+payload.Coordinates.Row+","+payload.Coordinates.Column+"): "+payload.State);
            SimpleVJoyInterface.Instance.ButtonState(_buttonId, SimpleVJoyInterface.ButtonAction.Toggle);
        }

        public override void KeyReleased(KeyPayload payload)
        {
            Connection.SetStateAsync(_buttonState ? 1u : 0u);
        }

        public override void OnTick()
        {
        }
        
        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            var oldVjoyId = _vJoyId;
            InitializeSettings();
            if (_vJoyId != oldVjoyId)
                await Connection.SetGlobalSettingsAsync(new JObject { { "vjoy", _vJoyId } });

            await SaveSettings();
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            settings.VJoyId = (string)payload.Settings["vjoy"];
            await SaveSettings();
        }

        private class PluginSettings
        {
            [JsonProperty(PropertyName = "vJoyId")]
            public string VJoyId { get; set; }

            [JsonProperty(PropertyName = "buttonId")]
            public string ButtonId { get; set; }
            
            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings();
                instance.VJoyId = string.Empty;
                instance.ButtonId = string.Empty;
                return instance;
            }
        }

        #region Private Members

        private readonly PluginSettings settings;
        private uint _vJoyId;
        private uint _buttonId;
        private bool _buttonState;
        private bool _propertyInspectorIsOpen;

        #endregion

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void InitializeSettings()
        {
            if (!uint.TryParse(settings.VJoyId, out _vJoyId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not parse vJoyId '{settings.VJoyId}'");
                // todo: error state
                return;
            }
            
            SimpleVJoyInterface.Instance.ConnectToVJoy(_vJoyId);
            
            if (!uint.TryParse(settings.ButtonId, out _buttonId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not parse ButtonId '{settings.ButtonId}'");
                // todo: error state
                return;
            }
        }

        #endregion
    }
}