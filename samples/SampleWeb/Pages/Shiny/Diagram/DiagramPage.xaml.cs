using Shiny.Controls.Diagramming;
using Shiny.Maui.Controls.Diagram;

namespace Sample.Features.Diagram;

public partial class DiagramPage : ContentPage
{
    int added;

    public DiagramPage()
    {
        InitializeComponent();
    }

    DiagramViewModel? Model => this.BindingContext as DiagramViewModel;

    void OnFit(object? sender, EventArgs e) => this.Diagram.ZoomToFit();

    void OnUndo(object? sender, EventArgs e) => this.Diagram.Undo();

    void OnRedo(object? sender, EventArgs e) => this.Diagram.Redo();

    void OnDelete(object? sender, EventArgs e) => this.Diagram.DeleteSelection();

    void OnAddNode(object? sender, EventArgs e)
    {
        if (this.Model is null)
            return;

        // Pinned and placed by hand, so the auto-layout leaves it where it lands rather than folding
        // it into a hierarchy it has no edges in yet.
        this.Model.Nodes.Add(new DiagramNode($"added{this.added}", $"New {++this.added}")
        {
            Shape = DiagramNodeShape.RoundedRectangle,
            IsPinned = true,
            X = 40,
            Y = 40
        });

        this.Model.StatusMessage = "Added a node. Select it and drag a connector handle to wire it up.";
    }

    void OnSelectionChanged(object? sender, DiagramSelectionEventArgs e)
    {
        if (this.Model is null)
            return;

        this.Model.StatusMessage = e.Selection.IsEmpty
            ? "Nothing selected"
            : $"{e.Selection.Nodes.Count} node(s), {e.Selection.Connections.Count} connection(s) selected";
    }

    void OnEdited(object? sender, DiagramEditedEventArgs e)
    {
        if (this.Model is null)
            return;

        this.Model.StatusMessage =
            $"{e.Plan.Kind}: {e.Plan.AffectedNodes.Count} node(s), {e.Plan.AffectedConnections.Count} connection(s)";
    }

    /// <summary>
    /// Validation is reported, never thrown, and this event is the only place it surfaces.
    /// </summary>
    void OnBuilt(object? sender, DiagramBuiltEventArgs e)
    {
        if (this.Model is null || e.Model.Issues.Count == 0)
            return;

        this.Model.StatusMessage = $"{e.Model.Issues.Count} issue(s): {e.Model.Issues[0].Message}";
    }
}
