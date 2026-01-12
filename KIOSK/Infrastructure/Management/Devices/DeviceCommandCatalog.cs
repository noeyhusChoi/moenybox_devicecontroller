using System;
using System.Collections.Generic;
using System.Linq;

namespace KIOSK.Infrastructure.Management.Devices;

public sealed record DeviceCommandDescriptor(string Name, string Description = "");

public interface IDeviceCommandCatalog
{
    IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceName);
    IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll();
}

/// <summary>
    /// "장치 타입" 기준으로 UI에 노출할 명령 목록을 제공한다.
/// (장치 코드와 분리해서, UI가 장치 내부 구현에 직접 의존하지 않도록 한다.)
/// </summary>
public sealed class DeviceCommandCatalog : IDeviceCommandCatalog
{
    private readonly IDeviceHost _runtime;
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> _commands;

    public DeviceCommandCatalog(
        IDeviceHost runtime)
    {
        _runtime = runtime;
        _commands = new Dictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>>(StringComparer.OrdinalIgnoreCase)
        {
            ["QR"] = QrCommands,
            ["PRINTER"] = PrinterCommands,
            ["IDSCANNER"] = IdScannerCommands,
            ["WITHDRAWAL"] = WithdrawalCommands,
            ["DEPOSIT"] = DepositCommands,
        };
    }

    public IReadOnlyCollection<DeviceCommandDescriptor> GetFor(string deviceName)
    {
        if (!_runtime.TryGetSupervisor(deviceName, out var sup))
            return Array.Empty<DeviceCommandDescriptor>();

        return GetByDeviceType(sup.DeviceType);
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<DeviceCommandDescriptor>> GetAll()
    {
        return _runtime.GetAllSupervisors()
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                s => s.Name,
                s => (IReadOnlyCollection<DeviceCommandDescriptor>)GetByDeviceType(s.DeviceType),
                StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyCollection<DeviceCommandDescriptor> GetByDeviceType(string deviceType)
    {
        if (_commands.TryGetValue(deviceType, out var commands))
            return commands;

        return Array.Empty<DeviceCommandDescriptor>();
    }

    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> QrCommands = new[]
    {
        new DeviceCommandDescriptor("SCAN_ENABLE", "스캔 활성화"),
        new DeviceCommandDescriptor("SCAN_DISABLE", "스캔 비활성화"),
        new DeviceCommandDescriptor("START_DECODE", "디코드 시작"),
        new DeviceCommandDescriptor("STOP_DECODE", "디코드 중지"),
        new DeviceCommandDescriptor("RESET", "리셋"),
        new DeviceCommandDescriptor("RESTART", "재시작"),
        new DeviceCommandDescriptor("SCAN.ONCE", "QR 단일 스캔"),
        new DeviceCommandDescriptor("SCAN.MANY", "QR 다중 스캔"),
        new DeviceCommandDescriptor("SCAN.TRIGGERON", "트리거 ON"),
        new DeviceCommandDescriptor("SCAN.TRIGGEROFF", "트리거 OFF"),
        new DeviceCommandDescriptor("SCAN.READ", "버퍼 읽기"),
        new DeviceCommandDescriptor("SET_HOST_TRIGGER", "Host Trigger 모드"),
        new DeviceCommandDescriptor("SET_AUTO_TRIGGER", "Auto-Induction 모드"),
        new DeviceCommandDescriptor("SET_PACKET_MODE", "Packet 모드"),
        new DeviceCommandDescriptor("REQUEST_REVISION", "Revision 조회"),
    };

    private static readonly IReadOnlyCollection<DeviceCommandDescriptor> PrinterCommands = new[]
    {
        new DeviceCommandDescriptor("RESTART", "장치 재시작"),
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
