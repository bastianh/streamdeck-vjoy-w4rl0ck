using BarRaider.SdTools;
using vJoyInterfaceWrap;

namespace streamdeck_vjoy_w4rl0ck.Utils;

public delegate void ButtonSignalHandler(uint button, bool active);

public delegate void AxisSignalHandler(uint axis, float value);

public delegate void VJoyStatusUpdateHandler();

public sealed class SimpleVJoyInterface
{
    public enum ButtonAction
    {
        Up,
        Down,
        Toggle
    }

    public enum VJoyStatus
    {
        Initialized,
        Deactivated,
        VJoyDeviceNotExistent,
        VJoyDeviceBusy,
        Connected,
        Disconnected
    }

    private static SimpleVJoyInterface _instance;
    private static readonly object SingletonLockObject = new();
    private readonly Configuration _configuration;
    private readonly Dictionary<uint, VJoyDevice> _devices = new();
    private readonly vJoy _vJoy;
    private VJoyStatus _interfaceStatus;

    private SimpleVJoyInterface()
    {
        _vJoy = new vJoy();
        ChangeStatus(VJoyStatus.Initialized);
        if (!_vJoy.vJoyEnabled()) ChangeStatus(VJoyStatus.Deactivated);
        _configuration = Configuration.Instance;
    }

    public uint CurrentVJoyId { get; private set; }
    public VJoyStatus Status => CurrentDevice?.Status ?? _interfaceStatus;

    private VJoyDevice CurrentDevice => _devices.GetValueOrDefault(CurrentVJoyId);

    #region Singleton

    public static SimpleVJoyInterface Instance
    {
        get
        {
            lock (SingletonLockObject) // Ensure thread safety
            {
                return _instance ??= new SimpleVJoyInterface();
            }
        }
    }

    #endregion

    public static event AxisSignalHandler AxisSignal;
    public static event ButtonSignalHandler UpdateButtonSignal;
    public static event VJoyStatusUpdateHandler VJoyStatusUpdateSignal;


    public List<VJoyDeviceListEntry> ConfiguredDevices()
    {
        var result = new List<VJoyDeviceListEntry>();

        for (uint i = 1; i <= 16; i++)
        {
            var status = _vJoy.GetVJDStatus(i);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                case VjdStat.VJD_STAT_FREE:
                case VjdStat.VJD_STAT_BUSY:
                    result.Add(new VJoyDeviceListEntry(i, status));
                    break;
                default:
                    continue;
            }
        }

        return result;
    }

    private TracingLevel GetTracingLevelForStatus(VJoyStatus status)
    {
        switch (status)
        {
            case VJoyStatus.VJoyDeviceNotExistent:
            case VJoyStatus.VJoyDeviceBusy:
                return TracingLevel.ERROR;
            default:
                return TracingLevel.INFO;
        }
    }

    private void ChangeStatus(VJoyStatus status)
    {
        _interfaceStatus = status;
        var level = GetTracingLevelForStatus(status);
        Logger.Instance.LogMessage(level, $"vJoy device '{CurrentVJoyId}' status is now '{status}'");
        SendStatusUpdateSignal();
    }

    public void SendStatusUpdateSignal()
    {
        VJoyStatusUpdateSignal?.Invoke();
    }

    public void SetPovSwitch(ushort pov, uint direction)
    {
        CurrentDevice?.SetPovSwitch(pov, direction);
    }

    public void ConnectToVJoy(uint id)
    {
        lock (SingletonLockObject) // Ensure thread safety
        {
            var currentDevice = CurrentDevice;
            if (currentDevice != null && currentDevice.Id == id && currentDevice.IsOwned) return;
            if (currentDevice != null) DisconnectFromVJoy();
            if (!_vJoy.vJoyEnabled())
            {
                ChangeStatus(VJoyStatus.Deactivated);
                return;
            }

            var device = new VJoyDevice(_vJoy, _configuration, id);
            var status = device.Acquire();
            if (status == VJoyStatus.Connected)
            {
                _devices[id] = device;
                CurrentVJoyId = id;
            }

            ChangeStatus(status);
        }
    }

    private void DisconnectFromVJoy()
    {
        var device = CurrentDevice;
        if (device == null) return;
        device.Relinquish();
        _devices.Remove(device.Id);
        ChangeStatus(VJoyStatus.Disconnected);
        CurrentVJoyId = 0;
    }

    #region Axis

    public float GetCurrentAxisValue(ushort axis)
    {
        return CurrentDevice?.GetCurrentAxisValue(axis) ?? 0;
    }

    public void SetAxis(ushort axis, float percent)
    {
        var device = CurrentDevice;
        if (device == null) return;
        if (device.SetAxis(axis, percent, out var value)) AxisSignal?.Invoke(axis, value);
    }

    public void MoveAxis(ushort axis, double percent)
    {
        var device = CurrentDevice;
        if (device == null) return;
        if (device.MoveAxis(axis, percent, out var value)) AxisSignal?.Invoke(axis, value);
    }

    #endregion

    #region Buttons

    public void ButtonState(uint button, ButtonAction action)
    {
        var device = CurrentDevice;
        if (device == null) return;
        if (device.ButtonState(button, action, out var newState)) UpdateButtonSignal?.Invoke(button, newState);
    }

    #endregion
}
