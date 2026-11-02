using UIKit;

namespace Microsoft.Maui.Platform.TvOS;

public static class ViewExtensions
{
    public static UIView ToTvOSPlatform(this IView view, IMauiContext context)
    {
        var handler = view.ToHandler(context);
        return handler.ToPlatformView();
    }

    public static UIView ToPlatformView(this IElementHandler handler)
    {
        if (handler.PlatformView is UIView uiView)
            return uiView;

        throw new InvalidOperationException(
            $"Unable to convert handler platform view ({handler.PlatformView?.GetType().Name}) to UIView");
    }

    public static IElementHandler ToHandler(this IView view, IMauiContext context)
    {
        // Reuse the view's handler (as MAUI's own ToHandler does) so switching tabs or
        // popping back to a page keeps its native views and state instead of rebuilding them.
        if (view.Handler is IElementHandler existing && existing.MauiContext == context && existing.PlatformView != null)
            return existing;

        var handler = context.Handlers.GetHandler(view.GetType());
        if (handler == null)
            throw new InvalidOperationException($"No handler found for view type {view.GetType().Name}");

        handler.SetMauiContext(context);
        handler.SetVirtualView((IElement)view);
        return handler;
    }
}
