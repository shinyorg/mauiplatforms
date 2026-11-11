using Shiny.Maui.Controls.Gantt;

namespace Sample.Features.Gantt;

public partial class GanttPage : ContentPage
{
    public GanttPage()
    {
        InitializeComponent();
        this.FitTaskPaneToIdiom();
    }


    /// <summary>
    /// Four columns and a 360pt task pane is a desktop layout. On a 420pt phone it leaves about
    /// sixty points of timeline, so the bars — the entire point of the control — are off screen.
    /// The columns are declared in XAML because that is what the sample is demonstrating; trimming
    /// them here keeps that intact while still fitting a phone.
    /// </summary>
    void FitTaskPaneToIdiom()
    {
        if (DeviceInfo.Idiom != DeviceIdiom.Phone)
            return;

        for (var i = this.Gantt.Columns.Count - 1; i >= 0; i--)
        {
            if (this.Gantt.Columns[i].Field is not (GanttColumn.NameField or GanttColumn.ProgressField))
                this.Gantt.Columns.RemoveAt(i);
        }

        this.Gantt.Columns[0].Width = 120;
        this.Gantt.TaskPaneWidth = 175;
    }

    GanttViewModel? Model => this.BindingContext as GanttViewModel;

    /// <summary>
    /// Cancels a drag that would push anything past the plan's own ship milestone. Shows the point of
    /// the cancellable event: the whole cascade is in hand before a single date has moved.
    /// </summary>
    void OnTaskChanging(object? sender, GanttTaskChangingEventArgs e)
    {
        if (this.Model is null)
            return;

        var blockers = e.Plan.Changes.Where(x => x.Task.Id != "ship" && x.NewEnd > SampleGanttPlan.Day(40)).ToList();
        if (blockers.Count == 0)
            return;

        e.Cancel = true;
        this.Model.StatusMessage = $"Blocked: {blockers[0].Task.Name} would run past the plan window";
    }

    void OnTaskChanged(object? sender, GanttTaskChangedEventArgs e) => this.Model?.Record(e.Plan);

    void OnZoomIn(object? sender, EventArgs e) => this.Gantt.ZoomIn();

    void OnZoomOut(object? sender, EventArgs e) => this.Gantt.ZoomOut();

    void OnFit(object? sender, EventArgs e) => this.Gantt.ZoomToFit();

    void OnToday(object? sender, EventArgs e) => _ = this.Gantt.ScrollToDate(DateTimeOffset.Now.AddDays(-3));

    void OnExpandAll(object? sender, EventArgs e) => this.Gantt.ExpandAll();

    void OnCollapseAll(object? sender, EventArgs e) => this.Gantt.CollapseAll();
}
