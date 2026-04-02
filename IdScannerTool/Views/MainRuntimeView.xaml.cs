using IdScannerTool.ViewModels;
using System.Windows.Controls;
using System.Windows.Data;

namespace IdScannerTool.Views;

public partial class MainRuntimeView : UserControl
{
    public MainRuntimeView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not MainRuntimeViewModel viewModel)
        {
            return;
        }

        viewModel.EnsureDefaultHistoryDateRange();
        StartDatePicker.GetBindingExpression(DatePicker.SelectedDateProperty)?.UpdateTarget();
        EndDatePicker.GetBindingExpression(DatePicker.SelectedDateProperty)?.UpdateTarget();
    }
}
