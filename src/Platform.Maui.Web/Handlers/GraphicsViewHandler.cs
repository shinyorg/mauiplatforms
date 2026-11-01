using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>GraphicsView on a &lt;canvas&gt;, drawn through <see cref="WebCanvas"/>.</summary>
public class GraphicsViewHandler : WebElementHandler<IGraphicsView, WebElement>
{
	public static IPropertyMapper<IGraphicsView, GraphicsViewHandler> Mapper =
		new PropertyMapper<IGraphicsView, GraphicsViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IGraphicsView.Drawable)] = MapInvalidate,
			[nameof(IView.Background)] = MapInvalidate,
		};

	public static CommandMapper<IGraphicsView, GraphicsViewHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(IGraphicsView.Invalidate)] = (h, v, _) => h.Draw(),
	};

	public GraphicsViewHandler() : base(Mapper, CommandMapper)
	{
	}

	readonly WebCanvas _canvas = new();
	bool _pressed;

	protected override WebElement CreatePlatformView()
	{
		var canvas = new WebElement("canvas", "maui-graphics-view");
		canvas.SetStyle("touch-action", "none");
		return canvas;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.OnPointer("pointerdown", (x, y, _) => { _pressed = true; VirtualView.StartInteraction([new PointF((float)x, (float)y)]); }, capture: true));
		Track(platformView.OnPointer("pointermove", (x, y, _) =>
		{
			if (_pressed)
				VirtualView.DragInteraction([new PointF((float)x, (float)y)]);
			else
				VirtualView.MoveHoverInteraction([new PointF((float)x, (float)y)]);
		}));
		Track(platformView.OnPointer("pointerup", (x, y, _) =>
		{
			_pressed = false;
			VirtualView.EndInteraction([new PointF((float)x, (float)y)], x >= 0 && y >= 0 && x <= ArrangedFrame.Width && y <= ArrangedFrame.Height);
		}));
		Track(platformView.On("pointercancel", () => { _pressed = false; VirtualView.CancelInteraction(); }));
		Track(platformView.OnPointer("pointerenter", (x, y, _) => VirtualView.StartHoverInteraction([new PointF((float)x, (float)y)])));
		Track(platformView.On("pointerleave", () => VirtualView.EndHoverInteraction()));
	}

	// Without a size request a GraphicsView has no natural size; it takes what the layout gives it.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint) => Size.Zero;

	public override void PlatformArrange(Rect frame)
	{
		var resized = frame.Size != ArrangedFrame.Size;
		base.PlatformArrange(frame);
		if (resized)
			Draw();
	}

	public static void MapInvalidate(GraphicsViewHandler handler, IGraphicsView view) => handler.Draw();

	void Draw()
	{
		var size = ArrangedFrame.Size;
		if (VirtualView is null || size.Width <= 0 || size.Height <= 0)
			return;

		var dirty = new RectF(0, 0, (float)size.Width, (float)size.Height);
		if (VirtualView.Background is { } background)
		{
			_canvas.SaveState();
			_canvas.SetFillPaint(background, dirty);
			_canvas.FillRectangle(dirty);
			_canvas.RestoreState();
		}
		VirtualView.Drawable?.Draw(_canvas, dirty);
		Dom.DrawCommands(PlatformView.Js, size.Width, size.Height, _canvas.Flush());
	}
}
