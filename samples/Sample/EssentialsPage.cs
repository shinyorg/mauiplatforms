using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
#if MACAPP
using Microsoft.Maui.Essentials.MacOS;
#elif TVAPP
using Microsoft.Maui.Essentials.TvOS;
#endif

namespace Sample;

public class EssentialsPage : ContentPage
{
    readonly Label _networkAccessLabel;
    readonly Label _profilesLabel;
    readonly Label _connectivityEventLog;
#if MACAPP
    readonly Label _chargeLevelLabel;
    readonly Label _batteryStateLabel;
    readonly Label _powerSourceLabel;
    readonly Label _energySaverLabel;
    readonly Label _batteryEventLog;
#endif

    public EssentialsPage()
    {
        BackgroundColor = Color.FromArgb("#1A1A2E");

        var layout = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(20)
        };

        layout.Children.Add(CreateHeader("App Info"));
        layout.Children.Add(CreateRow("Package Name", TryGet(() => AppInfo.PackageName)));
        layout.Children.Add(CreateRow("App Name", TryGet(() => AppInfo.Name)));
        layout.Children.Add(CreateRow("Version", TryGet(() => AppInfo.VersionString)));
        layout.Children.Add(CreateRow("Build", TryGet(() => AppInfo.BuildString)));
        layout.Children.Add(CreateRow("Theme", TryGet(() => AppInfo.RequestedTheme.ToString())));
        layout.Children.Add(CreateRow("Layout Direction", TryGet(() => AppInfo.RequestedLayoutDirection.ToString())));
        layout.Children.Add(CreateRow("Packaging Model", TryGet(() => AppInfo.PackagingModel.ToString())));

        layout.Children.Add(CreateHeader("Device Info"));
        layout.Children.Add(CreateRow("Model", TryGet(() => DeviceInfo.Model)));
        layout.Children.Add(CreateRow("Manufacturer", TryGet(() => DeviceInfo.Manufacturer)));
        layout.Children.Add(CreateRow("Device Name", TryGet(() => DeviceInfo.Name)));
        layout.Children.Add(CreateRow("OS Version", TryGet(() => DeviceInfo.VersionString)));
        layout.Children.Add(CreateRow("Platform", TryGet(() => DeviceInfo.Platform.ToString())));
        layout.Children.Add(CreateRow("Idiom", TryGet(() => DeviceInfo.Idiom.ToString())));
        layout.Children.Add(CreateRow("Device Type", TryGet(() => DeviceInfo.DeviceType.ToString())));

        layout.Children.Add(CreateHeader("Connectivity"));
        _networkAccessLabel = CreateValueLabel(TryGet(() => Connectivity.NetworkAccess.ToString()));
        _profilesLabel = CreateValueLabel(TryGet(() => string.Join(", ", Connectivity.ConnectionProfiles)));
        _connectivityEventLog = new Label { FontSize = 14, TextColor = Color.FromArgb("#FFD93D"), Text = "Listening for changes..." };
        layout.Children.Add(CreateRow("Network Access", _networkAccessLabel));
        layout.Children.Add(CreateRow("Profiles", _profilesLabel));
        layout.Children.Add(_connectivityEventLog);

        Connectivity.ConnectivityChanged += OnConnectivityChanged;

#if MACAPP
        layout.Children.Add(CreateHeader("Battery"));
        _chargeLevelLabel = CreateValueLabel(TryGet(() => $"{Battery.ChargeLevel:P0}"));
        _batteryStateLabel = CreateValueLabel(TryGet(() => Battery.State.ToString()));
        _powerSourceLabel = CreateValueLabel(TryGet(() => Battery.PowerSource.ToString()));
        _energySaverLabel = CreateValueLabel(TryGet(() => Battery.EnergySaverStatus.ToString()));
        _batteryEventLog = new Label { FontSize = 14, TextColor = Color.FromArgb("#FFD93D"), Text = "Listening for changes..." };
        layout.Children.Add(CreateRow("Charge Level", _chargeLevelLabel));
        layout.Children.Add(CreateRow("State", _batteryStateLabel));
        layout.Children.Add(CreateRow("Power Source", _powerSourceLabel));
        layout.Children.Add(CreateRow("Energy Saver", _energySaverLabel));
        layout.Children.Add(_batteryEventLog);

