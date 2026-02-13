using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace KIOSK.Presentation.Abstractions
{
    public abstract partial class LayoutViewModelBase : ObservableObject, ILayout
    {
        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private object? popupContent;

        public abstract Task OnLoadAsync(object? parameter, CancellationToken ct);
        public abstract Task OnUnloadAsync();

        partial void OnCurrentPageChanged(object? value)
        {
            OnCurrentPageChangedCore(value);
        }

        protected virtual void OnCurrentPageChangedCore(object? value)
        {
        }
    }
}
