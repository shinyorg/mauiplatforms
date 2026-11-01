using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// Border. Rectangles, rounded rectangles and ellipses use CSS radii; any other stroke shape
/// clips with clip-path and strokes with an SVG overlay. The stroke is drawn by an overlay so it
/// never shifts content: MAUI already insets content by StrokeThickness during layout.
/// </summary>
public class BorderHandler : ContentViewHandler
{
	public static new IPropertyMapper<IBorderView, BorderHandler> Mapper =
		new PropertyMapper<IBorderView, BorderHandler>(ContentViewHandler.Mapper)
		{
			[nameof(IBorderStroke.Shape)] = MapStroke,
			["StrokeShape"] = MapStroke,
			[nameof(IStroke.Stroke)] = MapStroke,
			[nameof(IStroke.StrokeThickness)] = MapStroke,
			[nameof(IStroke.StrokeDashPattern)] = MapStroke,
			["StrokeDashArray"] = MapStroke,
			[nameof(IStroke.StrokeLineCap)] = MapStroke,
			[nameof(IStroke.StrokeLineJoin)] = MapStroke,
		};

	public BorderHandler() : base(Mapper)
	{
	}

	WebElement _stroke = null!;
	WebElement? _svg;

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-border maui-clip");
		_stroke = new WebElement("div", "maui-border-stroke");
		root.Append(_stroke);
		return root;
	}

	public override void PlatformArrange(Rect frame)
	{
		var resized = frame.Size != ArrangedFrame.Size;
		base.PlatformArrange(frame);
		if (resized && VirtualView is IBorderView border)
			MapStroke(this, border);
	}

	public static void MapStroke(BorderHandler handler, IBorderView border)
	{
		var size = handler.ArrangedFrame.Size;
		var thickness = border.Stroke is null ? 0 : Math.Max(0, border.StrokeThickness);
		var color = WebCss.PaintColor(border.Stroke);
		var radius = border.Shape switch
		{
			RoundRectangle rr => WebCss.CornerRadius(rr.CornerRadius),
			Rectangle => "0",
			Ellipse => "50%",
			null => "0",
			_ => null,
		};

		var root = handler.PlatformView;
		var stroke = handler._stroke;
		if (radius is not null)
		{
			handler._svg?.Detach();
			handler._svg = null;
			root.SetStyle("clip-path", border.Clip is null ? null : WebCss.Clip(border.Clip, size));
			root.SetStyle("border-radius", radius == "0" ? null : radius);
			stroke.SetStyle("border-radius", radius == "0" ? null : radius);
			stroke.SetStyle("border-width", thickness > 0 ? WebCss.Px(thickness) : null);
			stroke.SetStyle("border-color", color);
			stroke.SetStyle("border-style", border.StrokeDashPattern is { Length: > 0 } ? "dashed" : null);
			if (WebCss.IsGradient(border.Stroke))
			{
				stroke.SetStyle("border-image", $"{WebCss.Paint(border.Stroke)} 1");
				stroke.SetStyle("border-color", null);
			}
			else
			{
				stroke.SetStyle("border-image", null);
			}
			return;
		}

		// Arbitrary geometry: clip the content and draw the stroke as SVG.
		stroke.SetStyle("border-width", null);
		root.SetStyle("border-radius", null);
		var inset = thickness / 2;
		var bounds = new Rect(inset, inset, Math.Max(0, size.Width - thickness), Math.Max(0, size.Height - thickness));
		root.SetStyle("clip-path", WebCss.Clip(border.Shape, size));
		if (handler._svg is null)
		{
			handler._svg = WebElement.CreateSvg("svg");
			handler._svg.SetAttribute("class", "maui-border-stroke-svg");
			handler._svg.Append(WebElement.CreateSvg("path"));
			root.Append(handler._svg);
		}

		handler._svg.SetAttribute("width", WebCss.Num(size.Width));
		handler._svg.SetAttribute("height", WebCss.Num(size.Height));
		handler._svg.SetHtml(null);
		var path = WebElement.CreateSvg("path");
		path.SetAttribute("d", WebCss.GeometryPath(border.Shape, bounds) ?? "");
		path.SetAttribute("fill", "none");
		path.SetAttribute("stroke", thickness > 0 ? color : "none");
		// Inside a clip only half the stroke is visible, so draw it twice as wide.
		path.SetAttribute("stroke-width", WebCss.Num(thickness * 2));
		path.SetAttribute("stroke-dasharray", WebCss.DashArray(border.StrokeDashPattern, thickness));
		path.SetAttribute("stroke-linejoin", WebCss.LineJoin(border.StrokeLineJoin));
		handler._svg.Append(path);
	}
}

/// <summary>The legacy Frame: a bordered, optionally shadowed, rounded ContentView.</summary>
#pragma warning disable CS0618 // Frame is obsolete but still shipped
public class FrameHandler : ContentViewHandler
{
	public static new IPropertyMapper<IContentView, FrameHandler> Mapper =
		new PropertyMapper<IContentView, FrameHandler>(ContentViewHandler.Mapper)
		{
			[nameof(Microsoft.Maui.Controls.Frame.BorderColor)] = MapFrame,
			[nameof(Microsoft.Maui.Controls.Frame.CornerRadius)] = MapFrame,
			[nameof(Microsoft.Maui.Controls.Frame.HasShadow)] = MapFrame,
		};

	public FrameHandler() : base(Mapper)
	{
	}

	protected override WebElement CreatePlatformView() => new("div", "maui-frame maui-clip");

	public static void MapFrame(FrameHandler handler, IContentView view)
	{
		if (view is not Microsoft.Maui.Controls.Frame frame)
			return;
		var el = handler.PlatformView;
		el.SetStyle("border-radius", WebCss.Px(frame.CornerRadius < 0 ? 5 : frame.CornerRadius));
		el.SetStyle("outline", frame.BorderColor is null ? null : $"1px solid {WebCss.Color(frame.BorderColor)}");
		el.SetStyle("outline-offset", frame.BorderColor is null ? null : "-1px");
		el.SetStyle("box-shadow", frame.HasShadow ? "0 2px 6px rgb(0 0 0 / 0.25)" : null);
	}
}
#pragma warning restore CS0618
