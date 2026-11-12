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
		var view = VirtualView;
		// Unbounded along the scrolling axes, as ScrollView.CrossPlatformMeasure does (Neither measures both ways).
		var orientation = view.Orientation;
		var content = MeasureContent(view,
			orientation is ScrollOrientation.Vertical ? widthConstraint : double.PositiveInfinity,
			orientation is ScrollOrientation.Horizontal ? heightConstraint : double.PositiveInfinity);
		return new Size(
			LayoutManager.ResolveConstraints(widthConstraint, view.Width, Math.Min(content.Width, widthConstraint), view.MinimumWidth, view.MaximumWidth),
			LayoutManager.ResolveConstraints(heightConstraint, view.Height, Math.Min(content.Height, heightConstraint), view.MinimumHeight, view.MaximumHeight));
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);

		// Measure the content against the viewport, unbounded along the scrolling axes,
		// then give it at least the viewport so Fill options still stretch.
		var view = VirtualView;
		var orientation = view.Orientation;
		var horizontal = orientation is ScrollOrientation.Horizontal or ScrollOrientation.Both;
		var vertical = orientation is ScrollOrientation.Vertical or ScrollOrientation.Both;

		// The viewport is what a classic scrollbar leaves of the frame (overlay scrollbars take nothing): always
		// for ScrollBarVisibility.Always, otherwise only once the content overflows that way.
		var bar = Dom.ScrollbarThickness(PlatformView.Js);
		var barRight = vertical && bar > 0 && view.VerticalScrollBarVisibility == ScrollBarVisibility.Always;
		var barBottom = horizontal && bar > 0 && view.HorizontalScrollBarVisibility == ScrollBarVisibility.Always;
		var width = frame.Width - (barRight ? bar : 0);
		var height = frame.Height - (barBottom ? bar : 0);
		var desired = MeasureContent(view, horizontal ? double.PositiveInfinity : width, vertical ? double.PositiveInfinity : height);
		if (bar > 0 && view.VerticalScrollBarVisibility != ScrollBarVisibility.Never && view.HorizontalScrollBarVisibility != ScrollBarVisibility.Never)
		{
			var overflowsDown = vertical && !barRight && desired.Height > height;
			var overflowsAcross = horizontal && !barBottom && desired.Width > width;
			if (overflowsDown || overflowsAcross)
			{
				width -= overflowsDown ? bar : 0;
				height -= overflowsAcross ? bar : 0;
				desired = MeasureContent(view, horizontal ? double.PositiveInfinity : width, vertical ? double.PositiveInfinity : height);
			}
		}

		view.CrossPlatformArrange(new Rect(0, 0,
			horizontal ? Math.Max(width, desired.Width) : width,
			vertical ? Math.Max(height, desired.Height) : height));
	}

	/// <summary>
	/// The content measured inside the scroll view's padding, plus the padding, as MAUI's own ScrollView handlers
	/// do (ScrollView.CrossPlatformMeasure leaves the padding to the handler). ScrollView.CrossPlatformArrange
	/// then insets the content by the padding, so it fits the viewport instead of overhanging it.
	/// </summary>
	static Size MeasureContent(IScrollView view, double width, double height)
	{
		var padding = view.Padding;
		var size = view.PresentedContent is { } content
			? content.Measure(width - padding.HorizontalThickness, height - padding.VerticalThickness)
			: Size.Zero;
		return new Size(size.Width + padding.HorizontalThickness, size.Height + padding.VerticalThickness);
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
