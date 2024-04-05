using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId("dev.w4rl0ck.streamdeck.vjoy.simplebutton")]
public class SimpleButtonAction : KeypadBase
{
    public SimpleButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        Connection.OnSendToPlugin += Connection_OnSendToPlugin;
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
    }

    private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
    {
        var action = e.Event.Payload["action"]?.ToString();
        if (action == "showconfig") Configuration.ShowConfiguration();
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
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Timer Released");
        SimpleVJoyInterface.Instance.ButtonState(_settings.ButtonId, SimpleVJoyInterface.ButtonAction.Up);
        _timer.Stop();    
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
    private readonly System.Timers.Timer _timer = new System.Timers.Timer(100);
    private bool _propertyInspectorIsOpen;

    #endregion
}