using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Kiosk.Behaviors;

public static class FocusBoundary
{
    public static readonly DependencyProperty EntryTargetProperty =
        DependencyProperty.RegisterAttached(
            "EntryTarget",
            typeof(FrameworkElement),
            typeof(FocusBoundary),
            new PropertyMetadata(null, OnConfigurationChanged));

    public static readonly DependencyProperty UpTargetProperty =
        DependencyProperty.RegisterAttached(
            "UpTarget",
            typeof(FrameworkElement),
            typeof(FocusBoundary),
            new PropertyMetadata(null, OnConfigurationChanged));

    public static readonly DependencyProperty DownTargetProperty =
        DependencyProperty.RegisterAttached(
            "DownTarget",
            typeof(FrameworkElement),
            typeof(FocusBoundary),
            new PropertyMetadata(null, OnConfigurationChanged));

    public static readonly DependencyProperty LeftTargetProperty =
        DependencyProperty.RegisterAttached(
            "LeftTarget",
            typeof(FrameworkElement),
            typeof(FocusBoundary),
            new PropertyMetadata(null, OnConfigurationChanged));

    public static readonly DependencyProperty RightTargetProperty =
        DependencyProperty.RegisterAttached(
            "RightTarget",
            typeof(FrameworkElement),
            typeof(FocusBoundary),
            new PropertyMetadata(null, OnConfigurationChanged));

    private static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(bool),
            typeof(FocusBoundary),
            new PropertyMetadata(false));

    public static FrameworkElement? GetUpTarget(DependencyObject obj) => (FrameworkElement?)obj.GetValue(UpTargetProperty);
    public static void SetUpTarget(DependencyObject obj, FrameworkElement? value) => obj.SetValue(UpTargetProperty, value);

    public static FrameworkElement? GetDownTarget(DependencyObject obj) => (FrameworkElement?)obj.GetValue(DownTargetProperty);
    public static void SetDownTarget(DependencyObject obj, FrameworkElement? value) => obj.SetValue(DownTargetProperty, value);

    public static FrameworkElement? GetLeftTarget(DependencyObject obj) => (FrameworkElement?)obj.GetValue(LeftTargetProperty);
    public static void SetLeftTarget(DependencyObject obj, FrameworkElement? value) => obj.SetValue(LeftTargetProperty, value);

    public static FrameworkElement? GetRightTarget(DependencyObject obj) => (FrameworkElement?)obj.GetValue(RightTargetProperty);
    public static void SetRightTarget(DependencyObject obj, FrameworkElement? value) => obj.SetValue(RightTargetProperty, value);

    private static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);
    private static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static FrameworkElement? GetEntryTarget(DependencyObject obj) => (FrameworkElement?)obj.GetValue(EntryTargetProperty);
    public static void SetEntryTarget(DependencyObject obj, FrameworkElement? value) => obj.SetValue(EntryTargetProperty, value);

    private static void OnConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement source || GetIsAttached(source))
            return;

        source.KeyDown += Source_KeyDown;
        SetIsAttached(source, true);
    }

    private static void Source_KeyDown(object sender, KeyEventArgs e)
    {
        if (!KeyboardNavigationState.Instance.IsEnabled)
            return;

        if (e.Handled || sender is not FrameworkElement source || !FocusContainer.ContainsFocusedElement(source))
            return;

        var target = e.Key switch
        {
            Key.Up => GetUpTarget(source),
            Key.Down => GetDownTarget(source),
            Key.Left => GetLeftTarget(source),
            Key.Right => GetRightTarget(source),
            _ => null
        };

        if (target is null)
            return;

        if (TryFocusBoundaryTarget(target))
            e.Handled = true;
    }

    private static bool TryFocusBoundaryTarget(FrameworkElement? target)
    {
        if (target is null)
            return false;

        if (FocusContainer.TryFocusTargetElement(target))
            return true;

        var explicitEntryTarget = GetEntryTarget(target);
        if (explicitEntryTarget is not null && FocusContainer.TryFocusTargetElement(explicitEntryTarget))
            return true;

        var descendantEntryTarget = FindDescendantEntryTarget(target);
        return descendantEntryTarget is not null && FocusContainer.TryFocusTargetElement(descendantEntryTarget);
    }

    private static FrameworkElement? FindDescendantEntryTarget(DependencyObject root)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            if (child is FrameworkElement frameworkElement)
            {
                var entryTarget = GetEntryTarget(frameworkElement);
                if (entryTarget is not null && entryTarget.IsVisible && entryTarget.IsEnabled)
                    return entryTarget;
            }

            var nestedEntryTarget = FindDescendantEntryTarget(child);
            if (nestedEntryTarget is not null)
                return nestedEntryTarget;
        }

        return null;
    }
}
