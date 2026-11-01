using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// ScrollView as an overflow container. The content is absolutely positioned at its full
/// extent, which the browser includes in the scrollable area.
/// </summary>
public class ScrollViewHandler : WebElementHandler<IScrollView, WebElement>
{
	public static IPropertyMapper<IScrollView, ScrollViewHandler> Mapper =
		new PropertyMapper<IScrollView, ScrollViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IContentView.Content)] = MapContent,
			[nameof(IScrollView.Orientation)] = MapOverflow,
			[nameof(IScrollView.HorizontalScrollBarVisibility)] = MapOverflow,
			[nameof(IScrollView.VerticalScrollBarVisibility)] = MapOverflow,
		};

	public static CommandMapper<IScrollView, ScrollViewHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(IScrollView.RequestScrollTo)] = MapRequestScrollTo,
	};

	public ScrollViewHandler() : base(Mapper, CommandMapper)
	{
	}

	WebElement? _content;

	protected override WebElement CreatePlatformView() => new("div", "maui-scroll");

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnScroll((x, y) =>
		{
			VirtualView.HorizontalOffset = x;
			VirtualView.VerticalOffset = y;
		}));
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var content = VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
		var view = VirtualView;
		return new Size(
			LayoutManager.ResolveConstraints(widthConstraint, view.Width, Math.Min(content.Width, widthConstraint), view.MinimumWidth, view.MaximumWidth),
			LayoutManager.ResolveConstraints(heightConstraint, view.Height, Math.Min(content.Height, heightConstraint), view.MinimumHeight, view.MaximumHeight));
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);

		// Measure the content against the viewport, unbounded along the scrolling axes,
		// then give it at least the viewport so Fill options still stretch.
		var orientation = VirtualView.Orientation;
		var horizontal = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both;
		var vertical = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both;
		var desired = VirtualView.CrossPlatformMeasure(
			horizontal ? double.PositiveInfinity : frame.Width,
			vertical ? double.PositiveInfinity : frame.Height);
		VirtualView.CrossPlatformArrange(new Rect(0, 0,
			horizontal ? Math.Max(frame.Width, desired.Width) : frame.Width,
			vertical ? Math.Max(frame.Height, desired.Height) : frame.Height));
	}

	public static void MapContent(ScrollViewHandler handler, IScrollView scrollView)
	{
		var content = scrollView.PresentedContent is { } presented ? handler.ToPlatform(presented) : null;
		if (handler._content == content)
			return;
		handler._content?.Detach();
		handler._content = content;
		if (content is not null)
			handler.PlatformView.Append(content);
		WebLayoutScheduler.Request();
	}

	public static void MapOverflow(ScrollViewHandler handler, IScrollView scrollView)
	{
		var el = handler.PlatformView;
		var orientation = scrollView.Orientation;
		var horizontal = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both;
		var vertical = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both;
		el.SetStyle("overflow-x", horizontal ? Overflow(scrollView.HorizontalScrollBarVisibility) : "hidden");
		el.SetStyle("overflow-y", vertical ? Overflow(scrollView.VerticalScrollBarVisibility) : "hidden");
		el.ToggleClass("no-bars",
			(!horizontal || scrollView.HorizontalScrollBarVisibility == ScrollBarVisibility.Never) &&
			(!vertical || scrollView.VerticalScrollBarVisibility == ScrollBarVisibility.Never));
		WebLayoutScheduler.Request();

		static string Overflow(ScrollBarVisibility visibility) => visibility == ScrollBarVisibility.Always ? "scroll" : "auto";
	}

	public static void MapRequestScrollTo(ScrollViewHandler handler, IScrollView scrollView, object? args)
	{
		if (args is not ScrollToRequest request)
			return;
		// Positions requested before the first layout need the content to be arranged first.
		WebLayoutScheduler.Flush();
		Dom.ScrollTo(handler.PlatformView.Js, request.HorizontalOffset, request.VerticalOffset, !request.Instant);
		scrollView.ScrollFinished();
	}
}
