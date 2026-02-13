using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels
{
    public partial class ExchangeV2IdScanCompleteViewModel : PageViewModelBase
    {
        private readonly IExchangeV2TransactionContext _tx;

        [ObservableProperty] private string idType = string.Empty;
        [ObservableProperty] private string customerName = string.Empty;
        [ObservableProperty] private string customerNumber = string.Empty;
        [ObservableProperty] private string customerNationality = string.Empty;

        public ExchangeV2IdScanCompleteViewModel(IExchangeV2TransactionContext tx)
        {
            _tx = tx;
        }

        public override Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            var customer = _tx.Current.Compliance.Customer;
            IdType = customer?.IdType ?? string.Empty;
            CustomerName = customer?.Name ?? string.Empty;
            CustomerNumber = customer?.IdNumber ?? string.Empty;
            CustomerNationality = customer?.Nationality ?? string.Empty;
            return Task.CompletedTask;
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
    }
}
