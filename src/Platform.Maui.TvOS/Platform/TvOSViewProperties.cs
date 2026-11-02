using System.Runtime.CompilerServices;
using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.TvOS.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

/// <summary>
/// The IView properties every tvOS handler shares (background, opacity, visibility, transforms…).
/// The platform-neutral ViewHandler maps them to no-ops, so they are registered here once.
/// </summary>
public static class TvOSViewProperties
{
    static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered)
            return;
        _registered = true;

        if (Microsoft.Maui.Handlers.ViewHandler.ViewMapper is not PropertyMapper<IView, IViewHandler> mapper)
            return;

        mapper[nameof(IView.Background)] = MapBackground;
        mapper[nameof(IView.Opacity)] = MapOpacity;
        mapper[nameof(IView.Visibility)] = MapVisibility;
        mapper[nameof(IView.IsEnabled)] = MapIsEnabled;
        mapper[nameof(IView.InputTransparent)] = MapInputTransparent;
        mapper[nameof(IView.AutomationId)] = MapAutomationId;
        mapper[nameof(IView.Shadow)] = MapShadow;
        mapper[nameof(IView.TranslationX)] = MapTransform;
        mapper[nameof(IView.TranslationY)] = MapTransform;
        mapper[nameof(IView.Scale)] = MapTransform;
        mapper[nameof(IView.ScaleX)] = MapTransform;
        mapper[nameof(IView.ScaleY)] = MapTransform;
        mapper[nameof(IView.Rotation)] = MapTransform;
        mapper[nameof(IView.RotationX)] = MapTransform;
        mapper[nameof(IView.RotationY)] = MapTransform;
        mapper[nameof(IView.AnchorX)] = MapTransform;
        mapper[nameof(IView.AnchorY)] = MapTransform;

        if (Microsoft.Maui.Handlers.ViewHandler.ViewCommandMapper is CommandMapper<IView, IViewHandler> commands)
        {
            commands[nameof(IView.Focus)] = TvOSFocusSystem.MapFocus;
            commands[nameof(IView.InvalidateMeasure)] = MapInvalidateMeasure;
        }
    }

    /// <summary>
    /// A view's size changed (text, visibility, content…): lay out again up to the nearest
    /// view that re-runs MAUI layout for its subtree — a collection cell, or the window.
    /// </summary>
    public static void MapInvalidateMeasure(IViewHandler handler, IView view, object? args)
    {
        for (var platformView = handler.PlatformView as UIView; platformView != null; platformView = platformView.Superview)
        {
            platformView.SetNeedsLayout();
            if (platformView is UICollectionViewCell)
                break;
        }
    }

    public static void MapBackground(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIView platformView)
            TvOSBackground.Apply(platformView, view.Background);
    }

    public static void MapOpacity(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIView platformView)
            platformView.Alpha = (nfloat)Math.Clamp(view.Opacity, 0, 1);
    }

    public static void MapVisibility(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIView platformView)
            platformView.Hidden = view.Visibility != Visibility.Visible;
    }

    public static void MapIsEnabled(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIControl control)
            control.Enabled = view.IsEnabled;
    }

    public static void MapInputTransparent(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIView platformView && view is not ILayout)
            platformView.UserInteractionEnabled = !view.InputTransparent;
    }

    public static void MapAutomationId(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIView platformView)
            platformView.AccessibilityIdentifier = view.AutomationId;
    }

    public static void MapShadow(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is not UIView platformView || platformView.Layer == null)
            return;

        var layer = platformView.Layer;
        var shadow = view.Shadow;
        if (shadow == null)
        {
            layer.ShadowOpacity = 0;
        }
        else
        {
            layer.ShadowOpacity = shadow.Opacity;
            layer.ShadowRadius = shadow.Radius;
            layer.ShadowOffset = new CGSize((float)shadow.Offset.X, (float)shadow.Offset.Y);
            layer.ShadowColor = shadow.Paint is SolidPaint { Color: not null } solid
                ? solid.Color.ToPlatformColor().CGColor
                : UIColor.Black.CGColor;
        }

        TvOSFocusEffect.RememberShadow(platformView);
    }

    public static void MapTransform(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is UIView platformView)
            TvOSTransforms.Apply(platformView, view);
    }
}

/// <summary>Solid and gradient backgrounds.</summary>
public static class TvOSBackground
{
    const string GradientLayerName = "MauiBackgroundGradient";

