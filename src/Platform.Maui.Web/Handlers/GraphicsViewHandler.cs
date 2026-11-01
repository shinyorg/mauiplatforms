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
		[nameof(IGraphicsView.Invalidate)] = (h, v, _) => h.RequestDraw(),
	};

	public GraphicsViewHandler() : base(Mapper, CommandMapper)
	{
	}

	readonly WebCanvas _canvas = new();
	bool _pressed;
	bool _drawPending;
	bool _visible = true;
	bool _staleWhileHidden;

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
		// Raised by the page when an image this canvas wanted to draw has finished decoding.
		Track(platformView.On("maui-image-ready", RequestDraw));
		// Off-screen canvases skip drawing and catch up when they scroll into view.
		Track(platformView.OnVisibility(visible =>
		{
			_visible = visible;
			if (visible && _staleWhileHidden)
				RequestDraw();
		}));
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

	public static void MapInvalidate(GraphicsViewHandler handler, IGraphicsView view) => handler.RequestDraw();

	/// <summary>
	/// Coalesces invalidations into at most one draw per animation frame, and none while the canvas is
	/// off screen. An animated drawable can invalidate many times a frame; drawing each time starves
	/// the page's single thread once a few canvases animate at once.
	/// </summary>
	void RequestDraw()
	{
		if (!_visible)
		{
			_staleWhileHidden = true;
			return;
		}
		if (_drawPending)
			return;
		_drawPending = true;
		Dom.RequestFrame(() =>
		{
			_drawPending = false;
			if (CurrentView is not null)
				Draw();
		});
	}

	void Draw()
	{
		_staleWhileHidden = false;
		var size = ArrangedFrame.Size;
		if (CurrentView is null || size.Width <= 0 || size.Height <= 0)
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
		foreach (var (id, bytes) in _canvas.TakePendingImages())
			Dom.RegisterCanvasImage(id, bytes);
		Dom.DrawCommands(PlatformView.Js, size.Width, size.Height, _canvas.Flush());
	}
}
