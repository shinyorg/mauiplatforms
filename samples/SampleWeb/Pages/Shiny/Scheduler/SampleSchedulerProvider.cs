using Shiny;
using Shiny.Maui.Controls.Dialogs;
using Shiny.Maui.Controls.Scheduler;

namespace Sample.Features.Scheduler;

public class SampleSchedulerProvider(IDialogService dialogs) : ISchedulerEventProvider
{
    static readonly Color[] CategoryColors =
    [
        Color.FromArgb("#4285F4"), // Blue - Meetings
        Color.FromArgb("#0F9D58"), // Green - Personal
        Color.FromArgb("#DB4437"), // Red - Important
        Color.FromArgb("#F4B400"), // Yellow - Reminders
        Color.FromArgb("#AB47BC"), // Purple - Projects
        Color.FromArgb("#00ACC1"), // Cyan - Travel
    ];

    // Days are generated once and then handed back as the same instances, so an event you drag
    // stays where you dropped it instead of being regenerated on the next load.
    readonly Dictionary<DateTime, List<SchedulerEvent>> dayCache = [];

    public async Task<IReadOnlyList<SchedulerEvent>> GetEvents(DateTimeOffset start, DateTimeOffset end)
    {
        await Task.Delay(500); // simulate network

        var events = new List<SchedulerEvent>();
        var current = start.LocalDateTime.Date;
        var endDate = end.LocalDateTime.Date;

        while (current <= endDate)
        {
            if (!dayCache.TryGetValue(current, out var dayEvents))
            {
                dayEvents = BuildDay(current);
                dayCache[current] = dayEvents;
            }
            events.AddRange(dayEvents);
            current = current.AddDays(1);
        }
        return events;
    }


    static List<SchedulerEvent> BuildDay(DateTime current)
    {
        var events = new List<SchedulerEvent>();
        {
            var dow = current.DayOfWeek;

            // Weekday meetings
            if (dow is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Team Standup",
                    Description = "Daily sync with the team",
                    Color = CategoryColors[0],
                    Start = new DateTimeOffset(current.AddHours(9)),
                    End = new DateTimeOffset(current.AddHours(9).AddMinutes(30))
                });

                events.Add(new SchedulerEvent
                {
                    Title = "Lunch Break",
                    Description = "Take a break",
                    Color = CategoryColors[1],
                    Start = new DateTimeOffset(current.AddHours(12)),
                    End = new DateTimeOffset(current.AddHours(13))
                });
            }

