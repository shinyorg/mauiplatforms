using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sample;

/// <summary>A title in the sample's made-up streaming catalog.</summary>
public class Show : INotifyPropertyChanged
{
    bool _inMyList;

    public required string Title { get; init; }
    public required string Genre { get; init; }
    public required int Year { get; init; }
    public required string Rating { get; init; }
    public required string Runtime { get; init; }
    public required string Synopsis { get; init; }
    public required Color From { get; init; }
    public required Color To { get; init; }
    public required string Glyph { get; init; }
    public double Progress { get; init; }

    /// <summary>Artwork seed; the image is served by picsum.photos.</summary>
    public string Seed => Title.Replace(" ", "").ToLowerInvariant();

    public string PosterUrl => $"https://picsum.photos/seed/{Seed}/400/600";
    public string BackdropUrl => $"https://picsum.photos/seed/{Seed}-wide/1280/720";

    public string Meta => $"{Year}  ·  {Genre}  ·  {Runtime}";

    public Brush Art => Theme.Gradient(From, To);

    public bool InMyList
    {
        get => _inMyList;
        set { if (_inMyList != value) { _inMyList = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public record Genre(string Name, string Glyph, Color Color);

public static class Catalog
{
    public static IReadOnlyList<Genre> Genres { get; } =
    [
        new("All", "✦", Theme.Violet),
        new("Sci-Fi", "🚀", Theme.Cyan),
        new("Drama", "🎭", Theme.Pink),
        new("Comedy", "😄", Theme.Amber),
        new("Nature", "🌿", Theme.Emerald),
        new("Mystery", "🔍", Theme.Coral),
    ];

    public static IReadOnlyList<Show> Shows { get; } =
    [
        Make("Orbital", "Sci-Fi", 2025, "TV-14", "8 episodes", "#22D3EE", "#4F46E5", "🛰️", 0.62,
            "A crew of six keeps a failing space station alive while the planet below stops answering their calls."),
        Make("Night Market", "Drama", 2024, "TV-MA", "2h 4m", "#F472B6", "#7C3AED", "🏮", 0.28,
            "Three generations run a food stall in a city that never sleeps, and never stops changing."),
        Make("Tidewater", "Nature", 2023, "TV-G", "6 episodes", "#34D399", "#0E7490", "🌊", 0.85,
            "Follow the life of a coastline through one year of storms, migrations and quiet mornings."),
        Make("The Quiet Hour", "Mystery", 2025, "TV-14", "1h 52m", "#FB7185", "#1E1B4B", "🕯️", 0,
            "When the lighthouse goes dark, the island's only detective has until dawn to find out why."),
        Make("Big Little Kitchen", "Comedy", 2024, "TV-PG", "10 episodes", "#FBBF24", "#EA580C", "🍳", 0.4,
            "A tiny restaurant, a huge ego and one very patient sous-chef."),
        Make("Signal", "Sci-Fi", 2023, "TV-14", "1h 58m", "#818CF8", "#0F172A", "📡", 0,
            "A radio astronomer hears a message she was never meant to receive, in her own voice."),
        Make("Wild Ridge", "Nature", 2025, "TV-G", "5 episodes", "#A3E635", "#166534", "🏔️", 0,
            "High above the tree line, every animal has a plan for winter."),
        Make("Paper Cranes", "Drama", 2022, "TV-PG", "1h 41m", "#FDA4AF", "#9D174D", "🕊️", 0,
            "A retired teacher folds a thousand cranes for the students she never forgot."),
        Make("Room 404", "Comedy", 2025, "TV-14", "12 episodes", "#F9A8D4", "#6D28D9", "🛎️", 0.15,
            "The hotel's worst room has the best stories."),
        Make("Deep Field", "Sci-Fi", 2024, "TV-PG", "2h 12m", "#67E8F9", "#1E3A8A", "🔭", 0,
            "An unlikely team builds a telescope that sees further back than anyone thought possible."),
        Make("Ashfall", "Mystery", 2023, "TV-MA", "8 episodes", "#FCA5A5", "#7F1D1D", "🌋", 0,
            "A volcano wakes, a town empties, and one house still has its lights on."),
        Make("Garden Party", "Comedy", 2023, "TV-PG", "1h 35m", "#86EFAC", "#CA8A04", "🌻", 0,
            "Nothing goes wrong at the village flower show. Almost nothing."),
    ];

    public static IEnumerable<Show> ContinueWatching => Shows.Where(s => s.Progress > 0);

    public static IEnumerable<Show> TopPicks => Shows.OrderBy(s => s.Title.Length % 5).ThenBy(s => s.Title).Take(8);

    public static Show Featured => Shows[0];

    static Show Make(string title, string genre, int year, string rating, string runtime, string from, string to, string glyph, double progress, string synopsis) => new()
    {
        Title = title,
        Genre = genre,
        Year = year,
        Rating = rating,
        Runtime = runtime,
        Synopsis = synopsis,
        From = Color.FromArgb(from),
        To = Color.FromArgb(to),
        Glyph = glyph,
        Progress = progress,
    };
}
