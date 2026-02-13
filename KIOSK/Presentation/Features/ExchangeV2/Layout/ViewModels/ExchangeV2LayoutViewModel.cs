using CommunityToolkit.Mvvm.ComponentModel;
using KIOSK.Application.Features.ExchangeV2.Services;
using KIOSK.Domain.Transactions;
using KIOSK.Presentation.Abstractions;
using KIOSK.Presentation.Features.ExchangeV2.Flow;
using KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels;

namespace KIOSK.Presentation.Features.ExchangeV2.Layout.ViewModels
{
    public partial class ExchangeV2LayoutViewModel : ObservableObject, ILayout
    {
        private readonly ExchangeV2FlowCoordinator _flow;
        private readonly IExchangeV2TransactionContext _transactionContext;

        public ExchangeV2FlowHeaderViewModel FlowHeader { get; }

        public ExchangeV2LayoutViewModel(
            ExchangeV2FlowCoordinator flow,
            ExchangeV2FlowHeaderViewModel flowHeader,
            IExchangeV2TransactionContext transactionContext)
        {
            _flow = flow;
            FlowHeader = flowHeader;
            _transactionContext = transactionContext;
        }

        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private object? popupContent;

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            _transactionContext.Start(ServiceType.Exchange, "KRW");
            await _flow.StartAsync();
        }

        public async Task OnUnloadAsync()
        {
            await Task.CompletedTask;
        }

        partial void OnCurrentPageChanged(object? value)
        {
            FlowHeader.UpdateForView(value);
        }
    }
}
