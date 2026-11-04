using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.Media;
using Shiny.Maui.Controls.Media.Web;
using Shiny.Maui.Controls.Media.Web.Interop;

namespace Shiny;

public static class WebMediaMauiAppBuilderExtensions
{
	/// <summary>
	/// Registers the Shiny <see cref="MediaElement"/> and <see cref="VideoText"/> for Platform.Maui.Web. Call this <b>instead of</b>
	/// <c>UseShinyMediaElement()</c>, which does nothing on the plain <c>net10.0</c> target a web app builds against.
	/// </summary>
	/// <remarks>
	/// Playback uses the browser's <c>&lt;video&gt;</c> element, so what plays depends on the browser's
	/// codecs (MP4/H.264, WebM and the common audio formats work everywhere). A video element
	/// needs no CORS headers to play a remote URL.
	/// </remarks>
	public static MauiAppBuilder UseShinyMediaElementWeb(this MauiAppBuilder builder)
	{
		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler<MediaSurface, WebMediaSurfaceHandler>();
			handlers.AddHandler<VideoText, WebVideoTextHandler>();
		});
		MediaPlayerBackends.Factory = () => new WebMediaPlayerBackend();

		// Start fetching the script now (the base URI is known before the app is built), so it is
		// usually ready by the time the first player is created.
		_ = MediaJs.EnsureLoadedAsync();
		return builder;
	}
}
