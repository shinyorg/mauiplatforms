using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny.Maui.Controls;
using Shiny;

namespace Sample.Features.Slider;

public partial class SliderViewModel : ObservableObject
{
    [ObservableProperty] double temperature = 50;
    [ObservableProperty] double intensity = 5;
    [ObservableProperty] double volume = 30;
    [ObservableProperty] double opacity = 0.75;
    [ObservableProperty] double fineValue = 2.5;
    [ObservableProperty] double quality = 2;
    [ObservableProperty] double plan = 50;
    [ObservableProperty] double pressure = 32;
    [ObservableProperty] double bass = 6;
    [ObservableProperty] double treble = 5;
    [ObservableProperty] double brightness = 60;
    [ObservableProperty] double warmth = 35;
    [ObservableProperty] double position = 40;
    [ObservableProperty] double setpoint = 40;
    [ObservableProperty] string committedSetpoint = "Committed: 40 (0 commits)";
    int setpointCommits;

    // Runs once per release, not on every step of the drag.
    [RelayCommand]
    void CommitSetpoint(double value) => this.CommittedSetpoint = $"Committed: {value:0} ({++this.setpointCommits} commits)";

    // Bound through MarksSource — numbers become unlabelled stops, SliderMarks keep their text and colour.
    public ObservableCollection<SliderMark> Bookmarks { get; } =
    [
        new() { Value = 20, Text = "Intro" },
        new() { Value = 65, Text = "Chorus", Color = Color.FromArgb("#F59E0B") }
    ];

    [RelayCommand]
    void AddBookmark() => this.Bookmarks.Add(new SliderMark { Value = Math.Round(this.Position), Text = $"#{this.Bookmarks.Count + 1}" });

    [RelayCommand]
    void ClearBookmarks() => this.Bookmarks.Clear();
}
