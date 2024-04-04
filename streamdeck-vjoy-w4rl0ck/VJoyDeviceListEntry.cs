using Newtonsoft.Json;

namespace streamdeck_vjoy_w4rl0ck;

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