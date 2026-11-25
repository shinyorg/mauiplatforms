using Microsoft.Maui.Controls.Shapes;
using SampleWeb;
using Shiny.Maui.Controls;

namespace Sample.Features.AnimatedBackgrounds;

public partial class AnimatedBackgroundsPage : ContentPage
{
    // Each entry builds a fresh effect, so switching back to one starts it from its defaults.
    static readonly (string Name, Func<BackgroundEffect> Create)[] Effects =
    [
        ("Aurora", () => new AuroraEffect()),
        ("Particles", () => new ParticlesEffect()),
        ("Snow", () => new SnowEffect { Density = 7, Wind = 0.3 }),
        ("Rain", () => new RainEffect()),
        ("Meteors", () => new MeteorsEffect { Density = 0.35, MaxDelay = TimeSpan.FromSeconds(3) }),
        ("Dot spotlight", () => new DotPatternEffect { SpotlightColor = Color.FromArgb("#A78BFA"), Glow = 0.25 }),
        ("Dot pulse", () => new DotPatternEffect { Pulse = true, Spotlight = SpotlightMode.None }),
        ("Noise", () => new NoiseEffect { Intensity = 0.3 }),
        ("Starfield", () => new StarfieldEffect()),
        ("Starfield (warp)", () => new StarfieldEffect { Warp = true }),
        ("Fireflies", () => new FirefliesEffect { Density = 0.8 }),
        ("Bubbles", () => new BubblesEffect { Density = 1 }),
        ("Waves", () => new WavesEffect()),
        ("Matrix", () => new MatrixEffect())
    ];

    public AnimatedBackgroundsPage()
    {
        InitializeComponent();

        foreach (var (name, _) in Effects)
            this.EffectPicker.Items.Add(name);
        this.EffectPicker.SelectedIndex = 0;

        this.BuildGallery();
        this.UpdateSelection();
    }


    // ---------------------------------------------------------------------------------------------
    // Gallery: each card is one AppBackground choice; tapping it makes it the app background.
    // ---------------------------------------------------------------------------------------------

    readonly List<(AppBackground.Choice Choice, Border Ring, View Badge)> cards = [];

    void BuildGallery()
    {
        var cardStyle = (Style)this.Resources["Card"];
        var ringStyle = (Style)this.Resources["CardRing"];
        var titleStyle = (Style)this.Resources["CardTitle"];
        var choices = AppBackground.Choices;

        for (var i = 0; i < choices.Count; i++)
        {
            if (i % 2 == 0)
                this.Gallery.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            this.Gallery.Add(this.CreateCard(choices[i], cardStyle, ringStyle, titleStyle), i % 2, i / 2);
        }
    }


    Border CreateCard(AppBackground.Choice choice, Style cardStyle, Style ringStyle, Style titleStyle)
    {
        // The cards are always dark, so the badge is a light pill in either theme.
        var badge = new Border
        {
            IsVisible = false,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(8, 3),
            BackgroundColor = Colors.White,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label { Text = "✓ App background", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1B1530") },
        };

        var card = new AnimatedBackground
        {
            Style = cardStyle,
            Effect = choice.CreateEffect(),
            Background = choice.CreateBackground(),
            AutomationId = $"{choice.Key}Card",
            Content = new Grid { Children = { new Label { Text = choice.Name, Style = titleStyle }, badge } },
        };

        var ring = new Border { Style = ringStyle, Content = card, AutomationId = $"{choice.Key}Choice" };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => AppBackground.Select(choice);
        ring.GestureRecognizers.Add(tap);

        this.cards.Add((choice, ring, badge));
        return ring;
    }


    void UpdateSelection()
    {
        var current = AppBackground.Current;
        foreach (var (choice, ring, badge) in this.cards)
        {
            var selected = choice == current;
            // Plain values rather than a theme binding: a binding's value outlives RemoveBinding/ClearValue.
            // The page re-runs this when the theme changes, so the ring follows the theme's violet.
            ring.Stroke = selected ? new SolidColorBrush(Theme.Current(Theme.Violet)) : Brush.Transparent;
            badge.IsVisible = selected;
            SemanticProperties.SetDescription(ring, selected ? $"{choice.Name}, the app background" : $"Use {choice.Name} as the app background");
        }

        this.AppBackgroundStatus.Text = current is null
            ? "Tap a card to make it the app background."
            : $"App background: {current.Name}";
        this.NoneButton.IsEnabled = current is not null;
    }


    void OnAppBackgroundChanged(object? sender, EventArgs e) => this.UpdateSelection();

    void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => this.UpdateSelection();

    void OnNone(object? sender, EventArgs e) => AppBackground.Select(null);

    // Static and app-wide events: subscribed only while the page is showing, so they do not keep it alive.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        AppBackground.Changed += this.OnAppBackgroundChanged;
        if (Application.Current is { } app)
            app.RequestedThemeChanged += this.OnThemeChanged;
        this.UpdateSelection();
    }

