using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KIOSK.Views.Exchange.Popup
{
    /// <summary>
    /// ExchangePopupIDScanInfoView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ExchangePopupIDScanInfoView : UserControl
    {
        public ExchangePopupIDScanInfoView()
        {
            InitializeComponent();

            this.Unloaded += _Unloaded;
        }

        private void _Unloaded(object? sender, RoutedEventArgs e)
        {
            //try
            //{
            //    AnimationBehavior.SetSourceUri(GifViewer, null);
            //    AnimationBehavior.SetSourceStream(GifViewer, null);
            //    GifViewer.Source = null;
            //}
            //catch (Exception ex)
            //{
            //    Trace.WriteLine("ReleaseGif failed: " + ex);
            //}
        }
    }
}
