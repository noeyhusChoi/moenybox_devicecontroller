namespace Kiosk.Application.Models.TaxRefund;

public sealed record TaxRefundCustomerLookupResult(
    string? PassportSerialNo,
    string Provider,
    string? Message);
