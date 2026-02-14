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
#if TVOS
            Text = "MAUI tvOS Sample",
#elif MACOS
            Text = "MAUI macOS Sample",
#endif
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

        // --- Navigation ---
        var navButton = new Button
        {
            Text = "Go to CollectionView Page →",
            BackgroundColor = Color.FromArgb("#2ECC71"),
            TextColor = Colors.White,
        };
        navButton.Clicked += async (s, e) => await Navigation.PushAsync(new CollectionViewPage());

#if MACAPP
        var blazorButton = new Button
        {
            Text = "Go to Blazor Page →",
            BackgroundColor = Color.FromArgb("#9B59B6"),
            TextColor = Colors.White,
        };
        blazorButton.Clicked += async (s, e) => await Navigation.PushAsync(new BlazorPage());

        var graphicsButton = new Button
        {
            Text = "Go to GraphicsView Page →",
            BackgroundColor = Color.FromArgb("#F39C12"),
            TextColor = Colors.White,
        };
        graphicsButton.Clicked += async (s, e) => await Navigation.PushAsync(new GraphicsViewPage());
#endif

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

                    SectionHeader("Navigation"),
                    navButton,

#if MACAPP
                    blazorButton,
                    graphicsButton,
#endif
#if !TVOS
                    SectionHeader("Dialogs"),
                    alertButton,
                    confirmButton,
                    promptButton,
                    _dialogResult,
#endif

                    SectionHeader("Entry"),
                    entry,

#if MACAPP
                    SectionHeader("Editor (Multiline)"),
                    new Editor
                    {
                        Placeholder = "Type multiline text here...",
                        HeightRequest = 120,
                        TextColor = Colors.White,
                        BackgroundColor = Color.FromArgb("#2A2A4A"),
                    },

                    SectionHeader("Date & Time Pickers"),
                    CreateDateTimePickerDemo(),
#endif

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

                    SectionHeader("WebView"),
                    new WebView
                    {
                        Source = new UrlWebViewSource { Url = "https://dotnet.microsoft.com" },
                        HeightRequest = 400,
                    },
#endif

                    SectionHeader("Images"),
                    image,
                    image2,

                    SectionHeader("Activity Indicator"),
                    activityIndicator,

                    SectionHeader("Progress Bar"),
                    new ProgressBar { Progress = 0.65, ProgressColor = Color.FromArgb("#4A90E2"), HeightRequest = 8 },
                    new ProgressBar { Progress = 0.3, ProgressColor = Color.FromArgb("#FF6B6B"), HeightRequest = 8 },

                    SectionHeader("Horizontal Layout"),
                    layoutRow,

                    SectionHeader("Border"),
                    CreateBorderDemo(),

                    SectionHeader("Shadow"),
                    new Label
                    {
                        Text = "Shadow on Label",
                        TextColor = Colors.White,
                        FontSize = 22,
                        BackgroundColor = Color.FromArgb("#4A90E2"),
                        Padding = new Thickness(20, 12),
                        Shadow = new Shadow
                        {
                            Brush = Colors.Black,
                            Offset = new Point(4, 4),
                            Radius = 8,
                            Opacity = 0.6f,
                        },
                    },
                    new Button
                    {
                        Text = "Shadow on Button",
                        BackgroundColor = Color.FromArgb("#2ECC71"),
                        TextColor = Colors.White,
                        Shadow = new Shadow
                        {
                            Brush = Color.FromArgb("#2ECC71"),
                            Offset = new Point(0, 6),
                            Radius = 12,
                            Opacity = 0.5f,
                        },
                    },

#if TVAPP
                    SectionHeader("Carousel View"),
                    CreateCarouselDemo(),
#endif

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

    static View CreateBorderDemo()
    {
        var stack = new VerticalStackLayout { Spacing = 15 };

        // Simple rounded border
        var border1 = new Border
        {
            Stroke = Color.FromArgb("#4A90E2"),
            StrokeThickness = 2,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(16),
            Content = new Label
            {
                Text = "Rounded Border",
                TextColor = Colors.White,
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
            },
        };

        // Thick dashed border
        var border2 = new Border
        {
            Stroke = Color.FromArgb("#FF6B6B"),
            StrokeThickness = 3,
            StrokeDashArray = new Microsoft.Maui.Controls.DoubleCollection { 6, 3 },
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(16),
            Content = new Label
            {
                Text = "Dashed Border",
                TextColor = Colors.White,
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
            },
        };

        // Border with background
        var border3 = new Border
        {
            Stroke = Color.FromArgb("#2ECC71"),
            StrokeThickness = 2,
            BackgroundColor = Color.FromArgb("#2A2A4A"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(20),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Card Style", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Border with background and rounded corners", TextColor = Color.FromArgb("#AAAAAA"), FontSize = 14 },
                },
            },
        };

        stack.Children.Add(border1);
        stack.Children.Add(border2);
        stack.Children.Add(border3);
        return stack;
    }

#if TVAPP
    static View CreateCarouselDemo()
    {
        var colors = new[]
        {
            ("#E74C3C", "Slide 1 — Red"),
            ("#3498DB", "Slide 2 — Blue"),
            ("#2ECC71", "Slide 3 — Green"),
            ("#9B59B6", "Slide 4 — Purple"),
            ("#F39C12", "Slide 5 — Orange"),
        };

        var positionLabel = new Label { Text = "Position: 0", TextColor = Colors.White, FontSize = 18 };

        var carousel = new CarouselView
        {
            HeightRequest = 200,
            ItemsSource = colors,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                };
                label.SetBinding(Label.TextProperty, "[Item2]");
                label.SetBinding(Label.BackgroundColorProperty, "[Item1]",
                    converter: new FuncConverter<string, Color>(hex => Color.FromArgb(hex)));

                return label;
            }),
        };

        carousel.PositionChanged += (s, e) => positionLabel.Text = $"Position: {e.CurrentPosition}";

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children = { carousel, positionLabel },
        };
    }

    class FuncConverter<TIn, TOut> : IValueConverter
    {
        readonly Func<TIn, TOut> _convert;
        public FuncConverter(Func<TIn, TOut> convert) => _convert = convert;
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => value is TIn input ? _convert(input) : default;
        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
#endif

#if MACAPP
    static View CreateDateTimePickerDemo()
    {
        var dateLabel = new Label { Text = "Selected date: (none)", TextColor = Colors.White, FontSize = 16 };
        var datePicker = new DatePicker
        {
            Date = DateTime.Today,
            MinimumDate = new DateTime(2020, 1, 1),
            MaximumDate = new DateTime(2030, 12, 31),
        };
        datePicker.DateSelected += (s, e) => dateLabel.Text = $"Selected date: {e.NewDate:d}";

        var timeLabel = new Label { Text = "Selected time: (none)", TextColor = Colors.White, FontSize = 16 };
        var timePicker = new TimePicker
        {
            Time = DateTime.Now.TimeOfDay,
        };
        timePicker.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimePicker.Time))
                timeLabel.Text = $"Selected time: {timePicker.Time:hh\\:mm\\:ss}";
        };

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Date Picker", TextColor = Color.FromArgb("#4A90E2"), FontSize = 18 },
                datePicker,
                dateLabel,
                new BoxView { HeightRequest = 4 },
                new Label { Text = "Time Picker", TextColor = Color.FromArgb("#4A90E2"), FontSize = 18 },
                timePicker,
                timeLabel,
            },
        };
    }
#endif

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
