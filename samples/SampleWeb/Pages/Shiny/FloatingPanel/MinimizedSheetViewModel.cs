using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.FloatingPanel;

public partial class MinimizedSheetViewModel : ObservableObject
{
    [ObservableProperty]
    bool isSheetOpen;

    [ObservableProperty]
    string statusMessage = "Music player header peeks from the bottom";

    [RelayCommand]
    void OpenSheet() => IsSheetOpen = true;

    [RelayCommand]
    void CloseSheet() => IsSheetOpen = false;

    partial void OnIsSheetOpenChanged(bool value)
    {
        StatusMessage = value ? "Music player is open" : "Music player header peeks from the bottom";
    }
}
