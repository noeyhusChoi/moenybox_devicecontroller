using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Threading;

namespace KIOSK.ViewModels
{
    public partial class ExchangeCompleteViewModel : ObservableObject, IStepMain, IStepError, INavigable
    {
        public Func<Task>? OnStepMain { get; set; }
        public Action<Exception>? OnStepError { get; set; }

        public ExchangeCompleteViewModel()
        {

        }

        public async Task OnLoadAsync(object? parameter, CancellationToken ct)
        {
            try
            {
                // 화면 표시 후 5초 대기
                await Task.Delay(TimeSpan.FromSeconds(3), ct);


                await System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await Main();
                }), DispatcherPriority.Background);
                // 다음 단계로 자동 진행
            }
            catch (TaskCanceledException)
            {
                // 화면 전환 등으로 취소된 경우 무시
            }
            catch (Exception ex)
            {
                OnStepError?.Invoke(ex);
            }
        }

        public async Task OnUnloadAsync()
        {
            // TODO: 언로드 시 필요한 작업 수행
        }

        #region Commands
        [RelayCommand]
        private async Task Main()
        {
            try
            {
                if (OnStepMain is not null)
                    await OnStepMain();
            }
            catch (Exception ex)
            {
                if (OnStepError is not null)
                    OnStepError(ex);
            }
        }
        #endregion
    }
}
