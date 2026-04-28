using Kiosk.Infrastructure.Media;
using Kiosk.ViewModels;
using Kiosk.Views;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Kiosk;

public partial class MainWindowView : Window
{
    private const double AccessibilityPanThreshold = 8.0;
    private static readonly string ButtonClickSoundPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Assets",
        "Sound",
        "sfx_click.wav");

    private readonly IAudioPlayService _audioPlayService;
    private bool _isAccessibilityPanPending;
    private bool _isAccessibilityPanning;
    private Point _accessibilityPanStartPoint;
    private Point _lastAccessibilityPanPoint;

    public MainWindowView(IAudioPlayService audioPlayService)
    {
        _audioPlayService = audioPlayService;
        InitializeComponent();
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonBaseClick), true);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        try
        {
            await vm.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"초기화 중 오류가 발생했습니다.\n{ex.Message}",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.CanAccessibilityPan())
            return;

        if (e.OriginalSource is DependencyObject source && FindAncestor<UtilityBarView>(source) is not null)
            return;

        _isAccessibilityPanPending = true;
        _isAccessibilityPanning = false;
        _accessibilityPanStartPoint = e.GetPosition(this);
        _lastAccessibilityPanPoint = _accessibilityPanStartPoint;
    }

    private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if ((!_isAccessibilityPanPending && !_isAccessibilityPanning) || DataContext is not MainWindowViewModel vm || !vm.CanAccessibilityPan())
            return;

        var currentPoint = e.GetPosition(this);

        if (!_isAccessibilityPanning)
        {
            var distance = currentPoint - _accessibilityPanStartPoint;
            if (Math.Abs(distance.X) < AccessibilityPanThreshold && Math.Abs(distance.Y) < AccessibilityPanThreshold)
                return;

            _isAccessibilityPanPending = false;
            _isAccessibilityPanning = true;
            Mouse.Capture(this);
        }

        var delta = currentPoint - _lastAccessibilityPanPoint;
        _lastAccessibilityPanPoint = currentPoint;
        vm.ApplyAccessibilityPanDelta(delta.X, delta.Y);
    }

    private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isAccessibilityPanPending = false;

        if (!_isAccessibilityPanning)
            return;

        _isAccessibilityPanning = false;
        Mouse.Capture(null);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1)
            return;

        MoveToNextMonitor();
        e.Handled = true;
    }

    private void OnButtonBaseClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
            return;

        if (FindAncestor<ButtonBase>(source) is null)
            return;

        _audioPlayService.Play(ButtonClickSoundPath);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        DependencyObject? node = current;

        while (node is not null)
        {
            if (node is T matched)
                return matched;

            node = node switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(node),
                FrameworkContentElement contentElement => contentElement.Parent ?? LogicalTreeHelper.GetParent(contentElement),
                _ => LogicalTreeHelper.GetParent(node)
            };
        }

        return null;
    }

    private void MoveToNextMonitor()
    {
        var monitors = MonitorInfo.Enumerate();
        if (monitors.Count <= 1)
            return;

        var currentBounds = new Rect(Left, Top, ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height);
        var centerX = currentBounds.Left + (currentBounds.Width / 2d);
        var centerY = currentBounds.Top + (currentBounds.Height / 2d);

        var currentIndex = monitors.FindIndex(monitor => monitor.WorkArea.Contains(new Point(centerX, centerY)));
        if (currentIndex < 0)
            currentIndex = 0;

        var currentMonitor = monitors[currentIndex];
        var nextMonitor = monitors[(currentIndex + 1) % monitors.Count];
        MoveToMonitor(currentMonitor, nextMonitor);
    }

    private void MoveToMonitor(MonitorInfo currentMonitor, MonitorInfo nextMonitor)
    {
        Left += nextMonitor.Bounds.Left - currentMonitor.Bounds.Left;
        Top += nextMonitor.Bounds.Top - currentMonitor.Bounds.Top;
    }

    private sealed record MonitorInfo(Rect Bounds, Rect WorkArea)
    {
        public static List<MonitorInfo> Enumerate()
        {
            var monitors = new List<MonitorInfo>();

            EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                (monitor, _, _, _) =>
                {
                    var info = new MONITORINFOEX();
                    info.cbSize = Marshal.SizeOf<MONITORINFOEX>();

                    if (!GetMonitorInfo(monitor, ref info))
                        return true;

                    monitors.Add(
                        new MonitorInfo(
                            new Rect(
                                info.rcMonitor.Left,
                                info.rcMonitor.Top,
                                info.rcMonitor.Right - info.rcMonitor.Left,
                                info.rcMonitor.Bottom - info.rcMonitor.Top),
                            new Rect(
                                info.rcWork.Left,
                                info.rcWork.Top,
                                info.rcWork.Right - info.rcWork.Left,
                                info.rcWork.Bottom - info.rcWork.Top)));

                    return true;
                },
                IntPtr.Zero);

            return monitors;
        }
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
