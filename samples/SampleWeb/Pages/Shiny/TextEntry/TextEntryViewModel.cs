using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;

namespace Sample.Features.TextEntry;

public partial class TextEntryViewModel : ObservableObject
{
    [ObservableProperty] string firstName = "";
    [ObservableProperty] string lastName = "";
    [ObservableProperty] string searchText = "";
    [ObservableProperty] string password = "";
    [ObservableProperty] string bio = "";
    [ObservableProperty] string customText = "";
    [ObservableProperty] string phone = "";
    [ObservableProperty] string amount = "";
    [ObservableProperty] string maskedPhone = "";
    [ObservableProperty] string maskedCard = "";
    [ObservableProperty] string maskedDate = "";
    [ObservableProperty] string quantity = "0";
    [ObservableProperty] string score = "50";
    [ObservableProperty] string floatingEmail = "";
    [ObservableProperty] string floatingPassword = "";
    [ObservableProperty] string floatingSearch = "";
    [ObservableProperty] string serial = "";
    [ObservableProperty] string addonAmount = "";
    [ObservableProperty] string accessoryAmount = "";
    [ObservableProperty] string accessoryQuantity = "";
    [ObservableProperty] string accessoryNotes = "";

    [RelayCommand]
    void InsertTag() => AccessoryNotes += "#";

    [ObservableProperty] string email = "";
    [ObservableProperty] bool hasEmailError;
    [ObservableProperty] string? emailError;

    partial void OnEmailChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            HasEmailError = false;
            EmailError = null;
        }
        else if (!value.Contains('@'))
        {
            HasEmailError = true;
            EmailError = "Please enter a valid email address";
        }
        else
        {
            HasEmailError = false;
            EmailError = null;
        }
    }
}
