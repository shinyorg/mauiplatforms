using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Skia;

namespace Sample.Features.Office;

public partial class DocumentViewerPage : ContentPage
{
    readonly UnsupportedFeatureCollector unsupported = new();
    WordDocument? document;
    double zoom = 1.0;
    bool dark;

    public DocumentViewerPage()
    {
        this.InitializeComponent();
        this.UpdateZoomLabel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (this.document is not null)
            return;

        var bytes = SampleOfficeDocuments.BuildDocument();
        this.document = await WordDocument.OpenAsync(new MemoryStream(bytes), this.unsupported);
        this.Viewer.Document = this.document;

        // The viewer preserves everything it cannot draw; this is how it says what that was.
        this.NotRenderedLabel.Text = this.unsupported.Features.Count == 0
            ? "Nothing in this document went unrendered."
            : "Preserved but not shown: " + string.Join(", ", this.unsupported.Features.Select(x => x.Feature).Distinct());
    }

    void OnZoomIn(object? sender, EventArgs e) => this.SetZoom(this.zoom + 0.1);

    void OnZoomOut(object? sender, EventArgs e) => this.SetZoom(this.zoom - 0.1);

    void SetZoom(double value)
    {
        this.zoom = Math.Clamp(value, 0.5, 2.0);
        this.Viewer.Zoom = this.zoom;
        this.UpdateZoomLabel();
    }

    void UpdateZoomLabel() => this.ZoomLabel.Text = $"{this.zoom * 100:0} %";

    void OnToggleTheme(object? sender, EventArgs e)
    {
        this.dark = !this.dark;
        // null, not DocumentTheme.Light: unset means "follow the app appearance", which is the
        // behaviour worth demoing. Passing Light would pin it and hide that.
        this.Viewer.Theme = this.dark ? DocumentTheme.Dark : null;
    }

    void OnScrollTop(object? sender, EventArgs e) => this.Viewer.Controller?.ScrollTo(0);

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (this.Handler is null)
        {
            this.document?.Dispose();
            this.document = null;
        }
    }
}
