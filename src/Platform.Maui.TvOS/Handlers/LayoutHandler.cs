using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class LayoutHandler : TvOSViewHandler<ILayout, TvOSContainerView>
{
    public static readonly IPropertyMapper<ILayout, LayoutHandler> Mapper =
        new PropertyMapper<ILayout, LayoutHandler>(ViewMapper)
        {
            [nameof(IView.Background)] = MapBackground,
            [nameof(ILayout.ClipsToBounds)] = MapClipsToBounds,
        };

    // Layout.Add/Insert/Remove/Clear after the first render arrive as handler commands; without these
    // mappings children added to a live layout were never shown.
    public static readonly CommandMapper<ILayout, LayoutHandler> CommandMapper = new(ViewCommandMapper)
    {
        [nameof(ILayoutHandler.Add)] = MapAdd,
        [nameof(ILayoutHandler.Insert)] = MapInsert,
        [nameof(ILayoutHandler.Remove)] = MapRemove,
        [nameof(ILayoutHandler.Clear)] = MapClear,
        [nameof(ILayoutHandler.Update)] = MapUpdate,
    };

    public LayoutHandler() : base(Mapper, CommandMapper)
    {
    }

    static void MapAdd(LayoutHandler handler, ILayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
            handler.Insert(update.Index, update.View);
    }

    static void MapInsert(LayoutHandler handler, ILayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
            handler.Insert(update.Index, update.View);
    }

    static void MapRemove(LayoutHandler handler, ILayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
            handler.Remove(update.View);
    }

    static void MapClear(LayoutHandler handler, ILayout layout, object? arg) => handler.Clear();

    static void MapUpdate(LayoutHandler handler, ILayout layout, object? arg)
    {
        if (arg is LayoutHandlerUpdate update)
            handler.Update(update.Index, update.View);
    }

    protected override TvOSContainerView CreatePlatformView()
    {
        var view = new TvOSContainerView();
        view.CrossPlatformMeasure = VirtualViewCrossPlatformMeasure;
        view.CrossPlatformArrange = VirtualViewCrossPlatformArrange;
        return view;
    }

    protected override void ConnectHandler(TvOSContainerView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.CrossPlatformMeasure = VirtualViewCrossPlatformMeasure;
        platformView.CrossPlatformArrange = VirtualViewCrossPlatformArrange;

        // Add existing children
        foreach (var child in VirtualView)
        {
            if (MauiContext != null)
            {
                var childView = child.ToTvOSPlatform(MauiContext);
                platformView.AddSubview(childView);
            }
        }
    }

    protected override void DisconnectHandler(TvOSContainerView platformView)
    {
        platformView.CrossPlatformMeasure = null;
        platformView.CrossPlatformArrange = null;
        base.DisconnectHandler(platformView);
    }

    Graphics.Size VirtualViewCrossPlatformMeasure(double widthConstraint, double heightConstraint)
    {
        return VirtualView?.CrossPlatformMeasure(widthConstraint, heightConstraint) ?? Graphics.Size.Zero;
    }

    Graphics.Size VirtualViewCrossPlatformArrange(Graphics.Rect bounds)
    {
        return VirtualView?.CrossPlatformArrange(bounds) ?? Graphics.Size.Zero;
    }

    public void Add(IView child)
    {
        if (MauiContext == null)
            return;

        var platformView = child.ToTvOSPlatform(MauiContext);
        PlatformView.AddSubview(platformView);
    }

    public void Remove(IView child)
    {
        if (child.Handler?.PlatformView is UIView platformView)
            platformView.RemoveFromSuperview();
    }

    public void Clear()
    {
        foreach (var subview in PlatformView.Subviews)
            subview.RemoveFromSuperview();
    }

    public void Insert(int index, IView child)
    {
        if (MauiContext == null)
            return;

        var platformView = child.ToTvOSPlatform(MauiContext);
        PlatformView.InsertSubview(platformView, Math.Min(index, PlatformView.Subviews.Length));
        PlatformView.SetNeedsLayout();
    }

    public void Update(int index, IView child)
    {
        if (MauiContext == null)
            return;

        // Remove existing at index
        if (index < PlatformView.Subviews.Length)
            PlatformView.Subviews[index].RemoveFromSuperview();

        var platformView = child.ToTvOSPlatform(MauiContext);
        PlatformView.InsertSubview(platformView, index);
    }

    public static void MapBackground(LayoutHandler handler, ILayout layout)
    {
        TvOSBackground.Apply(handler.PlatformView, layout.Background);
    }

    public static void MapClipsToBounds(LayoutHandler handler, ILayout layout)
    {
        handler.PlatformView.ClipsToBounds = layout.ClipsToBounds;
    }
}
