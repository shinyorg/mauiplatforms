using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Skia.Interop;

/// <summary>[JSImport] bindings for wwwroot/maui-web-skiasharp.js.</summary>
internal static partial class SkiaJs
{
	public const string Module = "maui-web-skiasharp";
	public const string ContentPath = "_content/Platform.Maui.Web.SkiaSharp/";

	static Task? _load;

	public static bool IsLoaded { get; private set; }

	/// <summary>Imports the module once. Static web assets serve it; nothing goes in index.html.</summary>
	public static Task EnsureLoadedAsync()
		=> _load ??= LoadAsync();

	static async Task LoadAsync()
	{
		await JSHost.ImportAsync(Module, new Uri(new Uri(WebMauiApplication.BaseUri), ContentPath + "maui-web-skiasharp.js").ToString());
		IsLoaded = true;
	}

	[JSImport("requestFrame", Module)]
	public static partial void RequestFrame([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("devicePixelRatio", Module)]
	public static partial double DevicePixelRatio();

	[JSImport("present", Module)]
	public static partial void Present(JSObject canvas, [JSMarshalAs<JSType.MemoryView>] Span<byte> pixels, int width, int height);

	[JSImport("watchPixelRatio", Module)]
	public static partial void WatchPixelRatio(JSObject canvas, [JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("unwatchPixelRatio", Module)]
	public static partial void UnwatchPixelRatio(JSObject canvas);

	[JSImport("attachTouch", Module)]
	public static partial void AttachTouch(JSObject canvas, [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>] Func<JSObject, bool> callback);

	[JSImport("detachTouch", Module)]
	public static partial void DetachTouch(JSObject canvas);
}
