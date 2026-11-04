using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.Badge;

public partial class BadgeViewModel : ObservableObject
{
    [ObservableProperty] string mailCount = "3";
    [ObservableProperty] string cartCount = "127";
    [ObservableProperty] bool hasNew = true;
    [ObservableProperty] bool isPulsing = true;

    [RelayCommand]
    void Clear() => this.MailCount = string.Empty;

    [RelayCommand]
    void Increment()
    {
        if (int.TryParse(this.MailCount, out var n))
            this.MailCount = (n + 1).ToString();
        else
            this.MailCount = "1";
    }

    [RelayCommand]
    void ToggleDot() => this.HasNew = !this.HasNew;

    [RelayCommand]
    void TogglePulse() => this.IsPulsing = !this.IsPulsing;
}
