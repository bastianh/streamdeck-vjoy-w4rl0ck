using System.Globalization;
using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId("dev.w4rl0ck.streamdeck.vjoy.dialbuttonaction")]
public class DialButtonAction : EncoderBase
{
    public DialButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        Connection.OnSendToPlugin += Connection_OnSendToPlugin;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;

        _timer.AutoReset = true;
        _timer.Elapsed += (sender, e) => TimerTick();

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
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
        _timer.Stop();
        _timer.Dispose();
    }

    private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
    {
        var action = e.Event.Payload["action"]?.ToString();
        if (action == "showconfig") Configuration.ShowConfiguration();
    }

    public override void DialRotate(DialRotatePayload payload)
    {
        // _simpleVJoyInterface.MoveAxis(_settings.Axis, payload.Ticks * _settings.Sensitivity / 100.0);if (_cwButtonId > 0 && payload.Ticks > 0)
        ushort buttonToAdd = payload.Ticks > 0 ? _cwButtonId : _ccwButtonId;
        QueueButton(buttonToAdd, Math.Abs(payload.Ticks));
    }

    public override void DialDown(DialPayload payload)
    {
        _simpleVJoyInterface.ButtonState(_dialButtonId, SimpleVJoyInterface.ButtonAction.Down);
    }

    public override void DialUp(DialPayload payload)
    {
        _simpleVJoyInterface.ButtonState(_dialButtonId, SimpleVJoyInterface.ButtonAction.Up);
    }

    public override void TouchPress(TouchpadPressPayload payload)
    {
        QueueButton(_touchButtonId, 1);
    }

    public override void OnTick()
    {
    }

    private void QueueButton(ushort buttonId, int count)
    {
        // Logger.Instance.LogMessage(TracingLevel.INFO,$"Queueing {count} * button {buttonId}");
        if (count == 0 || buttonId == 0) return;

        for (var i = 0; i < count; i++)
        {
            _buttonQueue.Add(buttonId);
        }

        if (_timer.Enabled) return;
        // Logger.Instance.LogMessage(TracingLevel.DEBUG, "timer on");
        TimerTick();
        _timer.Start();
    }
    
    private void TimerTick()
    {           
        if (_currentlyActiveButtonId == 0)
        {
            if (_buttonQueue.Count > 0)
            {
                _currentlyActiveButtonId = _buttonQueue[0];
                _simpleVJoyInterface.ButtonState(_currentlyActiveButtonId, SimpleVJoyInterface.ButtonAction.Down);
                // Logger.Instance.LogMessage(TracingLevel.DEBUG, $"button down {_currentlyActiveButtonId}");
                _buttonQueue.RemoveAt(0);
            }
            else
            {
                if (!_timer.Enabled) return;
                // Logger.Instance.LogMessage(TracingLevel.DEBUG, "timer off");
                _timer.Stop();
            }
        }
        else
        {
            _simpleVJoyInterface.ButtonState(_currentlyActiveButtonId, SimpleVJoyInterface.ButtonAction.Up);
            // Logger.Instance.LogMessage(TracingLevel.DEBUG, $"button up {_currentlyActiveButtonId}");
            _currentlyActiveButtonId = 0;
        }
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        Tools.AutoPopulateSettings(_settings, payload.Settings);
        ConvertSettings();
    }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
    }

    private class PluginSettings
    {
        [JsonProperty(PropertyName = "cw_button_id")]
        public string CwButtonId { get; set; }

        [JsonProperty(PropertyName = "ccw_button_id")]
        public string CcwButtonId { get; set; }
        
        [JsonProperty(PropertyName = "dial_button_id")]
        public string DialButtonId { get; set; }

        [JsonProperty(PropertyName = "touch_button_id")]
        public string TouchButtonId { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                DialButtonId = string.Empty,
                TouchButtonId = string.Empty,
                CcwButtonId = string.Empty,
                CwButtonId = string.Empty
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
        ushort.TryParse(_settings.CwButtonId, out _cwButtonId);
        ushort.TryParse(_settings.CcwButtonId, out _ccwButtonId);
        ushort.TryParse(_settings.TouchButtonId, out _touchButtonId);
        ushort.TryParse(_settings.DialButtonId, out _dialButtonId);
        // Logger.Instance.LogMessage(TracingLevel.INFO, $"> CW: {_cwButtonId} CCW: {_ccwButtonId} DB: {_dialButtonId} TB: {_touchButtonId}");
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
    private readonly Timer _timer = new(50);
    private readonly SimpleVJoyInterface _simpleVJoyInterface = SimpleVJoyInterface.Instance;
    private readonly Configuration _configuration = Configuration.Instance;
    private ushort _touchButtonId;
    private ushort _dialButtonId;
    private ushort _cwButtonId;
    private ushort _ccwButtonId;
    private ushort _currentlyActiveButtonId = 0;
    private readonly List<ushort> _buttonQueue = [];

    #endregion
}