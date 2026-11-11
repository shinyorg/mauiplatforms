using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Walkthrough;

public partial class WalkthroughViewModel : ObservableObject
{
    [ObservableProperty] string status = "The tour auto-starts once. Use Restart to see it again.";
    [ObservableProperty] bool isTouring;
    [ObservableProperty] bool useOverlay = true;
    [ObservableProperty] bool showAdvanced;
    [ObservableProperty] string searchText = string.Empty;
    [ObservableProperty] int saveCount;


    /// <summary>Bound to the advanced step's <c>IsVisible</c> — an unticked box drops it from the run.</summary>
    partial void OnShowAdvancedChanged(bool value)
        => this.Status = value
            ? "The Advanced step is now part of the tour."
            : "The Advanced step drops out of the tour.";


    [RelayCommand]
    void StepEntered(object? parameter) => this.Status = $"Entered step: {parameter ?? "(unnamed)"}";

    [RelayCommand]
    void Completed() => this.Status = "Tour completed — it will not auto-start again.";

    [RelayCommand]
    void Skipped() => this.Status = "Tour skipped. Restart clears the remembered flag.";

    [RelayCommand]
    void Save()
    {
        this.SaveCount++;
        this.Status = $"Saved {this.SaveCount} time(s).";
    }
}
