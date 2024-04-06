using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace streamdeck_vjoy_w4rl0ck.Actions;

[PluginActionId("dev.w4rl0ck.streamdeck.vjoy.togglebutton")]
public class ToggleButtonAction : KeypadBase
{
    public ToggleButtonAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
        Connection.OnSendToPlugin += Connection_OnSendToPlugin;

        SimpleVJoyInterface.UpdateButtonSignal += SimpleVJoyInterface_OnUpdateButtonSignal;
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
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        Connection.OnSendToPlugin -= Connection_OnSendToPlugin;

        SimpleVJoyInterface.UpdateButtonSignal -= SimpleVJoyInterface_OnUpdateButtonSignal;
    }

    private void Connection_OnSendToPlugin(object sender, SDEventReceivedEventArgs<SendToPlugin> e)
    {
        var action = e.Event.Payload["action"]?.ToString();
        if (action == "showconfig") Configuration.ShowConfiguration();
    }

    private void SimpleVJoyInterface_OnUpdateButtonSignal(uint buttonId, bool state)
    {
        if (buttonId != _settings.ButtonId) return;
        _buttonState = state;
        Connection.SetStateAsync(_buttonState ? 1u : 0u);
    }

    public override void KeyPressed(KeyPayload payload)
    {
        SimpleVJoyInterface.Instance.ButtonState(_settings.ButtonId, SimpleVJoyInterface.ButtonAction.Toggle);
    }

    public override void KeyReleased(KeyPayload payload)
    {
        Connection.SetStateAsync(_buttonState ? 1u : 0u);
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
    private bool _buttonState;
    private bool _propertyInspectorIsOpen;

    #endregion
}