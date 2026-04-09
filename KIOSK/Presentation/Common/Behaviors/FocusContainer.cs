using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Kiosk.Behaviors;

public enum FocusContainerMode
{
    None,
    VerticalStack,
    HorizontalStack,
    UniformGrid
}

public static class FocusContainer
{
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.RegisterAttached(
            "Mode",
            typeof(FocusContainerMode),
            typeof(FocusContainer),
            new PropertyMetadata(FocusContainerMode.None, OnContainerConfigurationChanged));

    public static readonly DependencyProperty InitialFocusProperty =
        DependencyProperty.RegisterAttached(
            "InitialFocus",
            typeof(bool),
            typeof(FocusContainer),
            new PropertyMetadata(false, OnContainerConfigurationChanged));

    public static readonly DependencyProperty InitialFocusIndexProperty =
        DependencyProperty.RegisterAttached(
            "InitialFocusIndex",
            typeof(int),
            typeof(FocusContainer),
            new PropertyMetadata(0));

    public static readonly DependencyProperty IsPreferredFocusProperty =
        DependencyProperty.RegisterAttached(
            "IsPreferredFocus",
            typeof(bool),
            typeof(FocusContainer),
            new PropertyMetadata(false));

    private static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached(
            "IsAttached",
            typeof(bool),
            typeof(FocusContainer),
            new PropertyMetadata(false));

    private static readonly DependencyProperty LastFocusedElementProperty =
        DependencyProperty.RegisterAttached(
            "LastFocusedElement",
            typeof(FrameworkElement),
            typeof(FocusContainer));

    public static FocusContainerMode GetMode(DependencyObject obj) => (FocusContainerMode)obj.GetValue(ModeProperty);
    public static void SetMode(DependencyObject obj, FocusContainerMode value) => obj.SetValue(ModeProperty, value);

    public static bool GetInitialFocus(DependencyObject obj) => (bool)obj.GetValue(InitialFocusProperty);
    public static void SetInitialFocus(DependencyObject obj, bool value) => obj.SetValue(InitialFocusProperty, value);

    public static int GetInitialFocusIndex(DependencyObject obj) => (int)obj.GetValue(InitialFocusIndexProperty);
    public static void SetInitialFocusIndex(DependencyObject obj, int value) => obj.SetValue(InitialFocusIndexProperty, value);

    public static bool GetIsPreferredFocus(DependencyObject obj) => (bool)obj.GetValue(IsPreferredFocusProperty);
    public static void SetIsPreferredFocus(DependencyObject obj, bool value) => obj.SetValue(IsPreferredFocusProperty, value);

    public static bool TryFocusTargetElement(FrameworkElement? target)
        => TryFocusTarget(target);

    public static bool ContainsFocusedElement(FrameworkElement container)
        => Keyboard.FocusedElement is DependencyObject focusedElement && IsDescendantOf(focusedElement, container);

