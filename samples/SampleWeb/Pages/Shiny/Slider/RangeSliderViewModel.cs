using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls;

namespace Sample.Features.Slider;

public partial class RangeSliderViewModel : ObservableObject
{
    [ObservableProperty] double priceLower = 250;
    [ObservableProperty] double priceUpper = 750;

    [ObservableProperty] double tempLower = 18;
    [ObservableProperty] double tempUpper = 24;

    [ObservableProperty] double bookingLower = 2;
    [ObservableProperty] double bookingUpper = 5;

    [ObservableProperty] double ageLower = 21;
    [ObservableProperty] double ageUpper = 65;

    [ObservableProperty] double shiftStart = 9;
    [ObservableProperty] double shiftEnd = 17;

    [ObservableProperty] double searchLower = 100;
    [ObservableProperty] double searchUpper = 500;
    [ObservableProperty] string committedSearch = "Searched: $100 – $500 (0 searches)";
    int searchCount;

    // Runs once per release, not on every step of the drag.
    [RelayCommand]
    void Search(SliderRange range) => this.CommittedSearch = $"Searched: {range.Lower:C0} – {range.Upper:C0} ({++this.searchCount} searches)";
}
