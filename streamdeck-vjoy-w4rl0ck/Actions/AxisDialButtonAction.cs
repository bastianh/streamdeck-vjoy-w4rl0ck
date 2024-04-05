using System.DirectoryServices.ActiveDirectory;
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
        Connection.OnSendToPlugin += Connection_OnSendToPlugin;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;
        SimpleVJoyInterface.AxisSignal += SimpleVJoyInterface_OnAxisSignal;
        
        _timer.AutoReset = true;
        _timer.Elapsed += (sender, e) => TimerTick();

        if (payload.Controller == "Encoder")
        {
            _isEncoder = true;
        }
        
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
        Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
        SimpleVJoyInterface.AxisSignal -= SimpleVJoyInterface_OnAxisSignal;
        _timer.Stop();
        _timer.Dispose();
    }

    private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
    {
        var action = e.Event.Payload["action"]?.ToString();
        if (action == "showconfig") Configuration.ShowConfiguration();
    }
    
    private async void SimpleVJoyInterface_OnAxisSignal(uint axis, float value)
    {
        if (axis != _settings.Axis) return;
        await SendAxisUpdate(value);
    }

    private async Task SendAxisUpdate(float value)
    {
        if (!_isEncoder && !_settings.SetTitleValue) return;
        if (_configuration.GlobalSettings.AxisConfiguration[_settings.Axis] == 1) value -= 0.5f;
        if (Math.Abs(value - (-0f)) < 0.001) value = Math.Abs(value);
        var valueString = value.ToString("P0").Replace(" ", string.Empty);;
        if (_isEncoder)
        {
            var dict = new Dictionary<string, string> { { "value", valueString } };
            await Connection.SetFeedbackAsync(dict);
        }
        if (_settings.SetTitleValue) await Connection.SetTitleAsync(valueString);
    }

    public override void KeyPressed(KeyPayload payload)
    {
        if (_settings.ButtonAction > 0)
        {
            if (payload.IsInMultiAction) _oneTickActive = true;
            _timer.Start();
        }
        else
        {
            ResetAxis();
        }
    }

    public override void KeyReleased(KeyPayload payload)
    {
        if (_timer.Enabled && !payload.IsInMultiAction) _timer.Stop();
    }


    public override void DialRotate(DialRotatePayload payload)
    {
        _simpleVJoyInterface.MoveAxis(_settings.Axis, payload.Ticks * _settings.Sensitivity / 100.0);
    }

    public override void DialDown(DialPayload payload)
    {
        ResetAxis();
    }

    public override void DialUp(DialPayload payload)
    {
    }

    public override void TouchPress(TouchpadPressPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, "TouchScreen Pressed");
    }

    public override void OnTick()
    {
    }

    private void ResetAxis()
    {
        if (_configuration.GlobalSettings.AxisConfiguration[_settings.Axis] == 1)
        {
            _simpleVJoyInterface.SetAxis(_settings.Axis, 50);   
        }
        else
        {
            _simpleVJoyInterface.SetAxis(_settings.Axis, 0);
        }
    }
    
    private void TimerTick()
    {
        switch (_settings.ButtonAction)
        {
            case 1:
                _simpleVJoyInterface.MoveAxis(_settings.Axis, _settings.Sensitivity / 100.0);
                break;
            case 2:
                _simpleVJoyInterface.MoveAxis(_settings.Axis, - _settings.Sensitivity / 100.0);
                break;
        }

        if (_oneTickActive || _settings.ButtonAction == 0) _timer.Stop();
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
        [JsonProperty(PropertyName = "axis")] 
        public ushort Axis { get; set; }

        [JsonProperty(PropertyName = "title_value")] 
        public bool SetTitleValue { get; set; }
        
        [JsonProperty(PropertyName = "sensitivity")]
        public ushort Sensitivity { get; set; }

        [JsonProperty(PropertyName = "button_action")]
        public ushort ButtonAction { get; set; }
        
        
        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                Axis = 0,
                Sensitivity = 100,
                ButtonAction = 0
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
        await Connection.SendToPropertyInspectorAsync(_configuration.GetPropertyInspectorData());
    }

    #endregion

    #region Private Members

    private readonly PluginSettings _settings;
    private bool _propertyInspectorIsOpen;
    private readonly System.Timers.Timer _timer = new System.Timers.Timer(100);
    private readonly SimpleVJoyInterface _simpleVJoyInterface = SimpleVJoyInterface.Instance;
    private readonly Configuration _configuration = Configuration.Instance;
    private bool _oneTickActive = false; 
    private readonly bool _isEncoder;

    #endregion
}