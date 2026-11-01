using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// Hosts a single presented view (ContentView, TemplatedView/ControlTemplate, and the base for pages).
/// </summary>
public class ContentViewHandler : WebElementHandler<IContentView, WebElement>
{
	public static IPropertyMapper<IContentView, ContentViewHandler> Mapper =
		new PropertyMapper<IContentView, ContentViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IContentView.Content)] = MapContent,
			[nameof(Microsoft.Maui.Controls.TemplatedView.IsClippedToBounds)] = MapClipsToBounds,
		};

	public ContentViewHandler() : base(Mapper)
	{
	}

	protected ContentViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper, commandMapper)
	{
	}

	WebElement? _content;

	protected override WebElement CreatePlatformView() => new("div", "maui-content-view");

	Size _measuredConstraints = new(-1, -1);

	/// <summary>Records the constraints of a content measure so arrange knows whether to re-measure.</summary>
	protected Size MeasureContent(double widthConstraint, double heightConstraint)
	{
		_measuredConstraints = new Size(widthConstraint, heightConstraint);
		return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
	}

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var size = MeasureContent(widthConstraint, heightConstraint);
		return new Size(
			LayoutManager.ResolveConstraints(widthConstraint, VirtualView.Width, size.Width, VirtualView.MinimumWidth, VirtualView.MaximumWidth),
			LayoutManager.ResolveConstraints(heightConstraint, VirtualView.Height, size.Height, VirtualView.MinimumHeight, VirtualView.MaximumHeight));
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		// See LayoutHandler.PlatformArrange: arrange must follow a measure at the final size.
		if (_measuredConstraints != frame.Size)
			MeasureContent(frame.Width, frame.Height);
		VirtualView.CrossPlatformArrange(new Rect(Point.Zero, frame.Size));
	}

	public static void MapContent(ContentViewHandler handler, IContentView view)
	{
		var content = view.PresentedContent is { } presented ? handler.ToPlatform(presented) : null;
		if (handler._content == content)
			return;
		if (handler._content is not null)
			handler.PlatformView.Remove(handler._content);
		handler._content = content;
		if (content is not null)
			handler.PlatformView.Insert(0, content);
		WebLayoutScheduler.Request();
	}

	// A Border always clips its content to its stroke shape, as on the native backends; other
	// content views clip only when IsClippedToBounds asks them to.
	public static void MapClipsToBounds(ContentViewHandler handler, IContentView view)
		=> handler.PlatformView.ToggleClass("maui-clip", view is IBorderView or Microsoft.Maui.Controls.TemplatedView { IsClippedToBounds: true });
}

/// <summary>ContentPage. Pages fill the space they are given.</summary>
public class PageHandler : ContentViewHandler
{
	public static new IPropertyMapper<IContentView, PageHandler> Mapper =
		new PropertyMapper<IContentView, PageHandler>(ContentViewHandler.Mapper)
		{
			[nameof(ITitledElement.Title)] = MapTitle,
		};

	public PageHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView() => new("div", "maui-page");

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		var content = MeasureContent(widthConstraint, heightConstraint);
		return new Size(
			double.IsFinite(widthConstraint) ? widthConstraint : content.Width,
			double.IsFinite(heightConstraint) ? heightConstraint : content.Height);
	}

	// Titles are shown by the hosting NavigationPage, TabbedPage, FlyoutPage or Shell.
	public static void MapTitle(PageHandler handler, IContentView page)
	{
		// Shell pages are parented by ShellContent/ShellSection, which have no handler.
		for (var e = (page as Microsoft.Maui.Controls.Element)?.Parent; e is not null; e = e.Parent)
		{
			if (e.Handler is IPageHost host)
			{
				host.OnPageTitleChanged(page);
				return;
			}
		}
	}
}

/// <summary>Implemented by page containers that render child page titles.</summary>
public interface IPageHost
{
	void OnPageTitleChanged(IView page);
}