    private static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);
    private static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    private static FrameworkElement? GetLastFocusedElement(DependencyObject obj) => (FrameworkElement?)obj.GetValue(LastFocusedElementProperty);
    private static void SetLastFocusedElement(DependencyObject obj, FrameworkElement? value) => obj.SetValue(LastFocusedElementProperty, value);

    private static void OnContainerConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement container || GetIsAttached(container))
            return;

        container.Loaded += Container_Loaded;
        container.KeyDown += Container_KeyDown;
        container.PreviewGotKeyboardFocus += Container_PreviewGotKeyboardFocus;
        SetIsAttached(container, true);
    }

    private static void Container_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement container || !GetInitialFocus(container))
            return;

        if (Keyboard.FocusedElement is DependencyObject focusedElement && IsDescendantOf(focusedElement, container))
            return;

        container.Dispatcher.BeginInvoke(() => FocusInto(container), DispatcherPriority.Input);
    }

    private static void Container_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not FrameworkElement container || e.NewFocus is not DependencyObject focusedElement)
            return;

        var ownedElement = ResolveFocusableOwner(container, focusedElement);
        if (ownedElement is not null)
            SetLastFocusedElement(container, ownedElement);
    }

    private static void Container_KeyDown(object sender, KeyEventArgs e)
    {
        if (!KeyboardNavigationState.Instance.IsEnabled)
            return;

        if (sender is not FrameworkElement container || Keyboard.FocusedElement is not DependencyObject focusedElement)
            return;

        if (!IsDescendantOf(focusedElement, container))
            return;

        var direction = ToDirection(e.Key);
        if (direction is null)
            return;

        if (!CanHandleDirection(container, direction.Value))
            return;

        if (TryMoveWithinContainer(container, direction.Value))
        {
            e.Handled = true;
        }
    }

    private static bool TryMoveWithinContainer(FrameworkElement container, FocusDirection direction)
    {
        var focusables = GetFocusableDescendants(container);
        if (focusables.Count == 0)
            return false;

        var currentElement = ResolveFocusableOwner(container, Keyboard.FocusedElement as DependencyObject);
        if (currentElement is null)
            return false;

        var currentIndex = focusables.IndexOf(currentElement);
        if (currentIndex < 0)
            return false;

        var targetIndex = GetTargetIndex(container, focusables.Count, currentIndex, direction);
        if (targetIndex < 0 || targetIndex >= focusables.Count)
            return false;

        return TryFocusElement(focusables[targetIndex]);
    }

    private static int GetTargetIndex(FrameworkElement container, int count, int currentIndex, FocusDirection direction)
    {
        return GetMode(container) switch
        {
            FocusContainerMode.VerticalStack => direction switch
            {
                FocusDirection.Up => currentIndex - 1,
                FocusDirection.Down => currentIndex + 1,
                _ => -1
            },
            FocusContainerMode.HorizontalStack => direction switch
            {
                FocusDirection.Left => currentIndex - 1,
                FocusDirection.Right => currentIndex + 1,
                _ => -1
            },
            FocusContainerMode.UniformGrid => GetUniformGridTargetIndex(container, count, currentIndex, direction),
            _ => -1
        };
    }

    private static bool CanHandleDirection(FrameworkElement container, FocusDirection direction)
    {
        return GetMode(container) switch
        {
            FocusContainerMode.VerticalStack => direction is FocusDirection.Up or FocusDirection.Down,
            FocusContainerMode.HorizontalStack => direction is FocusDirection.Left or FocusDirection.Right,
            FocusContainerMode.UniformGrid => true,
            _ => false
        };
    }

    private static int GetUniformGridTargetIndex(FrameworkElement container, int count, int currentIndex, FocusDirection direction)
    {
        var columns = container is System.Windows.Controls.Primitives.UniformGrid uniformGrid && uniformGrid.Columns > 0
            ? uniformGrid.Columns
            : 1;

        return direction switch
        {
            FocusDirection.Left => currentIndex - 1,
            FocusDirection.Right => currentIndex + 1,
            FocusDirection.Up => currentIndex - columns,
            FocusDirection.Down => currentIndex + columns,
            _ => -1
        };
    }

    private static bool TryFocusTarget(FrameworkElement? target)
    {
        if (target is null || !target.IsVisible || !target.IsEnabled)
            return false;

        if (TryFocusElement(target))
            return true;

        return FocusInto(target);
    }

    private static bool FocusInto(FrameworkElement container)
    {
        var remembered = GetLastFocusedElement(container);
        if (remembered is not null && IsNavigable(remembered) && IsDescendantOf(remembered, container))
        {
            if (GetFocusableDescendants(remembered).Count > 0 && FocusInto(remembered))
                return true;

            if (TryFocusElement(remembered))
                return true;
        }

        var focusables = GetFocusableDescendants(container);
        if (focusables.Count == 0)
            return false;

        var preferred = focusables.FirstOrDefault(focusable => GetIsPreferredFocus(focusable));
        if (preferred is not null && TryFocusElement(preferred))
            return true;

        var initialIndex = Math.Clamp(GetInitialFocusIndex(container), 0, focusables.Count - 1);
        return TryFocusElement(focusables[initialIndex]);
    }

    private static FocusDirection? ToDirection(Key key)
        => key switch
        {
            Key.Up => FocusDirection.Up,
            Key.Down => FocusDirection.Down,
            Key.Left => FocusDirection.Left,
            Key.Right => FocusDirection.Right,
            _ => null
        };

    private static FrameworkElement? ResolveFocusableOwner(FrameworkElement container, DependencyObject? element)
    {
        var current = element;
        while (current is not null && !ReferenceEquals(current, container))
        {
            if (current is FrameworkElement frameworkElement && IsNavigable(frameworkElement))
                return frameworkElement;

            current = GetParent(current);
        }

        return null;
    }

    private static List<FrameworkElement> GetFocusableDescendants(DependencyObject root)
    {
        var result = new List<FrameworkElement>();
        CollectFocusableDescendants(root, result);
        return result;
    }

    private static void CollectFocusableDescendants(DependencyObject parent, ICollection<FrameworkElement> result)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is FrameworkElement frameworkElement && IsNavigable(frameworkElement))
                result.Add(frameworkElement);

            CollectFocusableDescendants(child, result);
        }
    }

    private static bool TryFocusElement(FrameworkElement? element)
    {
        if (!IsNavigable(element))
            return false;

        var focused = element!.Focus() || Keyboard.Focus(element) == element;
        if (focused)
            LogFocusedElement("FocusMoved", element);

        return focused;
    }

    private static void LogFocusedElement(string reason, FrameworkElement? element = null)
    {
        var focusedElement = element ?? Keyboard.FocusedElement as FrameworkElement;
        if (focusedElement is null)
        {
            Trace.WriteLine($"[FocusContainer] {reason} -> <null>");
            return;
        }

        var name = string.IsNullOrWhiteSpace(focusedElement.Name) ? "<unnamed>" : focusedElement.Name;
        Trace.WriteLine($"[FocusContainer] {reason} -> {focusedElement.GetType().Name}({name})");
    }

    private static bool IsNavigable(FrameworkElement? element)
        => element is not null &&
           IsNavigationTarget(element) &&
           element.IsVisible &&
           element.IsEnabled;

    private static bool IsNavigationTarget(FrameworkElement element)
        => element switch
        {
            ButtonBase => true,
            TextBoxBase => true,
            _ => false
        };

    private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
    {
        var current = element;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
                return true;

            current = GetParent(current);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject element)
    {
        if (element is Visual || element is Visual3D)
            return VisualTreeHelper.GetParent(element);

        return null;
    }

    private enum FocusDirection
    {
        Up,
        Down,
        Left,
        Right
    }
}
