using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.Camera;
using Shiny.Maui.Controls.Camera.Media;
using Shiny.Maui.Controls.Camera.Web;
using Shiny.Maui.Controls.Camera.Web.Interop;

namespace Shiny;

public static class WebCameraMauiAppBuilderExtensions
{
	/// <summary>
	/// Registers the Shiny <see cref="CameraView"/> for Platform.Maui.Web. Call this <b>instead of</b>
	/// <c>UseShinyCamera()</c>; it calls that too, for the shared services, and then adds the web handler
	/// that <c>UseShinyCamera()</c> can't register on the plain <c>net10.0</c> target.
	/// </summary>
	/// <remarks>
	/// Browsers only give camera access to secure contexts (HTTPS or localhost) and ask the user first.
	/// The modal <see cref="IMediaService"/> capture flow is not available on the web yet.
	/// </remarks>
	public static MauiAppBuilder UseShinyCameraWeb(this MauiAppBuilder builder, Action<MediaServiceOptions>? configureMedia = null)
	{
		builder.UseShinyCamera(configureMedia);
		builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<CameraView, WebCameraViewHandler>());

		// Start fetching the script now (the base URI is known before the app is built).
		_ = CameraJs.EnsureLoadedAsync();
		return builder;
	}
}
