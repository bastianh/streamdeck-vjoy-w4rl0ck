using Newtonsoft.Json;
using vJoyInterfaceWrap;

namespace streamdeck_vjoy_w4rl0ck.Utils;

public class VJoyDeviceListEntry
{
    public VJoyDeviceListEntry(uint deviceIndex, VjdStat status)
    {
        DeviceIndex = (int)deviceIndex;
        Status = status.ToString();
    }

    [JsonProperty(PropertyName = "vJoyIndex")]
    public int DeviceIndex { get; set; }

    [JsonProperty(PropertyName = "vJoyStatus")]
    public string Status { get; set; }
}