    public static void Apply(UIView view, Paint? paint)
    {
        RemoveGradient(view);

        switch (paint)
        {
            case SolidPaint { Color: not null } solid:
                view.BackgroundColor = solid.Color.ToPlatformColor();
                break;

            case GradientPaint gradient when gradient.GradientStops is { Length: > 0 } stops:
                if (!SupportsGradientLayer(view))
                {
                    // A sublayer would draw over a label's or control's own content.
                    view.BackgroundColor = stops[0].Color?.ToPlatformColor();
                    break;
                }

                view.BackgroundColor = UIColor.Clear;
                var ordered = stops.OrderBy(s => s.Offset).ToArray();
                var layer = new CAGradientLayer
                {
                    Name = GradientLayerName,
                    Colors = ordered.Select(s => (s.Color ?? Colors.Transparent).ToPlatformColor().CGColor).ToArray(),
                    Locations = ordered.Select(s => new Foundation.NSNumber(s.Offset)).ToArray(),
                    Frame = view.Bounds,
                    CornerRadius = view.Layer.CornerRadius,
                };

                if (gradient is LinearGradientPaint linear)
                {
                    layer.StartPoint = new CGPoint(linear.StartPoint.X, linear.StartPoint.Y);
                    layer.EndPoint = new CGPoint(linear.EndPoint.X, linear.EndPoint.Y);
                }
                else if (gradient is RadialGradientPaint radial)
                {
                    layer.LayerType = CAGradientLayerType.Radial;
                    layer.StartPoint = new CGPoint(radial.Center.X, radial.Center.Y);
                    layer.EndPoint = new CGPoint(radial.Center.X + radial.Radius, radial.Center.Y + radial.Radius);
                }

                view.Layer.InsertSublayer(layer, 0);
                break;
        }
        // No paint: leave the view's own default background alone.
    }

    /// <summary>Keeps a gradient background sized to its view; called after every arrange.</summary>
    public static void UpdateFrame(UIView view)
    {
        var sublayers = view.Layer.Sublayers;
        if (sublayers == null)
            return;

        foreach (var layer in sublayers)
        {
            if (layer.Name != GradientLayerName)
                continue;

            CATransaction.Begin();
            CATransaction.DisableActions = true;
            layer.Frame = view.Bounds;
            layer.CornerRadius = view.Layer.CornerRadius;
            CATransaction.Commit();
            return;
        }
    }

    static bool SupportsGradientLayer(UIView view) => view is TvOSContainerView or BorderUIView;

    static void RemoveGradient(UIView view)
    {
        var sublayers = view.Layer.Sublayers;
        if (sublayers == null)
            return;

        foreach (var layer in sublayers)
        {
            if (layer.Name == GradientLayerName)
                layer.RemoveFromSuperLayer();
        }
    }
}

/// <summary>
/// MAUI transforms (translation, scale, rotation, anchor) combined with the focus engine's lift
/// scale, so focusing a view never discards a transform the app set.
/// </summary>
public static class TvOSTransforms
{
    sealed class State
    {
        public CATransform3D Base = CATransform3D.Identity;
        public nfloat FocusScale = 1;
        public CGPoint Anchor = new(0.5, 0.5);
        public CGRect Arranged;
        public bool HasArranged;
    }

    static readonly ConditionalWeakTable<UIView, State> States = new();

    public static void Apply(UIView view, IView virtualView)
    {
        var state = States.GetOrCreateValue(view);

        var transform = CATransform3D.Identity;
        transform.M34 = (nfloat)(-1.0 / 2000.0); // perspective for RotationX/Y
        transform = transform.Translate((nfloat)virtualView.TranslationX, (nfloat)virtualView.TranslationY, 0);
        if (virtualView.Rotation != 0)
            transform = transform.Rotate((nfloat)(virtualView.Rotation * Math.PI / 180), 0, 0, 1);
        if (virtualView.RotationX != 0)
            transform = transform.Rotate((nfloat)(virtualView.RotationX * Math.PI / 180), 1, 0, 0);
        if (virtualView.RotationY != 0)
            transform = transform.Rotate((nfloat)(virtualView.RotationY * Math.PI / 180), 0, 1, 0);
        transform = transform.Scale((nfloat)(virtualView.Scale * virtualView.ScaleX), (nfloat)(virtualView.Scale * virtualView.ScaleY), 1);

        state.Base = transform;
        state.Anchor = new CGPoint(virtualView.AnchorX, virtualView.AnchorY);
        Update(view, state);
        if (state.HasArranged)
            Position(view, state);
        else
            view.Layer.AnchorPoint = state.Anchor;
    }

    public static void SetFocusScale(UIView view, nfloat scale)
    {
        var state = States.GetOrCreateValue(view);
        state.FocusScale = scale;
        Update(view, state);
    }

    /// <summary>
    /// Places the view in the rect layout gave it. Uses the layer's anchor point, so a view with
    /// a custom AnchorX/AnchorY still occupies exactly that rect. Bounds and position are set
    /// rather than Frame, which is undefined once the layer has a transform.
    /// </summary>
    public static void Arrange(UIView view, CGRect rect)
    {
        var state = States.GetOrCreateValue(view);
        state.Arranged = rect;
        state.HasArranged = true;
        Position(view, state);
    }

    static void Position(UIView view, State state)
    {
        var rect = state.Arranged;
        var layer = view.Layer;
        if (layer.AnchorPoint != state.Anchor)
            layer.AnchorPoint = state.Anchor;
        view.Bounds = new CGRect(0, 0, rect.Width, rect.Height);
        layer.Position = new CGPoint(rect.X + (state.Anchor.X * rect.Width), rect.Y + (state.Anchor.Y * rect.Height));
    }

    static void Update(UIView view, State state)
    {
        view.Layer.Transform = state.FocusScale == 1
            ? state.Base
            : state.Base.Scale(state.FocusScale, state.FocusScale, 1);
    }
}
