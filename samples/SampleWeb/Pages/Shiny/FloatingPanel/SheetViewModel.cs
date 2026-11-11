using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sample.Controls;
using Shiny.Maui.Controls;

using Shiny;

namespace Sample.Features.FloatingPanel;

public partial class SheetViewModel : ObservableObject
{
    [ObservableProperty]
    bool isSheetOpen;

    [ObservableProperty]
    string entryText = string.Empty;

    [ObservableProperty]
    string editorText = string.Empty;

    [ObservableProperty]
    string statusMessage = "Sheet is closed";

    [RelayCommand]
    void OpenSheet() => IsSheetOpen = true;

    [RelayCommand]
    void CloseSheet() => IsSheetOpen = false;

    [RelayCommand]
    void ToggleSheet() => IsSheetOpen = !IsSheetOpen;

    partial void OnIsSheetOpenChanged(bool value)
    {
        StatusMessage = value ? "Sheet is open" : "Sheet is closed";
    }

    // -- Top Sheet --

    [ObservableProperty]
    bool isTopSheetOpen;

    [RelayCommand]
    void OpenTopSheet() => IsTopSheetOpen = true;

    [RelayCommand]
    void CloseTopSheet() => IsTopSheetOpen = false;

    // -- Locked: Signature --

    [ObservableProperty]
    bool isSignatureOpen;

    [ObservableProperty]
    ImageSource? signatureImage;

    public bool HasSignature => SignatureImage != null;
    public bool HasNoSignature => SignatureImage == null;

    [RelayCommand]
    void OpenSignature() => IsSignatureOpen = true;

    [RelayCommand]
    void CancelSignature() => IsSignatureOpen = false;

    [RelayCommand]
    void ClearSignature(DrawingCanvas canvas)
    {
        canvas.Clear();
    }

    [RelayCommand]
    void DoneSignature(DrawingCanvas canvas)
    {
        var stream = canvas.ExportToPng(300, 150);
        if (stream != null)
        {
            SignatureImage = ImageSource.FromStream(() => stream);
            OnPropertyChanged(nameof(HasSignature));
            OnPropertyChanged(nameof(HasNoSignature));
        }
        canvas.Clear();
        IsSignatureOpen = false;
    }

    // -- Locked: Selector --

    public ObservableCollection<DetentValue> SelectorDetents { get; } = new()
    {
        DetentValue.Half
    };

    [ObservableProperty]
    bool isSelectorOpen;

    [ObservableProperty]
    string? selectedCountry;

    public bool HasSelection => SelectedCountry != null;

    public ObservableCollection<string> Countries { get; } = new(new[]
    {
        "Argentina", "Australia", "Austria", "Belgium", "Brazil",
        "Canada", "Chile", "China", "Colombia", "Czech Republic",
        "Denmark", "Egypt", "Finland", "France", "Germany",
        "Greece", "Hungary", "India", "Indonesia", "Ireland",
        "Israel", "Italy", "Japan", "Kenya", "Malaysia",
        "Mexico", "Netherlands", "New Zealand", "Nigeria", "Norway",
        "Peru", "Philippines", "Poland", "Portugal", "Romania",
        "Russia", "Saudi Arabia", "Singapore", "South Africa", "South Korea",
        "Spain", "Sweden", "Switzerland", "Thailand", "Turkey",
        "Ukraine", "United Arab Emirates", "United Kingdom", "United States", "Vietnam"
    });

    [RelayCommand]
    void OpenSelector() => IsSelectorOpen = true;

    [RelayCommand]
    void CancelSelector() => IsSelectorOpen = false;

    [RelayCommand]
    void SelectCountry(object? item)
    {
        if (item is string selected)
        {
            SelectedCountry = selected;
            OnPropertyChanged(nameof(HasSelection));
            IsSelectorOpen = false;
        }
    }
}
