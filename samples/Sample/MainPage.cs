using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Sample;

public class MainPage : ContentPage
{
    int _buttonClickCount;
    readonly Label _statusLabel;
    readonly Label _sliderValue;
    readonly Label _pickerStatus;
#if !TVOS
    readonly Label _dialogResult;
#endif

    public MainPage()
    {
        BackgroundColor = Color.FromArgb("#1A1A2E");

        _statusLabel = new Label
        {
            Text = "Ready - interact with the controls!",
            FontSize = 18,
            TextColor = Color.FromArgb("#A8E6CF"),
        };

        _sliderValue = new Label
        {
            Text = "Value: 50",
            FontSize = 18,
            TextColor = Colors.White,
        };

        _pickerStatus = new Label
        {
            Text = "No selection",
            FontSize = 18,
            TextColor = Colors.White,
        };

#if !TVOS
        _dialogResult = new Label
        {
            Text = "No dialog shown yet",
            FontSize = 18,
            TextColor = Color.FromArgb("#A8E6CF"),
        };
#endif

        // --- Title ---
        var title = new Label
        {
            Text = "MAUI tvOS Controls",
            FontSize = 44,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        // --- Buttons ---
        var button1 = new Button
        {
            Text = "Primary Button",
            BackgroundColor = Color.FromArgb("#4A90E2"),
            TextColor = Colors.White,
        };
        button1.Clicked += OnButtonClicked;

        var button2 = new Button
        {
            Text = "Secondary Button",
            BackgroundColor = Color.FromArgb("#7B68EE"),
            TextColor = Colors.White,
        };
        button2.Clicked += OnButtonClicked;

#if !TVOS
        // --- Dialog Buttons ---
        var alertButton = new Button
        {
            Text = "Show Alert",
            BackgroundColor = Color.FromArgb("#FF6B6B"),
            TextColor = Colors.White,
        };
        alertButton.Clicked += OnAlertClicked;

        var confirmButton = new Button
        {
            Text = "Show Confirm",
            BackgroundColor = Color.FromArgb("#FFD93D"),
            TextColor = Colors.Black,
        };
        confirmButton.Clicked += OnConfirmClicked;

        var promptButton = new Button
        {
            Text = "Show Prompt",
            BackgroundColor = Color.FromArgb("#6BCB77"),
            TextColor = Colors.White,
        };
        promptButton.Clicked += OnPromptClicked;
#endif

        // --- Entry ---
        var entry = new Entry
        {
            Placeholder = "Type something...",
            BackgroundColor = Colors.White,
            TextColor = Colors.Black,
            PlaceholderColor = Color.FromArgb("#888888"),
        };

        // --- Picker ---
        var picker = new Picker
        {
            Title = "Pick a color...",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#333333"),
        };
        picker.Items.Add("Red");
        picker.Items.Add("Green");
        picker.Items.Add("Blue");
        picker.Items.Add("Purple");
        picker.SelectedIndexChanged += OnPickerChanged;

        // --- Slider ---
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
        };
        slider.ValueChanged += OnSliderChanged;

        // --- Switch ---
        var switchControl = new Switch
        {
            IsToggled = false,
        };

#if !TVOS
        // --- CheckBox ---
        var checkBox = new CheckBox
        {
            IsChecked = false,
            Color = Color.FromArgb("#4A90E2"),
        };
#endif

        // --- ActivityIndicator ---
        var activityIndicator = new ActivityIndicator
        {
            IsRunning = true,
            Color = Color.FromArgb("#4A90E2"),
        };

        // --- Image (from URI) ---
        var image = new Image
        {
            Source = new UriImageSource
            {
                Uri = new Uri("https://raw.githubusercontent.com/dotnet/brand/main/logo/dotnet-logo.png"),
            },
            HeightRequest = 200,
            WidthRequest = 200,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
        };

        // --- Image (from string URL) ---
        var image2 = new Image
        {
            Source = "https://avatars.githubusercontent.com/u/9141961",
            HeightRequest = 120,
            WidthRequest = 120,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
        };

        // --- BoxView ---
        var boxView = new BoxView
        {
            Color = Color.FromArgb("#FF6B6B"),
            HeightRequest = 6,
            CornerRadius = 3,
        };

        // --- Layout items in horizontal rows ---
        var layoutRow = new HorizontalStackLayout
        {
            Spacing = 15,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "Item A", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#FF6B6B"), Padding = 12 },
                new Label { Text = "Item B", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#4A90E2"), Padding = 12 },
                new Label { Text = "Item C", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#7B68EE"), Padding = 12 },
            },
        };

        // --- Assemble page in a ScrollView ---
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(60, 40),
                Spacing = 20,
                Children =
                {
                    title,
                    boxView,

                    SectionHeader("Grid Layout"),
                    CreateGridDemo(),

                    SectionHeader("Buttons"),
                    button1,
                    button2,

#if !TVOS
                    SectionHeader("Dialogs"),
                    alertButton,
                    confirmButton,
                    promptButton,
                    _dialogResult,
#endif

                    SectionHeader("Entry"),
                    entry,

                    SectionHeader("Picker"),
                    picker,
                    _pickerStatus,

                    SectionHeader("Slider"),
                    slider,
                    _sliderValue,

                    SectionHeader("Switch"),
                    switchControl,

#if !TVOS
                    SectionHeader("CheckBox"),
                    checkBox,
#endif

                    SectionHeader("Images"),
                    image,
                    image2,

                    SectionHeader("Activity Indicator"),
                    activityIndicator,

                    SectionHeader("Horizontal Layout"),
                    layoutRow,

                    SectionHeader("Status"),
                    _statusLabel,

                    new BoxView { HeightRequest = 40 },
                },
            },
        };
    }

    static Grid CreateGridDemo()
    {
        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            RowSpacing = 10,
            ColumnSpacing = 10,
        };

        var cell1 = new Label { Text = "Row 0, Col 0", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#E74C3C"), Padding = 16, HorizontalTextAlignment = TextAlignment.Center };
        var cell2 = new Label { Text = "Row 0, Col 1", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#3498DB"), Padding = 16, HorizontalTextAlignment = TextAlignment.Center };
        var cell3 = new Label { Text = "Row 1, Col 0", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#2ECC71"), Padding = 16, HorizontalTextAlignment = TextAlignment.Center };
        var cell4 = new Label { Text = "Row 1, Col 1", TextColor = Colors.White, BackgroundColor = Color.FromArgb("#9B59B6"), Padding = 16, HorizontalTextAlignment = TextAlignment.Center };

        grid.Add(cell1, 0, 0);
        grid.Add(cell2, 1, 0);
        grid.Add(cell3, 0, 1);
        grid.Add(cell4, 1, 1);

        return grid;
    }

    static Label SectionHeader(string text) => new()
    {
        Text = text,
        FontSize = 28,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#FF6B6B"),
    };

    void OnButtonClicked(object? sender, EventArgs e)
    {
        _buttonClickCount++;
        _statusLabel.Text = $"Button clicked {_buttonClickCount} time{(_buttonClickCount != 1 ? "s" : "")}";
    }

    void OnPickerChanged(object? sender, EventArgs e)
    {
        if (sender is Picker p && p.SelectedIndex >= 0)
        {
            _pickerStatus.Text = $"Selected: {p.Items[p.SelectedIndex]}";
            _statusLabel.Text = $"Picker: {p.Items[p.SelectedIndex]}";
        }
    }

    void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        _sliderValue.Text = $"Value: {(int)e.NewValue}";
        _statusLabel.Text = $"Slider: {(int)e.NewValue}";
    }

#if !TVOS
    async void OnAlertClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Alert", "This is a simple alert message.", "OK");
        _dialogResult.Text = "Alert dismissed";
        _statusLabel.Text = "Alert dismissed";
    }

    async void OnConfirmClicked(object? sender, EventArgs e)
    {
        var result = await DisplayAlertAsync("Confirm", "Do you want to proceed?", "Yes", "No");
        _dialogResult.Text = $"Confirm result: {(result ? "Yes" : "No")}";
        _statusLabel.Text = $"Confirm: {(result ? "Yes" : "No")}";
    }

    async void OnPromptClicked(object? sender, EventArgs e)
    {
        var result = await DisplayPromptAsync("Prompt", "Enter your name:", placeholder: "Type here...");
        _dialogResult.Text = result != null ? $"Prompt result: {result}" : "Prompt cancelled";
        _statusLabel.Text = result != null ? $"Prompt: {result}" : "Prompt cancelled";
    }
#endif
}
