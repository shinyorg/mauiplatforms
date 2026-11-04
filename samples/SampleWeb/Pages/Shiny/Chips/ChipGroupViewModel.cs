using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;

namespace Sample.Features.Chips;

/// <summary>One item that is more than its own name — what DisplayMemberPath and ItemTemplate are for.</summary>
public record Team(string Name, int Members);


public partial class ChipGroupViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Nothing has been picked yet";

    [ObservableProperty]
    object? range;

    [ObservableProperty]
    Team? team;

    public ObservableCollection<string> Ranges { get; } = ["Day", "Week", "Month", "Quarter", "Year"];
    public ObservableCollection<string> Filters { get; } = ["Open", "In progress", "Blocked", "In review", "Done"];
    public ObservableCollection<string> Toppings { get; } = ["Cheese", "Mushroom", "Olive", "Pepper", "Onion"];
    public ObservableCollection<string> Actions { get; } = ["Share", "Duplicate", "Archive"];
    public ObservableCollection<string> Recipients { get; } = ["design", "engineering", "sales"];
    public ObservableCollection<string> Frozen { get; } = ["Approved", "Signed"];
    public ObservableCollection<string> Languages { get; } =
        ["C#", "F#", "TypeScript", "Swift", "Kotlin", "Rust", "Go", "Python", "Dart", "Java"];

    public ObservableCollection<Team> Teams { get; } =
        [new("Design", 6), new("Engineering", 24), new("Sales", 11)];

    /// <summary>Bound two-way to the multi-select group — the control writes into this very instance.</summary>
    public ObservableCollection<object> SelectedFilters { get; } = [];

    public ObservableCollection<object> SelectedToppings { get; } = [];

    public string FilterSummary => this.SelectedFilters.Count == 0
        ? "No filters."
        : String.Join(", ", this.SelectedFilters);

    public ChipGroupViewModel()
        => this.SelectedFilters.CollectionChanged += (_, _) => this.OnPropertyChanged(nameof(this.FilterSummary));


    partial void OnRangeChanged(object? value)
        => this.StatusMessage = value is null ? "No range picked" : $"Range: {value}";

    partial void OnTeamChanged(Team? value)
        => this.StatusMessage = value is null ? "No team picked" : $"Team: {value.Name} ({value.Members})";
}
