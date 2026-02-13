using KIOSK.Domain.Transactions;

namespace KIOSK.Application.Features.ExchangeV2.UseCases
{
    public sealed record ExchangeV2PayoutRequest(PayoutMethodType Method, decimal Amount);
}
