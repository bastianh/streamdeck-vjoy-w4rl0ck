using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_vjoy_w4rl0ck.Utils;
using Timer = System.Timers.Timer;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId(ActionIds.PovButton)]
// ReSharper disable once ClassNeverInstantiated.Global
public class PovButtonAction : KeypadBase
{
    public PovButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;

        _timer.AutoReset = false;
        _timer.Elapsed += (_, _) => TimerTick();

        if (payload.Settings == null || payload.Settings.Count == 0)
        {
            _settings = PluginSettings.CreateDefaultSettings();
            _ = SaveSettings();
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
        _timer.Stop();
        _timer.Dispose();
    }

    public override void KeyPressed(KeyPayload payload)
    {
        _simpleVJoyInterface.SetPovSwitch(_settings.DeviceId, _settings.PovId, _settings.Direction);
        if (payload.IsInMultiAction && !_settings.Sticky) _timer.Start();
    }

    public override void KeyReleased(KeyPayload payload)
    {
        if (_settings.Sticky || payload.IsInMultiAction) return;
        _simpleVJoyInterface.SetPovSwitch(_settings.DeviceId, _settings.PovId, 0);
    }

    private void TimerTick()
    {
        _simpleVJoyInterface.SetPovSwitch(_settings.DeviceId, _settings.PovId, 0);
        _timer.Stop();
    }

    public override void OnTick()
    {
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        var oldDeviceId = _settings.DeviceId;
        var oldPovId = _settings.PovId;
        Tools.AutoPopulateSettings(_settings, payload.Settings);

        // Whatever this key deflected stays deflected once it stops pointing at it.
        if (oldDeviceId != _settings.DeviceId || oldPovId != _settings.PovId)
            _simpleVJoyInterface.SetPovSwitch(oldDeviceId, oldPovId, 0);
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
        [JsonProperty(PropertyName = "pov")] public ushort PovId { get; set; }

        [JsonProperty(PropertyName = "direction")]
        public ushort Direction { get; set; }

        [JsonProperty(PropertyName = "sticky")]
        public bool Sticky { get; set; }

        /// <summary>The vJoy device this key drives; 0 means the configured default.</summary>
        [JsonProperty(PropertyName = "device")]
        public uint DeviceId { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                PovId = 0,
                Direction = 0,
                Sticky = false,
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
        if (_propertyInspectorIsOpen) await SendPropertyInspectorData();
    }

    private async Task SendPropertyInspectorData()
    {
        await Connection.SendToPropertyInspectorAsync(Configuration.Instance.GetPropertyInspectorData());
    }

    #endregion

    #region Private Members

    private readonly PluginSettings _settings;
    private readonly Timer _timer = new(100);
    private bool _propertyInspectorIsOpen;
    private readonly SimpleVJoyInterface _simpleVJoyInterface = SimpleVJoyInterface.Instance;

    #endregion
}