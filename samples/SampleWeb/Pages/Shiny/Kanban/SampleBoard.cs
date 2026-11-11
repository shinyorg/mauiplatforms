using System.Collections.ObjectModel;
using Shiny.Maui.Controls.Kanban;

namespace Sample.Features.Kanban;

/// <summary>
/// The demo board: a small sprint with enough shape to show every feature - a WIP limit that is
/// already tight, a locked card, labels, assignees, due dates in all three states, and two teams to
/// switch swimlanes on.
/// </summary>
static class SampleBoard
{
    public static ObservableCollection<KanbanColumn> CreateColumns() =>
    [
        new() { Id = "backlog", Title = "Backlog", Description = "Not started", Color = "#94A3B8" },
        new() { Id = "ready", Title = "Ready", Description = "Groomed and estimated", Color = "#0EA5E9", WipLimit = 4 },
        new() { Id = "doing", Title = "In progress", Description = "Someone is on it", Color = "#F59E0B", WipLimit = 2 },
        new() { Id = "review", Title = "In review", Color = "#A855F7", WipLimit = 3 },
        new() { Id = "done", Title = "Done", Color = "#22C55E" }
    ];


    public static ObservableCollection<KanbanSwimlane> CreateSwimlanes() =>
    [
        new() { Id = "platform", Title = "Platform", Color = "#0EA5E9" },
        new() { Id = "mobile", Title = "Mobile", Color = "#A855F7" }
    ];


    public static ObservableCollection<KanbanCard> CreateCards()
    {
        var today = DateTimeOffset.Now.Date;

        return
        [
            Card("k1", "backlog", "platform", 0, "Audit the theme tokens",
                description: "Every control should read its colours from the pack, not a literal.",
                assignee: "Ada Lovelace", labels: [("chore", "#64748B")]),

            Card("k2", "backlog", "mobile", 1, "Sunset the old picker",
                description: "Two pickers ship today and only one of them is documented.",
                labels: [("tech debt", "#F97316")]),

            Card("k3", "ready", "platform", 0, "Frozen columns on Android",
                assignee: "Grace Hopper", due: today.AddDays(1),
                labels: [("bug", "#EF4444"), ("android", "#22C55E")], badge: "SH-412"),

            Card("k4", "ready", "mobile", 1, "Pull to refresh",
                assignee: "Alan Turing", due: today.AddDays(9),
                labels: [("feature", "#3B82F6")]),

            Card("k5", "doing", "platform", 0, "Drag and drop on GTK4",
                description: "The platform recognizers are missing, so this is the pan fallback.",
                assignee: "Ada Lovelace", due: today.AddDays(-2), color: "#EF4444",
                labels: [("bug", "#EF4444")], badge: "SH-398"),

            Card("k6", "doing", "mobile", 1, "Camera permissions prompt",
                assignee: "Katherine Johnson", due: today,
                labels: [("feature", "#3B82F6")]),

            // Locked on purpose: the release card is the one thing on this board that may not be
            // dragged, and a padlock is the only affordance that says so before you try.
            Card("k7", "review", "platform", 0, "Cut the 3.2 release",
                description: "Frozen until the sign-off lands.", locked: true,
                assignee: "Grace Hopper", color: "#A855F7", badge: "REL"),

            Card("k8", "review", "mobile", 1, "Voice input on iOS",
                assignee: "Alan Turing", due: today.AddDays(4),
                labels: [("feature", "#3B82F6"), ("ios", "#0EA5E9")]),

            Card("k9", "done", "platform", 0, "Ship the Gantt critical path",
                assignee: "Katherine Johnson", labels: [("feature", "#3B82F6")]),

            Card("k10", "done", "mobile", 1, "Barcode scanning went native",
                assignee: "Ada Lovelace", labels: [("feature", "#3B82F6")])
        ];
    }


    static KanbanCard Card(
        string id,
        string column,
        string swimlane,
        int order,
        string title,
        string? description = null,
        string? assignee = null,
        DateTimeOffset? due = null,
        string? color = null,
        string? badge = null,
        bool locked = false,
        (string Text, string Color)[]? labels = null
    )
    {
        var card = new KanbanCard
        {
            Id = id,
            ColumnId = column,
            SwimlaneId = swimlane,
            Order = order,
            Title = title,
            Description = description,
            AssigneeName = assignee,
            DueDate = due,
            Color = color,
            Badge = badge,
            IsLocked = locked
        };

        foreach (var (text, labelColor) in labels ?? [])
            card.Labels.Add(new KanbanLabel(text, labelColor));

        return card;
    }
}
