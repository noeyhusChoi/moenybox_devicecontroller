using System;
using System.Collections.Generic;
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

namespace KIOSK.Assets.Resources.Controls
{
    /// <summary>
    /// ElipseToggleButton.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EllipseToggleButton : UserControl
    {
        public EllipseToggleButton()
        {
            InitializeComponent();
        }

        // 토글 전/후 원 배경색
        public Brush EllipseBackgroundOff
        {
            get => (Brush)GetValue(EllipseBackgroundOffProperty);
            set => SetValue(EllipseBackgroundOffProperty, value);
        }
        public static readonly DependencyProperty EllipseBackgroundOffProperty =
            DependencyProperty.Register(nameof(EllipseBackgroundOff), typeof(Brush), typeof(EllipseToggleButton), new PropertyMetadata(Brushes.LightGray));

        public Brush EllipseBackgroundOn
        {
            get => (Brush)GetValue(EllipseBackgroundOnProperty);
            set => SetValue(EllipseBackgroundOnProperty, value);
        }
        public static readonly DependencyProperty EllipseBackgroundOnProperty =
            DependencyProperty.Register(nameof(EllipseBackgroundOn), typeof(Brush), typeof(EllipseToggleButton), new PropertyMetadata(Brushes.DodgerBlue));

        // 토글 전/후 버튼 배경색
        public Brush ButtonBackgroundOff
        {
            get => (Brush)GetValue(ButtonBackgroundOffProperty);
            set => SetValue(ButtonBackgroundOffProperty, value);
        }
        public static readonly DependencyProperty ButtonBackgroundOffProperty =
            DependencyProperty.Register(nameof(ButtonBackgroundOff), typeof(Brush), typeof(EllipseToggleButton), new PropertyMetadata(Brushes.Transparent));

        public Brush ButtonBackgroundOn
        {
            get => (Brush)GetValue(ButtonBackgroundOnProperty);
            set => SetValue(ButtonBackgroundOnProperty, value);
        }
        public static readonly DependencyProperty ButtonBackgroundOnProperty =
            DependencyProperty.Register(nameof(ButtonBackgroundOn), typeof(Brush), typeof(EllipseToggleButton), new PropertyMetadata(Brushes.DarkSlateBlue));

        // 체크 마크 색상
        public Brush TickStroke
        {
            get => (Brush)GetValue(TickStrokeProperty);
            set => SetValue(TickStrokeProperty, value);
        }
        public static readonly DependencyProperty TickStrokeProperty =
            DependencyProperty.Register(nameof(TickStroke), typeof(Brush), typeof(EllipseToggleButton), new PropertyMetadata(Brushes.Gray));

        public Brush TickStrokeChecked
        {
            get => (Brush)GetValue(TickStrokeCheckedProperty);
            set => SetValue(TickStrokeCheckedProperty, value);
        }
        public static readonly DependencyProperty TickStrokeCheckedProperty =
            DependencyProperty.Register(nameof(TickStrokeChecked), typeof(Brush), typeof(EllipseToggleButton), new PropertyMetadata(Brushes.White));
    }
}

