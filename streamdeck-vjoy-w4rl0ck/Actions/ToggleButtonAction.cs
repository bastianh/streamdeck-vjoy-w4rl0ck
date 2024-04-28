using BarRaider.SdTools;
using BarRaider.SdTools.Events;
using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_vjoy_w4rl0ck.Utils;

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
        }
        else
        {
            _settings = payload.Settings.ToObject<PluginSettings>();
        }

        SetButtonImage();
    }

    public override void Dispose()
    {
        Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
        Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;

        SimpleVJoyInterface.UpdateButtonSignal -= SimpleVJoyInterface_OnUpdateButtonSignal;
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
    private bool _buttonState;
    private bool _propertyInspectorIsOpen;

    #endregion
}