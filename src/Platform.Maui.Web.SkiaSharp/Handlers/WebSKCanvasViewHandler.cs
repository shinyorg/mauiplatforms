using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web.Skia.Interop;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace Microsoft.Maui.Platform.Web.Skia.Handlers;

/// <summary>
/// SkiaSharp's <see cref="ISKCanvasView"/> (<c>SKCanvasView</c>) on Platform.Maui.Web: Skia renders on
/// WebAssembly into a raster surface, and each frame is copied onto a &lt;canvas&gt; in one interop call.
/// </summary>
/// <remarks>
/// The surface matches the canvas at the device pixel ratio, unless <see cref="ISKCanvasView.IgnorePixelScaling"/>
/// is set, in which case it is sized in CSS pixels and the browser scales it. Touch locations are reported
/// in the same units as the surface, as on the other platforms.
/// </remarks>
public class WebSKCanvasViewHandler : WebElementHandler<ISKCanvasView, WebElement>
{
	public static IPropertyMapper<ISKCanvasView, WebSKCanvasViewHandler> Mapper =
		new PropertyMapper<ISKCanvasView, WebSKCanvasViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(ISKCanvasView.IgnorePixelScaling)] = MapIgnorePixelScaling,
			[nameof(ISKCanvasView.EnableTouchEvents)] = MapEnableTouchEvents,
		};

	public static CommandMapper<ISKCanvasView, WebSKCanvasViewHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(ISKCanvasView.InvalidateSurface)] = (h, _, _) => h.RequestDraw(),
	};

	public WebSKCanvasViewHandler() : base(Mapper, CommandMapper)
	{
	}

	SKSurface? _surface;
	SKImageInfo _info;
	SKSizeI _canvasSize;
	bool _touchAttached;
	bool _drawPending;
	bool _visible = true;
	bool _staleWhileHidden;

	protected override WebElement CreatePlatformView() => new("canvas", "maui-skia-canvas-view");

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		// Off-screen canvases skip drawing and catch up when they scroll into view.
		Track(platformView.OnVisibility(visible =>
		{
			_visible = visible;
			if (visible && _staleWhileHidden)
				RequestDraw();
		}));
		_ = AttachAsync(platformView);
	}

	async Task AttachAsync(WebElement platformView)
	{
		await SkiaJs.EnsureLoadedAsync();
		if (PlatformView != platformView)
			return;
		SkiaJs.WatchPixelRatio(platformView.Js, RequestDraw);
		UpdateTouch();
		RequestDraw();
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		if (SkiaJs.IsLoaded)
		{
			SkiaJs.UnwatchPixelRatio(platformView.Js);
			SkiaJs.DetachTouch(platformView.Js);
		}
		_touchAttached = false;
		_surface?.Dispose();
		_surface = null;
		base.DisconnectHandler(platformView);
	}

	// Without a size request a canvas has no natural size; it takes what the layout gives it.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint) => Size.Zero;

	public override void PlatformArrange(Rect frame)
	{
		var resized = frame.Size != ArrangedFrame.Size;
		base.PlatformArrange(frame);
		if (resized)
			Draw();
	}

	public static void MapIgnorePixelScaling(WebSKCanvasViewHandler handler, ISKCanvasView view) => handler.RequestDraw();

	public static void MapEnableTouchEvents(WebSKCanvasViewHandler handler, ISKCanvasView view) => handler.UpdateTouch();

	void UpdateTouch()
	{
		if (!SkiaJs.IsLoaded || CurrentView is not { } view)
			return;

		// Touch drawing needs the browser to leave panning and zooming of the canvas alone.
		PlatformView.SetStyle("touch-action", view.EnableTouchEvents ? "none" : null);
		if (view.EnableTouchEvents && !_touchAttached)
			SkiaJs.AttachTouch(PlatformView.Js, OnTouch);
		else if (!view.EnableTouchEvents && _touchAttached)
			SkiaJs.DetachTouch(PlatformView.Js);
		_touchAttached = view.EnableTouchEvents;
	}

	bool OnTouch(JSObject e)
	{
		using (e)
		{
			try
			{
				return RaiseTouch(e);
			}
			catch (Exception ex)
			{
				// An exception thrown back into a DOM event loses its .NET stack, so log it here.
				Console.Error.WriteLine($"[maui-web] SKCanvasView touch handler failed: {ex}");
				return false;
			}
		}
	}

	bool RaiseTouch(JSObject e)
	{
		if (CurrentView is not { } view)
			return false;

		var action = e.GetPropertyAsString("type") switch
		{
			"entered" => SKTouchAction.Entered,
			"pressed" => SKTouchAction.Pressed,
			"moved" => SKTouchAction.Moved,
			"released" => SKTouchAction.Released,
			"cancelled" => SKTouchAction.Cancelled,
			"exited" => SKTouchAction.Exited,
			_ => SKTouchAction.WheelChanged,
		};
		var device = e.GetPropertyAsString("device") switch
		{
			"touch" => SKTouchDeviceType.Touch,
			"pen" => SKTouchDeviceType.Pen,
			_ => SKTouchDeviceType.Mouse,
		};
		var button = e.GetPropertyAsInt32("button") switch
		{
			0 => SKMouseButton.Left,
			1 => SKMouseButton.Middle,
			2 => SKMouseButton.Right,
			_ => SKMouseButton.Unknown,
		};
		var scale = (float)PixelScale(view);
		var location = new SKPoint((float)e.GetPropertyAsDouble("x") * scale, (float)e.GetPropertyAsDouble("y") * scale);

		var args = new SKTouchEventArgs(
			e.GetPropertyAsInt32("id"),
			action,
			button,
			device,
			location,
			e.GetPropertyAsBoolean("inContact"),
			e.GetPropertyAsInt32("wheel"),
			(float)e.GetPropertyAsDouble("pressure"));
		view.OnTouch(args);
		return args.Handled;
	}

	static double PixelScale(ISKCanvasView view) => view.IgnorePixelScaling || !SkiaJs.IsLoaded ? 1 : SkiaJs.DevicePixelRatio();

	/// <summary>
	/// Coalesces invalidations into at most one draw per animation frame, and none while the canvas is
	/// off screen, as GraphicsView does.
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
		SkiaJs.RequestFrame(() =>
		{
			_drawPending = false;
			if (CurrentView is not null)
				Draw();
		});
	}

	void Draw()
	{
		if (!SkiaJs.IsLoaded || CurrentView is not { } view)
			return;
		_staleWhileHidden = false;

		var size = ArrangedFrame.Size;
		var scale = PixelScale(view);
		var width = (int)Math.Round(size.Width * scale);
		var height = (int)Math.Round(size.Height * scale);
		if (width <= 0 || height <= 0)
			return;

		if (_surface is null || _info.Width != width || _info.Height != height)
		{
			_surface?.Dispose();
			// ImageData is unpremultiplied RGBA, so Skia renders straight into that layout.
			_info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			_surface = SKSurface.Create(_info);
			if (_surface is null)
				return;
		}

		var canvasSize = new SKSizeI(width, height);
		if (canvasSize != _canvasSize)
		{
			_canvasSize = canvasSize;
			view.OnCanvasSizeChanged(canvasSize);
		}

		// The surface is reused between frames, so each paint starts from a clean matrix and clip, as a
		// fresh canvas does on the other platforms.
		var canvas = _surface.Canvas;
		var save = canvas.Save();
		canvas.Clear(SKColors.Transparent);
		view.OnPaintSurface(new SKPaintSurfaceEventArgs(_surface, _info, _info));
		canvas.RestoreToCount(save);

		using var pixmap = _surface.PeekPixels();
		SkiaJs.Present(PlatformView.Js, pixmap.GetPixelSpan(), width, height);
	}
}
