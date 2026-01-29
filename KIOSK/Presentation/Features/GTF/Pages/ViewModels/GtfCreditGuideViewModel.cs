using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Shared.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels
{
    public partial class GtfCreditGuideViewModel : StepViewModelBase
    {
        public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

        public override Task OnUnloadAsync() => Task.CompletedTask;

        #region Commands
        [RelayCommand]
        private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

        [RelayCommand]
        private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

        [RelayCommand]
        private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
        #endregion
    }
}
