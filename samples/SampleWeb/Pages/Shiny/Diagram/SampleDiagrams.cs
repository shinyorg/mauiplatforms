using System.Collections.ObjectModel;
using Shiny.Controls.Diagramming;

namespace Sample.Features.Diagram;

/// <summary>The four graphs the diagram demo switches between.</summary>
/// <remarks>
/// Built fresh on request rather than held as a static: the engine writes layout results back onto
/// the nodes, so two pages sharing one graph would fight over its coordinates.
/// </remarks>
public static class SampleDiagrams
{
    /// <summary>An org chart, declared as a nested hierarchy with no connections of its own.</summary>
    public static (ObservableCollection<DiagramNode> Nodes, ObservableCollection<DiagramConnection> Connections)
        OrgChart()
    {
        var ceo = Node("ceo", "Dana Whitfield\nCEO", DiagramNodeShape.RoundedRectangle);

        var eng = Node("eng", "Priya Raghunathan\nVP Engineering", DiagramNodeShape.RoundedRectangle);
        var sales = Node("sales", "Marco Oyelaran\nVP Sales", DiagramNodeShape.RoundedRectangle);
        var ops = Node("ops", "Jules Fontaine\nVP Operations", DiagramNodeShape.RoundedRectangle);

        ceo.Children.Add(eng);
        ceo.Children.Add(sales);
        ceo.Children.Add(ops);

        eng.Children.Add(Node("eng1", "Platform"));
        eng.Children.Add(Node("eng2", "Mobile"));
        eng.Children.Add(Node("eng3", "Quality"));

        sales.Children.Add(Node("sales1", "Enterprise"));
        sales.Children.Add(Node("sales2", "Partners"));

        ops.Children.Add(Node("ops1", "Support"));

        return (Collect(ceo), []);
    }

    /// <summary>A decision tree, declared as shapes and connections with no parent links at all.</summary>
    public static (ObservableCollection<DiagramNode> Nodes, ObservableCollection<DiagramConnection> Connections)
        DecisionTree()
    {
        var nodes = new ObservableCollection<DiagramNode>
        {
            Node("start", "Ticket raised", DiagramNodeShape.Stadium),
            Node("paid", "Paid plan?", DiagramNodeShape.Diamond),
            Node("urgent", "Sev 1 or 2?", DiagramNodeShape.Diamond),
            Node("oncall", "Page on-call"),
            Node("queue", "Support queue"),
            Node("community", "Community forum"),
            Node("done", "Resolved", DiagramNodeShape.Stadium)
        };

        var connections = new ObservableCollection<DiagramConnection>
        {
            new("start", "paid"),
            new("paid", "urgent", "Yes"),
            new("paid", "community", "No"),
            new("urgent", "oncall", "Yes"),
            new("urgent", "queue", "No"),
            new("oncall", "done"),
            new("queue", "done"),
            new("community", "done")
        };

        return (nodes, connections);
    }

    /// <summary>A build pipeline that loops back on itself, which is what the layered layout is for.</summary>
    public static (ObservableCollection<DiagramNode> Nodes, ObservableCollection<DiagramConnection> Connections)
        Flowchart()
    {
        var nodes = new ObservableCollection<DiagramNode>
        {
            Node("commit", "Commit pushed", DiagramNodeShape.Stadium),
            Node("build", "Build"),
            Node("unit", "Unit tests"),
            Node("integration", "Integration tests"),
            Node("green", "All green?", DiagramNodeShape.Diamond),
            Node("artifacts", "Publish artifacts", DiagramNodeShape.Document),
            Node("store", "Package feed", DiagramNodeShape.Cylinder),
            Node("fix", "Back to the author", DiagramNodeShape.Parallelogram),
            Node("deploy", "Deploy", DiagramNodeShape.Hexagon)
        };

        var connections = new ObservableCollection<DiagramConnection>
        {
            new("commit", "build"),
            new("build", "unit"),
            new("unit", "integration"),
            new("integration", "green"),
            new("green", "artifacts", "Yes"),
            new("green", "fix", "No"),

            // The edge that makes this a graph rather than a tree.
            new("fix", "commit", "Retry") { StrokeStyle = DiagramConnectionStroke.Dashed },

            new("artifacts", "store"),
            new("store", "deploy")
        };

        return (nodes, connections);
    }

    /// <summary>A mindmap, which reads best with the root's children split across both sides.</summary>
    public static (ObservableCollection<DiagramNode> Nodes, ObservableCollection<DiagramConnection> Connections)
        MindMap()
    {
        var root = Node("root", "Release 2.0", DiagramNodeShape.Stadium);

        var product = Node("product", "Product");
        product.Children.Add(Node("p1", "Onboarding"));
        product.Children.Add(Node("p2", "Search"));

        var tech = Node("tech", "Engineering");
        tech.Children.Add(Node("t1", "Migration"));
        tech.Children.Add(Node("t2", "Telemetry"));
        tech.Children.Add(Node("t3", "Test coverage"));

        var launch = Node("launch", "Launch");
        launch.Children.Add(Node("l1", "Docs"));
        launch.Children.Add(Node("l2", "Webinar"));

        var risk = Node("risk", "Risks");
        risk.Children.Add(Node("r1", "Data volume"));

        root.Children.Add(product);
        root.Children.Add(tech);
        root.Children.Add(launch);
        root.Children.Add(risk);

        return (Collect(root), []);
    }

    static DiagramNode Node(string id, string text, DiagramNodeShape shape = DiagramNodeShape.Rectangle) =>
        new(id, text) { Shape = shape };

    /// <summary>Flattens a nested hierarchy into the flat list the control binds to.</summary>
    static ObservableCollection<DiagramNode> Collect(DiagramNode root)
    {
        var all = new ObservableCollection<DiagramNode>();
        var stack = new Stack<DiagramNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            all.Add(node);

            for (var i = node.Children.Count - 1; i >= 0; i--)
                stack.Push(node.Children[i]);
        }

        return all;
    }
}
