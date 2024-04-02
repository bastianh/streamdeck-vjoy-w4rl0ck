using System.Collections.Generic;
using System.Threading.Tasks;
using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using dev.w4rl0ck.streamdeck.vjoy.libs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dev.w4rl0ck.streamdeck.vjoy
{
    [PluginActionId("dev.w4rl0ck.streamdeck.vjoy.axiskeydialaction")]
    public class AxisDialButtonAction : KeyAndEncoderBase
    {
        #region Private Members

        private readonly PluginSettings settings;
        private uint _vJoyId;
        private ushort _axis;
        private ushort _sensitivity;
        private bool _propertyInspectorIsOpen;

        #endregion

        public AxisDialButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
            Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
            SimpleVJoyInterface.VJoyStatusSignal += SimpleVJoyInterface_OnVJoyStatusSignal;
            SimpleVJoyInterface.AxisSignal += SimpleVJoyInterface_OnAxisSignal;
            
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
            SimpleVJoyInterface.VJoyStatusSignal -= SimpleVJoyInterface_OnVJoyStatusSignal;
            SimpleVJoyInterface.AxisSignal -= SimpleVJoyInterface_OnAxisSignal;
        }

        private async void Connection_OnPropertyInspectorDidAppear(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e)
        {
            await SendPropertyInspectorData();
            _propertyInspectorIsOpen = true;
        }
        private void Connection_OnPropertyInspectorDidDisappear(object sender, SDEventReceivedEventArgs<PropertyInspectorDidDisappear> e)
        {
            _propertyInspectorIsOpen = false;
        }

        private async void SimpleVJoyInterface_OnVJoyStatusSignal(SimpleVJoyInterface.VJoyStatus status)
        {
            if (_propertyInspectorIsOpen) 
                await SendPropertyInspectorData();
        }
        
        private async void SimpleVJoyInterface_OnAxisSignal(uint axis, float value)
        {
            if (axis != _axis) return;
            var dict = new Dictionary<string, string> { { "value", value.ToString("P") } };
            await Connection.SetFeedbackAsync(dict);
        }

        private async Task SendPropertyInspectorData()
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
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public override void KeyReleased(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }


        public override void DialRotate(DialRotatePayload payload)
        { 
            SimpleVJoyInterface.Instance.MoveAxis(_axis, payload.Ticks * _sensitivity / 100.0);
        }

        public override void DialDown(DialPayload payload)
        {
            SimpleVJoyInterface.Instance.SetAxis(_axis,0);
            Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Pressed");
        }

        public override void DialUp(DialPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Released ");
        }

        public override void TouchPress(TouchpadPressPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "TouchScreen Pressed");
        }

        public override void OnTick()
        {
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            var oldVJoyId = _vJoyId;
            InitializeSettings();
            if (_vJoyId != oldVJoyId)
                await Connection.SetGlobalSettingsAsync(new JObject { { "vjoy", _vJoyId } });

            await SaveSettings();
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            settings.VJoyId = (string)payload.Settings["vjoy"];
            InitializeSettings();
            await SaveSettings();
            await SendPropertyInspectorData();
        }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void InitializeSettings()
        {
            if (!uint.TryParse(settings.VJoyId, out _vJoyId))
            {
                // todo: error state
                return;
            }
            
            SimpleVJoyInterface.Instance.ConnectToVJoy(_vJoyId);
            
            if (!ushort.TryParse(settings.axis, out _axis))
            {
                // todo: error state
                return;
            }

            if (!ushort.TryParse(settings.sensitivity, out _sensitivity))
            {
                // todo: error state
                return;
            }
        }
        
        #endregion

        private class PluginSettings
        {
            [JsonProperty(PropertyName = "vJoyId")]
            public string VJoyId { get; set; }

            [JsonProperty(PropertyName = "axis")]
            public string axis { get; set; }

            [JsonProperty(PropertyName = "sensitivity")]
            public string sensitivity { get; set; }
            
            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings();
                instance.VJoyId = string.Empty;
                instance.axis = "0";
                instance.sensitivity = "100";
                return instance;
            }
        }
    }
}