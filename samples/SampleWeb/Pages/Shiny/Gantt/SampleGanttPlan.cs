using System.Collections.ObjectModel;
using Shiny.Controls.Gantt;

namespace Sample.Features.Gantt;

/// <summary>
/// The demo plan, shared by every Gantt sample page.
/// </summary>
/// <remarks>
/// Deliberately not a flat list of five tasks. It has three levels of hierarchy, all four dependency
/// types, a lag, a milestone, a baseline that has slipped, a deadline that is about to be missed and
/// a manually scheduled task — because every one of those is a code path, and a demo that exercises
/// none of them is a demo that cannot show a regression.
/// </remarks>
static class SampleGanttPlan
{
    /// <summary>The plan is anchored to the Monday of the current week so it always straddles today.</summary>
    public static DateTimeOffset Monday
    {
        get
        {
            var today = DateTimeOffset.Now.Date;
            var back = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return new DateTimeOffset(today.AddDays(-back).AddDays(-7), DateTimeOffset.Now.Offset);
        }
    }

    public static DateTimeOffset Day(double offset) => Monday.AddDays(offset);


    public static ObservableCollection<GanttTask> CreateTasks()
    {
        var tasks = new ObservableCollection<GanttTask>
        {
            Task("project", "Mobile app v2", 0, 0, kind: GanttTaskKind.Project),

            Task("discovery", "Discovery", 0, 0, parent: "project"),
            Task("interviews", "User interviews", 0, 4, parent: "discovery", progress: 1.0, resource: "Ada"),
            Task("synthesis", "Synthesis", 4, 6, parent: "discovery", progress: 1.0, resource: "Grace"),

            Task("design", "Design", 0, 0, parent: "project"),
            Task("wireframes", "Wireframes", 6, 10, parent: "design", progress: 0.8, resource: "Katherine"),
            Task("visuals", "Visual design", 9, 15, parent: "design", progress: 0.35, resource: "Katherine"),

            Task("build", "Build", 0, 0, parent: "project"),
            Task("api", "API", 10, 18, parent: "build", progress: 0.4, resource: "Alan"),
            Task("client", "Client", 15, 25, parent: "build", progress: 0.1, resource: "Margaret"),
            Task("qa", "QA pass", 24, 28, parent: "build", resource: "Barbara"),

            Milestone("ship", "Ship v2", 28, parent: "project")
        };

        // A baseline that has already slipped: the visual design was planned three days shorter.
        var visuals = tasks.First(x => x.Id == "visuals");
        visuals.BaselineStart = Day(9);
        visuals.BaselineEnd = Day(12);

        // A deadline the QA pass is currently going to miss, which is what the marker is for.
        var qa = tasks.First(x => x.Id == "qa");
        qa.Deadline = Day(27);

        // Pinned by a contract date: the auto-scheduler pushes everything else around it.
        var api = tasks.First(x => x.Id == "api");
        api.Constraint = GanttConstraintType.StartNoEarlierThan;
        api.ConstraintDate = Day(10);

        return tasks;
    }


    public static ObservableCollection<GanttDependency> CreateDependencies() =>
    [
        new("interviews", "synthesis"),
        new("synthesis", "wireframes"),

        // Visual design can start once wireframes are three days in, not when they finish.
        new("wireframes", "visuals", GanttDependencyType.StartToStart, TimeSpan.FromDays(3)),

        new("visuals", "client"),
        new("synthesis", "api", GanttDependencyType.FinishToStart, TimeSpan.FromDays(1)),

        // QA cannot finish before the client does — a finish-to-finish link, not a sequence.
        new("client", "qa", GanttDependencyType.FinishToFinish),
        new("qa", "ship")
    ];


    static GanttTask Task(
        string id,
        string name,
        double start,
        double end,
        string? parent = null,
        double progress = 0,
        string? resource = null,
        GanttTaskKind kind = GanttTaskKind.Task
    ) => new()
    {
        Id = id,
        Name = name,
        ParentId = parent,
        Start = Day(start),
        End = Day(end),
        Progress = progress,
        ResourceId = resource,
        Kind = kind
    };


    static GanttTask Milestone(string id, string name, double at, string? parent = null) => new()
    {
        Id = id,
        Name = name,
        ParentId = parent,
        Start = Day(at),
        End = Day(at),
        Kind = GanttTaskKind.Milestone
    };
}
