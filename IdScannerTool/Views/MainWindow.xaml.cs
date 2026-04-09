using System.ComponentModel;
using Wpf.Ui.Controls;

namespace IdScannerTool;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            WindowBackdropType = WindowBackdropType.Mica;
        }
    }
}