            // Monday planning
            if (dow == DayOfWeek.Monday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Sprint Planning",
                    Description = "Plan the week's work",
                    Color = CategoryColors[4],
                    Start = new DateTimeOffset(current.AddHours(10)),
                    End = new DateTimeOffset(current.AddHours(11).AddMinutes(30))
                });
            }

            // Tuesday overlapping events (3-way overlap at 2-3pm)
            if (dow == DayOfWeek.Tuesday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Project Alpha",
                    Description = "Architecture review",
                    Color = CategoryColors[4],
                    Start = new DateTimeOffset(current.AddHours(13).AddMinutes(30)),
                    End = new DateTimeOffset(current.AddHours(15))
                });

                events.Add(new SchedulerEvent
                {
                    Title = "Client Call",
                    Description = "Q1 deliverables discussion",
                    Color = CategoryColors[2],
                    Start = new DateTimeOffset(current.AddHours(14)),
                    End = new DateTimeOffset(current.AddHours(15).AddMinutes(30))
                });

                events.Add(new SchedulerEvent
                {
                    Title = "Code Review",
                    Description = "PR #342 review",
                    Color = CategoryColors[0],
                    Start = new DateTimeOffset(current.AddHours(14).AddMinutes(30)),
                    End = new DateTimeOffset(current.AddHours(16))
                });
            }

            // Wednesday design review
            if (dow == DayOfWeek.Wednesday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Design Review",
                    Description = "Review new mockups",
                    Color = CategoryColors[4],
                    Start = new DateTimeOffset(current.AddHours(14)),
                    End = new DateTimeOffset(current.AddHours(15))
                });

                events.Add(new SchedulerEvent
                {
                    Title = "1:1 with Manager",
                    Description = "Weekly check-in",
                    Color = CategoryColors[2],
                    Start = new DateTimeOffset(current.AddHours(15).AddMinutes(30)),
                    End = new DateTimeOffset(current.AddHours(16))
                });
            }

            // Friday retro
            if (dow == DayOfWeek.Friday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Sprint Retro",
                    Description = "What went well, what didn't",
                    Color = CategoryColors[4],
                    Start = new DateTimeOffset(current.AddHours(16)),
                    End = new DateTimeOffset(current.AddHours(17))
                });
            }

            // All-day: first Monday = sprint start
            if (dow == DayOfWeek.Monday && current.Day <= 7)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Sprint Start",
                    IsAllDay = true,
                    Color = CategoryColors[3],
                    Start = new DateTimeOffset(current),
                    End = new DateTimeOffset(current.AddDays(5))
                });
            }

            // All-day holiday on the 15th
            if (current.Day == 15)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Company Holiday",
                    IsAllDay = true,
                    Color = CategoryColors[2],
                    Start = new DateTimeOffset(current),
                    End = new DateTimeOffset(current.AddDays(1))
                });
            }

            // Multi-day conference (second week, Tue-Thu)
            if (dow == DayOfWeek.Tuesday && current.Day is > 7 and <= 14)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Tech Conference",
                    Description = "Annual developer conference",
                    IsAllDay = true,
                    Color = CategoryColors[5],
                    Start = new DateTimeOffset(current),
                    End = new DateTimeOffset(current.AddDays(3))
                });
            }

            // Multi-day vacation (third week, Mon-Fri)
            if (dow == DayOfWeek.Monday && current.Day is > 14 and <= 21)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Vacation",
                    Description = "Beach trip",
                    IsAllDay = true,
                    Color = CategoryColors[1],
                    Start = new DateTimeOffset(current),
                    End = new DateTimeOffset(current.AddDays(5))
                });
            }

            // All-day birthday on the 20th
            if (current.Day == 20)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Sarah's Birthday",
                    IsAllDay = true,
                    Color = CategoryColors[3],
                    Start = new DateTimeOffset(current),
                    End = new DateTimeOffset(current.AddDays(1))
                });
            }

            // Multi-day deadline spanning a weekend (last Thu-Mon)
            if (dow == DayOfWeek.Thursday && current.Day > 24)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Release Deadline",
                    Description = "Q1 release window",
                    IsAllDay = true,
                    Color = CategoryColors[2],
                    Start = new DateTimeOffset(current),
                    End = new DateTimeOffset(current.AddDays(4))
                });
            }

            // Overnight event on Saturdays (8pm - 1am next day)
            if (dow == DayOfWeek.Saturday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Game Night",
                    Description = "Board games at Dave's place",
                    Color = CategoryColors[1],
                    Start = new DateTimeOffset(current.AddHours(20)),
                    End = new DateTimeOffset(current.AddDays(1).AddHours(1))
                });
            }

            // Afternoon workout on Tue/Thu
            if (dow is DayOfWeek.Tuesday or DayOfWeek.Thursday)
            {
                events.Add(new SchedulerEvent
                {
                    Title = "Gym",
                    Description = "Afternoon workout",
                    Color = CategoryColors[1],
                    Start = new DateTimeOffset(current.AddHours(17).AddMinutes(30)),
                    End = new DateTimeOffset(current.AddHours(18).AddMinutes(30))
                });
            }
        }
        return events;
    }

    public void OnEventSelected(SchedulerEvent selectedEvent)
    {
        dialogs.Alert(
            selectedEvent.Title,
            $"{selectedEvent.Description}\n{selectedEvent.Start.LocalDateTime:g} - {selectedEvent.End.LocalDateTime:g}");
    }

    public bool CanCalendarSelect(DateOnly selectedDate) => true;

    public void OnCalendarDateSelected(DateOnly selectedDate) { }

    public void OnAgendaTimeSelected(DateTimeOffset selectedTime)
    {
        dialogs.Alert("Time Selected", $"{selectedTime.LocalDateTime:g}");
    }

    public bool CanSelectAgendaTime(DateTimeOffset selectedTime) => true;

    // ------------- drag / resize -------------

    /// <summary>Lunch is immovable - it demonstrates the per-event gate.</summary>
    public bool CanChangeEvent(SchedulerEvent evt) =>
        !evt.IsAllDay && evt.Title != "Lunch Break";

    /// <summary>Nothing before 07:00, to show a rejected position live under the finger.</summary>
    public bool CanChangeEventTo(SchedulerEventChange change) =>
        change.NewStart.LocalDateTime.TimeOfDay >= TimeSpan.FromHours(7);

    public async Task<bool> OnEventChanged(SchedulerEventChange change)
    {
        await Task.Delay(600); // simulate a save round trip

        // 10% of saves fail, so optimistic-commit-then-revert is visible rather than theoretical.
        if (Random.Shared.Next(10) == 0)
        {
            dialogs.Alert("Save failed", $"'{change.Event.Title}' snapped back to its original time.");
            return false;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[Sample] {change.Kind} {change.Event.Title} → {change.NewStart.LocalDateTime:g}-{change.NewEnd.LocalDateTime:t}");
        return true;
    }
}
