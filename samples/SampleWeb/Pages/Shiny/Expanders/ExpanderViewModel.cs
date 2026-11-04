using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls;

namespace Sample.Features.Expanders;

/// <summary>One row of the data-driven accordion at the bottom of the page.</summary>
public record FaqItem(string Question, string Answer, string Category);

public partial class ExpanderViewModel : ObservableObject
{
    [ObservableProperty] bool useFade = true;
    [ObservableProperty] bool useSlide;
    [ObservableProperty] bool useHeight = true;

    [ObservableProperty] ExpanderSlideFrom slideFrom = ExpanderSlideFrom.Top;
    [ObservableProperty] ExpandDirection direction = ExpandDirection.Down;
    [ObservableProperty] ExpanderIndicatorMode indicatorMode = ExpanderIndicatorMode.Rotate;
    [ObservableProperty] ExpanderIndicatorPosition indicatorPosition = ExpanderIndicatorPosition.End;
    [ObservableProperty] double duration = 250;

    [ObservableProperty] AccordionSelectionMode selectionMode = AccordionSelectionMode.Single;
    [ObservableProperty] bool allowCollapseAll = true;
    [ObservableProperty] int expandedIndex = -1;
    [ObservableProperty] string lastEvent = "nothing yet";

    public ExpanderSlideFrom[] SlideFroms { get; } = Enum.GetValues<ExpanderSlideFrom>();
    public ExpandDirection[] Directions { get; } = Enum.GetValues<ExpandDirection>();
    public ExpanderIndicatorMode[] IndicatorModes { get; } = Enum.GetValues<ExpanderIndicatorMode>();
    public ExpanderIndicatorPosition[] IndicatorPositions { get; } = Enum.GetValues<ExpanderIndicatorPosition>();
    public AccordionSelectionMode[] SelectionModes { get; } = Enum.GetValues<AccordionSelectionMode>();

    /// <summary>The flags the playground expander runs with, recomputed as the three switches move.</summary>
    public ExpanderAnimation Animation
    {
        get
        {
            var animation = ExpanderAnimation.None;
            if (this.UseFade)
                animation |= ExpanderAnimation.Fade;
            if (this.UseSlide)
                animation |= ExpanderAnimation.Slide;
            if (this.UseHeight)
                animation |= ExpanderAnimation.Height;
            return animation;
        }
    }

    public uint DurationMs => (uint)this.Duration;

    public ObservableCollection<FaqItem> Faqs { get; } =
    [
        new("How do I install it?", "Add the Shiny.Maui.Controls NuGet package and call UseShinyControls() in MauiProgram.", "Getting started"),
        new("Does it work on Windows?", "Yes — iOS, Android, Mac Catalyst, macOS, Windows and GTK4 on Linux.", "Platforms"),
        new("Can I theme it?", "Every colour and size falls back to a theme token, so a theme swap reaches inside the control.", "Theming"),
        new("Is the source available?", "MIT licensed, on GitHub at shinyorg/controls.", "Licensing")
    ];

    partial void OnUseFadeChanged(bool value) => this.OnPropertyChanged(nameof(this.Animation));
    partial void OnUseSlideChanged(bool value) => this.OnPropertyChanged(nameof(this.Animation));
    partial void OnUseHeightChanged(bool value) => this.OnPropertyChanged(nameof(this.Animation));
    partial void OnDurationChanged(double value) => this.OnPropertyChanged(nameof(this.DurationMs));

    [RelayCommand]
    void ItemExpanded(object? data)
        => this.LastEvent = data is FaqItem faq ? $"opened \"{faq.Question}\"" : $"opened {data}";
}