    protected override void OnDisappearing()
    {
        AppBackground.Changed -= this.OnAppBackgroundChanged;
        if (Application.Current is { } app)
            app.RequestedThemeChanged -= this.OnThemeChanged;
        base.OnDisappearing();
    }


    void OnEffectChanged(object? sender, EventArgs e)
    {
        var index = this.EffectPicker.SelectedIndex;
        if (index < 0)
            return;

        var (name, create) = Effects[index];
        var effect = create();
        effect.Speed = this.SpeedSlider.Value;
        this.Hero.Effect = effect;
        this.HeroTitle.Text = name;
    }


    void OnSpeedChanged(object? sender, ValueChangedEventArgs e)
    {
        // Settings are bindable and change live - no need to rebuild the effect.
        if (this.Hero.Effect is { } effect)
            effect.Speed = e.NewValue;
    }


    void OnPalette(object? sender, EventArgs e)
    {
        var value = (sender as Button)?.CommandParameter as string;
        var colors = string.IsNullOrEmpty(value)
            ? null
            : value.Split(',').Select(Color.FromArgb).ToArray();

        switch (this.Hero.Effect)
        {
            case AuroraEffect aurora: Apply(aurora, AuroraEffect.ColorsProperty, colors); break;
            case ParticlesEffect particles: Apply(particles, ParticlesEffect.ColorsProperty, colors); break;
            case FirefliesEffect fireflies: Apply(fireflies, FirefliesEffect.ColorsProperty, colors); break;
            case BubblesEffect bubbles: Apply(bubbles, BubblesEffect.ColorsProperty, colors); break;
            case WavesEffect waves: Apply(waves, WavesEffect.ColorsProperty, colors); break;
            case SnowEffect snow: ApplyOne(snow, SnowEffect.ColorProperty, colors); break;
            case RainEffect rain: ApplyOne(rain, RainEffect.ColorProperty, colors); break;
            case MeteorsEffect meteors: ApplyOne(meteors, MeteorsEffect.ColorProperty, colors); break;
            case DotPatternEffect dots: ApplyOne(dots, DotPatternEffect.SpotlightColorProperty, colors); break;
            case NoiseEffect noise: ApplyOne(noise, NoiseEffect.ColorProperty, colors); break;
            case StarfieldEffect stars: ApplyOne(stars, StarfieldEffect.ColorProperty, colors); break;
            case MatrixEffect matrix: ApplyOne(matrix, MatrixEffect.ColorProperty, colors); break;
        }
    }


    static void Apply(BindableObject effect, BindableProperty property, Color[]? colors)
    {
        if (colors is null)
            effect.ClearValue(property);
        else
            effect.SetValue(property, colors);
    }


    static void ApplyOne(BindableObject effect, BindableProperty property, Color[]? colors)
    {
        if (colors is null)
            effect.ClearValue(property);
        else
            effect.SetValue(property, colors[0]);
    }


    int heroClicks;

    // Counts rather than opening an alert, so a tap that reaches the button is visible on the page.
    void OnHeroButton(object? sender, EventArgs e)
        => this.HeroButton.Text = $"A real button ({++this.heroClicks})";
}
