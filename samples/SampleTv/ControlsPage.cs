using Microsoft.Maui.Controls.Shapes;

namespace Sample;

/// <summary>
/// One card per control. Everything here is reachable with the remote: buttons and inputs are
/// focusable, and cards with a TapGestureRecognizer become focusable "tap points" whose tap
/// runs when Select is pressed.
/// </summary>
public class ControlsPage : ContentPage
{
    public ControlsPage()
    {
        Title = "Controls";
        Background = Theme.PageBackground;

        var grid = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 36,
            RowSpacing = 36,
            Padding = new Thickness(Theme.Gutter, 0),
        };

        View[] cards =
        [
            ButtonsCard(),
            TapCardsCard(),
            DialogsCard(),
            ToggleCard(),
            SliderCard(),
            PickerCard(),
            TextCard(),
            ProgressCard(),
            TypographyCard(),
        ];

        for (int i = 0; i < cards.Length; i++)
        {
            if (i % 3 == 0)
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(cards[i], i % 3, i / 3);
        }

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
                        Theme.Eyebrow("Gallery", Theme.Pink),
                        Theme.Title("Controls"),
                        Theme.Body("MAUI controls rendered with native tvOS views, driven by the Siri Remote."),
                    },
                },
                grid,
            },
        });
    }

    static Border ControlCard(string title, string subtitle, Color accent, params View[] body)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 20,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 14,
                    Children =
                    {
                        new Border
                        {
                            WidthRequest = 14,
                            HeightRequest = 14,
                            StrokeThickness = 0,
                            StrokeShape = new Ellipse(),
                            BackgroundColor = accent,
                            VerticalOptions = LayoutOptions.Center,
                        },
                        new Label { Text = title, FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Theme.TextPrimary },
                    },
                },
                Theme.Caption(subtitle),
            },
        };
        foreach (var view in body)
            stack.Children.Add(view);

        var card = Theme.Card(stack, padding: new Thickness(36, 32));
        card.MinimumHeightRequest = 340;
        return card;
    }

    static View ButtonsCard()
    {
        var count = 0;
        var status = Theme.Body("Not pressed yet");

        Button Make(string text, Color? color)
        {
            var button = color is null ? Theme.SecondaryButton(text) : new Button
            {
                Text = text,
                BackgroundColor = color,
                TextColor = Colors.White,
                CornerRadius = 16,
                Padding = new Thickness(28, 16),
            };
            if (color is null)
                button.Padding = new Thickness(28, 16);
            button.Clicked += (s, e) => status.Text = $"{text} — pressed {++count} time{(count == 1 ? "" : "s")}";
            return button;
        }

        return ControlCard("Buttons", "Focused buttons lift; Select presses them.", Theme.Violet,
            new HorizontalStackLayout
            {
                Spacing = 20,
                Children = { Make("Primary", Theme.Violet), Make("Accent", Theme.Pink), Make("System", null) },
            },
            status);
    }

    static View TapCardsCard()
    {
        var status = Theme.Body("Move onto a tile and press Select");

        View Tile(string glyph, string name, Color color)
        {
            var taps = 0;
            var tile = new Border
            {
                WidthRequest = 140,
                HeightRequest = 140,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 28 },
                Background = Theme.Gradient(color, color.WithLuminosity(0.25f)),
                Content = new Label
                {
                    Text = glyph,
                    FontSize = 64,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => status.Text = $"{name} tapped {++taps} time{(taps == 1 ? "" : "s")}";
            tile.GestureRecognizers.Add(tap);
            return tile;
        }

        return ControlCard("Tap Gestures", "Any view with a TapGestureRecognizer becomes focusable.", Theme.Cyan,
            new HorizontalStackLayout
            {
                Spacing = 28,
                Children =
                {
                    Tile("🎬", "Movies", Theme.Indigo),
                    Tile("🎵", "Music", Theme.Pink),
                    Tile("🎮", "Games", Theme.Emerald),
                },
            },
            status);
    }

    View DialogsCard()
    {
        var result = Theme.Body("No dialog shown yet");

        var alert = Theme.SecondaryButton("Alert");
        alert.Padding = new Thickness(28, 16);
        alert.Clicked += async (s, e) =>
        {
            await DisplayAlertAsync("Hello, TV", "This alert is a native UIAlertController.", "OK");
            result.Text = "Alert dismissed";
        };

        var confirm = Theme.SecondaryButton("Confirm");
        confirm.Padding = new Thickness(28, 16);
        confirm.Clicked += async (s, e) =>
        {
            var yes = await DisplayAlertAsync("Download in 4K?", "This title is 18 GB in 4K HDR.", "Download", "Cancel");
            result.Text = yes ? "Download started" : "Download cancelled";
        };

        var sheet = Theme.SecondaryButton("Sheet");
        sheet.Padding = new Thickness(28, 16);
        sheet.Clicked += async (s, e) =>
        {
            var choice = await DisplayActionSheetAsync("Audio language", "Cancel", null, "English", "Español", "Français", "日本語");
            result.Text = choice is null or "Cancel" ? "No language chosen" : $"Audio: {choice}";
        };

        return ControlCard("Dialogs", "Alerts and action sheets from DisplayAlertAsync.", Theme.Amber,
            new HorizontalStackLayout { Spacing = 20, Children = { alert, confirm, sheet } },
            result);
    }

    static View ToggleCard()
    {
        var state = Theme.Body("Autoplay previews: off");
        var toggle = new Switch { OnColor = Theme.Emerald };
        toggle.Toggled += (s, e) => state.Text = $"Autoplay previews: {(e.Value ? "on" : "off")}";

        return ControlCard("Switch", "Select flips the switch.", Theme.Emerald,
            new HorizontalStackLayout { Spacing = 24, Children = { toggle } },
            state);
    }

    static View SliderCard()
    {
        var value = Theme.Body("Volume: 40");
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 40,
            MinimumTrackColor = Theme.Cyan,
        };
        slider.ValueChanged += (s, e) => value.Text = $"Volume: {(int)e.NewValue}";

        return ControlCard("Slider", "Select, then swipe left and right.", Theme.Cyan, slider, value);
    }

    static View PickerCard()
    {
        var picker = new Picker { Title = "Video quality" };
        foreach (var option in new[] { "Automatic", "4K HDR", "1080p", "720p", "Data saver" })
            picker.Items.Add(option);
        picker.SelectedIndex = 0;

        var chosen = Theme.Body("Quality: Automatic");
        picker.SelectedIndexChanged += (s, e) => chosen.Text = $"Quality: {picker.Items[picker.SelectedIndex]}";

        return ControlCard("Picker", "Opens a list to choose from.", Theme.Coral, picker, chosen);
    }

    static View TextCard()
    {
        var echo = Theme.Body("Type with the on-screen keyboard");
        var entry = new Entry
        {
            Placeholder = "Your name",
            BackgroundColor = Theme.SurfaceRaised,
            TextColor = Colors.White,
            PlaceholderColor = Theme.TextMuted,
        };
        entry.Completed += (s, e) => echo.Text = string.IsNullOrWhiteSpace(entry.Text) ? "Nothing entered" : $"Hello, {entry.Text}!";

        return ControlCard("Entry", "Select opens the tvOS keyboard.", Theme.Indigo, entry, echo);
    }

    static View ProgressCard()
    {
        return ControlCard("Progress", "Determinate and indeterminate.", Theme.Pink,
            new ProgressBar { Progress = 0.72, ProgressColor = Theme.Pink, HeightRequest = 8 },
            new ProgressBar { Progress = 0.35, ProgressColor = Theme.Violet, HeightRequest = 8 },
            new HorizontalStackLayout
            {
                Spacing = 20,
                Children =
                {
                    new ActivityIndicator { IsRunning = true, Color = Colors.White },
                    new Label { Text = "Loading the next episode…", FontSize = 24, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center },
                },
            });
    }

    static View TypographyCard()
    {
        return ControlCard("Typography", "Weights, styles and decorations.", Theme.Violet,
            new Label { Text = "Bold headline", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
            new Label { Text = "Italic aside", FontSize = 28, FontAttributes = FontAttributes.Italic, TextColor = Theme.TextSecondary },
            new Label { Text = "WIDE LETTER SPACING", FontSize = 24, CharacterSpacing = 6, TextColor = Theme.Cyan },
            new Label { Text = "Underlined link", FontSize = 26, TextDecorations = TextDecorations.Underline, TextColor = Theme.Amber });
    }
}
