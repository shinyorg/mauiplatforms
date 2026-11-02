using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using CoreGraphics;
using Microsoft.Maui.Controls;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

/// <summary>
/// A platform view the tvOS focus engine can land on when its MAUI view is actionable (for
/// example a Border or layout with a TapGestureRecognizer). Pressing Select on the remote
/// runs <see cref="PrimaryAction"/>.
/// </summary>
public interface ITvOSFocusableView
{
    Action? PrimaryAction { get; set; }

    /// <summary>Raised when the view gains or loses focus.</summary>
    Action<bool>? FocusChanged { get; set; }
}

/// <summary>Programmatic focus: what VisualElement.Focus() does on tvOS.</summary>
public static class TvOSFocusSystem
{
    /// <summary>
    /// Moves the focus engine to <paramref name="view"/>, or to the nearest focusable view
    /// around it (its focusable ancestor, such as a CollectionView cell, or first focusable
    /// descendant). Returns true when focus landed there.
    /// </summary>
    public static bool RequestFocus(UIView view)
    {
        var target = FindFocusable(view);
        if (target?.Window?.RootViewController is not Handlers.ThemeAwareViewController root)
            return false;

        root.PendingFocus = target;
        try
        {
            // Focus updates must be requested by an environment that contains the focused
            // item; the root controller contains everything and now prefers the target.
            root.SetNeedsFocusUpdate();
            root.UpdateFocusIfNeeded();
        }
        finally
        {
            root.PendingFocus = null;
        }
        return target.Focused;
    }

    static UIView? FindFocusable(UIView view)
    {
        for (var v = view; v != null; v = v.Superview)
        {
            if (v.CanBecomeFocused)
                return v;
        }
        return FirstFocusableDescendant(view);
    }

    static UIView? FirstFocusableDescendant(UIView view)
    {
        foreach (var child in view.Subviews)
        {
            if (child.Hidden)
                continue;
            if (child.CanBecomeFocused)
                return child;
            if (FirstFocusableDescendant(child) is { } found)
                return found;
        }
        return null;
    }

    internal static void MapFocus(IViewHandler handler, IView view, object? args)
    {
        var focused = handler.PlatformView is UIView platformView && RequestFocus(platformView);
        if (args is FocusRequest request)
            request.TrySetResult(focused);
    }
}

/// <summary>Shared behavior for <see cref="ITvOSFocusableView"/> implementations.</summary>
static class TvOSFocusableBehavior
{
    public static bool CanBecomeFocused(UIView view, Action? primaryAction)
    {
        if (primaryAction == null || view.Hidden || !view.UserInteractionEnabled || view.Alpha <= 0.01)
            return false;

        // Inside a CollectionView the cell is the focusable item; it runs the tap itself.
        for (var parent = view.Superview; parent != null; parent = parent.Superview)
        {
            if (parent is UICollectionViewCell)
                return false;
        }
        return true;
    }

    public static bool HandlePressesBegan(UIView view, NSSet<UIPress> presses, Action? primaryAction)
    {
        if (primaryAction == null || !IsSelect(presses))
            return false;

        UIView.Animate(0.1, () => TvOSTransforms.SetFocusScale(view, view.Focused ? TvOSFocusEffect.PressedScale : 0.97f));
        return true;
    }

    public static bool HandlePressesEnded(UIView view, NSSet<UIPress> presses, Action? primaryAction)
    {
        if (primaryAction == null || !IsSelect(presses))
            return false;

        UIView.Animate(0.15, () => TvOSTransforms.SetFocusScale(view, view.Focused ? TvOSFocusEffect.FocusedScale : 1));
        primaryAction();
        return true;
    }

    public static bool HandlePressesCancelled(UIView view, NSSet<UIPress> presses, Action? primaryAction)
    {
        if (primaryAction == null || !IsSelect(presses))
            return false;

        UIView.Animate(0.15, () => TvOSTransforms.SetFocusScale(view, view.Focused ? TvOSFocusEffect.FocusedScale : 1));
        return true;
    }

    static bool IsSelect(NSSet<UIPress> presses)
    {
        foreach (var press in presses)
        {
            if (press.Type == UIPressType.Select)
                return true;
        }
        return false;
    }
}

