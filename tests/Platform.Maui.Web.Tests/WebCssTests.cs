using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Tests;

public class WebCssTests
{
	[Fact]
	public void ColorUsesRgbaWithInvariantAlpha()
	{
		var culture = Thread.CurrentThread.CurrentCulture;
		try
		{
			// A comma decimal separator must not leak into CSS.
			Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
			Assert.Equal("rgba(255,0,0,0.5)", WebCss.Color(Colors.Red.WithAlpha(0.5f)));
		}
		finally
		{
			Thread.CurrentThread.CurrentCulture = culture;
		}
	}

	[Fact]
	public void NullColorIsUnset() => Assert.Null(WebCss.Color(null));

	[Theory]
	[InlineData(0, 0, 1, 0, "90deg")]
	[InlineData(0, 0, 0, 1, "180deg")]
	[InlineData(0, 1, 0, 0, "0deg")]
	[InlineData(0, 0, 1, 1, "135deg")]
	public void LinearGradientAngleMatchesCssConvention(double x1, double y1, double x2, double y2, string angle)
	{
		var paint = new LinearGradientPaint([new PaintGradientStop(0, Colors.Black), new PaintGradientStop(1, Colors.White)],
			new Point(x1, y1), new Point(x2, y2));
		Assert.StartsWith($"linear-gradient({angle}, ", WebCss.Paint(paint));
	}

	[Fact]
	public void GradientStopsAreOrderedByOffset()
	{
		var paint = new LinearGradientPaint([new PaintGradientStop(1, Colors.White), new PaintGradientStop(0, Colors.Black)],
			new Point(0, 0), new Point(1, 0));
		Assert.Equal("linear-gradient(90deg, rgba(0,0,0,1) 0%, rgba(255,255,255,1) 100%)", WebCss.Paint(paint));
	}

	[Fact]
	public void SolidPaintIsAColor() => Assert.Equal("rgba(0,0,255,1)", WebCss.Paint(new SolidPaint(Colors.Blue)));

	[Fact]
	public void DashLengthsScaleWithStrokeThickness()
		=> Assert.Equal("8 4", WebCss.DashArray([2, 1], 4));

	[Fact]
	public void ThinStrokesDoNotShrinkDashes()
		=> Assert.Equal("2 1", WebCss.DashArray([2, 1], 0.5));

	[Fact]
	public void ThicknessIsTopRightBottomLeft()
		=> Assert.Equal("1px 2px 3px 4px", WebCss.Thickness(new Thickness(4, 1, 2, 3)));

	[Fact]
	public void CornerRadiusIsClockwiseFromTopLeft()
		=> Assert.Equal("1px 2px 4px 3px", WebCss.CornerRadius(new CornerRadius(1, 2, 3, 4)));

	[Fact]
	public void TextDecorationsCombine()
		=> Assert.Equal("underline line-through", WebCss.TextDecoration(TextDecorations.Underline | TextDecorations.Strikethrough));

	[Fact]
	public void RectangleGeometryBecomesSvgPath()
	{
		var path = WebCss.GeometryPath(new Rectangle(), new Rect(0, 0, 10, 20));
		Assert.NotNull(path);
		Assert.StartsWith("M", path);
		Assert.EndsWith("Z", path);
	}

	[Fact]
	public void EmptyBoundsHaveNoPath() => Assert.Null(WebCss.GeometryPath(new Ellipse(), new Rect(0, 0, 0, 10)));

	[Fact]
	public void ClipUsesCssPathFunction()
		=> Assert.StartsWith("path('M", WebCss.Clip(new EllipseGeometry(new Point(5, 5), 5, 5), new Size(10, 10)));

	[Fact]
	public void IdentityTransformIsEmpty() => Assert.Equal(string.Empty, WebCss.Transform(new Microsoft.Maui.Controls.BoxView()));

	[Fact]
	public void TransformOrdersTranslateRotateScale()
	{
		var view = new Microsoft.Maui.Controls.BoxView { TranslationX = 5, Rotation = 45, Scale = 2, ScaleY = 0.5 };
		Assert.Equal("translate(5px, 0px) rotate(45deg) scale(2, 1)", WebCss.Transform(view));
	}

	[Fact]
	public void ShadowAppliesOpacityToColor()
	{
		var shadow = new Microsoft.Maui.Controls.Shadow { Brush = Colors.Black, Opacity = 0.5f, Radius = 8, Offset = new Point(0, 2) };
		Assert.Equal("0px 2px 8px rgba(0,0,0,0.5)", WebCss.Shadow(shadow));
	}
}
