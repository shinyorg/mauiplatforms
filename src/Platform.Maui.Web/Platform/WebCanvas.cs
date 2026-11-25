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

	public Color StrokeColor { set => ColorCommand("strokeStyle", value, "transparent"); }

	public LineCap StrokeLineCap { set => Command("lineCap", WebCss.LineCap(value)); }

	public LineJoin StrokeLineJoin { set => Command("lineJoin", WebCss.LineJoin(value)); }

	public float[] StrokeDashPattern { set => Command("dash", string.Join(' ', (value ?? []).Select(v => Num(v * _state.StrokeSize)))); }

	public float StrokeDashOffset { set => Command("lineDashOffset", value * _state.StrokeSize); }

	public Color FillColor { set => ColorCommand("fillStyle", value, "transparent"); }

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
		// Paths the page holds for PathFs that have since changed, been disposed or collected.
		if (!s_freedPaths.IsEmpty)
		{
			Begin("pathFree");
			while (s_freedPaths.TryDequeue(out var id))
				Arg(id);
			End();
		}
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

	/// <summary>
	/// Images are drawn by the browser: each IImage is given an id, and its encoded bytes are sent to
	/// the page once (see <see cref="TakePendingImages"/>), where the browser decodes them.
	/// </summary>
	public void DrawImage(Microsoft.Maui.Graphics.IImage image, float x, float y, float width, float height)
	{
		if (image is null || width <= 0 || height <= 0)
			return;
		if (!s_imageIds.TryGetValue(image, out var id))
		{
			byte[] bytes;
			try
			{
				using var stream = new MemoryStream();
				image.Save(stream);
				bytes = stream.ToArray();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"[maui-web] GraphicsView image could not be encoded: {ex.Message}");
				return;
			}
			id = new ImageId(++s_nextImageId);
			s_imageIds.Add(image, id);
			_pendingImages.Add((id.Value, bytes));
		}
		Command("image", id.Value, x, y, width, height);
	}

	sealed record ImageId(int Value);

	// Weak, so images the drawable drops are not kept alive by having been drawn once.
	static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Microsoft.Maui.Graphics.IImage, ImageId> s_imageIds = new();
	static int s_nextImageId;
	readonly List<(int Id, byte[] Bytes)> _pendingImages = new();

	/// <summary>Images first drawn since the last call, which the page has not been sent yet.</summary>
	internal List<(int Id, byte[] Bytes)> TakePendingImages()
	{
		var pending = new List<(int Id, byte[] Bytes)>(_pendingImages);
		_pendingImages.Clear();
		return pending;
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

	void ApplyFontColor() => ColorCommand("fillStyle", _state.FontColor, "#000");

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

	/// <summary>
	/// Writes a path operation. The path's SVG data is kept on the PathF (in
	/// <see cref="PathF.PlatformPath"/>, which PathF clears whenever the path changes), so a path
	/// drawn again is not serialized again. Drawn a second time, it is also handed to the page once
	/// as a Path2D and referred to by id from then on: drawables that cache their geometry (a grid
	/// of dots, a noise tile, a chart's series) then cost a few bytes a frame instead of the whole
	/// path, and the browser does not re-parse it every frame either.
	/// </summary>
	void Path(string op, PathF path)
	{
		if (path.PlatformPath is not WebPath cached)
		{
			// A path drawn once (a temporary built every frame) is written inline, as before.
			cached = new WebPath(PathData(path));
			path.PlatformPath = cached;
			Command(op, cached.Data);
			return;
		}

		if (cached.Id == 0)
		{
			Begin("pathDef");
			Arg(cached.Define());
			Arg(cached.Data);
			End();
		}
		Begin(op);
		Arg(cached.Id);
		End();
	}

	static int s_nextPathId;
	static readonly System.Collections.Concurrent.ConcurrentQueue<int> s_freedPaths = new();

	/// <summary>
	/// A PathF's serialized data, and the id of the Path2D the page holds for it once it has been
	/// sent. PathF disposes it when the path changes or is disposed; a path that is just dropped
	/// releases it from the finalizer. Either way the page is told to forget the Path2D on the next
	/// draw.
	/// </summary>
	sealed class WebPath : IDisposable
	{
		public WebPath(string data)
		{
			Data = data;
			// Most paths are temporaries drawn once; only one the page holds needs finalizing.
			GC.SuppressFinalize(this);
		}

		public string Data { get; }

		public int Id { get; private set; }

		/// <summary>Gives the path an id for the page to keep its Path2D under.</summary>
		public int Define()
		{
			Id = ++s_nextPathId;
			GC.ReRegisterForFinalize(this);
			return Id;
		}

		public void Dispose()
		{
			Release();
			GC.SuppressFinalize(this);
		}

		~WebPath() => Release();

		void Release()
		{
			if (Id == 0)
				return;
			s_freedPaths.Enqueue(Id);
			Id = 0;
		}
	}

	/// <summary>
	/// SVG path data for a PathF. PathF.ToDefinitionString() drops arc segments, which would leave
	/// every DrawArc/FillArc and AddArc path empty, so arcs are written as SVG arc commands here.
	/// </summary>
	internal static string PathData(PathF path)
	{
		var data = new StringBuilder(path.Count * 12 + path.OperationCount * 2);
		var hasCurrentPoint = false;
		// Walked in one pass, keeping the point and arc indexes as it goes: PathF.GetSegmentInfo counts
		// them up from the first segment on every call, which made serializing a path quadratic.
		var point = 0;
		var angle = 0;
		var clockwise = 0;
		foreach (var operation in path.SegmentTypes)
		{
			switch (operation)
			{
				case PathOperation.Move:
					Append(data, 'M', path[point]);
					point += 1;
					hasCurrentPoint = true;
					break;
				case PathOperation.Line:
					Append(data, 'L', path[point]);
					point += 1;
					break;
				case PathOperation.Quad:
					Append(data, 'Q', path[point], path[point + 1]);
					point += 2;
					break;
				case PathOperation.Cubic:
					Append(data, 'C', path[point], path[point + 1], path[point + 2]);
					point += 3;
					break;
				case PathOperation.Arc:
					AppendArc(data, path[point], path[point + 1], path.GetArcAngle(angle), path.GetArcAngle(angle + 1), path.GetArcClockwise(clockwise), hasCurrentPoint);
					point += 2;
					angle += 2;
					clockwise += 1;
					hasCurrentPoint = true;
					break;
				case PathOperation.Close:
					data.Append("Z ");
					break;
			}
		}
		// Every segment ends with a separator; drop the last one rather than copying the string to trim it.
		if (data.Length > 0 && data[^1] == ' ')
			data.Length--;
		return data.ToString();
	}

	static void Append(StringBuilder data, char command, PointF p1)
	{
		data.Append(command);
		AppendPoint(data, p1);
	}

	static void Append(StringBuilder data, char command, PointF p1, PointF p2)
	{
		data.Append(command);
		AppendPoint(data, p1);
		AppendPoint(data, p2);
	}

	static void Append(StringBuilder data, char command, PointF p1, PointF p2, PointF p3)
	{
		data.Append(command);
		AppendPoint(data, p1);
		AppendPoint(data, p2);
		AppendPoint(data, p3);
	}

	static void AppendPoint(StringBuilder data, PointF p)
	{
		AppendNum(data, p.X);
		data.Append(' ');
		AppendNum(data, p.Y);
		data.Append(' ');
	}

	// Follows the Skia backend: angles are degrees, counterclockwise from 3 o'clock in a y-up sense,
	// on the ellipse inscribed in the two corner points; the arc is joined to the current point by a line.
	static void AppendArc(StringBuilder data, PointF corner1, PointF corner2, float startAngle, float endAngle, bool clockwise, bool hasCurrentPoint)
	{
		var cx = (corner1.X + corner2.X) / 2d;
		var cy = (corner1.Y + corner2.Y) / 2d;
		var rx = Math.Abs(corner2.X - corner1.X) / 2d;
		var ry = Math.Abs(corner2.Y - corner1.Y) / 2d;
		var full = Math.Abs(endAngle - startAngle) >= 360;

		while (startAngle < 0) startAngle += 360;
		while (endAngle < 0) endAngle += 360;
		double sweep = full ? 360 : clockwise
			? (endAngle > startAngle ? 360 - (endAngle - startAngle) : startAngle - endAngle)
			: (startAngle > endAngle ? 360 - (startAngle - endAngle) : endAngle - startAngle);

		PointF At(double degrees)
		{
			var radians = degrees * Math.PI / 180;
			return new((float)(cx + rx * Math.Cos(radians)), (float)(cy - ry * Math.Sin(radians)));
		}

		Append(data, hasCurrentPoint ? 'L' : 'M', At(startAngle));
		if (sweep <= 0 || rx <= 0 || ry <= 0)
			return;

		// Clockwise on screen is decreasing angle here, and SVG's sweep flag 1.
		var direction = clockwise ? -1 : 1;
		var sweepFlag = clockwise ? '1' : '0';
		void Arc(PointF to, bool large)
			=> data.Append('A').Append(Num(rx)).Append(' ').Append(Num(ry)).Append(" 0 ").Append(large ? '1' : '0').Append(' ').Append(sweepFlag).Append(' ')
				.Append(Num(to.X)).Append(' ').Append(Num(to.Y)).Append(' ');

		if (sweep >= 360)
		{
			// One SVG arc can't start and end at the same point: draw two halves.
			Arc(At(startAngle + direction * 180), false);
			Arc(At(startAngle), false);
		}
		else
		{
			Arc(At(startAngle + direction * sweep), sweep > 180);
		}
	}

	// Commands are written straight into the JSON buffer. Drawables issue thousands a frame, and on the
	// WebAssembly interpreter boxing each argument into a params array and formatting each number through
	// double.ToString was most of the cost of a frame; the typed overloads below avoid both.

	void Begin(string name)
	{
		if (!_first)
			_commands.Append(',');
		_first = false;
		_commands.Append("[\"").Append(name).Append('"');
	}

	void Arg(double value)
	{
		_commands.Append(',');
		AppendNum(_commands, value);
	}

	void Arg(string? value)
	{
		_commands.Append(',');
		if (value is null)
			_commands.Append("null");
		else
			AppendString(_commands, value);
	}

	void End() => _commands.Append(']');

	void Command(string name)
	{
		Begin(name);
		End();
	}

	void Command(string name, double a)
	{
		Begin(name);
		Arg(a);
		End();
	}

	void Command(string name, double a, double b)
	{
		Begin(name);
		Arg(a);
		Arg(b);
		End();
	}

	void Command(string name, double a, double b, double c, double d)
	{
		Begin(name);
		Arg(a);
		Arg(b);
		Arg(c);
		Arg(d);
		End();
	}

	void Command(string name, string? value)
	{
		Begin(name);
		Arg(value);
		End();
	}

	void Command(string name, params object?[] args)
	{
		Begin(name);
		foreach (var arg in args)
		{
			switch (arg)
			{
				case null:
					Arg((string?)null);
					break;
				case string s:
					Arg(s);
					break;
				case float f:
					Arg(f);
					break;
				case double d:
					Arg(d);
					break;
				case int i:
					Arg(i);
					break;
				default:
					_commands.Append(',').Append(Convert.ToString(arg, CultureInfo.InvariantCulture));
					break;
			}
		}
		End();
	}

	/// <summary>A CSS colour, written as WebCss.Color would format it, without the intermediate strings.</summary>
	void ColorCommand(string name, Color? color, string fallback)
	{
		Begin(name);
		_commands.Append(',');
		if (color is null)
		{
			AppendString(_commands, fallback);
		}
		else
		{
			_commands.Append("\"rgba(")
				.Append((int)Math.Round(color.Red * 255)).Append(',')
				.Append((int)Math.Round(color.Green * 255)).Append(',')
				.Append((int)Math.Round(color.Blue * 255)).Append(',');
			AppendNum(_commands, color.Alpha);
			_commands.Append(")\"");
		}
		End();
	}

	static string Num(double value)
	{
		var sb = new StringBuilder(12);
		AppendNum(sb, value);
		return sb.ToString();
	}

	/// <summary>
	/// Appends a number rounded to 3 decimals with no exponent and no trailing zeros ("12", "-0.5",
	/// "3.142"), as Math.Round(value, 3).ToString(InvariantCulture) would, using integer arithmetic.
	/// </summary>
	internal static void AppendNum(StringBuilder sb, double value)
	{
		if (!double.IsFinite(value))
		{
			sb.Append('0');
			return;
		}

		var scaled = Math.Round(value * 1000, MidpointRounding.AwayFromZero);
		if (Math.Abs(scaled) >= 1e15)
		{
			// Too large for the integer path (and far outside any canvas); let the runtime format it.
			sb.Append(Math.Round(value, 3).ToString(CultureInfo.InvariantCulture));
			return;
		}

		var units = (long)scaled;
		if (units < 0)
		{
			sb.Append('-');
			units = -units;
		}
		sb.Append(units / 1000);

		var fraction = (int)(units % 1000);
		if (fraction == 0)
			return;

		sb.Append('.');
		if (fraction < 100)
			sb.Append('0');
		if (fraction < 10)
			sb.Append('0');
		while (fraction % 10 == 0)
			fraction /= 10;
		sb.Append(fraction);
	}

	static void AppendString(StringBuilder sb, string value)
	{
		sb.Append('"');
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
		sb.Append('"');
	}
}
