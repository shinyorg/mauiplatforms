namespace Microsoft.Maui.Platform.Web.Maps;

/// <summary>Tile sources for the web <c>Map</c>, one per <see cref="Microsoft.Maui.Maps.MapType"/>.</summary>
/// <remarks>
/// The defaults need no API key, but every tile provider has a usage policy. OpenStreetMap's public
/// tile servers are meant for light use (https://operations.osmfoundation.org/policies/tiles/); an app
/// with real traffic should point these at a commercial or self-hosted tile service.
/// </remarks>
public class WebMapOptions
{
	/// <summary>Tiles for <see cref="Microsoft.Maui.Maps.MapType.Street"/>. Defaults to OpenStreetMap.</summary>
	public WebMapTileLayer StreetTiles { get; set; } = WebMapTileLayer.OpenStreetMap;

	/// <summary>
	/// Imagery for <see cref="Microsoft.Maui.Maps.MapType.Satellite"/> and <see cref="Microsoft.Maui.Maps.MapType.Hybrid"/>.
	/// Defaults to Esri World Imagery. When null, those map types show <see cref="StreetTiles"/>.
	/// </summary>
	public WebMapTileLayer? SatelliteTiles { get; set; } = WebMapTileLayer.EsriWorldImagery;

	/// <summary>
	/// The transparent labels drawn over <see cref="SatelliteTiles"/> for <see cref="Microsoft.Maui.Maps.MapType.Hybrid"/>.
	/// Defaults to Esri's boundaries and places layer. When null, Hybrid looks like Satellite.
	/// </summary>
	public WebMapTileLayer? HybridLabelTiles { get; set; } = WebMapTileLayer.EsriLabels;
}

/// <summary>A raster tile source in Leaflet's URL template form, for example <c>https://{s}.example.com/{z}/{x}/{y}.png</c>.</summary>
/// <param name="UrlTemplate">The tile URL, with <c>{z}</c>, <c>{x}</c>, <c>{y}</c> and optionally <c>{s}</c> placeholders.</param>
/// <param name="Attribution">The credit shown in the map corner (HTML). Most providers require one.</param>
public sealed record WebMapTileLayer(string UrlTemplate, string Attribution)
{
	/// <summary>The deepest zoom level the source serves.</summary>
	public int MaxZoom { get; init; } = 19;

	/// <summary>The values substituted for <c>{s}</c>, one character each.</summary>
	public string Subdomains { get; init; } = "abc";

	public static WebMapTileLayer OpenStreetMap { get; } = new(
		"https://tile.openstreetmap.org/{z}/{x}/{y}.png",
		"&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors");

	public static WebMapTileLayer EsriWorldImagery { get; } = new(
		"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
		"Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community")
	{
		MaxZoom = 18,
	};

	public static WebMapTileLayer EsriLabels { get; } = new(
		"https://server.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}",
		"Labels &copy; Esri")
	{
		MaxZoom = 18,
	};
}
