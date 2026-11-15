using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Maps;
using Microsoft.Maui.Platform.Web.Maps.Handlers;
using Microsoft.Maui.Platform.Web.Maps.Interop;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace Microsoft.Maui.Platform.Web.Hosting;

public static class MapsWebAppHostBuilderExtensions
{
	/// <summary>
	/// Registers the .NET MAUI <see cref="Map"/> for Platform.Maui.Web. Call this <b>instead of</b>
	/// <c>UseMauiMaps()</c>; it calls that too, and then replaces the map and pin handlers, which throw
	/// on the plain <c>net11.0</c> target, with the Leaflet-based web ones.
	/// </summary>
	/// <param name="builder">The app builder.</param>
	/// <param name="configure">Changes the tile sources; the defaults are OpenStreetMap and Esri imagery.</param>
	/// <remarks>
	/// Leaflet is bundled in the package and loaded when the first map is shown; tiles come from the
	/// providers in <see cref="WebMapOptions"/>, which need no API key.
	/// </remarks>
	public static MauiAppBuilder UseMauiMapsWeb(this MauiAppBuilder builder, Action<WebMapOptions>? configure = null)
	{
		var options = new WebMapOptions();
		configure?.Invoke(options);
		builder.Services.AddSingleton(options);

		builder.UseMauiMaps();
		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler<Map, WebMapHandler>();
			handlers.AddHandler<Pin, WebMapPinHandler>();
		});

		// Start fetching the script now (the base URI is known before the app is built).
		if (OperatingSystem.IsBrowser())
			_ = MapsJs.EnsureLoadedAsync();
		return builder;
	}
}
