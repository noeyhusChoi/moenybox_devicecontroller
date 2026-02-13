namespace KIOSK.Application.Features.ExchangeV2.Services
{
    public enum DepositApplyStatus
    {
        Stacked,
        ReturnedInvalidInput,
        ReturnedCurrencyMismatch,
        ReturnedLimitNotApproved,
        ReturnedLimitExceeded
    }

    public sealed record DepositApplyResult(
        DepositApplyStatus Status,
        decimal ProjectedSourceAmount,
        decimal ProjectedConvertedAmount,
        string ReasonCode,
        string Message)
    {
        public bool IsStacked => Status == DepositApplyStatus.Stacked;
    }
}
