using Microsoft.Maui.Controls.Shapes;

namespace Sample;

/// <summary>
/// The landing page: a featured hero and horizontal shelves, as in the Apple TV app.
/// Each shelf is a horizontal CollectionView; the remote moves along a shelf, the page scrolls
/// between shelves, and Select opens the title.
/// </summary>
public class HomePage : ContentPage
{
    readonly Action<Genre> _browseGenre;

    public HomePage(Action<Genre> browseGenre, Action showControls)
    {
        _browseGenre = browseGenre;
        Title = "Home";
        Background = Theme.PageBackground;

        var continueWatching = Theme.HorizontalList();
        continueWatching.ItemsSource = Catalog.ContinueWatching.ToList();
        continueWatching.ItemTemplate = Templates.Wide(Open);

        var topPicks = Theme.HorizontalList();
        topPicks.ItemsSource = Catalog.TopPicks.ToList();
        topPicks.ItemTemplate = Templates.Poster(Open);

        // Genres use selection rather than a tap: SelectionChanged jumps to Browse.
        var genres = Theme.HorizontalList(24);
        genres.ItemsSource = Catalog.Genres.Skip(1).ToList();
        genres.ItemTemplate = Templates.GenreChip();
        genres.SelectionMode = SelectionMode.Single;
        genres.SelectionChanged += (s, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is Genre genre)
            {
                genres.SelectedItem = null;
                _browseGenre(genre);
            }
        };

        Content = Theme.PageScroll(new VerticalStackLayout
        {
            Spacing = 12,
            Padding = new Thickness(0, 8, 0, 80),
            Children =
            {
                Hero(Catalog.Featured, showControls),
                Theme.Shelf("Continue Watching", continueWatching),
                Theme.Shelf("Top Picks for You", topPicks),
                Theme.Shelf("Browse by Genre", genres),
            },
        });
    }

    View Hero(Show show, Action showControls)
    {
        var play = Theme.PrimaryButton("▶  Play");
        play.Clicked += (s, e) => Open(show);

        var explore = Theme.SecondaryButton("Explore Controls");
        explore.Clicked += (s, e) => showControls();

        var backdrop = new Image
        {
            Source = show.BackdropUrl,
            Aspect = Aspect.AspectFill,
            HeightRequest = 540,
            Opacity = 0.5,
        };

        // Darken the left side so the copy reads over any artwork.
        var leftScrim = new Grid
        {
            Background = new LinearGradientBrush(
                [
                    new GradientStop(Theme.Background.WithAlpha(0.95f), 0f),
                    new GradientStop(Theme.Background.WithAlpha(0.6f), 0.5f),
                    new GradientStop(Colors.Transparent, 1f),
                ],
                new Point(0, 0.5), new Point(1, 0.5)),
        };

        var copy = new VerticalStackLayout
        {
            Spacing = 18,
            Padding = new Thickness(72, 0),
            VerticalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 980,
            HorizontalOptions = LayoutOptions.Start,
            Children =
            {
                Theme.Eyebrow("Featured  ·  New season"),
                Theme.Display(show.Title),
                new HorizontalStackLayout
                {
                    Spacing = 14,
                    Children =
                    {
                        Theme.Pill(show.Rating, Colors.White.WithAlpha(0.18f)),
                        Theme.Pill("4K", Colors.White.WithAlpha(0.18f)),
                        Theme.Pill("HDR", Colors.White.WithAlpha(0.18f)),
                        new Label { Text = show.Meta, FontSize = 24, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center },
                    },
                },
                new Label { Text = show.Synopsis, FontSize = 30, TextColor = Colors.White.WithAlpha(0.85f) },
                new HorizontalStackLayout
                {
                    Spacing = 24,
                    Margin = new Thickness(0, 16, 0, 0),
                    Children = { play, explore },
                },
            },
        };

        return new Border
        {
            Margin = new Thickness(Theme.Gutter, 0),
            HeightRequest = 540,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 36 },
            Background = Theme.Gradient(show.To, Color.FromArgb("#0A0B12"), 20),
            Content = new Grid
            {
                Children =
                {
                    new Label
                    {
                        Text = show.Glyph,
                        FontSize = 280,
                        Opacity = 0.35,
                        HorizontalOptions = LayoutOptions.End,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 0, 120, 0),
                    },
                    backdrop,
                    leftScrim,
                    copy,
                },
            },
        };
    }

    async void Open(Show show) => await Navigation.PushAsync(new DetailPage(show));
}

