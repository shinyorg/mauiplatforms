using Shiny.Maui.Controls;

namespace Sample.Features.Timeline;

/// <summary>
/// The vertical timeline.
/// </summary>
/// <remarks>
/// The third entry is deliberately long: rows size to their content, and its rail segment has to
/// stretch to match rather than assume a fixed row height. That is the difference between a timeline
/// and a list with dots down the side.
/// </remarks>
public partial class TimelinePage : ContentPage
{
    record Delivery(string Title, string Detail, string At);

    static readonly List<Delivery> Events =
    [
        new("Order placed", "Payment authorised against the card ending 4412.", "09:02"),
        new("Picked", "Two items pulled from the Manchester shelf.", "11:20"),
        new("In transit",
            "Left the depot on the overnight run. Held for forty minutes outside Birmingham while the " +
            "M6 was closed, then re-routed via the A38 — which is why this entry is taller than the " +
            "others, and why the rail beside it has to stretch rather than assume a fixed row height.",
            "23:47"),
        new("Out for delivery", "On the van, seventh of nineteen drops.", "07:15"),
        new("Delivered", "Left in the porch as instructed.", "10:31")
    ];

    public TimelinePage()
    {
        this.InitializeComponent();

        this.Timeline.ItemsSource = Events;
        this.UpdateStatus();
    }

    void OnBack(object? sender, EventArgs e)
    {
        this.Timeline.ActiveIndex = Math.Max(-1, this.Timeline.ActiveIndex - 1);
        this.UpdateStatus();
    }

    void OnForward(object? sender, EventArgs e)
    {
        this.Timeline.ActiveIndex = Math.Min(Events.Count - 1, this.Timeline.ActiveIndex + 1);
        this.UpdateStatus();
    }

    void OnToggleAllActive(object? sender, EventArgs e)
    {
        this.Timeline.AllActive = !this.Timeline.AllActive;
        this.UpdateStatus();
    }

    void OnToggleSide(object? sender, EventArgs e)
    {
        this.Timeline.RailPosition = this.Timeline.RailPosition == TimelineRailPosition.Left
            ? TimelineRailPosition.Right
            : TimelineRailPosition.Left;

        this.UpdateStatus();
    }

    void OnNodeTapped(object? sender, Shiny.Maui.Controls.Collections.CollectionItemEventArgs e)
        => this.StatusLabel.Text = $"tapped #{e.Index + 1} — {((Delivery)e.Item).Title}";

    void UpdateStatus()
    {
        var where = this.Timeline.AllActive
            ? "all active"
            : this.Timeline.ActiveIndex < 0 ? "nothing active" : $"active: {Events[this.Timeline.ActiveIndex].Title}";

        this.StatusLabel.Text = $"{where} · rail {this.Timeline.RailPosition.ToString().ToLowerInvariant()}";
    }
}
