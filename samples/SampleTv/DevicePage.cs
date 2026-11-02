using Microsoft.Maui.Controls.Shapes;

namespace Sample;

/// <summary>Essentials on tvOS: device, display and network info, Preferences, SecureStorage, speech.</summary>
public class DevicePage : ContentPage
{
    const string CounterKey = "sample_counter";
    const string SecretKey = "sample_secret";

    readonly Label _network;

    public DevicePage()
    {
        Title = "Device";
        Background = Theme.PageBackground;

        _network = StatValue(TryGet(() => Connectivity.NetworkAccess.ToString()));

        var stats = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto) },
            ColumnSpacing = 32,
            RowSpacing = 32,
            Padding = new Thickness(Theme.Gutter, 0),
        };
        stats.Add(Stat("📺", "Device", StatValue(TryGet(() => DeviceInfo.Model)), Theme.Violet), 0, 0);
        stats.Add(Stat("⚙️", "tvOS", StatValue(TryGet(() => DeviceInfo.VersionString)), Theme.Cyan), 1, 0);
        stats.Add(Stat("🖥️", "Display", StatValue(TryGet(() =>
            $"{DeviceDisplay.MainDisplayInfo.Width:F0}×{DeviceDisplay.MainDisplayInfo.Height:F0}")), Theme.Pink), 2, 0);
        stats.Add(Stat("⏱️", "Refresh", StatValue(TryGet(() => $"{DeviceDisplay.MainDisplayInfo.RefreshRate:F0} Hz")), Theme.Amber), 3, 0);
        stats.Add(Stat("📶", "Network", _network, Theme.Emerald), 0, 1);
        stats.Add(Stat("📦", "App", StatValue(TryGet(() => $"{AppInfo.Name} {AppInfo.VersionString}")), Theme.Coral), 1, 1);
        stats.Add(Stat("🌗", "Theme", StatValue(TryGet(() => AppInfo.RequestedTheme.ToString())), Theme.Indigo), 2, 1);
        stats.Add(Stat("🧭", "Idiom", StatValue(TryGet(() => DeviceInfo.Idiom.ToString())), Theme.Violet), 3, 1);

        var tools = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 32,
            Padding = new Thickness(Theme.Gutter, 0),
        };
        tools.Add(PreferencesCard(), 0, 0);
        tools.Add(SecureStorageCard(), 1, 0);
        tools.Add(SpeechCard(), 2, 0);

        Content = Theme.PageScroll(new VerticalStackLayout
        {
            Spacing = 32,
            Padding = new Thickness(0, 8, 0, 80),
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 6,
                    Padding = new Thickness(Theme.Gutter, 0),
                    Children =
                    {
                        Theme.Eyebrow("Essentials", Theme.Emerald),
                        Theme.Title("This Apple TV"),
                    },
                },
                stats,
                Theme.SectionTitle("Storage & Speech"),
                tools,
            },
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    protected override void OnDisappearing()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        base.OnDisappearing();
    }

    void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) =>
        Dispatcher.Dispatch(() => _network.Text = e.NetworkAccess.ToString());

    static Label StatValue(string text) => new()
    {
        Text = text,
        FontSize = 32,
        FontAttributes = FontAttributes.Bold,
        TextColor = Theme.TextPrimary,
        MaxLines = 1,
        LineBreakMode = LineBreakMode.TailTruncation,
    };

    static View Stat(string glyph, string name, Label value, Color accent) => Theme.Card(new VerticalStackLayout
    {
        Spacing = 10,
        Children =
        {
            new Border
            {
                WidthRequest = 72,
                HeightRequest = 72,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Background = Theme.Gradient(accent, accent.WithLuminosity(0.22f)),
                HorizontalOptions = LayoutOptions.Start,
                Content = new Label { Text = glyph, FontSize = 36, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
            },
            new Label { Text = name.ToUpperInvariant(), FontSize = 20, FontAttributes = FontAttributes.Bold, CharacterSpacing = 2, TextColor = Theme.TextMuted, Margin = new Thickness(0, 8, 0, 0) },
            value,
        },
    }, padding: new Thickness(28));

    static View PreferencesCard()
    {
        var count = Preferences.Get(CounterKey, 0);
        var value = new Label
        {
            Text = count.ToString(),
            FontSize = 96,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
        };

        void Set(int next)
        {
            count = next;
            Preferences.Set(CounterKey, count);
            value.Text = count.ToString();
        }

        var minus = Theme.SecondaryButton("−");
        minus.Clicked += (s, e) => Set(count - 1);
        var plus = Theme.SecondaryButton("+");
        plus.Clicked += (s, e) => Set(count + 1);
        var reset = Theme.SecondaryButton("Reset");
        reset.Clicked += (s, e) => Set(0);

        return Theme.Card(new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                Theme.Eyebrow("Preferences", Theme.Amber),
                Theme.Caption("Survives relaunching the app."),
                value,
                new HorizontalStackLayout { Spacing = 20, HorizontalOptions = LayoutOptions.Center, Children = { minus, plus, reset } },
            },
        });
    }

    static View SecureStorageCard()
    {
        var status = Theme.Body("Nothing stored");

        var save = Theme.SecondaryButton("Save");
        save.Clicked += async (s, e) =>
        {
            var secret = $"token-{Random.Shared.Next(1000, 9999)}";
            await SecureStorage.SetAsync(SecretKey, secret);
            status.Text = $"Saved “{secret}” to the keychain";
        };

        var read = Theme.SecondaryButton("Read");
        read.Clicked += async (s, e) =>
        {
            var secret = await SecureStorage.GetAsync(SecretKey);
            status.Text = secret is null ? "Nothing stored" : $"Read back “{secret}”";
        };

        var clear = Theme.SecondaryButton("Clear");
        clear.Clicked += (s, e) =>
        {
            SecureStorage.Remove(SecretKey);
            status.Text = "Cleared";
        };

        return Theme.Card(new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                Theme.Eyebrow("Secure Storage", Theme.Cyan),
                Theme.Caption("Values live in the tvOS keychain."),
                status,
                new HorizontalStackLayout { Spacing = 20, Children = { Compact(save), Compact(read), Compact(clear) } },
            },
        });
    }

    static View SpeechCard()
    {
        var status = Theme.Body("Ready");
        var speak = Theme.SecondaryButton("🔊  Say hello");
        speak.Clicked += async (s, e) =>
        {
            status.Text = "Speaking…";
            try
            {
                await TextToSpeech.SpeakAsync("Hello from dot net MAUI on Apple TV.");
                status.Text = "Done";
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        };

        return Theme.Card(new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                Theme.Eyebrow("Text to Speech", Theme.Pink),
                Theme.Caption("Uses the system voice."),
                status,
                new HorizontalStackLayout { Children = { speak } },
            },
        });
    }

    static Button Compact(Button button)
    {
        button.Padding = new Thickness(28, 16);
        return button;
    }

    static string TryGet(Func<string> getter)
    {
        try { return getter(); }
        catch (Exception ex) { return ex.GetType().Name; }
    }
}
