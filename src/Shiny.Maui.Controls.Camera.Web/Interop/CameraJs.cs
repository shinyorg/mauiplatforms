using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Platform.Web;

namespace Shiny.Maui.Controls.Camera.Web.Interop;

/// <summary>[JSImport] bindings for wwwroot/shiny-camera-web.js.</summary>
internal static partial class CameraJs
{
	public const string Module = "shiny-camera-web";
	public const string ContentPath = "_content/Shiny.Maui.Controls.Camera.Web/";

	static Task? _load;

	/// <summary>Imports the module once. Static web assets serve it; nothing goes in index.html.</summary>
	public static Task EnsureLoadedAsync()
		=> _load ??= JSHost.ImportAsync(Module, new Uri(new Uri(WebMauiApplication.BaseUri), ContentPath + "shiny-camera-web.js").ToString());

	/// <summary>Copies a buffer parked by the module into managed memory.</summary>
	public static byte[] TakeBuffer(int id)
	{
		var bytes = new byte[BufferLength(id)];
		CopyBuffer(id, new ArraySegment<byte>(bytes));
		return bytes;
	}

	[JSImport("bufferLength", Module)]
	public static partial int BufferLength(int id);

	[JSImport("copyBuffer", Module)]
	public static partial void CopyBuffer(int id, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> view);

	[JSImport("requestPermission", Module)]
	public static partial Task<bool> RequestPermission(bool includeMicrophone);

	[JSImport("listCameras", Module)]
	public static partial Task<string> ListCameras();

	[JSImport("start", Module)]
	public static partial Task<string?> Start(JSObject video, string? deviceId, string? facingMode);

	[JSImport("stop", Module)]
	public static partial void Stop(JSObject video);

	[JSImport("setZoom", Module)]
	public static partial Task<bool> SetZoom(JSObject video, double zoom);

	[JSImport("setTorch", Module)]
	public static partial Task<bool> SetTorch(JSObject video, bool on);

	[JSImport("capture", Module)]
	public static partial Task<string> Capture(JSObject video, string? filterCss, double quality);

	[JSImport("startRecording", Module)]
	public static partial Task StartRecording(JSObject video, bool includeAudio);

	[JSImport("stopRecording", Module)]
	public static partial Task<string> StopRecording(JSObject video);
}
