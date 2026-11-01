using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// An <see cref="ICanvas"/> that records Canvas 2D commands and replays them in a single interop
/// call. State (transform, clip, styles) lives in the 2D context's own save/restore stack.
/// </summary>
public sealed class WebCanvas : ICanvas
{
	readonly StringBuilder _commands = new("[");
	readonly Stack<State> _states = new();
	State _state = new();
	bool _first = true;

	sealed record State
	{
		public float StrokeSize { get; set; } = 1;
		public float FontSize { get; set; } = 12;
		public IFont? Font { get; set; }
		public Color FontColor { get; set; } = Colors.Black;
	}

	public float DisplayScale { get; set; } = 1;

	public float StrokeSize { set { _state.StrokeSize = value; Command("lineWidth", value); } }

	public float MiterLimit { set => Command("miterLimit", value); }

	public Color StrokeColor { set => Command("strokeStyle", WebCss.Color(value) ?? "transparent"); }

	public LineCap StrokeLineCap { set => Command("lineCap", WebCss.LineCap(value)); }

	public LineJoin StrokeLineJoin { set => Command("lineJoin", WebCss.LineJoin(value)); }

	public float[] StrokeDashPattern { set => Command("dash", string.Join(' ', (value ?? []).Select(v => Num(v * _state.StrokeSize)))); }

	public float StrokeDashOffset { set => Command("lineDashOffset", value * _state.StrokeSize); }

	public Color FillColor { set => Command("fillStyle", WebCss.Color(value) ?? "transparent"); }

	public Color FontColor { set => _state.FontColor = value ?? Colors.Black; }

	public IFont Font { set { _state.Font = value; ApplyFont(); } }

	public float FontSize { set { _state.FontSize = value; ApplyFont(); } }

	public float Alpha { set => Command("globalAlpha", value); }

	public bool Antialias { set { } }

	public BlendMode BlendMode
	{
		set => Command("globalCompositeOperation", value switch
		{
			BlendMode.Multiply => "multiply",
			BlendMode.Screen => "screen",
			BlendMode.Overlay => "overlay",
			BlendMode.Darken => "darken",
			BlendMode.Lighten => "lighten",
			BlendMode.ColorDodge => "color-dodge",
			BlendMode.ColorBurn => "color-burn",
			BlendMode.SoftLight => "soft-light",
			BlendMode.HardLight => "hard-light",
			BlendMode.Difference => "difference",
			BlendMode.Exclusion => "exclusion",
			BlendMode.Hue => "hue",
			BlendMode.Saturation => "saturation",
			BlendMode.Color => "color",
			BlendMode.Luminosity => "luminosity",
			BlendMode.Clear => "destination-out",
			BlendMode.Copy => "copy",
			BlendMode.SourceIn => "source-in",
			BlendMode.SourceOut => "source-out",
			BlendMode.SourceAtop => "source-atop",
			BlendMode.DestinationOver => "destination-over",
			BlendMode.DestinationIn => "destination-in",
			BlendMode.DestinationOut => "destination-out",
			BlendMode.DestinationAtop => "destination-atop",
			BlendMode.Xor => "xor",
			_ => "source-over",
		});
	}

	/// <summary>Returns the recorded commands as JSON and resets the buffer.</summary>
	public string Flush()
	{
		_commands.Append(']');
		var json = _commands.ToString();
		_commands.Clear().Append('[');
		_first = true;
		_states.Clear();
		_state = new State();
		return json;
	}

	public void SaveState()
	{
		_states.Push(_state with { });
		Command("save");
	}

	public bool RestoreState()
	{
		if (!_states.TryPop(out var state))
			return false;
		_state = state;
		Command("restore");
		return true;
	}

	public void ResetState()
	{
		while (RestoreState())
		{
		}
		_state = new State();
		Command("reset");
	}

	public void DrawLine(float x1, float y1, float x2, float y2) => Command("line", x1, y1, x2, y2);

