using System;
using System.Collections.Generic;
using BarRaider.SdTools;
using vJoyInterfaceWrap;


namespace dev.w4rl0ck.streamdeck.vjoy.libs
{
    public delegate void ButtonSignalHandler(uint button, bool active);

    public delegate void VJoyStatusSignalHandler(SimpleVJoyInterface.VJoyStatus status);
    
    public sealed class SimpleVJoyInterface
    {
        private static SimpleVJoyInterface _instance;
        private static readonly object SingletonLockObject = new object();
        private readonly object _updateLockObject = new object();
        private readonly vJoy _vJoy;
        private vJoy.JoystickState _iReport;
        public uint CurrentVJoyId;
        public VJoyStatus Status;

        public static event ButtonSignalHandler UpdateButtonSignal;
        public static event VJoyStatusSignalHandler VJoyStatusSignal;
        private long _maxval;

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
            Disconnected,
        }
        
        private SimpleVJoyInterface()
        {
            _vJoy = new vJoy();
            _iReport = new vJoy.JoystickState();
            ChangeStatus(VJoyStatus.Initialized);
            if (!_vJoy.vJoyEnabled()) ChangeStatus(VJoyStatus.Deactivated);
        }
        
        #region Singleton

        public static SimpleVJoyInterface Instance
        {
            get
            {
                lock (SingletonLockObject) // Ensure thread safety
                {
                    if (_instance == null) _instance = new SimpleVJoyInterface();
                    return _instance;
                }
            }
        }

        #endregion

        public List<VJoyDeviceListEntry> CheckAvailableDevices()
        {
            var result = new List<VJoyDeviceListEntry>();
            for (uint i = 1; i <= 16; i++)
            {
                var status = _vJoy.GetVJDStatus(i);
                var message = "vJoy Device #" + i;
                switch (status)
                {
                    case VjdStat.VJD_STAT_OWN:
                        message += " (active)";
                        break;
                    case VjdStat.VJD_STAT_FREE:
                        message += " (available)";
                        break;
                    case VjdStat.VJD_STAT_BUSY:
                        message += " (BUSY)";
                        break;
                    default:
                        continue;
                }

                var device = new VJoyDeviceListEntry(message, i);
                result.Add(device);
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
            Status = status;
            var level = GetTracingLevelForStatus(status);
            Logger.Instance.LogMessage(level, $"vJoy device '{CurrentVJoyId}' status is now '{status}'");
            VJoyStatusSignal?.Invoke(status);
        }
        
        public float MoveAxis(ushort axis, int value)
        {
            if (_maxval == 0) return 0;
            ref int axisRef = ref _iReport.AxisX;
            switch (axis)
            {
                case 0:
                    axisRef = ref _iReport.AxisX;
                    break;
                case 1:
                    axisRef = ref _iReport.AxisY;
                    break;
                case 2:
                    axisRef = ref _iReport.AxisZ;
                    break;
                case 3:
                    axisRef = ref _iReport.AxisXRot;
                    break;
                case 4:
                    axisRef = ref _iReport.AxisYRot;
                    break;
                case 5:
                    axisRef = ref _iReport.AxisZRot;
                    break;
                case 6:
                    axisRef = ref _iReport.Slider;
                    break;
                case 7:
                    axisRef = ref _iReport.Dial;
                    break;
            }
            
            axisRef += value;
            
            if (axisRef > _maxval) axisRef = (int)_maxval;
            else if (axisRef < 0) axisRef = 0;

            if (UpdateVJoy()) return (float)axisRef / _maxval;
            return 0;
        }
        
        public void ButtonState(uint button, ButtonAction action)
        {
            var buttonId = button - 1;
            var arrayIndex = buttonId / 32;
            var bitPosition = buttonId % 32;
            var newState = false;


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

                if (UpdateVJoy()) UpdateButtonSignal?.Invoke(button, newState);
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

        public void ConnectToVJoy(uint id)
        {
            lock (SingletonLockObject) // Ensure thread safety
            {
                if (CurrentVJoyId == id && _vJoy.GetVJDStatus(CurrentVJoyId) == VjdStat.VJD_STAT_OWN) return;
                if (CurrentVJoyId > 0) DisconnectFromVJoy();
                if (!_vJoy.vJoyEnabled())
                {
                    ChangeStatus(VJoyStatus.Deactivated);
                    return;
                }

                if (!_vJoy.isVJDExists(id))
                {
                    ChangeStatus(VJoyStatus.VJoyDeviceNotExistent);
                    return;
                }

                if (!_vJoy.AcquireVJD(id))
                {
                    ChangeStatus(VJoyStatus.VJoyDeviceBusy);
                    return;
                }

                CurrentVJoyId = id;
                _vJoy.ResetVJD(id);
                _vJoy.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref _maxval);
                Logger.Instance.LogMessage(TracingLevel.DEBUG, $"vJoy Device: {id}, axis maxval is now '{_maxval}'");
                if (_maxval == 0) // TODO: find out why that happens sometimes
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"overwriting maxval to 32767 :(");
                    _maxval = 32767;
                }
                UpdateVJoy();
                ChangeStatus(VJoyStatus.Connected);
            }
        }

        private bool UpdateVJoy()
        {
            _iReport.bDevice = (byte)CurrentVJoyId;
            _iReport.bHats = _iReport.bHatsEx1 =_iReport.bHatsEx2 =_iReport.bHatsEx3 =0xFFFFFFFF;
            if (_vJoy.UpdateVJD(CurrentVJoyId, ref _iReport))
                return true;
            _vJoy.AcquireVJD(CurrentVJoyId);
            return false;
        }

        private void DisconnectFromVJoy()
        {
            if (CurrentVJoyId == 0) return;
            _vJoy.RelinquishVJD(CurrentVJoyId);
            ChangeStatus(VJoyStatus.Disconnected);
            CurrentVJoyId = 0;
        }
    }
}