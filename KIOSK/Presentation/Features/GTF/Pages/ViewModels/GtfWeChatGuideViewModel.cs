using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Presentation.Abstractions;

namespace KIOSK.Presentation.Features.GTF.Pages.ViewModels;

public partial class GtfWeChatGuideViewModel : PageViewModelBase
{
    public override Task OnLoadAsync(object? parameter, CancellationToken ct) => Task.CompletedTask;

    public override Task OnUnloadAsync() => Task.CompletedTask;

    [RelayCommand]
    private Task Main(object? parameter) => ExecuteStepAsync(OnStepMain, parameter);

    [RelayCommand]
    private Task Previous(object? parameter) => ExecuteStepAsync(OnStepPrevious, parameter);

    [RelayCommand]
    private Task Next(object? parameter) => ExecuteStepAsync(OnStepNext, parameter);
}
