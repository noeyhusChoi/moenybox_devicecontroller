using KIOSK.Device.Abstractions;

namespace KIOSK.Status;

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
/// - STATUS: 외부 Notice 대상
/// - ERROR: 로그 전용
/// - TIMEOUT: 재시도 가능
/// - CONNECT: 오프라인 전환
/// </summary>
public sealed class StandardErrorPolicy : IErrorPolicy
{
    public ErrorPolicyResult Apply(ErrorCode code)
    {
        var category = code.Category.ToUpperInvariant();

        return category switch
        {
            "STATUS" => new ErrorPolicyResult(
                Notify: true,
                Retryable: false,
                SetOffline: false,
                SeverityOverride: null),
            "ERROR" => new ErrorPolicyResult(
                Notify: false,
                Retryable: false,
                SetOffline: false,
                SeverityOverride: null),
            "TIMEOUT" => new ErrorPolicyResult(
                Notify: false,
                Retryable: true,
                SetOffline: false,
                SeverityOverride: Severity.Warning),
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
