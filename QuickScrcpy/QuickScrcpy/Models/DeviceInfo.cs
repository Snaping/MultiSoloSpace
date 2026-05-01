
using System;

namespace QuickScrcpy.Models
{
    public class DeviceInfo
    {
        public string? IPAddress { get; set; }
        public string? MACAddress { get; set; }
        public string? DeviceName { get; set; }
        public string? Manufacturer { get; set; }
        public bool IsAndroid { get; set; }
        public int Port { get; set; } = 5555;
        public bool IsConnected { get; set; }
        public string? Status { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
