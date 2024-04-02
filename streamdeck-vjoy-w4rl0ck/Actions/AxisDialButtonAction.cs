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
        private bool _propertyInspectorIsOpen;

        #endregion

        public AxisDialButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
            Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
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
            SimpleVJoyInterface.VJoyStatusSignal -= SimpleVJoyInterface_OnVJoyStatusSignal;
        }

        private void Connection_OnPropertyInspectorDidAppear(object sender, SDEventReceivedEventArgs<PropertyInspectorDidAppear> e)
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
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public override void KeyReleased(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }


        public override void DialRotate(DialRotatePayload payload)
        {
            var retval = SimpleVJoyInterface.Instance.MoveAxis(_axis,payload.Ticks * 600);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Dial Rotated: {payload.Ticks} {_axis}: {retval}");
            
            var dict = new Dictionary<string, string> { { "value", retval.ToString("P") } };
            Connection.SetFeedbackAsync(dict);
        }

        public override void DialDown(DialPayload payload)
        {
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
            await InitializeSettings();
            if (_vJoyId != oldVJoyId)
                await Connection.SetGlobalSettingsAsync(new JObject { { "vjoy", _vJoyId } });

            await SaveSettings();
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            settings.VJoyId = (string)payload.Settings["vjoy"];
            await InitializeSettings();
            await SaveSettings();
        }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private async Task InitializeSettings()
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
        }
        
        #endregion

        private class PluginSettings
        {
            [JsonProperty(PropertyName = "vJoyId")]
            public string VJoyId { get; set; }

            [JsonProperty(PropertyName = "axis")]
            public string axis { get; set; }

            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings();
                instance.VJoyId = string.Empty;
                instance.axis = "0";
                return instance;
            }
        }
    }
}