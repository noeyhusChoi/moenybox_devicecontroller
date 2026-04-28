using Kiosk.Application.Features.ExchangeV2.StateMachine;

namespace Kiosk.ViewModels;

public sealed record ExchangeEntrySelection(
    ExchangeMethod Method,
    string CurrencyCode,
    decimal ExchangeRate);
