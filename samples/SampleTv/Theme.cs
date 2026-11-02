using Microsoft.Maui.Controls.Shapes;

namespace Sample;

/// <summary>
/// The sample's look: a dark, cinematic palette in the style of the Apple TV app, sized for a
/// television viewed from across the room (1920×1080 points, generous type and spacing).
/// </summary>
public static class Theme
{
    // Surfaces
    public static readonly Color Background = Color.FromArgb("#0A0B12");
    public static readonly Color Surface = Color.FromArgb("#161926");
    public static readonly Color SurfaceRaised = Color.FromArgb("#1F2333");
    public static readonly Color Hairline = Color.FromArgb("#2A2F42");

    // Text
    public static readonly Color TextPrimary = Colors.White;
    public static readonly Color TextSecondary = Color.FromArgb("#A3A8BC");
    public static readonly Color TextMuted = Color.FromArgb("#6B7088");

    // Accents
    public static readonly Color Violet = Color.FromArgb("#8B5CF6");
    public static readonly Color Indigo = Color.FromArgb("#4F46E5");
    public static readonly Color Cyan = Color.FromArgb("#22D3EE");
    public static readonly Color Pink = Color.FromArgb("#F472B6");
    public static readonly Color Amber = Color.FromArgb("#FBBF24");
    public static readonly Color Emerald = Color.FromArgb("#34D399");
    public static readonly Color Coral = Color.FromArgb("#FB7185");

    /// <summary>Horizontal page margin: inside the TV's title-safe area.</summary>
    public const double Gutter = 80;

    public static Brush PageBackground => new LinearGradientBrush(
        [
            new GradientStop(Color.FromArgb("#1B1640"), 0f),
            new GradientStop(Color.FromArgb("#0E0F1C"), 0.45f),
            new GradientStop(Background, 1f),
        ],
        new Point(0, 0), new Point(1, 1));

    public static LinearGradientBrush Gradient(Color from, Color to, double angleDegrees = 135)
    {
        var radians = angleDegrees * Math.PI / 180;
        var dx = Math.Cos(radians) / 2;
        var dy = Math.Sin(radians) / 2;
        return new LinearGradientBrush(
            [new GradientStop(from, 0f), new GradientStop(to, 1f)],
            new Point(0.5 - dx, 0.5 - dy), new Point(0.5 + dx, 0.5 + dy));
    }

    /// <summary>A vertical fade to black, laid over artwork so text on it stays readable.</summary>
    public static Brush Scrim => new LinearGradientBrush(
        [
            new GradientStop(Colors.Transparent, 0.35f),
            new GradientStop(Colors.Black.WithAlpha(0.85f), 1f),
        ],
        new Point(0, 0), new Point(0, 1));

    // ── Type ─────────────────────────────────────────────────────────────

    public static Label Display(string text) => new()
    {
        Text = text,
        FontSize = 76,
        FontAttributes = FontAttributes.Bold,
        TextColor = TextPrimary,
    };

    public static Label Title(string text) => new()
    {
        Text = text,
        FontSize = 48,
        FontAttributes = FontAttributes.Bold,
        TextColor = TextPrimary,
    };

    public static Label SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = 36,
        FontAttributes = FontAttributes.Bold,
        TextColor = TextPrimary,
        Margin = new Thickness(Gutter, 24, Gutter, 0),
    };

    public static Label Eyebrow(string text, Color? color = null) => new()
    {
        Text = text.ToUpperInvariant(),
        FontSize = 22,
        FontAttributes = FontAttributes.Bold,
        CharacterSpacing = 3,
        TextColor = color ?? Cyan,
    };

    public static Label Body(string text, double size = 28) => new()
    {
        Text = text,
        FontSize = size,
        TextColor = TextSecondary,
    };

    public static Label Caption(string text) => new()
    {
        Text = text,
        FontSize = 22,
        TextColor = TextMuted,
    };

    // ── Building blocks ──────────────────────────────────────────────────

    public static Border Card(View content, double radius = 24, Thickness? padding = null, Brush? background = null) => new()
    {
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = radius },
        Background = background ?? new SolidColorBrush(Surface),
        Padding = padding ?? new Thickness(32),
        Content = content,
    };

    /// <summary>A small rounded badge, e.g. "4K" or a rating.</summary>
    public static Border Pill(string text, Color background, Color? textColor = null) => new()
    {
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 10 },
        BackgroundColor = background,
        Padding = new Thickness(14, 6),
        HorizontalOptions = LayoutOptions.Start,
        Content = new Label
        {
            Text = text,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = textColor ?? Colors.White,
        },
    };

    /// <summary>Primary call-to-action: white with dark text, like the TV app's Play button.</summary>
    public static Button PrimaryButton(string text) => new()
    {
        Text = text,
        BackgroundColor = Colors.White,
        TextColor = Background,
        FontAttributes = FontAttributes.Bold,
        CornerRadius = 16,
        Padding = new Thickness(48, 20),
    };

    /// <summary>Secondary action: the system translucent button.</summary>
    public static Button SecondaryButton(string text) => new()
    {
        Text = text,
        CornerRadius = 16,
        Padding = new Thickness(40, 20),
    };

    public static ScrollView PageScroll(View content) => new()
    {
        Orientation = ScrollOrientation.Vertical,
        VerticalScrollBarVisibility = ScrollBarVisibility.Never,
        Content = content,
    };

    /// <summary>A labelled horizontal shelf of items: the TV app's row of posters.</summary>
    public static View Shelf(string title, CollectionView items) => new VerticalStackLayout
    {
        Spacing = 0,
        Children = { SectionTitle(title), items },
    };

    public static CollectionView HorizontalList(double itemSpacing = 36) => new()
    {
        ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal) { ItemSpacing = itemSpacing },
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        // Line the first item up with the page gutter; the row runs on to the screen edge.
        Margin = new Thickness(Gutter - Microsoft.Maui.Platform.TvOS.Handlers.CollectionViewHandler.FocusInset.Left, 0, 0, 0),
    };
}
