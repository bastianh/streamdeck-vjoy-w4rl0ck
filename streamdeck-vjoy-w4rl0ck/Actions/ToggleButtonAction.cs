using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_vjoy_w4rl0ck.Utils;
using Timer = System.Timers.Timer;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId(ActionIds.ToggleButton)]
public class ToggleButtonAction : KeypadBase
{
    public ToggleButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;

        SimpleVJoyInterface.UpdateButtonSignal += SimpleVJoyInterface_OnUpdateButtonSignal;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;

        _longPressTimer.AutoReset = false;
        _longPressTimer.Elapsed += (sender, e) => LongPressTimerTick();
        _buttonTimer.AutoReset = false;
        _buttonTimer.Elapsed += (sender, e) => ButtonTimerTick();

        if (payload.Settings == null || payload.Settings.Count == 0)
        {
            _settings = PluginSettings.CreateDefaultSettings();
            SaveSettings();
        }
        else
        {
            _settings = payload.Settings.ToObject<PluginSettings>();
        }

        SetButtonImage();
        RefreshButtonState();
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;

        SimpleVJoyInterface.UpdateButtonSignal -= SimpleVJoyInterface_OnUpdateButtonSignal;
    }

    private void SimpleVJoyInterface_OnUpdateButtonSignal(uint deviceId, uint buttonId, bool state)
    {
        if (deviceId != SimpleVJoyInterface.Instance.ResolveDeviceId(_settings.DeviceId)) return;
        if (buttonId != _settings.ButtonId) return;
        _buttonState = state;
        Connection.SetStateAsync(_buttonState ? 1u : 0u);
    }

    public override void KeyPressed(KeyPayload payload)
    {
        if (_settings.LongPressButtonId != null && _settings.LongPressButtonId > 0) _longPressTimer.Start();
    }

    public override void KeyReleased(KeyPayload payload)
    {
        if (_settings.LongPressButtonId != null && _settings.LongPressButtonId > 0)
        {
            if (!_longPressTimer.Enabled) return;
            _longPressTimer.Stop();
        }

        if (!SimpleVJoyInterface.Instance.IsDeviceUsable(_settings.DeviceId)) Connection.ShowAlert();
        SimpleVJoyInterface.Instance.ButtonState(_settings.DeviceId, _settings.ButtonId,
            SimpleVJoyInterface.ButtonAction.Toggle);
        Connection.SetStateAsync(_buttonState ? 1u : 0u);
    }

    private void LongPressTimerTick()
    {
        if (_settings.LongPressButtonId == null) return;
        SimpleVJoyInterface.Instance.ButtonState(_settings.DeviceId, (uint)_settings.LongPressButtonId,
            SimpleVJoyInterface.ButtonAction.Down);
        _buttonTimer.Start();
        Connection.ShowOk();
    }

    private void ButtonTimerTick()
    {
        if (_settings.LongPressButtonId != null)
            SimpleVJoyInterface.Instance.ButtonState(_settings.DeviceId, (uint)_settings.LongPressButtonId,
                SimpleVJoyInterface.ButtonAction.Up);
    }


    public override void OnTick()
    {
    }

    // The state is not the key's to remember: several keys can address the same
    // button, and a device is reset behind their backs whenever it is acquired or
    // released. Take it from what the device reports instead.
    private void RefreshButtonState()
    {
        _buttonState = SimpleVJoyInterface.Instance.GetButtonState(_settings.DeviceId, _settings.ButtonId);
        Connection.SetStateAsync(_buttonState ? 1u : 0u);
    }

    private void SetButtonImage()
    {
        if (_settings != null)
        {
            Connection.SetImageAsync(SvgGenerator.CreateButtonSvgBase64(_settings.ButtonId));
            Connection.SetImageAsync(SvgGenerator.CreateButtonSvgBase64(_settings.ButtonId, true), 1);
        }
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        var oldId = _settings.ButtonId;
        var oldDeviceId = _settings.DeviceId;
        try
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
        }
        catch (FormatException e)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key config error: '{e.Message}'");
            Connection.ShowAlert();
        }

        // A latched button is left latched once the key stops pointing at it, with
        // nothing on the deck able to switch it off again.
        if (oldId != _settings.ButtonId || oldDeviceId != _settings.DeviceId)
            SimpleVJoyInterface.Instance.ButtonState(oldDeviceId, oldId, SimpleVJoyInterface.ButtonAction.Up);

        if (oldId != _settings.ButtonId) SetButtonImage();
        RefreshButtonState();
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
        [JsonProperty(PropertyName = "buttonId")]
        public uint ButtonId { get; set; }

        [JsonProperty(PropertyName = "lpButtonId")]
        public uint? LongPressButtonId { get; set; }

        /// <summary>The vJoy device this key drives; 0 means the configured default.</summary>
        [JsonProperty(PropertyName = "device")]
        public uint DeviceId { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                ButtonId = 1,
                LongPressButtonId = 0,
                DeviceId = 0
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
        RefreshButtonState();
        if (_propertyInspectorIsOpen) await SendPropertyInspectorData();
    }

    private async Task SendPropertyInspectorData()
    {
        await Connection.SendToPropertyInspectorAsync(
            Configuration.Instance.GetPropertyInspectorData(_settings.DeviceId));
    }

    #endregion

    #region Private Members

    private readonly PluginSettings _settings;
    private readonly Timer _buttonTimer = new(100);
    private readonly Timer _longPressTimer = new(600);
    private bool _buttonState;
    private bool _propertyInspectorIsOpen;

    #endregion
}