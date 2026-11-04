using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Shiny;
using Shiny.Maui.Controls;

namespace Sample.Features.PasswordStrength;

public partial class PasswordStrengthViewModel : ObservableObject
{
    [ObservableProperty]
    string passphrase = string.Empty;

    [ObservableProperty]
    bool isPassphraseAcceptable;

    [ObservableProperty]
    PasswordStrengthLevel passphraseLevel;

    [ObservableProperty]
    int passphraseScore;

    [ObservableProperty]
    string legacyPassword = string.Empty;

    [ObservableProperty]
    bool isLegacyAcceptable;

    [ObservableProperty]
    string crackTimePassword = string.Empty;

    [ObservableProperty]
    bool isCrackTimeAcceptable;

    [ObservableProperty]
    string personalPassword = string.Empty;

    [ObservableProperty]
    string status = "Type something.";

    /// <summary>What the "don't use your own details" rule is checking against.</summary>
    public IList<string> UserDetails { get; } = ["ada.lovelace@example.com", "Ada Lovelace"];

    /// <summary>The house rules — the sort of list that comes from a policy endpoint.</summary>
    public IList<string> Blocked { get; } = ["shiny", "shinycontrols", "letmein2026"];

    [RelayCommand]
    void Submit() => this.Status = $"Accepted a {this.PassphraseLevel} passphrase ({this.PassphraseScore}/100).";

    public void OnStrengthChanged(PasswordStrengthChangedEventArgs e)
        => this.Status = e.Result.Warning
                         ?? e.Result.Suggestions.FirstOrDefault()
                         ?? $"{e.Level} — {e.Score}/100.";
}
