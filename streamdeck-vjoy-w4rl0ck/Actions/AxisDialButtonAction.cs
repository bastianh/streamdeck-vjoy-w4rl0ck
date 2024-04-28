using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_vjoy_w4rl0ck.Utils;
using Timer = System.Timers.Timer;

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

        _timer.AutoReset = true;
        _timer.Elapsed += (sender, e) => TimerTick();

        if (payload.Controller == "Encoder") _isEncoder = true;

        if (payload.Settings == null || payload.Settings.Count == 0)
        {
            _settings = PluginSettings.CreateDefaultSettings();
            SaveSettings();
        }
        else
        {
            _settings = payload.Settings.ToObject<PluginSettings>();
        }

        ConvertSettings();
#pragma warning disable 4014
        if (_simpleVJoyInterface.Status == SimpleVJoyInterface.VJoyStatus.Connected)
            SendAxisUpdate(_simpleVJoyInterface.GetCurrentAxisValue(_settings.Axis));
#pragma warning restore 4014
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
        SimpleVJoyInterface.AxisSignal -= SimpleVJoyInterface_OnAxisSignal;
        _timer.Stop();
        _timer.Dispose();
    }

    private async void SimpleVJoyInterface_OnAxisSignal(uint axis, float value)
    {
        if (axis != _settings.Axis) return;
        await SendAxisUpdate(value);
    }

    private async Task SendAxisUpdate(float value)
    {
        if (!_isEncoder && !_settings.SetTitleValue) return;
        var indicator = (int)(value * 100);
        if (_configuration.GlobalSettings.AxisConfiguration[_settings.Axis] == 1) value = (value - 0.5f) * 2;
        if (Math.Abs(value - -0f) < 0.001) value = Math.Abs(value);
        var valueString = value.ToString("P0").Replace(" ", string.Empty);
        if (_isEncoder)
        {
            var dict = new JObject
            {
                ["value"] = valueString,
                ["indicator"] = new JObject { ["value"] = indicator, ["enabled"] = true }
            };
            await Connection.SetFeedbackAsync(dict);
        }

        if (_settings.SetTitleValue) await Connection.SetTitleAsync(valueString);
    }

    public override void KeyPressed(KeyPayload payload)
    {
        if (_settings.ButtonAction > 0)
        {
            TimerTick();
            if (!payload.IsInMultiAction) _timer.Start();
        }
        else
        {
            ResetAxis();
        }
    }

    public override void KeyReleased(KeyPayload payload)
    {
        if (_timer.Enabled) _timer.Stop();
    }


    public override void DialRotate(DialRotatePayload payload)
    {
        _simpleVJoyInterface.MoveAxis(_settings.Axis, payload.Ticks * _settings.Sensitivity / 100.0);
    }

    public override void DialDown(DialPayload payload)
    {
        if (_settings.DialResetAxis) ResetAxis();
        if (_settings.DialButtonAction)
            SimpleVJoyInterface.Instance.ButtonState(_dialButtonId, SimpleVJoyInterface.ButtonAction.Down);
    }

    public override void DialUp(DialPayload payload)
    {
        if (_settings.DialButtonAction)
            SimpleVJoyInterface.Instance.ButtonState(_dialButtonId, SimpleVJoyInterface.ButtonAction.Up);
    }

    public override void TouchPress(TouchpadPressPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, "TouchScreen Pressed");
        if (_settings.TouchResetAxis) ResetAxis();
        if (_settings.TouchButtonAction)
        {
            _touchButtonIsDown = true;
            _timer.Start();
            SimpleVJoyInterface.Instance.ButtonState(_touchButtonId, SimpleVJoyInterface.ButtonAction.Down);
        }
    }

    public override void OnTick()
    {
    }

    private void ResetAxis()
    {
        if (_configuration.GlobalSettings.AxisConfiguration[_settings.Axis] == 1)
            _simpleVJoyInterface.SetAxis(_settings.Axis, 50);
        else
            _simpleVJoyInterface.SetAxis(_settings.Axis, 0);
    }

    private void TimerTick()
    {
        switch (_settings.ButtonAction)
        {
            case 1:
                _simpleVJoyInterface.MoveAxis(_settings.Axis, _settings.Sensitivity / 100.0);
                break;
            case 2:
                _simpleVJoyInterface.MoveAxis(_settings.Axis, -_settings.Sensitivity / 100.0);
                break;
        }

        if (_touchButtonIsDown)
        {
            SimpleVJoyInterface.Instance.ButtonState(_touchButtonId, SimpleVJoyInterface.ButtonAction.Up);
            _timer.Stop();
        }

        if (_settings.ButtonAction == 0) _timer.Stop();
    }

    public override async void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        Tools.AutoPopulateSettings(_settings, payload.Settings);
        ConvertSettings();
        if (_simpleVJoyInterface.Status == SimpleVJoyInterface.VJoyStatus.Connected)
            await SendAxisUpdate(_simpleVJoyInterface.GetCurrentAxisValue(_settings.Axis));
        if (!_settings.SetTitleValue) await Connection.SetTitleAsync("");
    }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
    }

    private class PluginSettings
    {
        [JsonProperty(PropertyName = "axis")] public ushort Axis { get; set; }

        [JsonProperty(PropertyName = "title_value")]
        public bool SetTitleValue { get; set; }

        [JsonProperty(PropertyName = "sensitivity")]
        public ushort Sensitivity { get; set; }

        [JsonProperty(PropertyName = "button_action")]
        public ushort ButtonAction { get; set; }

        [JsonProperty(PropertyName = "dial_reset_axis")]
        public bool DialResetAxis { get; set; }

        [JsonProperty(PropertyName = "dial_button_action")]
        public bool DialButtonAction { get; set; }

        [JsonProperty(PropertyName = "dial_button_id")]
        public string DialButtonId { get; set; }

        [JsonProperty(PropertyName = "touch_reset_axis")]
        public bool TouchResetAxis { get; set; }

        [JsonProperty(PropertyName = "touch_action")]
        public bool TouchButtonAction { get; set; }

        [JsonProperty(PropertyName = "touch_button_id")]
        public string TouchButtonId { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                Axis = 0,
                Sensitivity = 100,
                ButtonAction = 0,
                DialResetAxis = true,
                DialButtonAction = false,
                DialButtonId = string.Empty,
                TouchResetAxis = false,
                TouchButtonAction = false,
                TouchButtonId = string.Empty
            };
            return instance;
        }
    }

    #region Private Methods

    private Task SaveSettings()
    {
        return Connection.SetSettingsAsync(JObject.FromObject(_settings));
    }

    private void ConvertSettings()
    {
        ushort.TryParse(_settings.TouchButtonId, out _touchButtonId);
        ushort.TryParse(_settings.DialButtonId, out _dialButtonId);
    }

    #endregion

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
        if (_simpleVJoyInterface.Status == SimpleVJoyInterface.VJoyStatus.Connected)
            await SendAxisUpdate(_simpleVJoyInterface.GetCurrentAxisValue(_settings.Axis));
        if (_propertyInspectorIsOpen) await SendPropertyInspectorData();
    }

    private async Task SendPropertyInspectorData()
    {
        await Connection.SendToPropertyInspectorAsync(_configuration.GetPropertyInspectorData());
    }

    #endregion

    #region Private Members

    private readonly PluginSettings _settings;
    private bool _propertyInspectorIsOpen;
    private bool _touchButtonIsDown;
    private readonly Timer _timer = new(100);
    private readonly SimpleVJoyInterface _simpleVJoyInterface = SimpleVJoyInterface.Instance;
    private readonly Configuration _configuration = Configuration.Instance;
    private readonly bool _isEncoder;
    private ushort _touchButtonId;
    private ushort _dialButtonId;

    #endregion
}