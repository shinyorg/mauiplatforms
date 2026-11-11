namespace Sample.Features.Diagrams;

public partial class InteractiveEditorPage : ContentPage
{
    const string DefaultDiagram =
        "graph LR\n" +
        "    A[Input] --> B[Process]\n" +
        "    B --> C[Output]";

    public InteractiveEditorPage()
    {
        InitializeComponent();
        DiagramEditor.Text = DefaultDiagram;
        DiagramControl.DiagramText = DefaultDiagram;
    }

    void OnPanToggled(object? sender, ToggledEventArgs e)
        => DiagramControl.AllowPanning = e.Value;

    void OnZoomToggled(object? sender, ToggledEventArgs e)
        => DiagramControl.AllowZooming = e.Value;

    void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue ?? string.Empty;
        DiagramControl.DiagramText = text;

        var parseError = DiagramControl.ParseError;
        if (!string.IsNullOrWhiteSpace(parseError))
        {
            ParseErrorLabel.Text = $"Parse error: {parseError}";
            ParseErrorLabel.IsVisible = true;
        }
        else
        {
            ParseErrorLabel.Text = string.Empty;
            ParseErrorLabel.IsVisible = false;
        }
    }
}