        Battery.BatteryInfoChanged += OnBatteryInfoChanged;
#endif

        layout.Children.Add(CreateHeader("Device Display"));
        layout.Children.Add(CreateRow("Width", TryGet(() => $"{DeviceDisplay.MainDisplayInfo.Width:F0} px")));
        layout.Children.Add(CreateRow("Height", TryGet(() => $"{DeviceDisplay.MainDisplayInfo.Height:F0} px")));
        layout.Children.Add(CreateRow("Density", TryGet(() => $"{DeviceDisplay.MainDisplayInfo.Density:F1}")));
        layout.Children.Add(CreateRow("Orientation", TryGet(() => DeviceDisplay.MainDisplayInfo.Orientation.ToString())));
        layout.Children.Add(CreateRow("Rotation", TryGet(() => DeviceDisplay.MainDisplayInfo.Rotation.ToString())));
        layout.Children.Add(CreateRow("Refresh Rate", TryGet(() => $"{DeviceDisplay.MainDisplayInfo.RefreshRate:F0} Hz")));
        layout.Children.Add(CreateRow("Keep Screen On", TryGet(() => DeviceDisplay.KeepScreenOn.ToString())));

        layout.Children.Add(CreateHeader("File System"));
        layout.Children.Add(CreateRow("Cache Dir", TryGet(() => FileSystem.CacheDirectory)));
        layout.Children.Add(CreateRow("App Data Dir", TryGet(() => FileSystem.AppDataDirectory)));

        Content = new ScrollView { Content = layout };
    }

    void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThreadHelper.BeginInvokeOnMainThread(() =>
        {
            _networkAccessLabel.Text = e.NetworkAccess.ToString();
            _profilesLabel.Text = string.Join(", ", e.ConnectionProfiles);
            _connectivityEventLog.Text = $"[{DateTime.Now:HH:mm:ss}] Changed: {e.NetworkAccess}, Profiles: {string.Join(", ", e.ConnectionProfiles)}";
        });
    }

#if MACAPP
    void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
    {
        MainThreadHelper.BeginInvokeOnMainThread(() =>
        {
            _chargeLevelLabel.Text = $"{e.ChargeLevel:P0}";
            _batteryStateLabel.Text = e.State.ToString();
            _powerSourceLabel.Text = e.PowerSource.ToString();
            _batteryEventLog.Text = $"[{DateTime.Now:HH:mm:ss}] Level: {e.ChargeLevel:P0}, State: {e.State}, Source: {e.PowerSource}";
        });
    }
#endif

    static string TryGet(Func<string> getter)
    {
        try { return getter(); }
        catch (Exception ex) { return $"Error: {ex.Message}"; }
    }

    static Label CreateValueLabel(string text) => new()
    {
        Text = text,
        FontSize = 16,
        TextColor = Colors.White
    };

    static Label CreateHeader(string text) => new()
    {
        Text = text,
        FontSize = 24,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#E94560"),
        Margin = new Thickness(0, 10, 0, 5)
    };

    static HorizontalStackLayout CreateRow(string label, string value) => new()
    {
        Spacing = 10,
        Children =
        {
            new Label
            {
                Text = $"{label}:",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#A8E6CF"),
                WidthRequest = 200
            },
            new Label
            {
                Text = value,
                FontSize = 16,
                TextColor = Colors.White
            }
        }
    };

    static HorizontalStackLayout CreateRow(string label, Label valueLabel) => new()
    {
        Spacing = 10,
        Children =
        {
            new Label
            {
                Text = $"{label}:",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#A8E6CF"),
                WidthRequest = 200
            },
            valueLabel
        }
    };
}
