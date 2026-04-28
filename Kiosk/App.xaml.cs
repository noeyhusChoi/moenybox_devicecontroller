using Kiosk.Application.Services.Resx;
using Kiosk.Infrastructure.Hosting;
using Kiosk.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Kiosk;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private AppBootstrapper? _bootstrapper;
    private IAppCulture? _appCulture;

    protected override void OnStartup(StartupEventArgs e)
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new DefaultTraceListener());
        Trace.AutoFlush = true;

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        base.OnStartup(e);
 
        _bootstrapper = new AppBootstrapper();

        var resxLocalizationService = _bootstrapper._serviceProvider.GetRequiredService<IResxLocalizationService>();
        ResxLocalizationProvider.Initialize(resxLocalizationService);
        _appCulture = _bootstrapper._serviceProvider.GetRequiredService<IAppCulture>();
        var mainWindow = _bootstrapper._serviceProvider.GetRequiredService<MainWindowView>();
        var mainWindowViewModel = _bootstrapper._serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainWindowViewModel;
        ApplyGlobalFontFamily(mainWindow, _appCulture.CurrentCulture);
        _appCulture.CultureChanged += OnCultureChanged;

        Current.MainWindow = mainWindow;
        mainWindow.Show();
        ScheduleFontComparisonLog(mainWindow);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_appCulture is not null)
        {
            _appCulture.CultureChanged -= OnCultureChanged;
        }

        _bootstrapper?.Dispose();
        base.OnExit(e);
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        if (_appCulture is null || Current.MainWindow is not Window mainWindow)
        {
            return;
        }

        ApplyGlobalFontFamily(mainWindow, _appCulture.CurrentCulture);
        ScheduleFontComparisonLog(mainWindow);
    }

    private void ApplyGlobalFontFamily(Window window, CultureInfo culture)
    {
        var fontKey = culture.Name switch
        {
            "ko-KR" => "Noto Sans KR",
            "ja-JP" => "Noto Sans JP",
            "zh-CN" => "Noto Sans SC",
            "zh-TW" => "Noto Sans TC",
            _ => "Noto Sans"
        };

        // Apply the same dynamic font resource to both control and text inheritance paths.
        window.SetResourceReference(Control.FontFamilyProperty, fontKey);
        window.SetResourceReference(TextElement.FontFamilyProperty, fontKey);
    }

    private static void ScheduleFontComparisonLog(Window window)
    {
        window.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() => LogFontComparison(window)));
    }

    private static void LogFontComparison(Window window)
    {
        var exchangeText = FindNamedDescendant<System.Windows.Controls.TextBlock>(window, "ExchangeCardTitleText");
        var taxRefundText = FindNamedDescendant<System.Windows.Controls.TextBlock>(window, "TaxRefundCardTitleText");

        var messages = new List<string>
        {
            DescribeTypeface("Window", window.FontFamily, FontWeights.Regular),
            DescribeTypeface("ExchangeCardTitleText", exchangeText?.FontFamily, exchangeText?.FontWeight ?? FontWeights.Regular),
            DescribeTypeface("TaxRefundCardTitleText", taxRefundText?.FontFamily, taxRefundText?.FontWeight ?? FontWeights.Regular)
        };

        messages.AddRange(DescribeGlyphRuns("ExchangeCardTitleText", exchangeText));
        messages.AddRange(DescribeGlyphRuns("TaxRefundCardTitleText", taxRefundText));

        foreach (var message in messages)
        {
            Debug.WriteLine(message);
            Trace.WriteLine(message);
        }
    }

    private static T? FindNamedDescendant<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        var childrenCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T matched && string.Equals(matched.Name, name, StringComparison.Ordinal))
            {
                return matched;
            }

            var descendant = FindNamedDescendant<T>(child, name);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static string DescribeTypeface(string label, FontFamily? fontFamily, FontWeight fontWeight)
    {
        if (fontFamily is null)
        {
            return $"[FontCompare] {label}='<null>'";
        }

        var typeface = new Typeface(fontFamily, FontStyles.Normal, fontWeight, FontStretches.Normal);
        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
        {
            return $"[FontCompare] {label} source='{fontFamily.Source}' glyph='<unresolved>' weight='{fontWeight}'";
        }

        return
            $"[FontCompare] {label} source='{fontFamily.Source}' glyph='{glyphTypeface.FontUri}' family='{GetDisplayName(glyphTypeface.FamilyNames)}' face='{GetDisplayName(glyphTypeface.FaceNames)}' weight='{glyphTypeface.Weight}'";
    }

    private static string GetDisplayName(IDictionary<CultureInfo, string> names)
    {
        return names.TryGetValue(CultureInfo.GetCultureInfo("en-US"), out var englishName)
            ? englishName
            : names.Values.FirstOrDefault() ?? "<unknown>";
    }

    private static IReadOnlyList<string> DescribeGlyphRuns(string label, TextBlock? textBlock)
    {
        var messages = new List<string>();

        if (textBlock is null)
        {
            messages.Add($"[GlyphRun] {label}='<missing>'");
            return messages;
        }

        var text = textBlock.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            messages.Add($"[GlyphRun] {label} text='<empty>'");
            return messages;
        }

        textBlock.UpdateLayout();

        var drawing = VisualTreeHelper.GetDrawing(textBlock);
        if (drawing is null)
        {
            messages.Add($"[GlyphRun] {label} drawing='<null>'");
            return messages;
        }

        var glyphRuns = new List<GlyphRun>();
        CollectGlyphRuns(drawing, glyphRuns);

        if (glyphRuns.Count == 0)
        {
            messages.Add($"[GlyphRun] {label} run-count=0");
            return messages;
        }

        messages.Add($"[GlyphRun] {label} text='{NormalizeForLog(text)}' run-count={glyphRuns.Count}");

        for (var index = 0; index < glyphRuns.Count; index++)
        {
            var glyphRun = glyphRuns[index];
            var glyphTypeface = glyphRun.GlyphTypeface;
            var snippet = glyphRun.Characters is null
                ? string.Empty
                : NormalizeForLog(new string(glyphRun.Characters.Select(c => (char)c).ToArray()));

            messages.Add(
                $"[GlyphRun] {label} run={index} text='{snippet}' glyph='{glyphTypeface.FontUri}' family='{GetDisplayName(glyphTypeface.FamilyNames)}' face='{GetDisplayName(glyphTypeface.FaceNames)}' weight='{glyphTypeface.Weight}'");
        }

        return messages;
    }

    private static void CollectGlyphRuns(Drawing drawing, ICollection<GlyphRun> glyphRuns)
    {
        if (drawing is GlyphRunDrawing glyphRunDrawing && glyphRunDrawing.GlyphRun is not null)
        {
            glyphRuns.Add(glyphRunDrawing.GlyphRun);
            return;
        }

        if (drawing is DrawingGroup drawingGroup)
        {
            foreach (Drawing child in drawingGroup.Children)
            {
                CollectGlyphRuns(child, glyphRuns);
            }
        }
    }

    private static string NormalizeForLog(string text)
    {
        return text
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
