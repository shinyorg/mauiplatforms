using System.Collections.ObjectModel;

namespace Sample;

/// <summary>
/// A grid CollectionView with multiple selection: Select toggles a title in "My List".
/// The genre row above it is a single-selection CollectionView that filters the grid.
/// </summary>
public class BrowsePage : ContentPage
{
    readonly CollectionView _genres;
    readonly CollectionView _grid;
    readonly ObservableCollection<Show> _shown = new();
    readonly Label _status;
    readonly Label _heading;

    public BrowsePage()
    {
        Title = "Browse";
        Background = Theme.PageBackground;

        _heading = Theme.Title("All Titles");
        _status = Theme.Body("");

        _genres = Theme.HorizontalList(24);
        _genres.ItemsSource = Catalog.Genres;
        _genres.ItemTemplate = Templates.GenreChip();
        _genres.SelectionMode = SelectionMode.Single;
        _genres.SelectedItem = Catalog.Genres[0];
        _genres.SelectionChanged += (s, e) => Filter();

        _grid = new CollectionView
        {
            ItemsSource = _shown,
            ItemTemplate = Templates.Poster(),
            ItemsLayout = new GridItemsLayout(6, ItemsLayoutOrientation.Vertical)
            {
                HorizontalItemSpacing = 32,
                VerticalItemSpacing = 40,
            },
            SelectionMode = SelectionMode.Multiple,
            Margin = new Thickness(Theme.Gutter - Microsoft.Maui.Platform.TvOS.Handlers.CollectionViewHandler.FocusInset.Left, 0),
            EmptyView = "Nothing here yet.",
        };
        _grid.SelectionChanged += OnGridSelectionChanged;

        Content = Theme.PageScroll(new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(0, 8, 0, 80),
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 6,
                    Padding = new Thickness(Theme.Gutter, 0),
                    Children =
                    {
                        Theme.Eyebrow("Library"),
                        _heading,
                        _status,
                    },
                },
                _genres,
                _grid,
            },
        });

        Filter();
    }

    /// <summary>Shows one genre's titles; set from Home's genre shelf.</summary>
    public void ShowGenre(Genre genre)
    {
        _genres.SelectedItem = Catalog.Genres.FirstOrDefault(g => g.Name == genre.Name) ?? Catalog.Genres[0];
    }

    void Filter()
    {
        var genre = _genres.SelectedItem as Genre ?? Catalog.Genres[0];
        _heading.Text = genre.Name == "All" ? "All Titles" : genre.Name;

        _shown.Clear();
        foreach (var show in Catalog.Shows.Where(s => genre.Name == "All" || s.Genre == genre.Name))
            _shown.Add(show);

        // Keep the grid's selection in step with My List for the titles now shown.
        _grid.SelectionChanged -= OnGridSelectionChanged;
        _grid.SelectedItems.Clear();
        foreach (var show in _shown.Where(s => s.InMyList))
            _grid.SelectedItems.Add(show);
        _grid.SelectionChanged += OnGridSelectionChanged;

        UpdateStatus();
    }

    void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (var show in e.PreviousSelection.OfType<Show>().Except(e.CurrentSelection.OfType<Show>()))
            show.InMyList = false;
        foreach (var show in e.CurrentSelection.OfType<Show>())
            show.InMyList = true;
        UpdateStatus();
    }

    void UpdateStatus()
    {
        var count = Catalog.Shows.Count(s => s.InMyList);
        _status.Text = $"{_shown.Count} titles  ·  {count} in My List  ·  press Select to add or remove";
    }
}
