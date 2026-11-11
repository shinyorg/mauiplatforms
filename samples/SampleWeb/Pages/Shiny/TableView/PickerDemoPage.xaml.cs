using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shiny;

namespace Sample.Features.TableView;

public partial class PickerDemoPage : Shiny.Maui.Controls.ShinyContentPage
{
    public PickerDemoPage()
    {
        InitializeComponent();
    }
}

public class PickerDemoViewModel : INotifyPropertyChanged
{
    int selectedColorIndex;
    object? selectedCountry;
    IList? selectedHobbies;
    DateTime? startDate = DateTime.Today;
    TimeSpan reminderTime = new(9, 0, 0);
    TimeSpan? duration = new TimeSpan(1, 30, 0);
    int? repeatCount = 1;

    public IList<string> Colors { get; } = new List<string>
    {
        "Red", "Green", "Blue", "Yellow", "Purple", "Orange"
    };

    public IList<string> Countries { get; } = new List<string>
    {
        "United States", "Canada", "United Kingdom", "Germany", "France",
        "Japan", "Australia", "Brazil", "India", "South Korea"
    };

    public IList<string> Hobbies { get; } = new List<string>
    {
        "Reading", "Gaming", "Cooking", "Hiking", "Photography",
        "Music", "Painting", "Cycling", "Swimming", "Gardening"
    };

    public int SelectedColorIndex { get => selectedColorIndex; set => SetProperty(ref selectedColorIndex, value); }
    public object? SelectedCountry { get => selectedCountry; set => SetProperty(ref selectedCountry, value); }
    public IList? SelectedHobbies { get => selectedHobbies; set => SetProperty(ref selectedHobbies, value); }
    public DateTime? StartDate { get => startDate; set => SetProperty(ref startDate, value); }
    public TimeSpan ReminderTime { get => reminderTime; set => SetProperty(ref reminderTime, value); }
    public TimeSpan? Duration { get => duration; set => SetProperty(ref duration, value); }
    public int? RepeatCount { get => repeatCount; set => SetProperty(ref repeatCount, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
