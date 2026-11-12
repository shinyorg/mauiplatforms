using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Desktop.Docking;

namespace Sample.Features.Docking;

/// <summary>
/// Demonstrates IDockableContent: custom tab title, close protection, and
/// activation callbacks. The counter proves panel state survives tab switches
/// and drag-redocking.
/// </summary>
public sealed class EditorPanel : ContentView, IDockableContent
{
    int count;
    readonly Label counter;
    readonly Label activations;
    int activatedCount;

    public string Title => "Program.cs";
    public object? Icon => null;
    public bool CanClose => true;
    public bool CanFloat => true;

    public EditorPanel()
    {
        counter = new Label { Text = "Clicks: 0", FontSize = 12, AutomationId = "DockEditorCounter" };
        activations = new Label { Text = "Activated 0 times", FontSize = 11, TextColor = Colors.Gray };

        Content = new VerticalStackLayout
        {
            Padding = 12,
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "var builder = MauiApp.CreateBuilder();\nbuilder.UseShinyDesktop(cfg => cfg.AddDockPanel<ExplorerPanel>(\"explorer\"));",
                    FontFamily = "Courier",
                    FontSize = 12
                },
                new Button
                {
                    Text = "Click me",
                    AutomationId = "DockEditorClickMe",
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Start,
                    Command = new Command(() => counter.Text = $"Clicks: {++count}")
                },
                counter,
                activations
            }
        };
    }

    public void OnActivated() => activations.Text = $"Activated {++activatedCount} times";
    public void OnDeactivated() { }
    public bool WantsPointerDown(double x, double y) => false;
}