	public void DrawArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed)
		=> Path("stroke", ArcPath(x, y, width, height, startAngle, endAngle, clockwise, closed));

	public void FillArc(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise)
		=> Path("fill", ArcPath(x, y, width, height, startAngle, endAngle, clockwise, true));

	static PathF ArcPath(float x, float y, float width, float height, float startAngle, float endAngle, bool clockwise, bool closed)
	{
		var path = new PathF();
		path.AddArc(x, y, x + width, y + height, startAngle, endAngle, clockwise);
		if (closed)
			path.Close();
		return path;
	}

	public void DrawRectangle(float x, float y, float width, float height) => Command("strokeRect", x, y, width, height);

	public void FillRectangle(float x, float y, float width, float height) => Command("fillRect", x, y, width, height);

	public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
		=> Command("strokeRoundRect", x, y, width, height, cornerRadius);

	public void FillRoundedRectangle(float x, float y, float width, float height, float cornerRadius)
		=> Command("fillRoundRect", x, y, width, height, cornerRadius);

	public void DrawEllipse(float x, float y, float width, float height) => Command("strokeEllipse", x, y, width, height);

	public void FillEllipse(float x, float y, float width, float height) => Command("fillEllipse", x, y, width, height);

	public void DrawPath(PathF path) => Path("stroke", path);

	public void FillPath(PathF path, WindingMode windingMode)
		=> Path(windingMode == WindingMode.EvenOdd ? "fillEvenOdd" : "fill", path);

	public void ClipPath(PathF path, WindingMode windingMode = WindingMode.NonZero)
		=> Path(windingMode == WindingMode.EvenOdd ? "clipEvenOdd" : "clip", path);

	public void ClipRectangle(float x, float y, float width, float height) => Command("clipRect", x, y, width, height);

	public void SubtractFromClip(float x, float y, float width, float height) => Command("subtractClip", x, y, width, height);

	public void DrawString(string value, float x, float y, HorizontalAlignment horizontalAlignment)
	{
		ApplyFontColor();
		Command("text", value, x, y, Align(horizontalAlignment), "alphabetic");
	}

	public void DrawString(string value, float x, float y, float width, float height, HorizontalAlignment horizontalAlignment,
		VerticalAlignment verticalAlignment, TextFlow textFlow = TextFlow.ClipBounds, float lineSpacingAdjustment = 0)
	{
		ApplyFontColor();
		var tx = horizontalAlignment switch
		{
			HorizontalAlignment.Center => x + width / 2,
			HorizontalAlignment.Right => x + width,
			_ => x,
		};
		var (ty, baseline) = verticalAlignment switch
		{
			VerticalAlignment.Center => (y + height / 2, "middle"),
			VerticalAlignment.Bottom => (y + height, "bottom"),
			_ => (y, "top"),
		};
		if (textFlow == TextFlow.ClipBounds)
		{
			Command("save");
			Command("clipRect", x, y, width, height);
		}
		Command("text", value, tx, ty, Align(horizontalAlignment), baseline);
		if (textFlow == TextFlow.ClipBounds)
			Command("restore");
	}

	public void DrawText(Microsoft.Maui.Graphics.Text.IAttributedText value, float x, float y, float width, float height)
		=> DrawString(value.Text, x, y, width, height, HorizontalAlignment.Left, VerticalAlignment.Top);

	// Platform images are not available to the browser canvas.
	public void DrawImage(Microsoft.Maui.Graphics.IImage image, float x, float y, float width, float height)
	{
	}

	public void Rotate(float degrees, float x, float y)
	{
		Command("translate", x, y);
		Command("rotate", degrees * MathF.PI / 180);
		Command("translate", -x, -y);
	}

	public void Rotate(float degrees) => Command("rotate", degrees * MathF.PI / 180);

	public void Scale(float sx, float sy) => Command("scale", sx, sy);

	public void Translate(float tx, float ty) => Command("translate", tx, ty);

	public void ConcatenateTransform(Matrix3x2 transform)
		=> Command("transform", transform.M11, transform.M12, transform.M21, transform.M22, transform.M31, transform.M32);

	public void SetShadow(SizeF offset, float blur, Color color)
		=> Command("shadow", offset.Width, offset.Height, blur, WebCss.Color(color) ?? "transparent");

	public void SetFillPaint(Paint paint, RectF rectangle)
	{
		switch (paint)
		{
			case SolidPaint solid:
				FillColor = solid.Color;
				break;
			case LinearGradientPaint linear:
				Gradient("linear", paint, rectangle.X + linear.StartPoint.X * rectangle.Width, rectangle.Y + linear.StartPoint.Y * rectangle.Height,
					rectangle.X + linear.EndPoint.X * rectangle.Width, rectangle.Y + linear.EndPoint.Y * rectangle.Height, 0);
				break;
			case RadialGradientPaint radial:
				Gradient("radial", paint, rectangle.X + radial.Center.X * rectangle.Width, rectangle.Y + radial.Center.Y * rectangle.Height,
					0, 0, radial.Radius * Math.Max(rectangle.Width, rectangle.Height));
				break;
			default:
				if (paint?.ToColor() is { } color)
					FillColor = color;
				break;
		}
	}

	void Gradient(string kind, Paint paint, double x0, double y0, double x1, double y1, double radius)
	{
		var stops = string.Join(';', ((GradientPaint)paint).GradientStops.Select(s => $"{Num(s.Offset)}|{WebCss.Color(s.Color)}"));
		Command("gradient", kind, x0, y0, x1, y1, radius, stops);
	}

	public SizeF GetStringSize(string value, IFont font, float fontSize)
	{
		var size = Dom.MeasureText(CssFont(font, fontSize), value);
		return new SizeF((float)size[0], (float)size[1]);
	}

	public SizeF GetStringSize(string value, IFont font, float fontSize, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
		=> GetStringSize(value, font, fontSize);

	void ApplyFont() => Command("font", CssFont(_state.Font, _state.FontSize));

	void ApplyFontColor() => Command("fillStyle", WebCss.Color(_state.FontColor) ?? "#000");

	static string CssFont(IFont? font, float size)
	{
		var weight = font?.Weight is int w and > 0 ? w : 400;
		var style = font?.StyleType is FontStyleType.Italic or FontStyleType.Oblique ? "italic " : string.Empty;
		// Microsoft.Maui.Graphics defaults to "Arial"; prefer the platform UI font for that default.
		var family = string.IsNullOrEmpty(font?.Name) || font.Name == Microsoft.Maui.Graphics.Font.Default.Name
			? "system-ui, sans-serif"
			: $"\"{WebCss.Escape(font.Name)}\", system-ui, sans-serif";
		return $"{style}{weight} {Num(size)}px {family}";
	}

	static string Align(HorizontalAlignment alignment) => alignment switch
	{
		HorizontalAlignment.Center => "center",
		HorizontalAlignment.Right => "right",
		_ => "left",
	};

	void Path(string op, PathF path) => Command(op, path.ToDefinitionString().Trim());

	void Command(string name, params object?[] args)
	{
		if (!_first)
			_commands.Append(',');
		_first = false;
		_commands.Append("[\"").Append(name).Append('"');
		foreach (var arg in args)
		{
			_commands.Append(',');
			switch (arg)
			{
				case null:
					_commands.Append("null");
					break;
				case string s:
					_commands.Append('"').Append(JsonEscape(s)).Append('"');
					break;
				case float f:
					_commands.Append(Num(f));
					break;
				case double d:
					_commands.Append(Num(d));
					break;
				default:
					_commands.Append(Convert.ToString(arg, CultureInfo.InvariantCulture));
					break;
			}
		}
		_commands.Append(']');
	}

	static string Num(double value) => double.IsFinite(value) ? Math.Round(value, 3).ToString(CultureInfo.InvariantCulture) : "0";

	static string JsonEscape(string value)
	{
		var sb = new StringBuilder(value.Length);
		foreach (var c in value)
		{
			switch (c)
			{
				case '"': sb.Append("\\\""); break;
				case '\\': sb.Append("\\\\"); break;
				case '\n': sb.Append("\\n"); break;
				case '\r': sb.Append("\\r"); break;
				case '\t': sb.Append("\\t"); break;
				default:
					if (c < ' ')
						sb.Append("\\u").Append(((int)c).ToString("x4"));
					else
						sb.Append(c);
					break;
			}
		}
		return sb.ToString();
	}
}
