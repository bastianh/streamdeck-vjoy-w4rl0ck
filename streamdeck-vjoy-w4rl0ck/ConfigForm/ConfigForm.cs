using BarRaider.SdTools;

namespace streamdeck_vjoy_w4rl0ck.ConfigForm;

public partial class ConfigForm : Form
{
    private readonly List<uint> _vJoyDevices;

    public ConfigForm()
    {
        InitializeComponent();

        _vJoyDevices = SimpleVJoyInterface.Instance.ConfiguredDevices();

        foreach (var i in _vJoyDevices)
        {
            vJoySelector.Items.Add("vJoy Device #" + i);
            if (Configuration.Instance.GlobalSettings.VJoyDeviceId == i) vJoySelector.SelectedIndex = _vJoyDevices.IndexOf(i);
        }
    }

    private void vJoySelector_SelectedIndexChanged(object sender, EventArgs e)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"Selected Device {_vJoyDevices[vJoySelector.SelectedIndex]}");
        if (_vJoyDevices[vJoySelector.SelectedIndex] != Configuration.Instance.GlobalSettings.VJoyDeviceId)
        {
            Configuration.Instance.GlobalSettings.VJoyDeviceId = _vJoyDevices[vJoySelector.SelectedIndex];
            Configuration.Instance.SetGlobalSettings();
        }
    }

    private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, "Form Closed");
    }
}