/// <summary>A title's page: backdrop, details and actions. The remote's Menu button goes back.</summary>
public class DetailPage : ContentPage
{
    public DetailPage(Show show)
    {
        Title = show.Title;
        BackgroundColor = Theme.Background;

        var play = Theme.PrimaryButton(show.Progress > 0 ? $"▶  Resume" : "▶  Play");
        play.Clicked += async (s, e) =>
            await DisplayAlertAsync(show.Title, show.Progress > 0
                ? $"Resuming at {show.Progress:P0}. (This is a sample — there's no video.)"
                : "Starting from the beginning. (This is a sample — there's no video.)", "OK");

        var myList = Theme.SecondaryButton(ListText(show));
        myList.Clicked += (s, e) =>
        {
            show.InMyList = !show.InMyList;
            myList.Text = ListText(show);
        };

        var more = Theme.HorizontalList();
        more.ItemsSource = Catalog.Shows.Where(s => s.Genre == show.Genre && s != show)
            .Concat(Catalog.Shows.Where(s => s.Genre != show.Genre).Take(4))
            .ToList();
        more.ItemTemplate = Templates.Poster(async s => await Navigation.PushAsync(new DetailPage(s)));

        var header = new Grid
        {
            HeightRequest = 800,
            Children =
            {
                new Image
                {
                    Source = show.BackdropUrl,
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 800,
                    Opacity = 0.55,
                },
                // Fade the backdrop into the page.
                new Grid
                {
                    Background = new LinearGradientBrush(
                        [new GradientStop(Colors.Transparent, 0.55f), new GradientStop(Theme.Background, 1f)],
                        new Point(0, 0), new Point(0, 1)),
                },
                new Grid
                {
                    Background = new LinearGradientBrush(
                        [
                            new GradientStop(Theme.Background.WithAlpha(0.95f), 0f),
                            new GradientStop(Theme.Background.WithAlpha(0.3f), 0.7f),
                            new GradientStop(Colors.Transparent, 1f),
                        ],
                        new Point(0, 0.5), new Point(1, 0.5)),
                },
                new VerticalStackLayout
                {
                    Spacing = 20,
                    Padding = new Thickness(Theme.Gutter, 60, Theme.Gutter, 64),
                    VerticalOptions = LayoutOptions.End,
                    MaximumWidthRequest = 1100,
                    HorizontalOptions = LayoutOptions.Start,
                    Children =
                    {
                        Theme.Eyebrow(show.Genre, show.From),
                        Theme.Display(show.Title),
                        new HorizontalStackLayout
                        {
                            Spacing = 14,
                            Children =
                            {
                                Theme.Pill(show.Rating, Colors.White.WithAlpha(0.18f)),
                                Theme.Pill("4K", Colors.White.WithAlpha(0.18f)),
                                Theme.Pill("Dolby Atmos", Colors.White.WithAlpha(0.18f)),
                                new Label { Text = show.Meta, FontSize = 24, TextColor = Theme.TextSecondary, VerticalOptions = LayoutOptions.Center },
                            },
                        },
                        new Label { Text = show.Synopsis, FontSize = 32, TextColor = Colors.White.WithAlpha(0.85f) },
                        new HorizontalStackLayout
                        {
                            Spacing = 24,
                            Margin = new Thickness(0, 12, 0, 0),
                            Children = { play, myList },
                        },
                    },
                },
            },
        };

        Content = Theme.PageScroll(new VerticalStackLayout
        {
            Spacing = 12,
            Padding = new Thickness(0, 0, 0, 60),
            Children =
            {
                header,
                Theme.Shelf("More Like This", more),
                new Label
                {
                    Text = "Press  MENU  on the remote to go back",
                    FontSize = 22,
                    TextColor = Theme.TextMuted,
                    Margin = new Thickness(Theme.Gutter, 24, Theme.Gutter, 0),
                },
            },
        });
    }

    static string ListText(Show show) => show.InMyList ? "✓  In My List" : "＋  My List";
}
