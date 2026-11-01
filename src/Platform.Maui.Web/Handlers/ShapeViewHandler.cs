using System.Text;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// Shapes (Rectangle, Ellipse, Line, Path, Polygon, Polyline) and BoxView as inline SVG.
/// The path is regenerated from the shape's geometry whenever the frame or a shape property changes.
/// </summary>
public class ShapeViewHandler : WebElementHandler<IShapeView, WebElement>
{
	static int _nextGradientId;

	public static IPropertyMapper<IShapeView, ShapeViewHandler> Mapper =
		new PropertyMapper<IShapeView, ShapeViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IShapeView.Shape)] = MapShape,
			[nameof(IShapeView.Aspect)] = MapShape,
			[nameof(IShapeView.Fill)] = MapShape,
			[nameof(IStroke.Stroke)] = MapShape,
			[nameof(IStroke.StrokeThickness)] = MapShape,
			[nameof(IStroke.StrokeDashPattern)] = MapShape,
			[nameof(IStroke.StrokeDashOffset)] = MapShape,
			[nameof(IStroke.StrokeLineCap)] = MapShape,
			[nameof(IStroke.StrokeLineJoin)] = MapShape,
			[nameof(IStroke.StrokeMiterLimit)] = MapShape,
			// Shape subclasses raise their own property names; any of them can change the geometry.
			["X1"] = MapShape, ["Y1"] = MapShape, ["X2"] = MapShape, ["Y2"] = MapShape,
			["Points"] = MapShape, ["Data"] = MapShape, ["RadiusX"] = MapShape, ["RadiusY"] = MapShape,
			["CornerRadius"] = MapShape, ["Color"] = MapShape, ["StrokeDashArray"] = MapShape, ["FillRule"] = MapShape,
			// The fill is only BoxView.Color / Shape.Fill; a Background brush is the view's ordinary
			// background, painted behind the shape as on the native backends.
			[nameof(IView.Background)] = MapBackground,
		};

	public ShapeViewHandler() : base(Mapper)
	{
	}

	WebElement _svg = null!;
	WebElement _defs = null!;
	WebElement _path = null!;
	readonly string _gradientId = $"maui-shape-gradient-{++_nextGradientId}";

	protected override WebElement CreatePlatformView()
	{
		var root = new WebElement("div", "maui-shape");
		_svg = WebElement.CreateSvg("svg");
		_defs = WebElement.CreateSvg("defs");
		_path = WebElement.CreateSvg("path");
		_svg.Append(_defs);
		_svg.Append(_path);
		root.Append(_svg);
		return root;
	}

	// BoxView defaults to 40x40; other shapes size themselves in Shape.MeasureOverride.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> VirtualView is Microsoft.Maui.Controls.BoxView ? new Size(40, 40) : Size.Zero;

	public override void PlatformArrange(Rect frame)
	{
		var resized = frame.Size != ArrangedFrame.Size;
		base.PlatformArrange(frame);
		if (resized)
			Render();
	}

	public static void MapShape(ShapeViewHandler handler, IShapeView view) => handler.Render();

	public static void MapBackground(ShapeViewHandler handler, IShapeView view)
	{
		WebElementHandler.MapBackground(handler, view);
		handler.Render();
	}

	void Render()
	{
		var view = VirtualView;
		var size = ArrangedFrame.Size;
		if (view is null || size.Width <= 0 || size.Height <= 0)
			return;

		_svg.SetAttribute("width", WebCss.Num(size.Width));
		_svg.SetAttribute("height", WebCss.Num(size.Height));
		_svg.SetAttribute("viewBox", $"0 0 {WebCss.Num(size.Width)} {WebCss.Num(size.Height)}");

		var thickness = view.Stroke is null ? 0 : Math.Max(0, view.StrokeThickness);
		var inset = thickness / 2;
		var bounds = new Rect(inset, inset, Math.Max(0, size.Width - thickness), Math.Max(0, size.Height - thickness));
		_path.SetAttribute("d", WebCss.GeometryPath(view.Shape, bounds) ?? "");

		// A BoxView's background follows its rounded corners, so a gradient bar set through
		// Background (BoxView.Color can only be solid) keeps the shape a Color fill would have.
		PlatformView.SetStyle("border-radius", view is Microsoft.Maui.Controls.BoxView box && view.Background is not null
			? WebCss.CornerRadius(box.CornerRadius) : null);
		_path.SetAttribute("fill", PaintAttribute(view.Fill));
		_path.SetAttribute("fill-rule", view is Microsoft.Maui.Controls.Shapes.Path { Data: Microsoft.Maui.Controls.Shapes.PathGeometry { FillRule: Microsoft.Maui.Controls.Shapes.FillRule.Nonzero } } ? "nonzero" : "evenodd");
		_path.SetAttribute("stroke", thickness > 0 ? WebCss.PaintColor(view.Stroke) : "none");
		_path.SetAttribute("stroke-width", thickness > 0 ? WebCss.Num(thickness) : null);
		_path.SetAttribute("stroke-linecap", WebCss.LineCap(view.StrokeLineCap));
		_path.SetAttribute("stroke-linejoin", WebCss.LineJoin(view.StrokeLineJoin));
		_path.SetAttribute("stroke-miterlimit", WebCss.Num(view.StrokeMiterLimit));
		_path.SetAttribute("stroke-dasharray", WebCss.DashArray(view.StrokeDashPattern, thickness));
		_path.SetAttribute("stroke-dashoffset", view.StrokeDashOffset == 0 ? null : WebCss.Num(view.StrokeDashOffset * Math.Max(1, thickness)));
	}

	string PaintAttribute(Paint? paint)
	{
		_defs.Clear();
		if (paint is not GradientPaint gradient)
			return WebCss.Paint(paint) ?? "none";

		var element = gradient switch
		{
			LinearGradientPaint linear => Gradient("linearGradient",
				("x1", linear.StartPoint.X), ("y1", linear.StartPoint.Y), ("x2", linear.EndPoint.X), ("y2", linear.EndPoint.Y)),
			RadialGradientPaint radial => Gradient("radialGradient",
				("cx", radial.Center.X), ("cy", radial.Center.Y), ("r", radial.Radius)),
			_ => null,
		};
		if (element is null)
			return "none";

		foreach (var stop in gradient.GradientStops.OrderBy(s => s.Offset))
		{
			var stopElement = WebElement.CreateSvg("stop");
			stopElement.SetAttribute("offset", WebCss.Num(stop.Offset));
			stopElement.SetAttribute("stop-color", WebCss.Color(stop.Color));
			element.Append(stopElement);
		}
		_defs.Append(element);
		return $"url(#{_gradientId})";
	}

	WebElement Gradient(string tag, params (string Name, double Value)[] attributes)
	{
		var element = WebElement.CreateSvg(tag);
		element.SetAttribute("id", _gradientId);
		foreach (var (name, value) in attributes)
			element.SetAttribute(name, WebCss.Num(value));
		return element;
	}
}
