using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Shiny.Maui.Controls.Scheduler;

using Shiny;

namespace Sample.Features.Scheduler;

public class AgendaViewModel(ISchedulerEventProvider provider) : INotifyPropertyChanged
{
    DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);
    int daysToShow = 1;
    bool showAdditionalTimezones;
    AgendaDatePickerMode datePickerMode = AgendaDatePickerMode.Carousel;
    bool allowEventDrag = true;
    bool allowEventResize = true;
    int dragSnapMinutes = 15;

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

    public bool ShowAdditionalTimezones
    {
        get => showAdditionalTimezones;
        set { showAdditionalTimezones = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimezoneToggleText)); }
    }

    public AgendaDatePickerMode DatePickerMode
    {
        get => datePickerMode;
        set { datePickerMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(DatePickerToggleText)); }
    }

    public bool AllowEventDrag
    {
        get => allowEventDrag;
        set { allowEventDrag = value; OnPropertyChanged(); }
    }

    public bool AllowEventResize
    {
        get => allowEventResize;
        set { allowEventResize = value; OnPropertyChanged(); }
    }

    public int DragSnapMinutes
    {
        get => dragSnapMinutes;
        set { dragSnapMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(SnapToggleText)); }
    }

    public string SnapToggleText => $"Snap {DragSnapMinutes}m";

    /// <summary>Cycles 5 -> 15 -> 30 so the snap granularity is actually demonstrable.</summary>
    public ICommand CycleSnapCommand => new Command(() => DragSnapMinutes = DragSnapMinutes switch
    {
        5 => 15,
        15 => 30,
        _ => 5
    });

    public string DaysToggleText => DaysToShow == 1 ? "3-Day" : "1-Day";
    public string TimezoneToggleText => ShowAdditionalTimezones ? "Hide TZ" : "Show TZ";
    public string DatePickerToggleText => DatePickerMode == AgendaDatePickerMode.Carousel ? "Calendar" : "Carousel";

    public ICommand ToggleDaysCommand => new Command(() => DaysToShow = DaysToShow == 1 ? 3 : 1);
    public ICommand ToggleTimezonesCommand => new Command(() => ShowAdditionalTimezones = !ShowAdditionalTimezones);
    public ICommand ToggleDatePickerModeCommand => new Command(() =>
        DatePickerMode = DatePickerMode == AgendaDatePickerMode.Carousel
            ? AgendaDatePickerMode.Calendar
            : AgendaDatePickerMode.Carousel);

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
