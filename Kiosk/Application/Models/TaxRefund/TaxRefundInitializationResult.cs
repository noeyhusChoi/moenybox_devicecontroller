namespace Kiosk.Application.Models.TaxRefund;

public sealed record TaxRefundInitializationResult(
    string? KioskNo,
    string? KioskType,
    string? RefundLimitAmount,
    string Provider,
    string? Message);
