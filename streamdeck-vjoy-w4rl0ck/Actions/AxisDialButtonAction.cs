using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId("dev.w4rl0ck.streamdeck.vjoy.axiskeydialaction")]
public class AxisDialButtonAction : KeyAndEncoderBase
{
    public AxisDialButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;
        SimpleVJoyInterface.AxisSignal += SimpleVJoyInterface_OnAxisSignal;

        if (payload.Settings == null || payload.Settings.Count == 0)
        {
            _settings = PluginSettings.CreateDefaultSettings();
            SaveSettings();
        }
        else
        {
            _settings = payload.Settings.ToObject<PluginSettings>();
        }
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
        SimpleVJoyInterface.AxisSignal -= SimpleVJoyInterface_OnAxisSignal;
    }

    private async void SimpleVJoyInterface_OnAxisSignal(uint axis, float value)
    {
        if (axis != _settings.Axis) return;
        var dict = new Dictionary<string, string> { { "value", value.ToString("P") } };
        await Connection.SetFeedbackAsync(dict);
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
        SimpleVJoyInterface.Instance.MoveAxis(_settings.Axis, payload.Ticks * _settings.Sensitivity / 100.0);
    }

    public override void DialDown(DialPayload payload)
    {
        SimpleVJoyInterface.Instance.SetAxis(_settings.Axis, 0);
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

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        Tools.AutoPopulateSettings(_settings, payload.Settings);
    }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
    }

    #region Private Methods

    private Task SaveSettings()
    {
        return Connection.SetSettingsAsync(JObject.FromObject(_settings));
    }

    #endregion

    private class PluginSettings
    {
        [JsonProperty(PropertyName = "axis")] public ushort Axis { get; set; }

        [JsonProperty(PropertyName = "sensitivity")]
        public ushort Sensitivity { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                Axis = 0,
                Sensitivity = 100
            };
            return instance;
        }
    }

    #region Property Inspector

    private async void Connection_OnPropertyInspectorDidAppear(object sender,
        SDEventReceivedEventArgs<PropertyInspectorDidAppear> e)
    {
        await SendPropertyInspectorData();
        _propertyInspectorIsOpen = true;
    }

    private void Connection_OnPropertyInspectorDidDisappear(object sender,
        SDEventReceivedEventArgs<PropertyInspectorDidDisappear> e)
    {
        _propertyInspectorIsOpen = false;
    }

    private async void SimpleVJoyInterface_OnVJoyStatusUpdate()
    {
        if (_propertyInspectorIsOpen) await SendPropertyInspectorData();
    }

    private async Task SendPropertyInspectorData()
    {
        await Connection.SendToPropertyInspectorAsync(Configuration.Instance.GetPropertyInspectorData());
    }

    #endregion

    #region Private Members

    private readonly PluginSettings _settings;
    private bool _propertyInspectorIsOpen;

    #endregion
}