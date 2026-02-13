using System.Threading;
using System.Threading.Tasks;
using KIOSK.Domain.Entities;

namespace KIOSK.Application.Features.ExchangeV2.Services
{
    public sealed record ExchangeV2LimitInquiryResult(
        bool IsApproved,
        decimal DailyLimitAmount,
        decimal DailyRemainingAmount,
        decimal PerTransactionLimitAmount,
        string ReasonCode,
        string Message);

    public interface IExchangeV2LimitProvider
    {
        Task<ExchangeV2LimitInquiryResult> InquireAsync(CustomerInfo customer, CancellationToken ct);
    }

    public sealed class ExchangeV2LimitProvider : IExchangeV2LimitProvider
    {
        public Task<ExchangeV2LimitInquiryResult> InquireAsync(CustomerInfo customer, CancellationToken ct)
        {
            var result = new ExchangeV2LimitInquiryResult(
                IsApproved: true,
                DailyLimitAmount: decimal.MaxValue,
                DailyRemainingAmount: decimal.MaxValue,
                PerTransactionLimitAmount: decimal.MaxValue,
                ReasonCode: string.Empty,
                Message: "NOT_CONNECTED");

            return Task.FromResult(result);
        }
    }
}
