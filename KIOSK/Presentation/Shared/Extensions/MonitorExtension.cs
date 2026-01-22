using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

public static class MonitorMover
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private static (double dx, double dy) GetDipScale(Window w)
    {
        var h = new WindowInteropHelper(w).Handle;
        if (h != IntPtr.Zero)
        {
            try
            {
                uint dpi = GetDpiForWindow(h);
                double scale = 96.0 / dpi;
                return (scale, scale);
            }
            catch { }
        }
        return (1.0, 1.0);
    }

    public static void MoveToScreen(Window w, Screen screen, bool maximize = false, bool useWorkingArea = true)
    {
        if (w == null || screen == null) return;

        var bounds = useWorkingArea ? screen.WorkingArea : screen.Bounds;
        var (dx, dy) = GetDipScale(w);

        var prevState = w.WindowState;
        w.WindowState = WindowState.Normal;

        w.Left = bounds.Left * dx;
        w.Top = bounds.Top * dy;
        w.Width = bounds.Width * dx;
        w.Height = bounds.Height * dy;
        w.WindowState = maximize ? WindowState.Maximized : prevState;
    }

    public static Screen GetCurrentScreen(Window w)
    {
        var hwnd = new WindowInteropHelper(w).Handle;
        return hwnd != IntPtr.Zero
            ? Screen.FromHandle(hwnd)
            : Screen.FromPoint(System.Windows.Forms.Cursor.Position);
    }

    public static Screen? GetScreenByIndex(int index)
        => (index >= 0 && index < Screen.AllScreens.Length) ? Screen.AllScreens[index] : null;

    public static void MoveToNextScreen(Window w, bool maximize = false)
    {
        var screens = Screen.AllScreens;
        if (screens.Length <= 1) return;

        var cur = GetCurrentScreen(w);
        int i = Array.IndexOf(screens, cur);
        int next = (i + 1) % screens.Length;
        MoveToScreen(w, screens[next], maximize);
    }

    /// <summary>
    /// 현재 활성 윈도우(MainWindow 또는 활성창)를 찾아 다음 모니터로 이동
    /// </summary>
    public static void MoveActiveWindowToNextScreen(bool maximize = false)
    {
        void Do()
        {
            var w = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(win => win.IsActive)
                    ?? System.Windows.Application.Current?.MainWindow;
            if (w != null) MoveToNextScreen(w, maximize);
        }

        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp is null || disp.CheckAccess()) Do();
        else disp.Invoke(Do);
    }

    /// <summary>
    /// 현재 활성 윈도우를 특정 인덱스 모니터로 이동
    /// </summary>
    public static void MoveActiveWindowToScreenIndex(int index, bool maximize = false)
    {
        void Do()
        {
            var w = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(win => win.IsActive)
                    ?? System.Windows.Application.Current?.MainWindow;
            if (w is null) return;

            var s = GetScreenByIndex(index) ?? GetCurrentScreen(w);
            if (s != null) MoveToScreen(w, s, maximize);
        }

        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp is null || disp.CheckAccess()) Do();
        else disp.Invoke(Do);
    }
}
