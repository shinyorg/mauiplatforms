using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web.Maps.Interop;
using IMap = Microsoft.Maui.Maps.IMap;

namespace Microsoft.Maui.Platform.Web.Maps.Handlers;

/// <summary>
/// The .NET MAUI <c>Map</c> (<see cref="IMap"/>) on Platform.Maui.Web, drawn by Leaflet with the tile
/// sources in <see cref="WebMapOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// Supported: <see cref="IMap.MapType"/> (Street, Satellite and Hybrid), scroll and zoom toggles,
/// <see cref="IMap.MoveToRegion(MapSpan, bool)"/> and <see cref="IMap.VisibleRegion"/>, pins with labels,
/// addresses, custom images and info windows, polylines, polygons and circles (with click events),
/// map clicks and long presses (right click on desktop), the user's location through the browser's
/// geolocation, and pin clustering (Leaflet.markercluster) with custom cluster images.
/// </para>
/// <para>
/// Not supported: <see cref="IMap.IsTrafficEnabled"/> (there is no keyless traffic layer) and
/// <see cref="IMap.MapStyle"/> (a Google Maps style JSON). The browser asks before sharing the
/// user's location, and only in a secure context (HTTPS or localhost).
/// </para>
/// </remarks>
public class WebMapHandler : WebElementHandler<IMap, WebElement>, IMapHandler
{
	public static IPropertyMapper<IMap, WebMapHandler> Mapper =
		new PropertyMapper<IMap, WebMapHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IMap.MapType)] = MapMapType,
			[nameof(IMap.IsShowingUser)] = MapIsShowingUser,
			[nameof(IMap.IsScrollEnabled)] = MapInteraction,
			[nameof(IMap.IsZoomEnabled)] = MapInteraction,
			[nameof(IMap.IsClusteringEnabled)] = MapIsClusteringEnabled,
			[nameof(IMap.Pins)] = MapPins,
			[nameof(IMap.Elements)] = MapElements,
		};

	public static CommandMapper<IMap, WebMapHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(IMap.MoveToRegion)] = MapMoveToRegion,
		[nameof(IMap.ShowInfoWindow)] = MapShowInfoWindow,
		[nameof(IMap.HideInfoWindow)] = MapHideInfoWindow,
		[nameof(IMapHandler.UpdateMapElement)] = MapUpdateMapElement,
	};

	public WebMapHandler() : base(Mapper, CommandMapper)
	{
	}

	const string DefaultClusterGroup = "maui_default_cluster";

	readonly Dictionary<IMapPin, PinEntry> _pins = new(ReferenceEqualityComparer.Instance);
	readonly Dictionary<int, IMapPin> _pinsById = new();
	readonly Dictionary<IMapElement, int> _elements = new(ReferenceEqualityComparer.Instance);
	readonly Dictionary<int, IMapElement> _elementsById = new();
	int _nextId;
	bool _ready;
	int _clusterImageVersion = -1;
	MoveToRegionRequest? _pendingRegion;
	// Bumped on every connect and disconnect, so an attach that finishes after a disconnect is dropped.
	int _generation;

	IMap IMapHandler.VirtualView => VirtualView;

	object IMapHandler.PlatformView => PlatformView;

	protected override WebElement CreatePlatformView()
	{
		var host = new WebElement("div", "maui-map");
		host.SetStyle("overflow", "hidden");
		return host;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		_ = AttachAsync(platformView, ++_generation);
	}

	async Task AttachAsync(WebElement platformView, int generation)
	{
		try
		{
			await MapsJs.EnsureLoadedAsync();
			await MapsJs.Create(platformView.Js, OptionsJson(), OnMapEvent, GetClusterImageUrl);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] The map failed to load: {ex}");
			return;
		}

		if (generation != _generation || CurrentView is not { } map)
		{
			MapsJs.Destroy(platformView.Js);
			return;
		}

		// The mappers ran before the map existed; apply everything now.
		_ready = true;
		MapsJs.SetMapType(platformView.Js, (int)map.MapType);
		MapsJs.SetInteraction(platformView.Js, map.IsScrollEnabled, map.IsZoomEnabled);
		MapsJs.SetShowUser(platformView.Js, map.IsShowingUser);
		SyncPins();
		SyncElements();
		UpdateClustering();
		if (_pendingRegion is { Region: { } region } request)
			MapsJs.MoveTo(platformView.Js, region.Center.Latitude, region.Center.Longitude, region.LatitudeDegrees, region.LongitudeDegrees, request.Animated);
		_pendingRegion = null;
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		_generation++;
		if (_ready)
			MapsJs.Destroy(platformView.Js);
		_ready = false;
		foreach (var entry in _pins.Values)
			entry.ReleaseIcon();
		_pins.Clear();
		_pinsById.Clear();
		_elements.Clear();
		_elementsById.Clear();
		_clusterImageVersion = -1;
		base.DisconnectHandler(platformView);
	}

	// A map has no natural size; it fills what the layout offers.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 300, double.IsFinite(heightConstraint) ? heightConstraint : 300);

	// ---- mappers ------------------------------------------------------------

	public static void MapMapType(WebMapHandler handler, IMap map)
	{
		if (handler._ready)
			MapsJs.SetMapType(handler.PlatformView.Js, (int)map.MapType);
	}

	public static void MapInteraction(WebMapHandler handler, IMap map)
	{
		if (handler._ready)
			MapsJs.SetInteraction(handler.PlatformView.Js, map.IsScrollEnabled, map.IsZoomEnabled);
	}

	public static void MapIsShowingUser(WebMapHandler handler, IMap map)
	{
		if (handler._ready)
			MapsJs.SetShowUser(handler.PlatformView.Js, map.IsShowingUser);
	}

	public static void MapIsClusteringEnabled(WebMapHandler handler, IMap map) => handler.UpdateClustering();

	public static void MapPins(WebMapHandler handler, IMap map) => handler.SyncPins();

	public static void MapElements(WebMapHandler handler, IMap map) => handler.SyncElements();

	public static void MapMoveToRegion(WebMapHandler handler, IMap map, object? arg)
	{
		var request = arg switch
		{
			MoveToRegionRequest r => r,
			MapSpan span => new MoveToRegionRequest(span, true),
			_ => null,
		};
		if (request?.Region is not { } region)
			return;

		if (!handler._ready)
		{
			handler._pendingRegion = request;
			return;
		}
		MapsJs.MoveTo(handler.PlatformView.Js, region.Center.Latitude, region.Center.Longitude, region.LatitudeDegrees, region.LongitudeDegrees, request.Animated);
	}

	public static void MapShowInfoWindow(WebMapHandler handler, IMap map, object? arg)
	{
		if (handler._ready && arg is IMapPin pin && handler._pins.TryGetValue(pin, out var entry))
			MapsJs.ShowInfoWindow(handler.PlatformView.Js, entry.Id);
	}

	public static void MapHideInfoWindow(WebMapHandler handler, IMap map, object? arg)
	{
		if (handler._ready && arg is IMapPin pin && handler._pins.TryGetValue(pin, out var entry))
			MapsJs.HideInfoWindow(handler.PlatformView.Js, entry.Id);
	}

	public static void MapUpdateMapElement(WebMapHandler handler, IMap map, object? arg)
	{
		if (arg is MapElementHandlerUpdate update)
			handler.UpdateMapElement(update.MapElement);
	}

	// ---- pins ---------------------------------------------------------------

	void SyncPins()
	{
		if (!_ready || CurrentView is not { } map)
			return;

		var current = new HashSet<IMapPin>(map.Pins, ReferenceEqualityComparer.Instance);
		foreach (var removed in _pins.Keys.Where(p => !current.Contains(p)).ToList())
			ForgetPin(removed);

		foreach (var pin in map.Pins.Where(p => !_pins.ContainsKey(p)))
			TrackPin(pin);

		var replacedIcons = map.Pins.Select(p => LoadIcon(p, _pins[p])).OfType<string>().ToList();

		MapsJs.SetPins(PlatformView.Js, Json(w =>
		{
			w.WriteStartArray();
			foreach (var pin in map.Pins.Where(p => p.Location is not null))
				WritePin(w, pin, _pins[pin]);
			w.WriteEndArray();
		}));
		replacedIcons.ForEach(WebImageSource.Release);

		// A new cluster image (Map.ClusterImageSource or ClusterImageProvider) arrives as a Pins update.
		if (map is IMapClusterImageProvider provider && provider.ClusterImageVersion != _clusterImageVersion)
		{
			var first = _clusterImageVersion < 0;
			_clusterImageVersion = provider.ClusterImageVersion;
			if (!first && map.IsClusteringEnabled)
				MapsJs.RefreshClusters(PlatformView.Js, HasCustomClusterImages(map));
		}
	}

	void TrackPin(IMapPin pin)
	{
		// Pins only report their own property changes to their handler, which forwards them to UpdatePin.
		if (pin.Handler is null && MauiContext is { } context)
			pin.ToHandler(context);

		var entry = new PinEntry(++_nextId);
		pin.MarkerId = entry.Id;
		_pins[pin] = entry;
		_pinsById[entry.Id] = pin;
	}

	void ForgetPin(IMapPin pin)
	{
		var entry = _pins[pin];
		entry.ReleaseIcon();
		_pins.Remove(pin);
		_pinsById.Remove(entry.Id);
		if (Equals(pin.MarkerId, entry.Id))
			pin.MarkerId = null;
	}

	/// <summary>Redraws one pin after a change to its location, label, address, image or cluster group.</summary>
	public void UpdatePin(IMapPin pin)
	{
		if (!_ready || !_pins.TryGetValue(pin, out var entry))
			return;

		var replacedIcon = LoadIcon(pin, entry);
		SendPin(pin, entry);
		WebImageSource.Release(replacedIcon);
	}

	void SendPin(IMapPin pin, PinEntry entry)
	{
		if (pin.Location is null)
		{
			SyncPins();
			return;
		}
		MapsJs.UpdatePin(PlatformView.Js, Json(w => WritePin(w, pin, entry)));
	}

	/// <summary>
	/// Starts loading the pin's image if it changed. Returns the URL it replaces, which the marker still
	/// shows, for the caller to release once the pin is redrawn.
	/// </summary>
	string? LoadIcon(IMapPin pin, PinEntry entry)
	{
		if (ReferenceEquals(pin.ImageSource, entry.IconSource))
			return null;

		entry.IconSource = pin.ImageSource;
		entry.IconLoad?.Cancel();
		entry.IconLoad = null;
		var replaced = entry.IconUrl;
		entry.IconUrl = null;
		if (pin.ImageSource is { } source)
		{
			var cts = entry.IconLoad = new CancellationTokenSource();
			_ = LoadIconAsync(pin, entry, source, cts.Token);
		}
		return replaced;
	}

	async Task LoadIconAsync(IMapPin pin, PinEntry entry, IImageSource source, CancellationToken token)
	{
		try
		{
			var url = await WebImageSource.ResolveAsync(source, MauiContext?.Services.GetService<IFontManager>(), token);
			if (token.IsCancellationRequested || !_ready || !_pins.TryGetValue(pin, out var current) || current != entry)
			{
				WebImageSource.Release(url);
				return;
			}
			entry.IconUrl = url;
			SendPin(pin, entry);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] A map pin image failed to load: {ex.Message}");
		}
	}

	static void WritePin(Utf8JsonWriter w, IMapPin pin, PinEntry entry)
	{
		w.WriteStartObject();
		w.WriteNumber("id", entry.Id);
		w.WriteNumber("lat", pin.Location.Latitude);
		w.WriteNumber("lng", pin.Location.Longitude);
		w.WriteString("label", pin.Label);
		w.WriteString("address", pin.Address);
		w.WriteString("icon", entry.IconUrl);
		w.WriteString("group", string.IsNullOrEmpty(pin.ClusteringIdentifier) ? DefaultClusterGroup : pin.ClusteringIdentifier);
		w.WriteEndObject();
	}

	// ---- clustering ---------------------------------------------------------

	void UpdateClustering()
	{
		if (!_ready || CurrentView is not { } map)
			return;
		_ = SetClusteringAsync(map.IsClusteringEnabled, HasCustomClusterImages(map));
	}

	async Task SetClusteringAsync(bool enabled, bool customIcons)
	{
		try
		{
			await MapsJs.SetClustering(PlatformView.Js, enabled, customIcons);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] Map clustering failed to load: {ex.Message}");
		}
	}

	// Asking .NET for every cluster icon costs an interop call per cluster per zoom; only do it when the
	// map has a custom image to give.
	static bool HasCustomClusterImages(IMap map) => map switch
	{
		Controls.Maps.Map m => m.ClusterImageSource is not null || m.ClusterImageProvider is not null,
		IMapClusterImageProvider => true,
		_ => false,
	};

	string? GetClusterImageUrl(string json)
	{
		try
		{
			if (CurrentView is not IMapClusterImageProvider provider)
				return null;

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			var image = provider.GetClusterImage(PinsFor(root.GetProperty("ids")), root.GetProperty("count").GetInt32(), ReadLocation(root));

			// Cluster icons are drawn synchronously, so only sources with a URL up front can be used.
			return image switch
			{
				IUriImageSource { Uri: { } uri } => uri.ToString(),
				IFileImageSource file => WebImageSource.ResolveFile(file.File),
				_ => null,
			};
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] The map's cluster image failed: {ex}");
			return null;
		}
	}

	List<IMapPin> PinsFor(JsonElement ids)
		=> ids.EnumerateArray()
			.Select(id => _pinsById.GetValueOrDefault(id.GetInt32()))
			.OfType<IMapPin>()
			.ToList();

	// ---- map elements -------------------------------------------------------

	void SyncElements()
	{
		if (!_ready || CurrentView is not { } map)
			return;

		var current = new HashSet<IMapElement>(map.Elements, ReferenceEqualityComparer.Instance);
		foreach (var removed in _elements.Keys.Where(e => !current.Contains(e)).ToList())
		{
			_elementsById.Remove(_elements[removed]);
			_elements.Remove(removed);
			removed.MapElementId = null;
		}

		foreach (var element in map.Elements.Where(e => !_elements.ContainsKey(e)))
		{
			var id = ++_nextId;
			_elements[element] = id;
			_elementsById[id] = element;
			element.MapElementId = id;
		}

		MapsJs.SetElements(PlatformView.Js, Json(w =>
		{
			w.WriteStartArray();
			for (var i = 0; i < map.Elements.Count; i++)
				WriteElement(w, map.Elements[i], _elements[map.Elements[i]], i);
			w.WriteEndArray();
		}));
	}

	public void UpdateMapElement(IMapElement element)
	{
		if (!_ready || CurrentView is not { } map)
			return;

		if (!_elements.TryGetValue(element, out var id))
		{
			SyncElements();
			return;
		}
		MapsJs.UpdateElement(PlatformView.Js, Json(w => WriteElement(w, element, id, map.Elements.IndexOf(element))));
	}

	static void WriteElement(Utf8JsonWriter w, IMapElement element, int id, int order)
	{
		w.WriteStartObject();
		w.WriteNumber("id", id);
		w.WriteNumber("order", order);
		w.WriteNumber("z", element.ZIndex);
		// A circle without a center has nowhere to be drawn.
		w.WriteBoolean("visible", element.IsVisible && element is not ICircleMapElement { Center: null });
		w.WriteString("stroke", Css(element.Stroke));
		w.WriteNumber("width", element.StrokeThickness);
		w.WriteString("fill", element is IFilledMapElement filled ? Css(filled.Fill) : null);

		switch (element)
		{
			case ICircleMapElement circle:
				w.WriteString("kind", "circle");
				w.WriteNumber("radius", circle.Radius.Meters);
				w.WriteStartArray("center");
				w.WriteNumberValue(circle.Center?.Latitude ?? 0);
				w.WriteNumberValue(circle.Center?.Longitude ?? 0);
				w.WriteEndArray();
				break;

			case IGeoPathMapElement path:
				w.WriteString("kind", element is IFilledMapElement ? "polygon" : "polyline");
				w.WriteStartArray("points");
				foreach (var location in path)
				{
					w.WriteStartArray();
					w.WriteNumberValue(location.Latitude);
					w.WriteNumberValue(location.Longitude);
					w.WriteEndArray();
				}
				w.WriteEndArray();
				break;

			default:
				w.WriteString("kind", "unknown");
				break;
		}
		w.WriteEndObject();
	}

	static string? Css(Paint? paint) => paint switch
	{
		SolidPaint { Color: { } color } => color.ToRgbaHex(true),
		_ => null,
	};

	// ---- events from the map ------------------------------------------------

	// Returns true to suppress the event's default action (see maui-web-maps.js).
	bool OnMapEvent(string type, string json)
	{
		try
		{
			if (CurrentView is not { } map)
				return false;

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			switch (type)
			{
				case "region":
					map.VisibleRegion = new MapSpan(ReadLocation(root), root.GetProperty("latDeg").GetDouble(), root.GetProperty("lngDeg").GetDouble());
					return false;

				case "click":
					map.Clicked(ReadLocation(root));
					return false;

				case "longclick":
					map.LongClicked(ReadLocation(root));
					return false;

				case "user":
					map.UserLocationUpdated(ReadLocation(root));
					return false;

				case "usererror":
					Console.Error.WriteLine($"[maui-web] The map can't show the user's location: {root.GetProperty("message").GetString()}");
					return false;

				// True keeps the info window closed.
				case "marker":
					return _pinsById.TryGetValue(root.GetProperty("id").GetInt32(), out var marker) && marker.SendMarkerClick();

				// True closes the info window.
				case "info":
					return _pinsById.TryGetValue(root.GetProperty("id").GetInt32(), out var info) && info.SendInfoWindowClick();

				case "element":
					if (_elementsById.TryGetValue(root.GetProperty("id").GetInt32(), out var element))
						element.Clicked();
					return false;

				// True skips zooming into the cluster.
				case "cluster":
					return map.ClusterClicked(PinsFor(root.GetProperty("ids")), ReadLocation(root));

				default:
					return false;
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] Map '{type}' event handler failed: {ex}");
			return false;
		}
	}

	static Location ReadLocation(JsonElement root)
		=> new(root.GetProperty("lat").GetDouble(), root.GetProperty("lng").GetDouble());

	// ---- helpers ------------------------------------------------------------

	string OptionsJson()
	{
		var options = MauiContext?.Services.GetService<WebMapOptions>() ?? new WebMapOptions();
		return Json(w =>
		{
			w.WriteStartObject();
			WriteTiles(w, "street", options.StreetTiles);
			WriteTiles(w, "satellite", options.SatelliteTiles);
			WriteTiles(w, "labels", options.HybridLabelTiles);
			w.WriteEndObject();
		});
	}

	static void WriteTiles(Utf8JsonWriter w, string name, WebMapTileLayer? tiles)
	{
		if (tiles is null)
		{
			w.WriteNull(name);
			return;
		}
		w.WriteStartObject(name);
		w.WriteString("url", tiles.UrlTemplate);
		w.WriteString("attribution", tiles.Attribution);
		w.WriteNumber("maxZoom", tiles.MaxZoom);
		w.WriteString("subdomains", tiles.Subdomains);
		w.WriteEndObject();
	}

	static string Json(Action<Utf8JsonWriter> write)
	{
		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(buffer))
			write(writer);
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	sealed class PinEntry(int id)
	{
		public int Id { get; } = id;
		public IImageSource? IconSource { get; set; }
		public string? IconUrl { get; set; }
		public CancellationTokenSource? IconLoad { get; set; }

		public void ReleaseIcon()
		{
			IconLoad?.Cancel();
			IconLoad = null;
			WebImageSource.Release(IconUrl);
			IconUrl = null;
			IconSource = null;
		}
	}
}
