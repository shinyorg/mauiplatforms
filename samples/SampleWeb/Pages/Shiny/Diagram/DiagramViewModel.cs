using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Controls.Diagramming;

namespace Sample.Features.Diagram;

/// <summary>Which of the four sample graphs is on screen.</summary>
public enum DiagramSample
{
    /// <summary>A nested hierarchy with no connections of its own.</summary>
    OrgChart,

    /// <summary>Diamonds and labelled branches that rejoin at the end.</summary>
    DecisionTree,

    /// <summary>A build pipeline with a loop back to an earlier step.</summary>
    Flowchart,

    /// <summary>A root with branches fanned out to both sides.</summary>
    MindMap
}


public partial class DiagramViewModel : ObservableObject
{
    [ObservableProperty]
    string statusMessage = "Drag the background to pan, pinch to zoom, tap a node to select it";

    [ObservableProperty]
    DiagramSample sample = DiagramSample.OrgChart;

    [ObservableProperty]
    DiagramLayoutKind layoutKind = DiagramLayoutKind.Tree;

    [ObservableProperty]
    DiagramDirection direction = DiagramDirection.TopToBottom;

    [ObservableProperty]
    DiagramConnectionRouter router = DiagramConnectionRouter.Orthogonal;

    [ObservableProperty]
    bool tipOver;

    [ObservableProperty]
    bool allowNodeDrag = true;

    [ObservableProperty]
    bool allowConnectionEdit = true;

    [ObservableProperty]
    bool showGrid = true;

    [ObservableProperty]
    bool snapToGrid;

    [ObservableProperty]
    DiagramNode? selectedNode;

    public DiagramViewModel() => this.Load();

    public ObservableCollection<DiagramNode> Nodes { get; private set; } = [];

    public ObservableCollection<DiagramConnection> Connections { get; private set; } = [];

    /// <summary>The tree style the control should use, derived from the one switch on the page.</summary>
    public DiagramTreeStyle TreeStyle => this.TipOver ? DiagramTreeStyle.TipOver : DiagramTreeStyle.Normal;

    partial void OnTipOverChanged(bool value) => this.OnPropertyChanged(nameof(this.TreeStyle));

    partial void OnSampleChanged(DiagramSample value) => this.Load();

    [RelayCommand]
    void Reset() => this.Load();

    void Load()
    {
        var (nodes, connections) = this.Sample switch
        {
            DiagramSample.DecisionTree => SampleDiagrams.DecisionTree(),
            DiagramSample.Flowchart => SampleDiagrams.Flowchart(),
            DiagramSample.MindMap => SampleDiagrams.MindMap(),
            _ => SampleDiagrams.OrgChart()
        };

        this.Nodes = nodes;
        this.Connections = connections;

        // Each sample reads best under a different layout, so switching sample suggests one rather
        // than leaving a decision tree drawn as a mindmap. Both of the rejoining graphs get Layered:
        // a tree layout has to throw an edge away to draw them.
        this.LayoutKind = this.Sample switch
        {
            DiagramSample.Flowchart or DiagramSample.DecisionTree => DiagramLayoutKind.Layered,
            DiagramSample.MindMap => DiagramLayoutKind.MindMap,
            _ => DiagramLayoutKind.Tree
        };

        this.OnPropertyChanged(nameof(this.Nodes));
        this.OnPropertyChanged(nameof(this.Connections));

        this.StatusMessage = $"{this.Nodes.Count} nodes, {this.Connections.Count} connections";
    }

    /// <summary>The values the sample's pickers offer, so the page does not hardcode them. IList, not
    /// IReadOnlyList: that is Picker.ItemsSource's type in MAUI 10.0.120, which XamlC checks in Release.</summary>
    public static System.Collections.IList Samples { get; } = Enum.GetValues<DiagramSample>();

    /// <inheritdoc cref="Samples" />
    public static System.Collections.IList Layouts { get; } = Enum.GetValues<DiagramLayoutKind>();

    /// <inheritdoc cref="Samples" />
    public static System.Collections.IList Directions { get; } = Enum.GetValues<DiagramDirection>();

    /// <inheritdoc cref="Samples" />
    public static System.Collections.IList Routers { get; } =
        Enum.GetValues<DiagramConnectionRouter>();
}
