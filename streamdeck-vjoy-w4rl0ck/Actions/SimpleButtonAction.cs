using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_vjoy_w4rl0ck.Utils;
using Timer = System.Timers.Timer;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId("dev.w4rl0ck.streamdeck.vjoy.simplebutton")]
public class SimpleButtonAction : KeypadBase
{
    public SimpleButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;

        _timer.AutoReset = false;
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

        SetButtonImage();
    }

    private void SetButtonImage()
    {
        if (_settings != null) Connection.SetImageAsync(SvgGenerator.CreateButtonSvgBase64(_settings.ButtonId));
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
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Pressed '{payload.IsInMultiAction}'");
        SimpleVJoyInterface.Instance.ButtonState(_settings.ButtonId, SimpleVJoyInterface.ButtonAction.Down);
        if (payload.IsInMultiAction) _timer.Start();
    }

    public override void KeyReleased(KeyPayload payload)
    {
        if (payload.IsInMultiAction) return;
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Key Released '{payload.IsInMultiAction}'");
        SimpleVJoyInterface.Instance.ButtonState(_settings.ButtonId, SimpleVJoyInterface.ButtonAction.Up);
    }

    private void TimerTick()
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, "Timer Released");
        SimpleVJoyInterface.Instance.ButtonState(_settings.ButtonId, SimpleVJoyInterface.ButtonAction.Up);
        _timer.Stop();
    }

    public override void OnTick()
    {
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        var oldId = _settings.ButtonId;
        try
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
        }
        catch (FormatException e)
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Key config error: '{e.Message}'");
            Connection.ShowAlert();
        }

        if (oldId != _settings.ButtonId) SetButtonImage();
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

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                ButtonId = 1
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

    #endregion
}