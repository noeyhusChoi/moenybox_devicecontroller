using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using KIOSK.Infrastructure.UI;
using KIOSK.Infrastructure.UI.Navigation;

namespace KIOSK.Modules.GTF.ViewModels
{
    public partial class GtfTestPopupViewModel : ObservableObject
    {
        private readonly IPopupService _popup;

        [ObservableProperty]
        private string textItem = "This is a test popup for GTF feature.";

        public GtfTestPopupViewModel(IPopupService popup)
        {
            _popup = popup;
        }

        [RelayCommand]
        private async Task Close()
        {
            _popup.CloseLocal();
        }
    }
}
