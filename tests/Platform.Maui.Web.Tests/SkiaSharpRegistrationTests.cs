using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Hosting;
using Microsoft.Maui.Platform.Web.Skia.Handlers;
using SkiaSharp.Views.Maui.Controls;

namespace Microsoft.Maui.Platform.Web.Tests;

[Collection("MAUI app")]
public class SkiaSharpRegistrationTests
{
	sealed class TestApp : Application;

	static IMauiHandlersFactory BuildFactory(out MauiApp app)
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiAppWeb<TestApp>().UseSkiaSharpWeb();
		app = builder.Build();
		return app.Services.GetRequiredService<IMauiHandlersFactory>();
	}

	/// <summary>
	/// UseSkiaSharp registers SkiaSharp's own handlers, which are NotImplementedException stubs on
	/// net10.0; UseSkiaSharpWeb must replace the SKCanvasView one.
	/// </summary>
	[Fact]
	public void SKCanvasViewResolvesToTheWebHandler()
	{
		var factory = BuildFactory(out var app);
		using (app)
			Assert.Equal(typeof(WebSKCanvasViewHandler), factory.GetHandlerType(typeof(SKCanvasView)));
	}

	/// <summary>Plain net10.0 has no working image loading or bitmap export; Skia provides both.</summary>
	[Fact]
	public void RegistersSkiaImageServices()
	{
		BuildFactory(out var app);
		using (app)
		{
			Assert.IsType<Microsoft.Maui.Graphics.Skia.SkiaImageLoadingService>(app.Services.GetRequiredService<Microsoft.Maui.Graphics.IImageLoadingService>());
			var export = app.Services.GetRequiredService<Microsoft.Maui.Graphics.IBitmapExportService>();
			using var context = export.CreateContext(4, 3);
			context.Canvas.FillColor = Microsoft.Maui.Graphics.Colors.Red;
			context.Canvas.FillRectangle(0, 0, 4, 3);
			using var png = new MemoryStream();
			context.WriteToStream(png);
			Assert.True(png.Length > 0);
		}
	}

	[Fact]
	public void SKCanvasViewHandlerConnectsToTheView()
	{
		var factory = BuildFactory(out var app);
		using (app)
		{
			var handler = factory.GetHandler(typeof(SKCanvasView));
			Assert.IsType<WebSKCanvasViewHandler>(handler);
			Assert.Contains(nameof(SkiaSharp.Views.Maui.ISKCanvasView.EnableTouchEvents), WebSKCanvasViewHandler.Mapper.GetKeys());
			Assert.Contains(nameof(SkiaSharp.Views.Maui.ISKCanvasView.IgnorePixelScaling), WebSKCanvasViewHandler.Mapper.GetKeys());
		}
	}
}
