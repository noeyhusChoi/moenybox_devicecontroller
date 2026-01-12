using KIOSK.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KIOSK;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindowView : Window
{
    public MainWindowView()
    {
        InitializeComponent();

        //Cursor = Cursors.None;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            try
            {
                // 부팅 초기화 실행
                await vm.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"장비 초기화 중 오류가 발생했습니다.\n{ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}