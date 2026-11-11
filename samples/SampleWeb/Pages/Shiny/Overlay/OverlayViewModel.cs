using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.Overlay;

public partial class OverlayViewModel : ObservableObject
{
    [ObservableProperty] bool isCustomOverlayShown;
    [ObservableProperty] bool isLoadingShown;
    [ObservableProperty] bool isIndeterminate = true;
    [ObservableProperty] double progress;
    [ObservableProperty] string? loadingMessage;

    // Built-in page-level loading overlay (ShinyContentPage.IsLoading)
    [ObservableProperty] bool isPageLoading;

    [RelayCommand]
    async Task ShowPageLoading()
    {
        IsPageLoading = true;
        await Task.Delay(2500);
        IsPageLoading = false;
    }

    [RelayCommand]
    void ShowCustomOverlay() => IsCustomOverlayShown = true;

    [RelayCommand]
    void DismissCustomOverlay() => IsCustomOverlayShown = false;

    [RelayCommand]
    async Task ShowIndeterminate()
    {
        IsIndeterminate = true;
        LoadingMessage = "Loading, please wait...";
        IsLoadingShown = true;

        await Task.Delay(3000);
        IsLoadingShown = false;
    }

    [RelayCommand]
    async Task ShowDeterminate()
    {
        IsIndeterminate = false;
        Progress = 0;
        LoadingMessage = "Downloading...";
        IsLoadingShown = true;

        for (var i = 0; i <= 100; i += 5)
        {
            Progress = i;
            await Task.Delay(100);
        }

        LoadingMessage = "Complete!";
        await Task.Delay(500);
        IsLoadingShown = false;
    }
}
