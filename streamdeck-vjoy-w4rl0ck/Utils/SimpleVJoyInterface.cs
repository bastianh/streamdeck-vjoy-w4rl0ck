using BarRaider.SdTools;
using vJoyInterfaceWrap;

namespace streamdeck_vjoy_w4rl0ck.Utils;

public delegate void ButtonSignalHandler(uint device, uint button, bool active);

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
        ChangeStatus(VJoyStatus.Initialized, 0);
        if (!_vJoy.vJoyEnabled()) ChangeStatus(VJoyStatus.Deactivated, 0);
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

    private void ChangeStatus(VJoyStatus status, uint deviceId)
    {
        _interfaceStatus = status;
        LogDeviceStatus(deviceId, status);
        SendStatusUpdateSignal();
    }

    private void LogDeviceStatus(uint deviceId, VJoyStatus status)
    {
        var level = GetTracingLevelForStatus(status);
        Logger.Instance.LogMessage(level, $"vJoy device '{deviceId}' status is now '{status}'");
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

            var status = AcquireDevice(id, out var device);
            if (device != null) CurrentVJoyId = id;
            ChangeStatus(status, id);
        }
    }

    /// <summary>
    ///     The device id a key setting points at, without acquiring anything:
    ///     the one it selected, or the configured default for id 0.
    /// </summary>
    public uint ResolveDeviceId(uint id)
    {
        return id > 0 ? id : _configuration.GlobalSettings.VJoyDeviceId;
    }

    /// <summary>
    ///     The device a key drives: the one it selected, or the configured default
    ///     for id 0. Acquired on first use; null when it cannot be acquired.
    /// </summary>
    public VJoyDevice GetOrAcquireDevice(uint id)
    {
        lock (SingletonLockObject) // Ensure thread safety
        {
            var deviceId = ResolveDeviceId(id);
            if (_devices.TryGetValue(deviceId, out var device))
            {
                if (device.IsOwned) return device;
                _devices.Remove(deviceId); // lost to another process
            }

            var status = AcquireDevice(deviceId, out device);
            // A key can be the first to reach the default device, when that device
            // was still busy elsewhere at the time the global settings arrived.
            if (deviceId == _configuration.GlobalSettings.VJoyDeviceId)
            {
                if (device != null) CurrentVJoyId = deviceId;
                ChangeStatus(status, deviceId);
            }
            else
            {
                LogDeviceStatus(deviceId, status);
            }

            return device;
        }
    }

    private VJoyStatus AcquireDevice(uint id, out VJoyDevice device)
    {
        device = null;
        if (id == 0) return VJoyStatus.VJoyDeviceNotExistent;
        if (!_vJoy.vJoyEnabled()) return VJoyStatus.Deactivated;

        var acquired = new VJoyDevice(_vJoy, _configuration, id);
        var status = acquired.Acquire();
        if (status != VJoyStatus.Connected) return status;

        _devices[id] = acquired;
        device = acquired;
        return status;
    }

    private void DisconnectFromVJoy()
    {
        var device = CurrentDevice;
        if (device == null) return;
        device.Release();
        _devices.Remove(device.Id);
        ChangeStatus(VJoyStatus.Disconnected, device.Id);
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

    /// <summary>
    ///     Whether the key's device currently reports that button as pressed.
    ///     False when the device is not held, since nothing of ours is down on a
    ///     device we do not own — releasing one resets it.
    /// </summary>
    public bool GetButtonState(uint deviceId, uint button)
    {
        lock (SingletonLockObject) // Ensure thread safety
        {
            var device = _devices.GetValueOrDefault(ResolveDeviceId(deviceId));
            return device != null && device.IsOwned && device.GetButtonState(button);
        }
    }

    public void ButtonState(uint button, ButtonAction action)
    {
        ButtonState(0, button, action);
    }

    public void ButtonState(uint deviceId, uint button, ButtonAction action)
    {
        var device = GetOrAcquireDevice(deviceId);
        if (device == null) return;
        if (device.ButtonState(button, action, out var newState))
            UpdateButtonSignal?.Invoke(device.Id, button, newState);
    }

    #endregion
}
