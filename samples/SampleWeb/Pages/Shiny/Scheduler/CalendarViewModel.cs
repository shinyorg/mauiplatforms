using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shiny.Maui.Controls.Scheduler;

using Shiny;

namespace Sample.Features.Scheduler;

public class CalendarViewModel(ISchedulerEventProvider provider) : INotifyPropertyChanged
{
    DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);
    DateOnly displayMonth = DateOnly.FromDateTime(DateTime.Today);

    public ISchedulerEventProvider Provider => provider;

    public DateOnly SelectedDate
    {
        get => selectedDate;
        set { selectedDate = value; OnPropertyChanged(); }
    }

    public DateOnly DisplayMonth
    {
        get => displayMonth;
        set { displayMonth = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
