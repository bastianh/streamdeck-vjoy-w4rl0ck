using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace streamdeck_vjoy_w4rl0ck;

public class GlobalSettings
{
    [JsonProperty(PropertyName = "vjoy")] public uint VJoyDeviceId { get; set; }
}

public sealed class Configuration : IDisposable
{
    public GlobalSettings GlobalSettings;
    public bool Ready;

    private Configuration()
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, "Configuration initialized");
        GlobalSettings = new GlobalSettings
        {
            VJoyDeviceId = 0
        };
        GlobalSettingsManager.Instance.OnReceivedGlobalSettings += OnReceivedGlobalSettings;
        GlobalSettingsManager.Instance.RequestGlobalSettings();
    }

    #region Singleton

    public static Configuration Instance
    {
        get
        {
            lock (LockObject)
            {
                return _instance ??= new Configuration();
            }
        }
    }

    #endregion

    public void Dispose()
    {
        GlobalSettingsManager.Instance.OnReceivedGlobalSettings -= OnReceivedGlobalSettings;
    }

    private void OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
    {
        // Global Settings exist
        if (payload?.Settings != null && payload.Settings.Count > 0)
        {
            GlobalSettings = payload.Settings.ToObject<GlobalSettings>();
        }
        else // Global settings do not exist, create new one and SAVE it
        {
            Logger.Instance.LogMessage(TracingLevel.WARN, "No global settings found, creating new object");
            GlobalSettings = new GlobalSettings
            {
                VJoyDeviceId = 0
            };
            SetGlobalSettings();
        }
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Configuration GotSettings '{GlobalSettings.VJoyDeviceId}'");
        Ready = true;
    }

    public void ShowConfiguration()
    {
        var configForm = new ConfigForm.ConfigForm();
        configForm.ShowDialog();
    }

    public void SetGlobalSettings()
    {
        GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(GlobalSettings));
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Configuration SAVED '{GlobalSettings.VJoyDeviceId}'");
    }

    #region Singleton

    private static Configuration _instance;
    private static readonly object LockObject = new();

    #endregion
}