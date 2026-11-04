using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;

namespace Sample.Features.Tags;

public partial class TagEntryViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Nothing has happened yet";

    public ObservableCollection<string> Teams { get; } = ["design", "engineering"];
    public ObservableCollection<string> Recipients { get; } = [];
    public ObservableCollection<string> Phrases { get; } = [];
    public ObservableCollection<string> Limited { get; } = ["one", "two"];
    public ObservableCollection<string> Duplicates { get; } = ["echo", "echo"];
    public ObservableCollection<string> Validated { get; } = [];
    public ObservableCollection<string> Labels { get; } = ["release", "bugfix"];
    public ObservableCollection<string> Frozen { get; } = ["look", "no touching"];
    public ObservableCollection<string> Topics { get; } = ["getting-started"];

    /// <summary>
    /// Delimiter lists live here rather than in markup: a XAML array needs an x:Array and a type
    /// argument, which is a lot of ceremony for two commas.
    /// </summary>
    public IList<string> CommaOrSemicolon { get; } = [",", ";"];

    /// <summary>An empty list means Enter only — the way a tag gets to contain a comma.</summary>
    public IList<string> NoDelimiters { get; } = [];

    public string TeamSummary => this.Teams.Count == 0
        ? "No teams yet."
        : $"{this.Teams.Count} team(s): {String.Join(", ", this.Teams)}";

    public TagEntryViewModel()
        => this.Teams.CollectionChanged += (_, _) =>
        {
            this.OnPropertyChanged(nameof(this.TeamSummary));
            this.StatusMessage = $"Teams changed at {DateTime.Now:T}";
        };
}
