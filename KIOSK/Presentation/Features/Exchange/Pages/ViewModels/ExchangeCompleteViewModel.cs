using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.Exchange.Pages.ViewModels
{
    public partial class ExchangeCompleteViewModel : PageViewModelBase
    {
        public override async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);

                await System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await ExecuteStepAsync(OnStepMain, parameter);
                }), DispatcherPriority.Background);
            }
            catch (TaskCanceledException)
            {
                // 화면 전환 등으로 취소된 경우 무시
            }
            catch (Exception ex)
            {
                await RaiseStepErrorAsync(ex);
            }
        }

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);
        #endregion
    }
}
