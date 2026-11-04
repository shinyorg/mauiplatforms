using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Platform.Web;

namespace Shiny.Maui.Controls.Media.Web.Interop;

/// <summary>[JSImport] bindings for wwwroot/shiny-mediaelement-web.js.</summary>
internal static partial class MediaJs
{
	public const string Module = "shiny-mediaelement-web";
	public const string ContentPath = "_content/Shiny.Maui.Controls.MediaElement.Web/";

	static Task? _load;

	public static bool IsLoaded => _load?.IsCompletedSuccessfully == true;

	/// <summary>Imports the module once. Static web assets serve it; nothing goes in index.html.</summary>
	public static Task EnsureLoadedAsync()
		=> _load ??= JSHost.ImportAsync(Module, new Uri(new Uri(WebMauiApplication.BaseUri), ContentPath + "shiny-mediaelement-web.js").ToString());

	[JSImport("create", Module)]
	public static partial JSObject Create([JSMarshalAs<JSType.Function<JSType.String, JSType.Number>>] Action<string, double> onEvent);

	[JSImport("attach", Module)]
	public static partial void Attach(JSObject video, JSObject? container);

	[JSImport("setSource", Module)]
	public static partial void SetSource(JSObject video, string? url);

	[JSImport("setBytesSource", Module)]
	public static partial void SetBytesSource(JSObject video, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> bytes, string? mime);

	[JSImport("errorMessage", Module)]
	public static partial string ErrorMessage(JSObject video);

	[JSImport("play", Module)]
	public static partial void Play(JSObject video);

	[JSImport("pause", Module)]
	public static partial void Pause(JSObject video);

	[JSImport("stop", Module)]
	public static partial void Stop(JSObject video);

	[JSImport("currentTime", Module)]
	public static partial double CurrentTime(JSObject video);

	[JSImport("videoWidth", Module)]
	public static partial double VideoWidth(JSObject video);

	[JSImport("videoHeight", Module)]
	public static partial double VideoHeight(JSObject video);

	[JSImport("seek", Module)]
	public static partial void Seek(JSObject video, double seconds);

	[JSImport("setVolume", Module)]
	public static partial void SetVolume(JSObject video, double volume);

	[JSImport("setMuted", Module)]
	public static partial void SetMuted(JSObject video, bool muted);

	[JSImport("setRate", Module)]
	public static partial void SetRate(JSObject video, double rate);

	[JSImport("setLoop", Module)]
	public static partial void SetLoop(JSObject video, bool loop);

	[JSImport("setObjectFit", Module)]
	public static partial void SetObjectFit(JSObject video, string fit);

	[JSImport("supportsPictureInPicture", Module)]
	public static partial bool SupportsPictureInPicture();

	[JSImport("enterPictureInPicture", Module)]
	public static partial Task<bool> EnterPictureInPicture(JSObject video);

	[JSImport("exitPictureInPicture", Module)]
	public static partial Task ExitPictureInPicture(JSObject video);

	[JSImport("setKeepScreenOn", Module)]
	public static partial void SetKeepScreenOn(JSObject video, bool on);

	[JSImport("supportsMediaSession", Module)]
	public static partial bool SupportsMediaSession();

	[JSImport("setMediaSession", Module)]
	public static partial void SetMediaSession(JSObject video, bool enabled, string? title, string? artist, string? album, string? artwork);

	[JSImport("videoTextSetMuted", Module)]
	public static partial void VideoTextSetMuted(JSObject video, bool muted);

	[JSImport("videoTextStop", Module)]
	public static partial void VideoTextStop(JSObject video);

	[JSImport("measureLines", Module)]
	public static partial Task<string> MeasureLines(string[] lines, string? family, string? weight, string? style);

	[JSImport("dispose", Module)]
	public static partial void Dispose(JSObject video);
}
