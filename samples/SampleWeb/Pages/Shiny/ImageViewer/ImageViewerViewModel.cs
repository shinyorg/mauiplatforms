using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.ImageViewer;

public partial class ImageViewerViewModel : ObservableObject
{
    [ObservableProperty]
    bool isViewerOpen;

    /// <summary>What the Opened/Closed commands have written - the sample's way of showing that
    /// both ends of the overlay report back.</summary>
    [ObservableProperty]
    string lastViewerEvent = "The viewer has not been opened yet";

    [RelayCommand]
    void CloseViewer() => IsViewerOpen = false;

    [RelayCommand]
    void ViewerOpened() => LastViewerEvent = "Opened";

    [RelayCommand]
    void ViewerClosed() => LastViewerEvent = "Closed";
}
