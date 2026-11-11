using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.FloatingPanel;

public partial class TopSheetViewModel : ObservableObject
{
    [ObservableProperty]
    bool isSheetOpen;

    [ObservableProperty]
    string statusMessage = "Weather header peeks from the top";

    [RelayCommand]
    void OpenSheet() => IsSheetOpen = true;

    [RelayCommand]
    void CloseSheet() => IsSheetOpen = false;

    partial void OnIsSheetOpenChanged(bool value)
    {
        StatusMessage = value ? "Weather details are open" : "Weather header peeks from the top";
    }
}
