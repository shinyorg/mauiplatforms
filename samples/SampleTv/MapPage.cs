using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform.TvOS.Controls;

namespace Sample;

/// <summary>
/// MapKit through MapView, with a vertical CollectionView of cities beside it: moving through
/// the list and pressing Select flies the map to that city.
/// </summary>
public class MapPage : ContentPage
{
    public record City(string Name, string Country, string Glyph, double Lat, double Lon);

    static readonly City[] Cities =
    [
        new("Seattle", "United States", "🌲", 47.6062, -122.3321),
        new("San Francisco", "United States", "🌉", 37.7749, -122.4194),
        new("New York", "United States", "🗽", 40.7128, -74.0060),
        new("London", "United Kingdom", "🎡", 51.5074, -0.1278),
        new("Tokyo", "Japan", "🗼", 35.6762, 139.6503),
        new("Sydney", "Australia", "🦘", -33.8688, 151.2093),
    ];

    readonly MapView _map;
    readonly Label _where;

    public MapPage()
    {
        Title = "Map";
        Background = Theme.PageBackground;

        _map = new MapView
        {
            Latitude = Cities[0].Lat,
            Longitude = Cities[0].Lon,
            LatitudeDelta = 0.08,
            LongitudeDelta = 0.08,
        };

        foreach (var city in Cities)
        {
            _map.Pins.Add(new MapPin
            {
                Latitude = city.Lat,
                Longitude = city.Lon,
                Label = city.Name,
                Address = city.Country,
            });
        }

        _map.Circles.Add(new MapCircle
        {
            CenterLatitude = 47.6062,
            CenterLongitude = -122.3321,
            Radius = 2000,
            StrokeColor = Theme.Cyan,
            FillColor = Theme.Cyan.WithAlpha(0.2f),
            StrokeWidth = 3,
        });

        _map.Polylines.Add(new MapPolyline
        {
            Positions = [(47.6062, -122.3321), (45.5152, -122.6784), (42.3601, -122.9120), (39.8283, -121.9780), (37.7749, -122.4194)],
            StrokeColor = Theme.Pink,
            StrokeWidth = 4,
        });

        _where = Theme.Body($"{Cities[0].Name}  ·  {Cities[0].Lat:F2}, {Cities[0].Lon:F2}", 24);

        var cities = new CollectionView
        {
            ItemsSource = Cities,
            ItemTemplate = new DataTemplate(CityRow),
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 14 },
            SelectionMode = SelectionMode.Single,
            SelectedItem = Cities[0],
        };
        cities.SelectionChanged += (s, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is City city)
                FlyTo(city);
        };

        var standard = Theme.SecondaryButton("Map");
        standard.Clicked += (s, e) => _map.MapType = MapType.Standard;
        var satellite = Theme.SecondaryButton("Satellite");
        satellite.Clicked += (s, e) => _map.MapType = MapType.Satellite;
        var zoomIn = Theme.SecondaryButton("＋");
        zoomIn.Clicked += (s, e) => Zoom(0.5);
        var zoomOut = Theme.SecondaryButton("−");
        zoomOut.Clicked += (s, e) => Zoom(2);

        var panel = new Grid
        {
            RowDefinitions =
            {
                new(GridLength.Auto),
                new(GridLength.Star),
                new(GridLength.Auto),
            },
            RowSpacing = 20,
            WidthRequest = 520,
        };
        panel.Add(new VerticalStackLayout
        {
            Spacing = 6,
            // Line up with the list's items, which sit inside its focus inset.
            Margin = new Thickness(Microsoft.Maui.Platform.TvOS.Handlers.CollectionViewHandler.FocusInset.Left, 0, 0, 0),
            Children = { Theme.Eyebrow("MapKit", Theme.Cyan), Theme.Title("Explore"), _where },
        }, 0, 0);
        panel.Add(cities, 0, 1);
        panel.Add(new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                new HorizontalStackLayout { Spacing = 16, Children = { standard, satellite } },
                new HorizontalStackLayout { Spacing = 16, Children = { zoomIn, zoomOut } },
            },
        }, 0, 2);

        var mapFrame = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 32 },
            Content = _map,
        };

        var layout = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 48,
            Padding = new Thickness(Theme.Gutter - 24, 8, Theme.Gutter, 60),
        };
        layout.Add(panel, 0, 0);
        layout.Add(mapFrame, 1, 0);
        Content = layout;
    }

    static View CityRow()
    {
        var glyph = new Label { FontSize = 40, VerticalOptions = LayoutOptions.Center };
        glyph.SetBinding(Label.TextProperty, nameof(City.Glyph));

        var name = new Label { FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
        name.SetBinding(Label.TextProperty, nameof(City.Name));

        var country = new Label { FontSize = 22, TextColor = Theme.TextSecondary };
        country.SetBinding(Label.TextProperty, nameof(City.Country));

        var row = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            BackgroundColor = Theme.Surface,
            Padding = new Thickness(24, 16),
            Content = new HorizontalStackLayout
            {
                Spacing = 20,
                Children = { glyph, new VerticalStackLayout { Spacing = 2, Children = { name, country } } },
            },
        };

        var normal = new VisualState { Name = "Normal" };
        var selected = new VisualState { Name = "Selected" };
        selected.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Theme.Indigo });
        var focused = new VisualState { Name = "Focused" };
        focused.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Theme.SurfaceRaised });
        var group = new VisualStateGroup { Name = "CommonStates" };
        group.States.Add(normal);
        group.States.Add(selected);
        group.States.Add(focused);
        VisualStateManager.GetVisualStateGroups(row).Add(group);
        return row;
    }

    void FlyTo(City city)
    {
        _map.Latitude = city.Lat;
        _map.Longitude = city.Lon;
        _where.Text = $"{city.Name}  ·  {city.Lat:F2}, {city.Lon:F2}";
    }

    void Zoom(double factor)
    {
        _map.LatitudeDelta = Math.Clamp(_map.LatitudeDelta * factor, 0.002, 90);
        _map.LongitudeDelta = Math.Clamp(_map.LongitudeDelta * factor, 0.002, 180);
    }
}
