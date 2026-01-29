using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIOSK.Application.Services.ExchangeV2;
using KIOSK.Presentation.Features.ExchangeV2.Flow;
using KIOSK.Presentation.Features.ExchangeV2.Pages.ViewModels;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.ExchangeV2.Shell.ViewModels
{
    public partial class ExchangeV2ShellViewModel : ObservableObject, ILayout
    {
        private readonly ExchangeV2FlowCoordinator _flow;
        private readonly IExchangeV2TransactionContext _transactionContext;
        public ExchangeV2FlowHeaderViewModel FlowHeader { get; }

        public ExchangeV2ShellViewModel(
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
            _transactionContext.Start(Domain.Entities.ExchangeTransactionType.Unknown);
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
