using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Presentation;
using Shiny.Controls.Office.Skia;

namespace Sample.Features.Office;

public partial class SlideViewerPage : ContentPage
{
    readonly UnsupportedFeatureCollector unsupported = new();
    SlideDeck? deck;
    bool dark;

    public SlideViewerPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (this.deck is not null)
            return;

        var bytes = SampleOfficeDocuments.BuildDeck();
        this.deck = await SlideDeck.OpenAsync(new MemoryStream(bytes), this.unsupported);
        this.Viewer.Deck = this.deck;
        this.Update();
    }

    void OnNext(object? sender, EventArgs e) => this.Viewer.Next();

    void OnPrevious(object? sender, EventArgs e) => this.Viewer.Previous();

    void OnToggleMode(object? sender, EventArgs e)
    {
        this.Viewer.Mode = this.Viewer.Mode == SlideViewMode.Grid ? SlideViewMode.Single : SlideViewMode.Grid;
        this.ModeButton.Text = this.Viewer.Mode == SlideViewMode.Grid ? "Single" : "Grid";
    }

    void OnToggleTheme(object? sender, EventArgs e)
    {
        this.dark = !this.dark;
        // null, not SlideTheme.Light: unset means "follow the app appearance", which is the
        // behaviour worth demoing. Passing Light would pin it and hide that.
        this.Viewer.Theme = this.dark ? SlideTheme.Dark : null;
    }

    // Full screen on a black surround, tap or the auto-hiding bar to drive it, speaker notes on demand.
    void OnPresent(object? sender, EventArgs e) => this.Viewer.StartPresenting();

    // The show writes the slide it ended on back to the viewer, so the counter has to catch up.
    void OnPresentingChanged(object? sender, bool presenting)
    {
        if (!presenting)
            this.Update();
    }

    void OnSlideChanged(object? sender, int index) => this.Update();

    void Update()
    {
        if (this.deck is null)
            return;

        var index = this.Viewer.SlideIndex;
        this.CounterLabel.Text = $"{index + 1} / {this.deck.Slides.Count}";
        this.NotesLabel.Text = this.deck.Slides[index].Notes ?? string.Empty;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (this.Handler is null)
        {
            this.deck?.Dispose();
            this.deck = null;
        }
    }
}
