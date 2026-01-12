using KIOSK.Device.Abstractions;

namespace KIOSK.Infrastructure.Management.Status;

public record ErrorPolicyResult(
    bool Notify,
    bool Retryable,
    bool SetOffline,
    Severity? SeverityOverride = null);

public interface IErrorPolicy
{
    ErrorPolicyResult Apply(ErrorCode code);
}

/// <summary>
/// 표준 코드 기반 기본 정책.
/// - STATUS: 외부 Notice 대상(세부 TIMEOUT은 경고/비알림)
/// - COMMAND: 명령 결과(알림 없음)
/// - CONNECT: 오프라인 전환
/// </summary>
public sealed class StandardErrorPolicy : IErrorPolicy
{
    public ErrorPolicyResult Apply(ErrorCode code)
    {
        var category = code.Category.ToUpperInvariant();
        var detail = code.Detail.ToUpperInvariant();

        return category switch
        {
            "STATUS" => new ErrorPolicyResult(
                Notify: detail != "TIMEOUT",
                Retryable: detail == "TIMEOUT",
                SetOffline: false,
                SeverityOverride: detail == "TIMEOUT" ? Severity.Warning : null),
            "COMMAND" => new ErrorPolicyResult(
                Notify: false,
                Retryable: detail == "TIMEOUT",
                SetOffline: false,
                SeverityOverride: detail == "TIMEOUT" ? Severity.Warning : null),
            "CONNECT" => new ErrorPolicyResult(
                Notify: true,
                Retryable: true,
                SetOffline: true,
                SeverityOverride: Severity.Error),
            _ => new ErrorPolicyResult(
                Notify: false,
                Retryable: false,
                SetOffline: false,
                SeverityOverride: null)
        };
    }
}
