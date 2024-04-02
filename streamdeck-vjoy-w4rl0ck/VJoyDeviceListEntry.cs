using Newtonsoft.Json;

namespace dev.w4rl0ck.streamdeck.vjoy.libs
{
    public class VJoyDeviceListEntry
    {
        public VJoyDeviceListEntry(string deviceName, uint deviceIndex)
        {
            DeviceName = deviceName;
            DeviceIndex = (int)deviceIndex;
        }

        [JsonProperty(PropertyName = "vJoyName")]
        public string DeviceName { get; set; }

        [JsonProperty(PropertyName = "vJoyIndex")]
        public int DeviceIndex { get; set; }
    }
}