using Shiny.Controls.RangePickers;
using Shiny.Maui.Controls;

namespace Sample.Features.RangePickers;

public partial class RangePickersPage : ContentPage
{
    public RangePickersPage()
    {
        InitializeComponent();

        var firstDay = CalendarMath.FirstDayOfWeek();
        this.ReportPicker.Presets = [.. DateRangePresets.Reporting(firstDay)];

        // A few "already booked" nights, a week or two out.
        this.BookingPicker.MinDate = DateTime.Today;
        this.BookingPicker.DisabledDates = [DateTime.Today.AddDays(9), DateTime.Today.AddDays(10), DateTime.Today.AddDays(18)];

        this.EventPicker.RangeSelected += (_, e) =>
            this.EventStatus.Text = e.Range is { } r ? $"{RangeFormatter.FormatDuration(r.Duration)}" : "Cleared";
    }

    void OnInlineRange(object? sender, DateRangeSelectedEventArgs e)
        => this.InlineStatus.Text = e.Range is { } r ? $"{r} ({r.Days} days)" : "Cleared";
}