/// <summary>
/// The lift effect tvOS uses for focused content: the item grows and casts a deeper shadow,
/// animated alongside the focus engine's own transition.
/// </summary>
public static class TvOSFocusEffect
{
    public const float FocusedScale = 1.06f;
    public const float PressedScale = 1.02f;

    sealed class SavedShadow
    {
        public float Opacity;
        public nfloat Radius;
        public CGSize Offset;
        public CGColor? Color;
    }

    static readonly ConditionalWeakTable<UIView, SavedShadow> Shadows = new();

    /// <summary>Records the view's resting shadow so unfocusing restores it.</summary>
    public static void RememberShadow(UIView view)
    {
        var layer = view.Layer;
        var saved = Shadows.GetOrCreateValue(view);
        saved.Opacity = layer.ShadowOpacity;
        saved.Radius = layer.ShadowRadius;
        saved.Offset = layer.ShadowOffset;
        saved.Color = layer.ShadowColor;
    }

    public static void Animate(UIView view, bool focused, UIFocusAnimationCoordinator? coordinator, bool shadow = true)
    {
        if (focused && !Shadows.TryGetValue(view, out _))
            RememberShadow(view);

        void Apply() => Set(view, focused, shadow);

        if (coordinator != null)
            coordinator.AddCoordinatedAnimations(Apply, null);
        else
            UIView.Animate(0.2, Apply);
    }

    static void Set(UIView view, bool focused, bool shadow)
    {
        TvOSTransforms.SetFocusScale(view, focused ? FocusedScale : 1);

        if (!shadow)
            return;

        var layer = view.Layer;
        if (focused)
        {
            layer.ShadowColor = UIColor.Black.CGColor;
            layer.ShadowOpacity = 0.55f;
            layer.ShadowRadius = 24;
            layer.ShadowOffset = new CGSize(0, 18);
        }
        else if (Shadows.TryGetValue(view, out var saved))
        {
            layer.ShadowColor = saved.Color;
            layer.ShadowOpacity = saved.Opacity;
            layer.ShadowRadius = saved.Radius;
            layer.ShadowOffset = saved.Offset;
        }
    }
}

/// <summary>
/// Makes a view with TapGestureRecognizers actionable on tvOS: the platform view becomes
/// focusable and the remote's Select button raises the taps (Tapped event and Command).
/// </summary>
sealed class TvOSTapGestureManager : IDisposable
{
    const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [DynamicDependency("SendTapped", typeof(TapGestureRecognizer))]
    static readonly MethodInfo? SendTapped = typeof(TapGestureRecognizer).GetMethod("SendTapped", Internal);

    readonly View _view;
    readonly ITvOSFocusableView _platformView;

    public TvOSTapGestureManager(View view, ITvOSFocusableView platformView)
    {
        _view = view;
        _platformView = platformView;
        if (_view.GestureRecognizers is INotifyCollectionChanged observable)
            observable.CollectionChanged += OnGesturesChanged;
        Update();
    }

    /// <summary>True when the view has a tap the remote's Select button should raise.</summary>
    public static bool HasTap(View view) =>
        view.GestureRecognizers.OfType<TapGestureRecognizer>().Any();

    /// <summary>Raises every TapGestureRecognizer on the view, as a tap would on a touch screen.</summary>
    public static void RaiseTaps(View view)
    {
        foreach (var tap in view.GestureRecognizers.OfType<TapGestureRecognizer>().ToArray())
        {
            if (SendTapped != null)
            {
                SendTapped.Invoke(tap, [view, (Func<IElement?, Graphics.Point?>)(_ => null)]);
            }
            else if (tap.Command?.CanExecute(tap.CommandParameter) == true)
            {
                tap.Command.Execute(tap.CommandParameter);
            }
        }
    }

    void OnGesturesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Update();

    void Update()
    {
        _platformView.PrimaryAction = HasTap(_view) ? () => RaiseTaps(_view) : null;
    }

    public void Dispose()
    {
        if (_view.GestureRecognizers is INotifyCollectionChanged observable)
            observable.CollectionChanged -= OnGesturesChanged;
        _platformView.PrimaryAction = null;
    }
}
