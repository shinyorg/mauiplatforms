using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

using Shiny;

namespace Sample.Features.DataGrid;

public partial class TreeDataGridViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Tap a caret to open a branch";

    public ObservableCollection<CostNode> Accounts { get; } =
    [
        new("Engineering", "Division", 612_000, false,
        [
            new("Platform", "Team", 318_000, false,
            [
                new("Runtime", "Squad", 174_000),
                new("Tooling", "Squad", 144_000)
            ]),
            new("Mobile", "Team", 294_000, false,
            [
                new("iOS", "Squad", 151_000),
                new("Android", "Squad", 143_000)
            ])
        ]),
        new("Research", "Division", 388_000, false,
        [
            new("Applied", "Team", 205_000),
            new("Long range", "Team", 183_000)
        ]),
        // Loaded on demand - the caret shows a spinner glyph while the loader runs.
        new("Field operations", "Division", 240_000, lazy: true)
    ];

    /// <summary>Synchronous branches. Returning null for a lazy node hands it to the loader instead.</summary>
    public Func<object, IEnumerable?> ChildrenSelector
        => item => ((CostNode)item).Lazy ? null : ((CostNode)item).Children;

    public Func<object, bool> HasChildrenSelector
        => item => ((CostNode)item).Lazy || ((CostNode)item).Children.Count > 0;

    /// <summary>Stands in for a network call so the loading caret is actually visible.</summary>
    public Func<object, Task<IEnumerable>> ChildrenLoader
        => async item =>
        {
            await Task.Delay(600);
            this.StatusMessage = $"Loaded {((CostNode)item).Name}";
            return new List<CostNode>
            {
                new("Northern region", "Team", 128_000),
                new("Southern region", "Team", 112_000)
            };
        };
}

public partial class CostNode(string name, string kind, decimal budget, bool lazy = false, List<CostNode>? children = null)
    : ObservableObject
{
    [ObservableProperty] string name = name;
    [ObservableProperty] string kind = kind;
    [ObservableProperty] decimal budget = budget;

    public bool Lazy { get; } = lazy;
    public List<CostNode> Children { get; } = children ?? new();
}
