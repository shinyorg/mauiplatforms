using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;

namespace Sample.Features.Fab;

public partial class FabViewModel : ObservableObject
{
    [ObservableProperty]
    bool isMenuOpen;

    [ObservableProperty]
    string statusMessage = "Tap the FAB in the bottom-right";

    [ObservableProperty]
    int tapCount;

    [RelayCommand]
    void Add()
    {
        TapCount++;
        StatusMessage = $"Add tapped ({TapCount})";
    }

    [RelayCommand]
    void Share() => StatusMessage = "Share tapped";

    [RelayCommand]
    void Edit() => StatusMessage = "Edit tapped";

    [RelayCommand]
    void Delete() => StatusMessage = "Delete tapped";

    [RelayCommand]
    void ToggleMenu() => IsMenuOpen = !IsMenuOpen;
}
