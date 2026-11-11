using Shiny.Maui.Controls.Dialogs;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Shiny;

namespace Sample.Features.TableView;

public partial class BasicSettingsPage : ContentPage
{
    public BasicSettingsPage()
    {
        InitializeComponent();
    }
}

public class BasicSettingsViewModel(IDialogService dialogs) : INotifyPropertyChanged
{
    bool wifiEnabled = true;
    bool bluetoothEnabled;
    bool termsAccepted;
    bool darkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
    string username = string.Empty;
    string password = string.Empty;
    string email = string.Empty;
    string phone = string.Empty;
    string cardNumber = string.Empty;
    string expiry = string.Empty;
    string selectedTheme = "System";
    int? fontSizeValue = 14;
    TimeSpan alarmTime = new(7, 0, 0);
    DateTime? birthDate = new DateTime(1990, 1, 1);
    TimeSpan? sessionLength = TimeSpan.FromMinutes(90);

    public bool WifiEnabled { get => wifiEnabled; set => SetProperty(ref wifiEnabled, value); }
    public bool BluetoothEnabled { get => bluetoothEnabled; set => SetProperty(ref bluetoothEnabled, value); }
    public bool TermsAccepted { get => termsAccepted; set => SetProperty(ref termsAccepted, value); }
    public bool DarkMode
    {
        get => darkMode;
        set
        {
            if (SetProperty(ref darkMode, value))
            {
                if (Application.Current != null)
                    Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
            }
        }
    }
    public string Username { get => username; set => SetProperty(ref username, value); }
    public string Password { get => password; set => SetProperty(ref password, value); }
    public string Email { get => email; set => SetProperty(ref email, value); }
    public string Phone { get => phone; set => SetProperty(ref phone, value); }
    public string CardNumber { get => cardNumber; set => SetProperty(ref cardNumber, value); }
    public string Expiry { get => expiry; set => SetProperty(ref expiry, value); }
    public string SelectedTheme { get => selectedTheme; set => SetProperty(ref selectedTheme, value); }
    public int? FontSizeValue { get => fontSizeValue; set => SetProperty(ref fontSizeValue, value); }
    public TimeSpan AlarmTime { get => alarmTime; set => SetProperty(ref alarmTime, value); }
    public DateTime? BirthDate { get => birthDate; set => SetProperty(ref birthDate, value); }
    public TimeSpan? SessionLength { get => sessionLength; set => SetProperty(ref sessionLength, value); }

    public ICommand AboutCommand { get; } = new Command(async () =>
        await dialogs.Alert("About", "Shiny.Maui.Controls Sample v1.0"));

    public ICommand PrivacyCommand { get; } = new Command(async () =>
        await dialogs.Alert("Privacy", "Your data is safe."));

    public ICommand ResetCommand => new Command(() =>
    {
        WifiEnabled = true;
        BluetoothEnabled = false;
        TermsAccepted = false;
        DarkMode = false;
        Username = string.Empty;
        Password = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        CardNumber = string.Empty;
        Expiry = string.Empty;
        SelectedTheme = "System";
        FontSizeValue = 14;
        AlarmTime = new TimeSpan(7, 0, 0);
        BirthDate = new DateTime(1990, 1, 1);
        SessionLength = TimeSpan.FromMinutes(90);
    });

    public event PropertyChangedEventHandler? PropertyChanged;

    bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
