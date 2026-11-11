using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.FloatingPanel;

public partial class DualPanelViewModel : ObservableObject
{
    [ObservableProperty]
    bool isTopOpen;

    [ObservableProperty]
    bool isBottomOpen;

    [ObservableProperty]
    string statusMessage = "Both panels have peek headers";

    [RelayCommand]
    void OpenTop() => IsTopOpen = true;

    [RelayCommand]
    void CloseTop() => IsTopOpen = false;

    [RelayCommand]
    void OpenBottom() => IsBottomOpen = true;

    [RelayCommand]
    void CloseBottom() => IsBottomOpen = false;

    [RelayCommand]
    void OpenBoth()
    {
        IsTopOpen = true;
        IsBottomOpen = true;
    }

    [RelayCommand]
    void CloseBoth()
    {
        IsTopOpen = false;
        IsBottomOpen = false;
    }

    partial void OnIsTopOpenChanged(bool value) => UpdateStatus();
    partial void OnIsBottomOpenChanged(bool value) => UpdateStatus();

    void UpdateStatus()
    {
        StatusMessage = (IsTopOpen, IsBottomOpen) switch
        {
            (true, true) => "Both panels open",
            (true, false) => "Top panel open",
            (false, true) => "Bottom panel open",
            _ => "Both panels have peek headers"
        };
    }
}
