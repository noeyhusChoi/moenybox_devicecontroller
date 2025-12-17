using System;
using System.Collections.Generic;
using System.Linq;
using KIOSK.Device.Abstractions;

namespace KIOSK.Devices.Management;

public sealed record DeviceCommandDescriptor(string Name, string Description = "");

public interface IDeviceCommandCatalog
{
    IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceName);
    IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll();
}

/// <summary>
/// "장치 모델" 기준으로 UI에 노출할 명령 목록을 제공한다.
/// (장치 코드와 분리해서, UI가 장치 내부 구현에 직접 의존하지 않도록 한다.)
/// </summary>
public sealed class DeviceCommandCatalog : IDeviceCommandCatalog
{
    private readonly IDeviceRuntime _runtime;

    public DeviceCommandCatalog(IDeviceRuntime runtime)
    {
        _runtime = runtime;
    }

    public IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceName)
    {
        if (!_runtime.TryGetSupervisor(deviceName, out var sup))
            return Array.Empty<DeviceCommandDescriptor>();

        return GetByModel(sup.Model);
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll()
    {
        return _runtime.GetAllSupervisors()
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(s => s.Name, s => (IReadOnlyCollection<DeviceCommandDescriptor>)GetByModel(s.Model), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<DeviceCommandDescriptor> GetByModel(string model)
    {
        if (model.Equals("QR_TOTINFO", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                new DeviceCommandDescriptor("SCAN_ENABLE", "스캔 활성화"),
                new DeviceCommandDescriptor("SCAN_DISABLE", "스캔 비활성화"),
                new DeviceCommandDescriptor("START_DECODE", "디코드 시작"),
                new DeviceCommandDescriptor("STOP_DECODE", "디코드 중지"),
                new DeviceCommandDescriptor("RESET", "리셋"),
                new DeviceCommandDescriptor("SET_HOST_TRIGGER", "Host Trigger 모드"),
                new DeviceCommandDescriptor("SET_AUTO_TRIGGER", "Auto-Induction 모드"),
                new DeviceCommandDescriptor("SET_PACKET_MODE", "Packet 모드"),
                new DeviceCommandDescriptor("REQUEST_REVISION", "Revision 조회"),
            };
        }

        return Array.Empty<DeviceCommandDescriptor>();
    }
}

