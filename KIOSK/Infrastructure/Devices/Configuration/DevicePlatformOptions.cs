using System.Collections.Generic;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Configuration;

public sealed class DevicePlatformOptions
{
    public const string SectionName = "DevicePlatform";

    public List<DeviceDescriptor> Devices { get; set; } = new();
}

