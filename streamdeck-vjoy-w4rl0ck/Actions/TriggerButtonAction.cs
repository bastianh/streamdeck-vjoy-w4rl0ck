using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_vjoy_w4rl0ck.Utils;
using Timer = System.Timers.Timer;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId("dev.w4rl0ck.streamdeck.vjoy.triggerbutton")]
public class TriggerButtonAction : KeypadBase
{
    public TriggerButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;

        _buttonTimer.AutoReset = false;
        _longPressTimer.AutoReset = false;
        _buttonTimer.Elapsed += (sender, e) => ButtonTimerTick();
        _longPressTimer.Elapsed += (sender, e) => LongPressTimerTick();

        if (payload.Settings == null || payload.Settings.Count == 0)
        {
            _settings = PluginSettings.CreateDefaultSettings();
            SaveSettings();
        }
        else
        {
            _settings = payload.Settings.ToObject<PluginSettings>();
        }

        SetButtonImage(0, _settings.ButtonId1);
        SetButtonImage(1, _settings.ButtonId2);
    }

    private void SetButtonImage(uint state, uint buttonId)
    {
        if (_settings != null) Connection.SetImageAsync(SvgGenerator.CreateButtonSvgBase64(buttonId), (int)state);
    }


    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
        _buttonTimer.Stop();
        _buttonTimer.Dispose();
        _longPressTimer.Stop();
        _longPressTimer.Dispose();
    }

    public override void KeyPressed(KeyPayload payload)
    {
        _currentButtonId = payload.State == 0 ? _settings.ButtonId1 : _settings.ButtonId2;
        _longPressTimer.Start();
    }

    public override void KeyReleased(KeyPayload payload)
    {
        if (!_longPressTimer.Enabled) return;
        _longPressTimer.Stop();
        var newState = payload.State == 0 ? 1u : 0;
        _currentButtonId = newState == 0 ? _settings.ButtonId1 : _settings.ButtonId2;
        SimpleVJoyInterface.Instance.ButtonState(_currentButtonId, SimpleVJoyInterface.ButtonAction.Down);
        Connection.SetStateAsync(newState);
        _buttonTimer.Start();
    }

    private void LongPressTimerTick()
    {
        SimpleVJoyInterface.Instance.ButtonState(_currentButtonId, SimpleVJoyInterface.ButtonAction.Down);
        _buttonTimer.Start();
        Connection.ShowOk();
    }

    private void ButtonTimerTick()
    {
        SimpleVJoyInterface.Instance.ButtonState(_currentButtonId, SimpleVJoyInterface.ButtonAction.Up);
    }

    public override void OnTick()
    {
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        var oldId1 = _settings.ButtonId1;
        var oldId2 = _settings.ButtonId2;
        try
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
        }
        catch (FormatException e)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key config error: '{e.Message}'");
            Connection.ShowAlert();
        }

        if (oldId1 != _settings.ButtonId1) SetButtonImage(0, _settings.ButtonId1);
        if (oldId2 != _settings.ButtonId2) SetButtonImage(1, _settings.ButtonId2);
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
        [JsonProperty(PropertyName = "buttonId1")]
        public uint ButtonId1 { get; set; }

        [JsonProperty(PropertyName = "buttonId2")]
        public uint ButtonId2 { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                ButtonId1 = 1,
                ButtonId2 = 2
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
    private readonly Timer _buttonTimer = new(100);
    private readonly Timer _longPressTimer = new(600);
    private uint _currentButtonId;
    private bool _propertyInspectorIsOpen;

    #endregion
}