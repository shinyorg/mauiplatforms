using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Walkthrough;

public partial class TooltipViewModel : ObservableObject
{
    [ObservableProperty] bool showHint;
    [ObservableProperty] string status = "Long-press, hover or focus a control to see its tooltip.";

    [RelayCommand]
    void HintTapped()
    {
        this.ShowHint = false;
        this.Status = "The bound tooltip was tapped, which ran its Command and closed it.";
    }

    [RelayCommand]
    void ToggleHint() => this.ShowHint = !this.ShowHint;
}
