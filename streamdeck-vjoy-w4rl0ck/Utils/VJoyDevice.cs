using BarRaider.SdTools;
using vJoyInterfaceWrap;
using ButtonAction = streamdeck_vjoy_w4rl0ck.Utils.SimpleVJoyInterface.ButtonAction;
using VJoyStatus = streamdeck_vjoy_w4rl0ck.Utils.SimpleVJoyInterface.VJoyStatus;

namespace streamdeck_vjoy_w4rl0ck.Utils;

/// <summary>
///     One vJoy device owned by this process: its report, its axis range and its status.
///     A class rather than a struct, so the axis and POV accessors can return references
///     into the report.
/// </summary>
public sealed class VJoyDevice
{
    private readonly Configuration _configuration;
    private readonly object _updateLockObject = new();
    private readonly vJoy _vJoy;
    private vJoy.JoystickState _iReport;
    private long _maxAxisValue;

    public VJoyDevice(vJoy vJoy, Configuration configuration, uint id)
    {
        _vJoy = vJoy;
        _configuration = configuration;
        _iReport = new vJoy.JoystickState();
        Id = id;
    }

    public uint Id { get; }
    public VJoyStatus Status { get; private set; }
    public bool IsOwned => _vJoy.GetVJDStatus(Id) == VjdStat.VJD_STAT_OWN;

    public VJoyStatus Acquire()
    {
        lock (_updateLockObject)
        {
            if (!_vJoy.isVJDExists(Id)) return Status = VJoyStatus.VJoyDeviceNotExistent;
            if (!_vJoy.AcquireVJD(Id)) return Status = VJoyStatus.VJoyDeviceBusy;

            _vJoy.ResetVJD(Id);
            _vJoy.GetVJDAxisMax(Id, HID_USAGES.HID_USAGE_X, ref _maxAxisValue);
            Logger.Instance.LogMessage(TracingLevel.DEBUG,
                $"vJoy Device: {Id}, axis maxval is now '{_maxAxisValue}'");
            if (_maxAxisValue == 0) // TODO: find out why that happens sometimes
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "overwriting maxval to 32767 :(");
                _maxAxisValue = 32767;
            }

            ResetAxisAndPovs();
            Update();
            return Status = VJoyStatus.Connected;
        }
    }

    public void Relinquish()
    {
        lock (_updateLockObject)
        {
            _vJoy.RelinquishVJD(Id);
            Status = VJoyStatus.Disconnected;
        }
    }

    private void ResetAxisAndPovs()
    {
        for (ushort index = 0; index < _configuration.GlobalSettings.AxisConfiguration.Length; index++)
        {
            var axisConf = _configuration.GlobalSettings.AxisConfiguration[index];
            ref var axisRef = ref GetAxisReference(index);
            if (axisConf == 1) axisRef = (int)_maxAxisValue / 2;
            else axisRef = 0;
        }

        _iReport.bHats = _iReport.bHatsEx1 = _iReport.bHatsEx2 = _iReport.bHatsEx3 = 0xFFFFFFFF;
    }

    private bool Update()
    {
        _iReport.bDevice = (byte)Id;
        if (_vJoy.UpdateVJD(Id, ref _iReport))
            return true;
        _vJoy.AcquireVJD(Id);
        return false;
    }

    #region POV

    private ref uint GetPovReference(ushort pov)
    {
        switch (pov)
        {
            case 0:
                return ref _iReport.bHats;
            case 1:
                return ref _iReport.bHatsEx1;
            case 2:
                return ref _iReport.bHatsEx2;
            case 3:
                return ref _iReport.bHatsEx3;
        }

        return ref _iReport.bHats;
    }

    private uint GetPovDirection(ushort direction)
    {
        switch (direction)
        {
            case 1:
            default:
                return 0xFFFFFFFF;
        }
    }

    public void SetPovSwitch(ushort pov, uint direction)
    {
        lock (_updateLockObject)
        {
            ref var povRef = ref GetPovReference(pov);
            if (direction == 0) povRef = 0xFFFFFFFF;
            else povRef = (direction - 1) * 4500;
            Update();
        }
    }

    #endregion

    #region Axis

    public float GetCurrentAxisValue(ushort axis)
    {
        lock (_updateLockObject)
        {
            if (_maxAxisValue == 0) return 0;
            return (float)GetAxisReference(axis) / _maxAxisValue;
        }
    }

    private ref int GetAxisReference(ushort axis)
    {
        switch (axis)
        {
            case 0:
                return ref _iReport.AxisX;
            case 1:
                return ref _iReport.AxisY;
            case 2:
                return ref _iReport.AxisZ;
            case 3:
                return ref _iReport.AxisXRot;
            case 4:
                return ref _iReport.AxisYRot;
            case 5:
                return ref _iReport.AxisZRot;
            case 6:
                return ref _iReport.Slider;
            case 7:
                return ref _iReport.Dial;
        }

        return ref _iReport.AxisX;
    }

    public bool SetAxis(ushort axis, float percent, out float newValue)
    {
        newValue = 0;
        if (_maxAxisValue == 0) return false;
        lock (_updateLockObject)
        {
            ref var axisRef = ref GetAxisReference(axis);
            var value = (int)(_maxAxisValue / 100.0 * percent);
            axisRef = Math.Clamp(value, 0, (int)_maxAxisValue);

            newValue = (float)axisRef / _maxAxisValue;
            return Update();
        }
    }

    public bool MoveAxis(ushort axis, double percent, out float newValue)
    {
        newValue = 0;
        if (_maxAxisValue == 0) return false;
        lock (_updateLockObject)
        {
            ref var axisRef = ref GetAxisReference(axis);
            var value = (int)(_maxAxisValue / 100.0 * percent);
            axisRef = Math.Clamp(axisRef + value, 0, (int)_maxAxisValue);

            newValue = (float)axisRef / _maxAxisValue;
            return Update();
        }
    }

    #endregion

    #region Buttons

    public bool ButtonState(uint button, ButtonAction action, out bool newState)
    {
        var buttonId = button - 1;
        var arrayIndex = buttonId / 32;
        var bitPosition = buttonId % 32;
        newState = false;

        lock (_updateLockObject)
        {
            switch (arrayIndex)
            {
                case 0: // For 1-32 buttons
                    newState = SetButtonState(ref _iReport.Buttons, bitPosition, action);
                    break;
                case 1: // For 33-64 buttons
                    newState = SetButtonState(ref _iReport.ButtonsEx1, bitPosition, action);
                    break;
                case 2: // For 65-96 buttons
                    newState = SetButtonState(ref _iReport.ButtonsEx2, bitPosition, action);
                    break;
                case 3: // For 97-128 buttons
                    newState = SetButtonState(ref _iReport.ButtonsEx3, bitPosition, action);
                    break;
            }

            return Update();
        }
    }

    private bool SetButtonState(ref uint buttons, uint bitPosition, ButtonAction action)
    {
        switch (action)
        {
            case ButtonAction.Toggle:
                buttons ^= 1U << (int)bitPosition;
                return (buttons & (1u << (int)bitPosition)) != 0;
            case ButtonAction.Down:
                buttons |= 1u << (int)bitPosition;
                return true;
            case ButtonAction.Up:
                buttons &= ~(1u << (int)bitPosition);
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    #endregion
}
