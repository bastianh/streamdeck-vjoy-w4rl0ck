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

        SimpleVJoyInterface.UpdateButtonSignal += SimpleVJoyInterface_OnUpdateButtonSignal;
        SimpleVJoyInterface.VJoyStatusUpdateSignal += SimpleVJoyInterface_OnVJoyStatusUpdate;

        if (payload.Settings == null || payload.Settings.Count == 0)
        {
            _settings = PluginSettings.CreateDefaultSettings();
            SaveSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }
        else
        {
            _settings = payload.Settings.ToObject<PluginSettings>();
        }

#pragma warning disable 4014
        InitializeSettings();
#pragma warning restore 4014
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
        SimpleVJoyInterface.UpdateButtonSignal -= SimpleVJoyInterface_OnUpdateButtonSignal;
    }

    private void Connection_OnPropertyInspectorDidAppear(object sender,
        SDEventReceivedEventArgs<PropertyInspectorDidAppear> e)
    {
        SendPropertyInspectorData();
        _propertyInspectorIsOpen = true;
    }

    private void Connection_OnPropertyInspectorDidDisappear(object sender,
        SDEventReceivedEventArgs<PropertyInspectorDidDisappear> e)
    {
        _propertyInspectorIsOpen = false;
    }

    private void SimpleVJoyInterface_OnVJoyStatusUpdate()
    {
        if (_propertyInspectorIsOpen) SendPropertyInspectorData();
    }

    private void SimpleVJoyInterface_OnUpdateButtonSignal(uint buttonId, bool state)
    {
        if (buttonId != _buttonId) return;
        _buttonState = state;
        Connection.SetStateAsync(_buttonState ? 1u : 0u);
    }

    private async void SendPropertyInspectorData()
    {
        var data = new JObject
        {
            ["device"] = SimpleVJoyInterface.Instance.CurrentVJoyId,
            ["status"] = SimpleVJoyInterface.Instance.Status.ToString()
        };
        await Connection.SendToPropertyInspectorAsync(data);
    }

    public override void KeyPressed(KeyPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO,
            "Key Pressed (" + payload.Coordinates.Row + "," + payload.Coordinates.Column + "): " + payload.State);
        SimpleVJoyInterface.Instance.ButtonState(_buttonId, SimpleVJoyInterface.ButtonAction.Toggle);
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
        InitializeSettings();
    }

    public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
    }

    private class PluginSettings
    {
        [JsonProperty(PropertyName = "buttonId")]
        public string ButtonId { get; set; }

        public static PluginSettings CreateDefaultSettings()
        {
            var instance = new PluginSettings();
            instance.ButtonId = string.Empty;
            return instance;
        }
    }

    #region Private Members

    private readonly PluginSettings _settings;
    private uint _buttonId;
    private bool _buttonState;
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
        {
            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not parse ButtonId '{_settings.ButtonId}'");
            // todo: error state
        }
    }

    #endregion
}