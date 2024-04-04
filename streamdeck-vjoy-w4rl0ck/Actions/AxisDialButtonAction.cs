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

#pragma warning disable 4014
        InitializeSettings();
#pragma warning restore 4014
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
        SimpleVJoyInterface.AxisSignal -= SimpleVJoyInterface_OnAxisSignal;
    }

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

    private async void SimpleVJoyInterface_OnAxisSignal(uint axis, float value)
    {
        if (axis != _axis) return;
        var dict = new Dictionary<string, string> { { "value", value.ToString("P") } };
        await Connection.SetFeedbackAsync(dict);
    }

    private async Task SendPropertyInspectorData()
    {
        var data = new JObject
        {
            ["device"] = SimpleVJoyInterface.Instance.CurrentVJoyId,
            ["status"] = SimpleVJoyInterface.Instance.Status.ToString()
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
        SimpleVJoyInterface.Instance.SetAxis(_axis, 0);
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
        InitializeSettings();
    }

    public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
    }

    private class PluginSettings
    {
        [JsonProperty(PropertyName = "axis")] 
        public string axis { get; set; }

        [JsonProperty(PropertyName = "sensitivity")]
        public string sensitivity { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings();
            instance.axis = "0";
            instance.sensitivity = "100";
            return instance;
        }
    }

    #region Private Members

    private readonly PluginSettings _settings;
    private ushort _axis;
    private ushort _sensitivity;
    private bool _propertyInspectorIsOpen;

    #endregion

    #region Private Methods

    private Task SaveSettings()
    {
        return Connection.SetSettingsAsync(JObject.FromObject(_settings));
    }

    private void InitializeSettings()
    {
        if (!ushort.TryParse(_settings.axis, out _axis))
            // todo: error state
            return;

        if (!ushort.TryParse(_settings.sensitivity, out _sensitivity))
            // todo: error state
            return;
    }

    #endregion
}