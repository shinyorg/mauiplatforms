using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Shiny.Maui.Controls.Scheduler;

using Shiny;

namespace Sample.Features.Scheduler;

public class AgendaCalendarPickerViewModel(ISchedulerEventProvider provider) : INotifyPropertyChanged
{
    DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);
    int daysToShow = 1;

    public ISchedulerEventProvider Provider => provider;

    public DateOnly SelectedDate
    {
        get => selectedDate;
        set { selectedDate = value; OnPropertyChanged(); }
    }

    public int DaysToShow
    {
        get => daysToShow;
        set { daysToShow = value; OnPropertyChanged(); OnPropertyChanged(nameof(DaysToggleText)); }
    }

    public string DaysToggleText => DaysToShow == 1 ? "3-Day" : "1-Day";

    public ICommand ToggleDaysCommand => new Command(() => DaysToShow = DaysToShow == 1 ? 3 : 1);

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
