using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Maps.Interop;

/// <summary>[JSImport] bindings for wwwroot/maui-web-maps.js.</summary>
internal static partial class MapsJs
{
	public const string Module = "maui-web-maps";
	public const string ContentPath = "_content/Platform.Maui.Web.Maps/";

	static Task? _load;

	public static bool IsLoaded { get; private set; }

	/// <summary>
	/// Imports the module once. Static web assets serve it; nothing goes in index.html. Leaflet itself
	/// is loaded by the module when the first map is created.
	/// </summary>
	public static Task EnsureLoadedAsync()
		=> _load ??= LoadAsync();

	static async Task LoadAsync()
	{
		await JSHost.ImportAsync(Module, new Uri(new Uri(WebMauiApplication.BaseUri), ContentPath + "maui-web-maps.js").ToString());
		IsLoaded = true;
	}

	/// <summary>
	/// Creates a Leaflet map inside <paramref name="host"/>. <paramref name="onEvent"/> receives
	/// (event, JSON payload) and returns true to suppress the event's default action;
	/// <paramref name="clusterIcon"/> returns an image URL for a cluster, or null for the default bubble.
	/// </summary>
	[JSImport("create", Module)]
	public static partial Task Create(JSObject host, string options,
		[JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.Boolean>>] Func<string, string, bool> onEvent,
		[JSMarshalAs<JSType.Function<JSType.String, JSType.String>>] Func<string, string?> clusterIcon);

	[JSImport("destroy", Module)]
	public static partial void Destroy(JSObject host);

	[JSImport("setMapType", Module)]
	public static partial void SetMapType(JSObject host, int mapType);

	[JSImport("setInteraction", Module)]
	public static partial void SetInteraction(JSObject host, bool scroll, bool zoom);

	[JSImport("setShowUser", Module)]
	public static partial void SetShowUser(JSObject host, bool show);

	[JSImport("setClustering", Module)]
	public static partial Task SetClustering(JSObject host, bool enabled, bool customIcons);

	[JSImport("refreshClusters", Module)]
	public static partial void RefreshClusters(JSObject host, bool customIcons);

	[JSImport("setPins", Module)]
	public static partial void SetPins(JSObject host, string pins);

	[JSImport("updatePin", Module)]
	public static partial void UpdatePin(JSObject host, string pin);

	[JSImport("setElements", Module)]
	public static partial void SetElements(JSObject host, string elements);

	[JSImport("updateElement", Module)]
	public static partial void UpdateElement(JSObject host, string element);

	[JSImport("moveTo", Module)]
	public static partial void MoveTo(JSObject host, double latitude, double longitude, double latitudeDegrees, double longitudeDegrees, bool animated);

	[JSImport("showInfoWindow", Module)]
	public static partial void ShowInfoWindow(JSObject host, int pinId);

	[JSImport("hideInfoWindow", Module)]
	public static partial void HideInfoWindow(JSObject host, int pinId);
}
