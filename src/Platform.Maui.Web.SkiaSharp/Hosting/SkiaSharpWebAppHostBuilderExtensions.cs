using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Skia;
using Microsoft.Maui.Platform.Web.Skia.Handlers;
using Microsoft.Maui.Platform.Web.Skia.Interop;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Microsoft.Maui.Platform.Web.Hosting;

public static class SkiaSharpWebAppHostBuilderExtensions
{
	/// <summary>
	/// Registers SkiaSharp's <see cref="SKCanvasView"/> for Platform.Maui.Web. Call this <b>instead of</b>
	/// <c>UseSkiaSharp()</c>; it calls that too, for the shared services, and then adds the web handler
	/// that <c>UseSkiaSharp()</c> can't register on the plain <c>net10.0</c> target.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Skia itself is linked into the app's <c>dotnet.native.wasm</c>, so the app needs the
	/// <c>wasm-tools</c> workload. <c>SKGLView</c> is not supported yet.
	/// </para>
	/// <para>
	/// Also registers Skia as MAUI's <see cref="IImageLoadingService"/> and <see cref="IBitmapExportService"/>
	/// (unless the app registered its own). Plain <c>net10.0</c> has neither: its <c>PlatformImage</c>
	/// misreads image sizes and there is no bitmap export, so controls that load or produce bitmaps
	/// resolve these instead.
	/// </para>
	/// </remarks>
	public static MauiAppBuilder UseSkiaSharpWeb(this MauiAppBuilder builder)
	{
		builder.UseSkiaSharp();
		builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<SKCanvasView, WebSKCanvasViewHandler>());
		builder.Services.TryAddSingleton<IImageLoadingService, SkiaImageLoadingService>();
		builder.Services.TryAddSingleton<IBitmapExportService, SkiaBitmapExportService>();

		// Start fetching the script now (the base URI is known before the app is built).
		if (OperatingSystem.IsBrowser())
			_ = SkiaJs.EnsureLoadedAsync();
		return builder;
	}
}
