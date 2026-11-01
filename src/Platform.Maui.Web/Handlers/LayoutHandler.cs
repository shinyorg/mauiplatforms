using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// One handler for every MAUI layout (Grid, StackLayout, FlexLayout, AbsoluteLayout, custom):
/// the layout's ILayoutManager computes child frames and children are absolutely positioned.
/// </summary>
public class LayoutHandler : WebElementHandler<ILayout, WebElement>, ILayoutHandler
{
	public static IPropertyMapper<ILayout, LayoutHandler> Mapper =
		new PropertyMapper<ILayout, LayoutHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(ILayout.ClipsToBounds)] = MapClipsToBounds,
			[nameof(IView.InputTransparent)] = MapInputTransparent,
		};

	public static CommandMapper<ILayout, LayoutHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(ILayoutHandler.Add)] = MapAdd,
		[nameof(ILayoutHandler.Remove)] = MapRemove,
		[nameof(ILayoutHandler.Clear)] = MapClear,
		[nameof(ILayoutHandler.Insert)] = MapInsert,
		[nameof(ILayoutHandler.Update)] = MapUpdate,
		[nameof(ILayoutHandler.UpdateZIndex)] = MapUpdateZIndex,
	};

	public LayoutHandler() : base(Mapper, CommandMapper)
	{
	}

	public LayoutHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
		: base(mapper ?? Mapper, commandMapper ?? CommandMapper)
	{
	}

	object ILayoutHandler.PlatformView => PlatformView;

	protected override WebElement CreatePlatformView() => new("div", "maui-layout");

	public override void SetVirtualView(IView view)
	{
		base.SetVirtualView(view);

		// Existing children are not announced through Add; mirror them now.
		PlatformView.Clear();
		foreach (var child in VirtualView.OrderBy(c => c.ZIndex))
			PlatformView.Append(ToPlatform(child));
	}

	Size _measuredConstraints = new(-1, -1);

	public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
	{
		_measuredConstraints = new Size(widthConstraint, heightConstraint);
		return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
	}

	public override void PlatformArrange(Rect frame)
	{
		base.PlatformArrange(frame);
		// Layout managers arrange from their last measure. When the frame differs from the
		// constraints that measure used (alignment, MaximumWidthRequest, a parent re-arranging),
		// re-measure at the final size first, as the native MAUI layout views do.
		if (_measuredConstraints != frame.Size)
		{
			_measuredConstraints = frame.Size;
			VirtualView.CrossPlatformMeasure(frame.Width, frame.Height);
		}
		VirtualView.CrossPlatformArrange(new Rect(Point.Zero, frame.Size));
	}

	public void Add(IView child)
	{
		PlatformView.Append(ToPlatform(child));
		WebLayoutScheduler.Request();
	}

	public void Insert(int index, IView child)
	{
		PlatformView.Insert(index, ToPlatform(child));
		WebLayoutScheduler.Request();
	}

	public void Remove(IView child)
	{
		if (child.Handler?.PlatformView is WebElement element)
			PlatformView.Remove(element);
		WebLayoutScheduler.Request();
	}

	public void Clear()
	{
		PlatformView.Clear();
		WebLayoutScheduler.Request();
	}

	public void Update(int index, IView child)
	{
		PlatformView.Insert(index, ToPlatform(child));
		WebLayoutScheduler.Request();
	}

	// z-index is applied per child by the ZIndex mapper; DOM order is the tie-breaker.
	public void UpdateZIndex(IView child) => WebElementHandler.MapZIndex(child.Handler!, child);

	public static void MapClipsToBounds(LayoutHandler handler, ILayout layout)
		=> handler.PlatformView.ToggleClass("maui-clip", layout.ClipsToBounds);

	public static void MapInputTransparent(LayoutHandler handler, ILayout layout)
	{
		WebElementHandler.MapInputTransparent(handler, layout);
		// Without cascading, children stay hit-testable while the layout's own area passes through.
		var cascade = layout is not Microsoft.Maui.Controls.Layout controlsLayout || controlsLayout.CascadeInputTransparent;
		handler.PlatformView.ToggleClass("maui-input-passthrough", layout.InputTransparent && !cascade);
	}

	static void MapAdd(LayoutHandler handler, ILayout layout, object? arg)
	{
		if (arg is LayoutHandlerUpdate update)
			handler.Add(update.View);
	}

	static void MapRemove(LayoutHandler handler, ILayout layout, object? arg)
	{
		if (arg is LayoutHandlerUpdate update)
			handler.Remove(update.View);
	}

	static void MapInsert(LayoutHandler handler, ILayout layout, object? arg)
	{
		if (arg is LayoutHandlerUpdate update)
			handler.Insert(update.Index, update.View);
	}

	static void MapClear(LayoutHandler handler, ILayout layout, object? arg) => handler.Clear();

	static void MapUpdate(LayoutHandler handler, ILayout layout, object? arg)
	{
		if (arg is LayoutHandlerUpdate update)
			handler.Update(update.Index, update.View);
	}

	static void MapUpdateZIndex(LayoutHandler handler, ILayout layout, object? arg)
	{
		if (arg is IView view)
			handler.UpdateZIndex(view);
	}
}
