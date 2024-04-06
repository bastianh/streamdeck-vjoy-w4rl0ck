using BarRaider.SdTools;

namespace streamdeck_vjoy_w4rl0ck.ConfigForm;

public partial class ConfigForm : Form
{
    private readonly ComboBox[] _axis;
    private readonly List<uint> _vJoyDevices;

    public ConfigForm()
    {
        InitializeComponent();
        _axis = new[] { axis1, axis2, axis3, axis4, axis5, axis6, axis7, axis8 };
        _vJoyDevices = SimpleVJoyInterface.Instance.ConfiguredDevices();

        foreach (var i in _vJoyDevices)
        {
            vJoySelector.Items.Add("vJoy Device #" + i);
            if (Configuration.Instance.GlobalSettings.VJoyDeviceId == i)
                vJoySelector.SelectedIndex = _vJoyDevices.IndexOf(i);
        }

        var axisConfiguration = Configuration.Instance.GlobalSettings.AxisConfiguration;

        Logger.Instance.LogMessage(TracingLevel.WARN,
            $"Message {Configuration.Instance.GlobalSettings.AxisConfiguration}");
        for (var index = 0; index < _axis.Length; index++)
        {
            var comboBox = _axis[index];
            comboBox.Items.Add("Slider (Initialized at 0%)");
            comboBox.Items.Add("Axis (Initialized at center)");
            comboBox.SelectedIndex = axisConfiguration[index];
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

    private void axis_SelectedIndexChanged(object sender, EventArgs e)
    {
        var axis = Array.IndexOf(_axis, sender);
        if (axis != -1)
        {
            Configuration.Instance.GlobalSettings.AxisConfiguration[axis] = (ushort)_axis[axis].SelectedIndex;
            Configuration.Instance.SetGlobalSettings();
        }
    }
}