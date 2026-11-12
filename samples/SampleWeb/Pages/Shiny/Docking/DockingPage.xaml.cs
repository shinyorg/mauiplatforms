using Shiny.Maui.Controls.Desktop.Docking;

namespace Sample.Features.Docking;

public partial class DockingPage : ContentPage
{
    public DockingPage()
    {
        InitializeComponent();

        Dock.InitialLayout = BuildLayout();
        Dock.Events.PanelActivated += (_, e) => Status.Text = $"Activated: {e.PanelTypeId}";
        Dock.Events.LayoutChanged += (_, e) => Status.Text = $"Layout changed: {e.Reason}";
    }

    static DockRoot BuildLayout() => new()
    {
        MainWindow = new DockWindowState
        {
            LeftRail = new DockGroup
            {
                Tabs = { new DockTab { PanelTypeId = "solution-explorer" } }
            },
            RightRail = new DockGroup
            {
                Tabs = { new DockTab { PanelTypeId = "properties" } }
            },
            DocumentArea = new DockSplit
            {
                Orientation = DockOrientation.Vertical,
                Ratio = 0.7,
                First = new DockGroup
                {
                    Tabs = { new DockTab { PanelTypeId = "editor" } }
                },
                Second = new DockGroup
                {
                    Tabs = { new DockTab { PanelTypeId = "output" } }
                }
            }
        }
    };

    void OnShowExplorer(object sender, EventArgs e) => _ = Dock.ShowPanelAsync("solution-explorer");
    void OnShowOutput(object sender, EventArgs e) => _ = Dock.ShowPanelAsync("output", DockArea.Bottom);
    void OnReset(object sender, EventArgs e) => _ = Dock.ResetLayoutAsync();
    void OnLockChanged(object sender, CheckedChangedEventArgs e) => Dock.IsLocked = e.Value;
}
