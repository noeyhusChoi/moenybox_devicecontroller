using System.ComponentModel;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace IdScannerTool;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            WindowBackdropType = WindowBackdropType.Mica;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1)
        {
            return;
        }

        if (DataContext is ViewModels.ShellViewModel shell
            && shell.OpenUpdatePopupCommand.CanExecute(null))
        {
            shell.OpenUpdatePopupCommand.Execute(null);
            e.Handled = true;
        }
    }
}
