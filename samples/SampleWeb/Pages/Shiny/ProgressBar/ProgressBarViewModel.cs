using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.ProgressBar;

public partial class ProgressBarViewModel : ObservableObject
{
    [ObservableProperty] double basicValue = 45;
    [ObservableProperty] double pulseValue = 30;

    [RelayCommand]
    void Increment()
    {
        BasicValue = Math.Min(100, BasicValue + 10);
        PulseValue = Math.Min(100, PulseValue + 10);
    }

    [RelayCommand]
    void Decrement()
    {
        BasicValue = Math.Max(0, BasicValue - 10);
        PulseValue = Math.Max(0, PulseValue - 10);
    }
}
