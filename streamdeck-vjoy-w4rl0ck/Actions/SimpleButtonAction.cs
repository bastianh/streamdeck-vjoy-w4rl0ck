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

    private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
    {
        var action = e.Event.Payload["action"]?.ToString();
        if (action == "showconfig") Configuration.Instance.ShowConfiguration();
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Connection_OnSendToPlugin '{action}'");
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        Connection.OnSendToPlugin += Connection_OnSendToPlugin;
        SimpleVJoyInterface.VJoyStatusUpdateSignal -= SimpleVJoyInterface_OnVJoyStatusUpdate;
    }

    private async void SimpleVJoyInterface_OnVJoyStatusUpdate()
    {
        if (_propertyInspectorIsOpen) await SendPropertyInspectorData();
    }

    public override void KeyPressed(KeyPayload payload)
    {
        SimpleVJoyInterface.Instance.ButtonState(_buttonId, SimpleVJoyInterface.ButtonAction.Down);
    }

    public override void KeyReleased(KeyPayload payload)
    {
        SimpleVJoyInterface.Instance.ButtonState(_buttonId, SimpleVJoyInterface.ButtonAction.Up);
    }

    public override void OnTick()
    {
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        Tools.AutoPopulateSettings(_settings, payload.Settings);
        InitializeSettings();
    }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
    }

    private class PluginSettings
    {
        [JsonProperty(PropertyName = "buttonId")]
        public string ButtonId { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings
            {
                ButtonId = String.Empty
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

    private async Task SendPropertyInspectorData()
    {
        var data = new JObject
        {
            ["device"] = SimpleVJoyInterface.Instance.CurrentVJoyId,
            ["status"] = SimpleVJoyInterface.Instance.Status.ToString()
        };
        await Connection.SendToPropertyInspectorAsync(data);
    }

    #endregion

    #region Private Members

    private readonly PluginSettings _settings;
    private uint _buttonId;
    private bool _propertyInspectorIsOpen;

    #endregion

    #region Private Methods

    private Task SaveSettings()
    {
        return Connection.SetSettingsAsync(JObject.FromObject(_settings));
    }

    private void InitializeSettings()
    {
        if (!uint.TryParse(_settings.ButtonId, out _buttonId))
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not parse ButtonId '{_settings.ButtonId}'");
        // todo: error state
    }

    #endregion
}