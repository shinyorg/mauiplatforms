using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace SampleWeb.Pages;

/// <summary>The .NET MAUI Map control, drawn by Leaflet with OpenStreetMap tiles (Platform.Maui.Web.Maps).</summary>
public class MapsPage : ContentPage
{
	static readonly Location London = new(51.5074, -0.1278);

	readonly Map _map;
	readonly Label _status;
	readonly Label _region;
	readonly List<Pin> _crowd = new();

	public MapsPage()
	{
		Title = "Maps";

		_map = new Map(new MapSpan(London, 0.12, 0.12)) { HeightRequest = 420 };
		_status = new Label { Text = "Click the map, a pin or a shape.", TextColor = Colors.Gray };
		_region = new Label { FontSize = 12, TextColor = Colors.Gray };

		AddLandmarks();
		AddShapes();

		_map.MapClicked += (_, e) => _status.Text = $"Clicked {Format(e.Location)}";
		_map.MapLongClicked += (_, e) =>
		{
			_status.Text = $"Long press at {Format(e.Location)}: dropped a pin";
			_map.Pins.Add(new Pin { Label = "Dropped pin", Address = Format(e.Location), Location = e.Location });
		};
		_map.ClusterClicked += (_, e) => _status.Text = $"Cluster of {e.Pins.Count} pins";
		_map.UserLocationChanged += (_, e) => _status.Text = $"You are at {Format(e.Location)}";
		_map.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Map.VisibleRegion) && _map.VisibleRegion is { } r)
				_region.Text = $"Visible region: {Format(r.Center)}, {r.LatitudeDegrees:0.###}° × {r.LongitudeDegrees:0.###}°";
		};

		var mapType = new Picker { Title = "Map type", ItemsSource = new[] { "Street", "Satellite", "Hybrid" }, SelectedIndex = 0 };
		mapType.SelectedIndexChanged += (_, _) => _map.MapType = (MapType)mapType.SelectedIndex;

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 12,
				Padding = new Thickness(24),
				Children =
				{
					new Label { Text = "Maps (Microsoft.Maui.Controls.Maps)", FontSize = 24, FontAttributes = FontAttributes.Bold },
					new BoxView { HeightRequest = 2, Color = Colors.DodgerBlue },
					new Label { Text = "MAUI's own Map control, drawn by Leaflet with OpenStreetMap tiles; no API key. Right click (or long press) drops a pin." },
					_map,
					_status,
					_region,
					mapType,
					Row(
						Toggle("Scroll", true, on => _map.IsScrollEnabled = on),
						Toggle("Zoom", true, on => _map.IsZoomEnabled = on),
						Toggle("My location", false, on => _map.IsShowingUser = on)),
					Row(
						Toggle("Clustering", false, on => _map.IsClusteringEnabled = on),
						Button("Add 200 pins", AddCrowd),
						Button("Remove them", RemoveCrowd)),
					Row(
						Button("London", () => _map.MoveToRegion(new MapSpan(London, 0.12, 0.12))),
						Button("Paris", () => _map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(48.8566, 2.3522), Distance.FromKilometers(5)))),
						Button("New York", () => _map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(40.7128, -74.0060), Distance.FromKilometers(8))))),
					Row(
						Button("Show Big Ben info", () => _map.Pins[0].ShowInfoWindow()),
						Button("Move Big Ben", () => _map.Pins[0].Location = new Location(_map.Pins[0].Location.Latitude + 0.002, _map.Pins[0].Location.Longitude))),
				}
			}
		};
	}

	void AddLandmarks()
	{
		var bigBen = new Pin { Label = "Big Ben", Address = "Westminster, London SW1A 0AA", Location = new Location(51.5007, -0.1246) };
		bigBen.MarkerClicked += (_, _) => _status.Text = "Marker: Big Ben";
		bigBen.InfoWindowClicked += (_, e) =>
		{
			_status.Text = "Info window: Big Ben (closed it)";
			e.HideInfoWindow = true;
		};
		_map.Pins.Add(bigBen);
		_map.Pins.Add(new Pin { Label = "Tower of London", Address = "London EC3N 4AB", Location = new Location(51.5081, -0.0759) });
		_map.Pins.Add(new Pin { Label = "British Museum", Address = "Great Russell St, London WC1B 3DG", Location = new Location(51.5194, -0.1270) });
		_map.Pins.Add(new Pin
		{
			Label = "London Eye (custom image)",
			Address = "Riverside Building, London SE1 7PB",
			Location = new Location(51.5033, -0.1196),
			ImageSource = Theme.Icon(Theme.Glyph.Globe, Colors.MediumPurple, 32),
		});
	}

	void AddShapes()
	{
		var park = new Polygon { StrokeColor = Colors.ForestGreen, StrokeWidth = 3, FillColor = Colors.ForestGreen.WithAlpha(0.25f) };
		foreach (var (lat, lng) in new[] { (51.5085, -0.1880), (51.5125, -0.1590), (51.5045, -0.1500), (51.5005, -0.1780) })
			park.Geopath.Add(new Location(lat, lng));
		park.PolygonClicked += (_, _) => _status.Text = "Polygon: Hyde Park";

		var route = new Polyline { StrokeColor = Colors.OrangeRed, StrokeWidth = 5 };
		foreach (var (lat, lng) in new[] { (51.5007, -0.1246), (51.5033, -0.1196), (51.5055, -0.0900), (51.5081, -0.0759) })
			route.Geopath.Add(new Location(lat, lng));
		route.PolylineClicked += (_, _) => _status.Text = "Polyline: river walk";

		var zone = new Circle { Center = new Location(51.5313, -0.1570), Radius = Distance.FromMeters(700), StrokeColor = Colors.DodgerBlue, StrokeWidth = 2, FillColor = Colors.DodgerBlue.WithAlpha(0.2f) };
		zone.CircleClicked += (_, _) => _status.Text = "Circle: 700 m around Regent's Park";

		_map.MapElements.Add(park);
		_map.MapElements.Add(route);
		_map.MapElements.Add(zone);
	}

	void AddCrowd()
	{
		RemoveCrowd();
		var random = new Random(7);
		for (var i = 0; i < 200; i++)
		{
			var pin = new Pin
			{
				Label = $"Pin {i + 1}",
				Location = new Location(London.Latitude + (random.NextDouble() - 0.5) * 0.3, London.Longitude + (random.NextDouble() - 0.5) * 0.5),
			};
			_crowd.Add(pin);
			_map.Pins.Add(pin);
		}
		_status.Text = "Added 200 pins; turn on clustering to group them.";
	}

	void RemoveCrowd()
	{
		_crowd.ForEach(p => _map.Pins.Remove(p));
		_crowd.Clear();
	}

	static string Format(Location l) => $"{l.Latitude:0.0000}, {l.Longitude:0.0000}";

	static HorizontalStackLayout Row(params View[] children)
	{
		var row = new HorizontalStackLayout { Spacing = 12 };
		foreach (var child in children)
			row.Children.Add(child);
		return row;
	}

	static Button Button(string text, Action action)
	{
		var button = new Button { Text = text };
		button.Clicked += (_, _) => action();
		return button;
	}

	static HorizontalStackLayout Toggle(string text, bool on, Action<bool> changed)
	{
		var toggle = new Switch { IsToggled = on };
		toggle.Toggled += (_, e) => changed(e.Value);
		return new HorizontalStackLayout
		{
			Spacing = 6,
			Children = { toggle, new Label { Text = text, VerticalOptions = LayoutOptions.Center } },
		};
	}
}
