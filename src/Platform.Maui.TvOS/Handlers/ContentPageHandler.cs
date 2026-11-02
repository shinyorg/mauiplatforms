using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui.Platform.TvOS.Handlers;

public partial class ContentPageHandler : TvOSViewHandler<IContentView, TvOSContainerView>
{
    public static readonly IPropertyMapper<IContentView, ContentPageHandler> Mapper =
        new PropertyMapper<IContentView, ContentPageHandler>(ViewMapper)
        {
            [nameof(IContentView.Content)] = MapContent,
            [nameof(IView.Background)] = MapBackground,
        };

    public ContentPageHandler() : base(Mapper)
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

    public static void MapContent(ContentPageHandler handler, IContentView page)
    {
        if (handler.PlatformView == null || handler.MauiContext == null)
            return;

        foreach (var subview in handler.PlatformView.Subviews)
            subview.RemoveFromSuperview();

        if (page.PresentedContent is IView content)
        {
            var platformView = content.ToTvOSPlatform(handler.MauiContext);
            handler.PlatformView.AddSubview(platformView);
        }
    }

    public static void MapBackground(ContentPageHandler handler, IContentView page)
    {
        TvOSBackground.Apply(handler.PlatformView, page.Background);
    }
}
