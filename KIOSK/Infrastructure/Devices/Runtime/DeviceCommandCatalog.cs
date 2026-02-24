using System;
using System.Collections.Generic;
using System.Linq;

namespace KIOSK.Infrastructure.Devices.Runtime;

public sealed record DeviceCommandDescriptor(string Name, string Description = "");

public interface IDeviceCommandCatalog
{
    IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceId);
    IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll();
}

/// <summary>
    /// "장치 타입" 기준으로 UI에 노출할 명령 목록을 제공한다.
/// (장치 코드와 분리해서, UI가 장치 내부 구현에 직접 의존하지 않도록 한다.)
/// </summary>
public sealed class DeviceCommandCatalog : IDeviceCommandCatalog
{
    private readonly IDeviceManager _runtime;
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> _commands;

    public DeviceCommandCatalog(
        IDeviceManager runtime)
    {
        _runtime = runtime;
        _commands = new Dictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PRINTER"] = PrinterCommands,
            ["IDSCANNER"] = IdScannerCommands,
            ["WITHDRAWAL"] = WithdrawalCommands,
            ["DEPOSIT"] = DepositCommands,
        };
    }

    public IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceId)
    {
        if (!_runtime.TryGetDevice(deviceId, out var device))
            return Array.Empty<DeviceCommandDescriptor>();

        return GetByDeviceType(device.DeviceType);
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll()
    {
        return _runtime.GetAllDevices()
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                s => s.DeviceId,
                s => (IReadOnlyCollection<DeviceCommandDescriptor>)GetByDeviceType(s.DeviceType),
                StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyCollection<DeviceCommandDescriptor> GetByDeviceType(string deviceType)
    {
        if (_commands.TryGetValue(deviceType, out var commands))
            return commands;

        return Array.Empty<DeviceCommandDescriptor>();
    }

    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> PrinterCommands = new[]
    {
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("PRINTCONTENT", "본문 인쇄"),
        new DeviceCommandDescriptor("PRINTTITLE", "제목 인쇄"),
        new DeviceCommandDescriptor("CUT", "용지 컷"),
        new DeviceCommandDescriptor("QR", "QR 코드 인쇄"),
        new DeviceCommandDescriptor("ALIGN", "정렬 설정"),
    };

    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> IdScannerCommands = new[]
    {
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("SCANSTART", "스캔 시작"),
        new DeviceCommandDescriptor("SCANSTOP", "스캔 중지"),
        new DeviceCommandDescriptor("GETSCANSTATUS", "스캔 상태 조회"),
        new DeviceCommandDescriptor("SAVEIMAGE", "이미지 저장"),
    };

    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> WithdrawalCommands = new[]
    {
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("SENSOR", "센서 조회"),
        new DeviceCommandDescriptor("INIT", "초기화"),
        new DeviceCommandDescriptor("VERSION", "버전 조회"),
        new DeviceCommandDescriptor("DISPENSE", "지폐 방출"),
        new DeviceCommandDescriptor("EJECT", "방출/회수"),
    };

    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> DepositCommands = new[]
    {
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("START", "입금 시작"),
        new DeviceCommandDescriptor("STOP", "입금 중지"),
        new DeviceCommandDescriptor("STACK", "스택 처리"),
        new DeviceCommandDescriptor("RETURN", "리턴 처리"),
    };
}
