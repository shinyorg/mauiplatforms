using Microsoft.Maui.Controls.Shapes;

namespace Sample;

/// <summary>Item templates for the catalog's CollectionViews.</summary>
public static class Templates
{
    public const double PosterWidth = 260;
    public const double PosterHeight = 390;

    /// <summary>
    /// A 2:3 poster: gradient art with the title's glyph, the remote image faded in on top,
    /// and a "My List" badge. The "Selected" visual state outlines it.
    /// </summary>
    public static DataTemplate Poster(Action<Show>? onTap = null) => new(() =>
    {
        var glyph = new Label
        {
            FontSize = 96,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        glyph.SetBinding(Label.TextProperty, nameof(Show.Glyph));

        var image = new Image
        {
            Aspect = Aspect.AspectFill,
            WidthRequest = PosterWidth,
            HeightRequest = PosterHeight,
            Opacity = 0.9,
        };
        image.SetBinding(Image.SourceProperty, nameof(Show.PosterUrl));

        var title = new Label
        {
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
        title.SetBinding(Label.TextProperty, nameof(Show.Title));

        var genre = new Label { FontSize = 20, TextColor = Colors.White.WithAlpha(0.75f) };
        genre.SetBinding(Label.TextProperty, nameof(Show.Genre));

        var badge = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Theme.Emerald,
            WidthRequest = 44,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(14),
            Content = new Label
            {
                Text = "✓",
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                TextColor = Theme.Background,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        };
        badge.SetBinding(VisualElement.IsVisibleProperty, nameof(Show.InMyList));

        var card = new Border
        {
            WidthRequest = PosterWidth,
            HeightRequest = PosterHeight,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = new Grid
            {
                Children =
                {
                    glyph,
                    image,
                    new Grid { Background = Theme.Scrim },
                    new VerticalStackLayout
                    {
                        Spacing = 2,
                        Padding = new Thickness(20, 0, 20, 18),
                        VerticalOptions = LayoutOptions.End,
                        Children = { title, genre },
                    },
                    badge,
                },
            },
        };
        card.SetBinding(Border.BackgroundProperty, nameof(Show.Art));
        AddSelectionStates(card, Theme.Emerald);
        AddTap(card, onTap);
        return card;
    });

    /// <summary>A 16:9 "continue watching" card with a progress bar.</summary>
    public static DataTemplate Wide(Action<Show>? onTap = null) => new(() =>
    {
        const double width = 480, height = 270;

        var glyph = new Label
        {
            FontSize = 110,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        glyph.SetBinding(Label.TextProperty, nameof(Show.Glyph));

        var image = new Image { Aspect = Aspect.AspectFill, WidthRequest = width, HeightRequest = height, Opacity = 0.85 };
        image.SetBinding(Image.SourceProperty, nameof(Show.BackdropUrl));

        var title = new Label { FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
        title.SetBinding(Label.TextProperty, nameof(Show.Title));

        var progress = new ProgressBar { ProgressColor = Colors.White, HeightRequest = 6 };
        progress.SetBinding(ProgressBar.ProgressProperty, nameof(Show.Progress));

        var card = new Border
        {
            WidthRequest = width,
            HeightRequest = height,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Content = new Grid
            {
                Children =
                {
                    glyph,
                    image,
                    new Grid { Background = Theme.Scrim },
                    new VerticalStackLayout
                    {
                        Spacing = 12,
                        Padding = new Thickness(24, 0, 24, 22),
                        VerticalOptions = LayoutOptions.End,
                        Children = { title, progress },
                    },
                },
            },
        };
        card.SetBinding(Border.BackgroundProperty, nameof(Show.Art));
        AddTap(card, onTap);
        return card;
    });

    /// <summary>A genre tile: the genre's color, glyph and name. Selected genres get a ring.</summary>
    public static DataTemplate GenreChip() => new(() =>
    {
        var glyph = new Label { FontSize = 40, VerticalOptions = LayoutOptions.Center };
        glyph.SetBinding(Label.TextProperty, nameof(Genre.Glyph));

        var name = new Label
        {
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
        };
        name.SetBinding(Label.TextProperty, nameof(Genre.Name));

        var chip = new Border
        {
            HeightRequest = 96,
            MinimumWidthRequest = 200,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            BackgroundColor = Theme.SurfaceRaised,
            Padding = new Thickness(28, 0, 36, 0),
            Content = new HorizontalStackLayout
            {
                Spacing = 16,
                Children = { glyph, name },
            },
        };
        AddSelectionStates(chip, Colors.White, selectedBackground: true);
        return chip;
    });

    /// <summary>
    /// Visual states CollectionView drives on tvOS: "Selected" when the item is selected and
    /// "Focused" while the remote is on it (focused items also lift, from the backend).
    /// </summary>
    static void AddSelectionStates(Border border, Color ring, bool selectedBackground = false)
    {
        var normal = new VisualState { Name = "Normal" };
        normal.Setters.Add(new Setter { Property = Border.StrokeThicknessProperty, Value = 0.0 });

        var selected = new VisualState { Name = "Selected" };
        selected.Setters.Add(new Setter { Property = Border.StrokeProperty, Value = new SolidColorBrush(ring) });
        selected.Setters.Add(new Setter { Property = Border.StrokeThicknessProperty, Value = 5.0 });
        if (selectedBackground)
            selected.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Theme.Violet });

        var focused = new VisualState { Name = "Focused" };
        if (selectedBackground)
            focused.Setters.Add(new Setter { Property = VisualElement.BackgroundColorProperty, Value = Color.FromArgb("#3A3F58") });

        var group = new VisualStateGroup { Name = "CommonStates" };
        group.States.Add(normal);
        group.States.Add(selected);
        group.States.Add(focused);
        VisualStateManager.GetVisualStateGroups(border).Add(group);
    }

    static void AddTap(View view, Action<Show>? onTap)
    {
        if (onTap == null)
            return;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (s is BindableObject { BindingContext: Show show })
                onTap(show);
        };
        view.GestureRecognizers.Add(tap);
    }
}
