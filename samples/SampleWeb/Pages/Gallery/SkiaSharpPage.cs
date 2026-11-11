using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace SampleWeb.Pages;

/// <summary>SkiaSharp's SKCanvasView, rendered by Skia on WebAssembly (Platform.Maui.Web.SkiaSharp).</summary>
public class SkiaSharpPage : ContentPage
{
	readonly SKCanvasView _spinner;
	readonly List<SKPath> _strokes = new();
	readonly Dictionary<long, SKPath> _active = new();
	IDispatcherTimer? _timer;
	float _angle;

	public SkiaSharpPage()
	{
		Title = "SkiaSharp";

		var shapes = new SKCanvasView { HeightRequest = 180 };
		shapes.PaintSurface += OnPaintShapes;

		_spinner = new SKCanvasView { HeightRequest = 180 };
		_spinner.PaintSurface += OnPaintSpinner;

		var paint = new SKCanvasView { HeightRequest = 260, EnableTouchEvents = true };
		paint.PaintSurface += OnPaintStrokes;
		paint.Touch += (_, e) => OnTouch(paint, e);

		var clear = new Button { Text = "Clear", HorizontalOptions = LayoutOptions.Start };
		clear.Clicked += (_, _) =>
		{
			_strokes.ForEach(p => p.Dispose());
			_strokes.Clear();
			paint.InvalidateSurface();
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 16,
				Padding = new Thickness(24),
				Children =
				{
					new Label { Text = "SkiaSharp (SKCanvasView)", FontSize = 24, FontAttributes = FontAttributes.Bold },
					new BoxView { HeightRequest = 2, Color = Colors.DodgerBlue },
					new Label { Text = "Skia runs on WebAssembly and renders at the device pixel ratio; each frame is copied onto a <canvas>." },

					new Label { Text = "Shapes, Gradients & Text", FontSize = 16, FontAttributes = FontAttributes.Bold },
					shapes,

					new Label { Text = "Animated (InvalidateSurface on a timer)", FontSize = 16, FontAttributes = FontAttributes.Bold },
					_spinner,

					new Label { Text = "Finger Paint (EnableTouchEvents)", FontSize = 16, FontAttributes = FontAttributes.Bold },
					paint,
					clear,
				}
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromMilliseconds(16);
		_timer.Tick += (_, _) =>
		{
			_angle = (_angle + 2) % 360;
			_spinner.InvalidateSurface();
		};
		_timer.Start();
	}

	protected override void OnDisappearing()
	{
		_timer?.Stop();
		_timer = null;
		base.OnDisappearing();
	}

	static void OnPaintShapes(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		var info = e.Info;
		// Draw in CSS pixels; the surface is at the device pixel ratio.
		var scale = info.Width / (float)((SKCanvasView)sender!).Width;
		canvas.Clear(new SKColor(0xf8, 0xf9, 0xfa));
		canvas.Scale(scale);

		using var gradient = new SKPaint
		{
			IsAntialias = true,
			Shader = SKShader.CreateLinearGradient(new SKPoint(20, 20), new SKPoint(160, 160),
				[SKColors.DeepSkyBlue, SKColors.MediumPurple], SKShaderTileMode.Clamp),
		};
		canvas.DrawRoundRect(20, 20, 140, 140, 24, 24, gradient);

		using var radial = new SKPaint
		{
			IsAntialias = true,
			Shader = SKShader.CreateRadialGradient(new SKPoint(260, 90), 70, [SKColors.Gold, SKColors.OrangeRed], SKShaderTileMode.Clamp),
		};
		canvas.DrawCircle(260, 90, 70, radial);

		using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 6, Color = SKColors.SeaGreen, StrokeCap = SKStrokeCap.Round };
		using var star = new SKPath();
		for (var i = 0; i < 5; i++)
		{
			var a = (float)(Math.PI / 2 + i * 4 * Math.PI / 5);
			var point = new SKPoint(420 + 65 * MathF.Cos(a), 90 - 65 * MathF.Sin(a));
			if (i == 0)
				star.MoveTo(point);
			else
				star.LineTo(point);
		}
		star.Close();
		canvas.DrawPath(star, stroke);

		using var font = new SKFont { Size = 22 };
		using var text = new SKPaint { IsAntialias = true, Color = SKColors.DimGray };
		canvas.DrawText("Hello from Skia", 520, 98, font, text);
	}

	void OnPaintSpinner(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		var info = e.Info;
		canvas.Clear(new SKColor(0x1e, 0x1e, 0x2e));

		var center = new SKPoint(info.Width / 2f, info.Height / 2f);
		var radius = Math.Min(info.Width, info.Height) * 0.38f;
		using var arc = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = radius * 0.18f,
			StrokeCap = SKStrokeCap.Round,
			Shader = SKShader.CreateSweepGradient(center, [SKColors.Transparent, SKColors.Cyan, SKColors.Magenta]),
		};
		canvas.Save();
		canvas.RotateDegrees(_angle, center.X, center.Y);
		canvas.DrawArc(new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius), 0, 300, false, arc);
		canvas.Restore();
	}

	void OnPaintStrokes(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.White);
		using var border = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.LightGray, StrokeWidth = 2 };
		canvas.DrawRect(1, 1, e.Info.Width - 2, e.Info.Height - 2, border);

		using var ink = new SKPaint
		{
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			Color = SKColors.DodgerBlue,
			StrokeWidth = 4 * e.Info.Width / (float)((SKCanvasView)sender!).Width,
			StrokeCap = SKStrokeCap.Round,
			StrokeJoin = SKStrokeJoin.Round,
		};
		foreach (var path in _strokes)
			canvas.DrawPath(path, ink);
		foreach (var path in _active.Values)
			canvas.DrawPath(path, ink);
	}

	void OnTouch(SKCanvasView view, SKTouchEventArgs e)
	{
		switch (e.ActionType)
		{
			case SKTouchAction.Pressed:
				var path = new SKPath();
				path.MoveTo(e.Location);
				_active[e.Id] = path;
				break;
			case SKTouchAction.Moved when _active.TryGetValue(e.Id, out var current):
				current.LineTo(e.Location);
				break;
			case SKTouchAction.Released or SKTouchAction.Cancelled when _active.Remove(e.Id, out var done):
				_strokes.Add(done);
				break;
		}
		e.Handled = true;
		view.InvalidateSurface();
	}
}
