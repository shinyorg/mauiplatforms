using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Controls.Gantt;

namespace Sample.Features.Gantt;

public partial class GanttViewModel : ObservableObject
{
    /// <summary>
    /// Every applied plan, newest first. A <see cref="GanttSchedulePlan"/> knows how to revert itself
    /// — cascade included — so undo is a stack of these and nothing else.
    /// </summary>
    readonly Stack<GanttSchedulePlan> undo = new();

    [ObservableProperty]
    string statusMessage = "Drag a bar, its edges, or the progress handle";

    [ObservableProperty]
    bool showCriticalPath = true;

    [ObservableProperty]
    bool useWorkingCalendar = true;

    [ObservableProperty]
    bool allowDependencyEdit;

    [ObservableProperty]
    GanttTimeScale timeScale = GanttTimeScale.Day;

    [ObservableProperty]
    GanttCascadeMode cascadeMode = GanttCascadeMode.PushOnly;

    [ObservableProperty]
    GanttTask? selectedTask;

    public ObservableCollection<GanttTask> Tasks { get; } = SampleGanttPlan.CreateTasks();
    public ObservableCollection<GanttDependency> Dependencies { get; } = SampleGanttPlan.CreateDependencies();

    /// <summary>Weekdays only. Null hands the control <see cref="GanttCalendar.Continuous"/>.</summary>
    public GanttCalendar? Calendar => this.UseWorkingCalendar ? GanttCalendar.StandardDays : null;

    partial void OnUseWorkingCalendarChanged(bool value) => this.OnPropertyChanged(nameof(this.Calendar));

    public bool CanUndo => this.undo.Count > 0;


    /// <summary>Records an applied change so the Undo button has something to pop.</summary>
    public void Record(GanttSchedulePlan plan)
    {
        this.undo.Push(plan);
        this.OnPropertyChanged(nameof(this.CanUndo));

        var cascaded = plan.Cascade.Count();
        this.StatusMessage = cascaded == 0
            ? $"{plan.Task?.Name}: {plan.Kind}"
            : $"{plan.Task?.Name}: {plan.Kind} — {cascaded} other task{(cascaded == 1 ? "" : "s")} moved";
    }


    [RelayCommand]
    void Undo()
    {
        if (this.undo.Count == 0)
            return;

        var plan = this.undo.Pop();
        plan.Revert();

        this.OnPropertyChanged(nameof(this.CanUndo));
        this.StatusMessage = $"Reverted {plan.Kind} on {plan.Task?.Name}";
    }


    [RelayCommand]
    void Reset()
    {
        this.undo.Clear();
        this.Tasks.Clear();

        foreach (var task in SampleGanttPlan.CreateTasks())
            this.Tasks.Add(task);

        this.Dependencies.Clear();

        foreach (var dependency in SampleGanttPlan.CreateDependencies())
            this.Dependencies.Add(dependency);

        this.OnPropertyChanged(nameof(this.CanUndo));
        this.StatusMessage = "Plan reset";
    }
}
