using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class ContentViewHandler : TvOSViewHandler<IContentView, TvOSContainerView>
{
    public static readonly IPropertyMapper<IContentView, ContentViewHandler> Mapper =
        new PropertyMapper<IContentView, ContentViewHandler>(ViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent,
            [nameof(IView.Background)] = MapBackground,
        };

    public ContentViewHandler() : base(Mapper)
    {
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

    public static void MapContent(ContentViewHandler handler, IContentView contentView)
    {
        if (handler.PlatformView == null || handler.MauiContext == null)
            return;

        // Clear existing content
        foreach (var subview in handler.PlatformView.Subviews)
            subview.RemoveFromSuperview();

        if (contentView.PresentedContent is IView view)
        {
            var platformView = view.ToTvOSPlatform(handler.MauiContext);
            handler.PlatformView.AddSubview(platformView);
        }
    }

    public static void MapBackground(ContentViewHandler handler, IContentView contentView)
    {
        TvOSBackground.Apply(handler.PlatformView, contentView.Background);
    }
}
