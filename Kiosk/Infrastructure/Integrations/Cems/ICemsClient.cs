using Kiosk.Infrastructure.Integrations.Cems.Models;
using Kiosk.Infrastructure.Integrations.Cems.Requests;
using Kiosk.Infrastructure.Integrations.Cems.Responses;

namespace Kiosk.Infrastructure.Integrations.Cems;

public interface ICemsClient
{
    Task<CemsCommandResult<CemsGetRateResponse>> GetRateAsync(CemsGetRateRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsGetRateAllResponse>> GetRateAllAsync(CemsGetRateAllRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsCheckLimitResponse>> CheckLimitAsync(CemsCheckLimitRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsRegisterTransactionResponse>> RegisterTransactionAsync(CemsRegisterTransactionRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsSetCashResponse>> SetCashAsync(CemsSetCashRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsPullCashResponse>> PullCashAsync(CemsPullCashRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsIncidentResponse>> ReportErrorAsync(CemsReportErrorRequest request, CancellationToken ct = default);
    Task<CemsCommandResult<CemsSmsResponse>> SendSmsAsync(CemsSendSmsRequest request, CancellationToken ct = default);
}
