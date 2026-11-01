using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// RefreshView: hosts its content and shows a spinner while IsRefreshing. Pull-to-refresh
/// gestures are not implemented; set IsRefreshing (or invoke Command) from your own UI.
/// </summary>
public class RefreshViewHandler : ContentViewHandler
{
	public static new IPropertyMapper<IContentView, RefreshViewHandler> Mapper =
		new PropertyMapper<IContentView, RefreshViewHandler>(ContentViewHandler.Mapper)
		{
			[nameof(IRefreshView.IsRefreshing)] = MapIsRefreshing,
			[nameof(IRefreshView.RefreshColor)] = MapRefreshColor,
		};

	public RefreshViewHandler() : base(Mapper)
	{
	}

	WebElement _spinner = null!;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-refresh-view maui-clip");
		_spinner = new WebElement("div", "maui-activity-indicator");
		_spinner.Append(new WebElement("div"));
		_spinner.SetStyle("left", "calc(50% - 16px)");
		_spinner.SetStyle("top", "12px");
		_spinner.SetStyle("z-index", "5");
		_spinner.SetStyle("position", "absolute");
		root.Append(_spinner);
		return root;
	}

	public static void MapIsRefreshing(RefreshViewHandler handler, IContentView view)
		=> handler._spinner.ToggleClass("running", view is IRefreshView { IsRefreshing: true });

	public static void MapRefreshColor(RefreshViewHandler handler, IContentView view)
		=> handler._spinner.SetStyle("--maui-spinner", WebCss.PaintColor((view as IRefreshView)?.RefreshColor));
}

/// <summary>SwipeView: hosts its content. Swipe items are not revealed by gestures yet.</summary>
public class SwipeViewHandler : ContentViewHandler
{
	public SwipeViewHandler() : base(Mapper)
	{
	}
}

public class IndicatorViewHandler : WebElementHandler<IIndicatorView, WebElement>
{
	public static IPropertyMapper<IIndicatorView, IndicatorViewHandler> Mapper =
		new PropertyMapper<IIndicatorView, IndicatorViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IIndicatorView.Count)] = MapIndicators,
			[nameof(IIndicatorView.Position)] = MapIndicators,
			[nameof(IIndicatorView.HideSingle)] = MapIndicators,
			[nameof(IIndicatorView.MaximumVisible)] = MapIndicators,
			[nameof(IIndicatorView.IndicatorSize)] = MapIndicators,
			[nameof(IIndicatorView.IndicatorColor)] = MapIndicators,
			[nameof(IIndicatorView.SelectedIndicatorColor)] = MapIndicators,
			[nameof(IIndicatorView.IndicatorsShape)] = MapIndicators,
		};

	public IndicatorViewHandler() : base(Mapper)
	{
	}

	const double Spacing = 6;
	readonly List<IDisposable> _dotSubscriptions = new();

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-indicator-view");
		root.SetStyle("display", "flex");
		root.SetStyle("gap", WebCss.Px(Spacing));
		root.SetStyle("align-items", "center");
		root.SetStyle("justify-content", "center");
		root.SetAttribute("role", "tablist");
		return root;
	}

	int VisibleCount
	{
		get
		{
			var view = VirtualView;
			var count = Math.Min(view.Count, view.MaximumVisible);
			return view.HideSingle && count <= 1 ? 0 : count;
		}
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
	{
		var size = Math.Max(1, VirtualView.IndicatorSize);
		var count = VisibleCount;
		return count == 0 ? Size.Zero : new Size(count * size + (count - 1) * Spacing, size);
	}

	public static void MapIndicators(IndicatorViewHandler handler, IIndicatorView view)
	{
		foreach (var s in handler._dotSubscriptions)
			s.Dispose();
		handler._dotSubscriptions.Clear();
		handler.PlatformView.Clear();

		var size = WebCss.Px(Math.Max(1, view.IndicatorSize));
		var round = view.IndicatorsShape is not Microsoft.Maui.Controls.Shapes.Rectangle;
		for (var i = 0; i < handler.VisibleCount; i++)
		{
			var selected = i == view.Position;
			var dot = new WebElement("button");
			dot.SetAttribute("type", "button");
			dot.SetAttribute("role", "tab");
			dot.SetAttribute("aria-label", $"Item {i + 1}");
			dot.SetAttribute("aria-selected", selected ? "true" : "false");
			dot.SetStyle("width", size);
			dot.SetStyle("height", size);
			dot.SetStyle("padding", "0");
			dot.SetStyle("border", "0");
			dot.SetStyle("cursor", "pointer");
			dot.SetStyle("border-radius", round ? "50%" : "0");
			dot.SetStyle("background", (selected ? WebCss.PaintColor(view.SelectedIndicatorColor) : WebCss.PaintColor(view.IndicatorColor))
				?? (selected ? "var(--maui-accent)" : "var(--maui-border)"));
			var index = i;
			handler._dotSubscriptions.Add(dot.On("click", () => view.Position = index));
			handler.PlatformView.Append(dot);
		}
		handler.VirtualView?.InvalidateMeasure();
	}
}

/// <summary>
/// Placeholder for controls this backend does not render (the obsolete ListView and TableView, HybridWebView).
/// It keeps the app running and says what to use instead.
/// </summary>
public class UnsupportedViewHandler : WebElementHandler<IView, WebElement>
{
	public UnsupportedViewHandler() : base(WebElementHandler.ViewMapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-unsupported");
		root.SetStyle("border", "1px dashed var(--maui-border)");
		root.SetStyle("border-radius", "8px");
		root.SetStyle("padding", "12px");
		root.SetStyle("color", "var(--maui-muted)");
		return root;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		var name = VirtualView.GetType().Name;
		platformView.SetText(VirtualView is Microsoft.Maui.Controls.HybridWebView
			? $"{name} is not supported by the web backend yet. Use WebView instead."
			: $"{name} is not supported by the web backend. Use CollectionView instead.");
		Dom.Log($"{name} is not supported by the web backend; rendering a placeholder.");
	}
}

/// <summary>A plain <see cref="Microsoft.Maui.Controls.Page"/> (not a ContentPage): an empty page that fills its space.</summary>
public class EmptyPageHandler : WebElementHandler<IView, WebElement>
{
	public EmptyPageHandler() : base(WebElementHandler.ViewMapper)
	{
	}

	protected override WebElement CreatePlatformView() => new("div", "maui-page");

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 0, double.IsFinite(heightConstraint) ? heightConstraint : 0);
}

/// <summary>
/// For non-visual elements whose native counterparts the web backend doesn't render (menu bar and
/// context menu items, swipe items). Resolving their handler succeeds and does nothing.
/// </summary>
public class NullElementHandler : Microsoft.Maui.Handlers.ElementHandler<IElement, object>
{
	public NullElementHandler() : base(ElementMapper)
	{
	}

	protected override object CreatePlatformElement() => new();
}
