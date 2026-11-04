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
