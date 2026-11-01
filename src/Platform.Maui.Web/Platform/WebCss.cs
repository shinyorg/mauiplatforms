using System.Globalization;
using System.Text;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform.Web;

/// <summary>Converts MAUI values to CSS.</summary>
public static class WebCss
{
	static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

	public static string Px(double value) => Num(value) + "px";

	public static string Num(double value) => Math.Round(value, 3).ToString(Invariant);

	public static string? Color(Color? color)
	{
		if (color is null)
			return null;
		return string.Create(Invariant, $"rgba({(int)Math.Round(color.Red * 255)},{(int)Math.Round(color.Green * 255)},{(int)Math.Round(color.Blue * 255)},{Num(color.Alpha)})");
	}

	/// <summary>A CSS image or color value for a MAUI paint, or null when unset.</summary>
	public static string? Paint(Paint? paint) => paint switch
	{
		SolidPaint { Color: { } c } => Color(c),
		LinearGradientPaint lg => LinearGradient(lg),
		RadialGradientPaint rg => RadialGradient(rg),
		_ => null,
	};

	public static bool IsGradient(Paint? paint) => paint is GradientPaint;

	/// <summary>The paint's representative color, for places CSS only accepts colors.</summary>
	public static string? PaintColor(Paint? paint) => paint switch
	{
		SolidPaint { Color: { } c } => Color(c),
		GradientPaint { GradientStops.Length: > 0 } gp => Color(gp.GradientStops[0].Color),
		_ => null,
	};

	static string LinearGradient(LinearGradientPaint paint)
	{
		// CSS: 0deg points up, 90deg points right. MAUI points are relative (0..1).
		var dx = paint.EndPoint.X - paint.StartPoint.X;
		var dy = paint.EndPoint.Y - paint.StartPoint.Y;
		var degrees = (Math.Atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
		return $"linear-gradient({Num(degrees)}deg, {Stops(paint)})";
	}

	static string RadialGradient(RadialGradientPaint paint)
		=> $"radial-gradient(circle {Num(paint.Radius * 100)}% at {Num(paint.Center.X * 100)}% {Num(paint.Center.Y * 100)}%, {Stops(paint)})";

	static string Stops(GradientPaint paint)
	{
		var sb = new StringBuilder();
		foreach (var stop in paint.GradientStops.OrderBy(s => s.Offset))
		{
			if (sb.Length > 0)
				sb.Append(", ");
			sb.Append(Color(stop.Color)).Append(' ').Append(Num(stop.Offset * 100)).Append('%');
		}
		return sb.ToString();
	}

	public static string FontWeight(FontWeight weight) => ((int)weight).ToString(Invariant);

	public static string? TextAlign(TextAlignment alignment) => alignment switch
	{
		TextAlignment.Center => "center",
		TextAlignment.End => "end",
		TextAlignment.Justify => "justify",
		_ => null,
	};

	public static string FlexAlign(TextAlignment alignment) => alignment switch
	{
		TextAlignment.Center => "center",
		TextAlignment.End => "flex-end",
		_ => "flex-start",
	};

	public static string? TextDecoration(TextDecorations decorations)
	{
		var parts = new List<string>(2);
		if (decorations.HasFlag(TextDecorations.Underline))
			parts.Add("underline");
		if (decorations.HasFlag(TextDecorations.Strikethrough))
			parts.Add("line-through");
		return parts.Count == 0 ? null : string.Join(' ', parts);
	}

	public static string? TextTransform(TextTransform transform) => transform switch
	{
		Microsoft.Maui.TextTransform.Uppercase => "uppercase",
		Microsoft.Maui.TextTransform.Lowercase => "lowercase",
		_ => null,
	};

	public static string Thickness(Thickness t) => $"{Px(t.Top)} {Px(t.Right)} {Px(t.Bottom)} {Px(t.Left)}";

	public static string CornerRadius(CornerRadius r) => $"{Px(r.TopLeft)} {Px(r.TopRight)} {Px(r.BottomRight)} {Px(r.BottomLeft)}";

	/// <summary>The offset, blur and color of a shadow, valid for box-shadow, text-shadow and drop-shadow().</summary>
	public static string? Shadow(IShadow? shadow)
	{
		if (shadow?.Paint is null || PaintColor(shadow.Paint) is not { } color)
			return null;

		if (shadow.Paint is SolidPaint { Color: { } solid })
			color = Color(solid.WithAlpha(solid.Alpha * shadow.Opacity))!;
		return $"{Px(shadow.Offset.X)} {Px(shadow.Offset.Y)} {Px(shadow.Radius)} {color}";
	}

	public static string? LineCap(LineCap cap) => cap switch
	{
		Microsoft.Maui.Graphics.LineCap.Round => "round",
		Microsoft.Maui.Graphics.LineCap.Square => "square",
		_ => "butt",
	};

	public static string? LineJoin(LineJoin join) => join switch
	{
		Microsoft.Maui.Graphics.LineJoin.Round => "round",
		Microsoft.Maui.Graphics.LineJoin.Bevel => "bevel",
		_ => "miter",
	};

	public static string? DashArray(float[]? pattern, double thickness)
	{
		if (pattern is not { Length: > 0 })
			return null;
		// MAUI dash lengths are multiples of the stroke thickness.
		var scale = Math.Max(1, thickness);
		return string.Join(' ', pattern.Select(p => Num(p * scale)));
	}

	/// <summary>Builds an SVG/CSS path() string for a geometry within the given bounds.</summary>
	public static string? GeometryPath(IShape? shape, Rect bounds)
	{
		if (shape is null || bounds.Width <= 0 || bounds.Height <= 0)
			return null;
		var path = shape.PathForBounds(bounds);
		var d = path?.ToDefinitionString();
		return string.IsNullOrWhiteSpace(d) ? null : d.Trim();
	}

	public static string? Clip(IShape? clip, Size size)
		=> GeometryPath(clip, new Rect(0, 0, size.Width, size.Height)) is { } d ? $"path('{d}')" : null;

	public static string Transform(IView view)
	{
		var sb = new StringBuilder();
		if (view.TranslationX != 0 || view.TranslationY != 0)
			sb.Append($"translate({Px(view.TranslationX)}, {Px(view.TranslationY)}) ");
		if (view.RotationX != 0 || view.RotationY != 0)
			sb.Append("perspective(1000px) ");
		if (view.Rotation != 0)
			sb.Append($"rotate({Num(view.Rotation)}deg) ");
		if (view.RotationX != 0)
			sb.Append($"rotateX({Num(view.RotationX)}deg) ");
		if (view.RotationY != 0)
			sb.Append($"rotateY({Num(view.RotationY)}deg) ");
		var sx = view.Scale * view.ScaleX;
		var sy = view.Scale * view.ScaleY;
		if (sx != 1 || sy != 1)
			sb.Append($"scale({Num(sx)}, {Num(sy)}) ");
		return sb.ToString().TrimEnd();
	}

	public static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
