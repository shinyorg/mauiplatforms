using Microsoft.Maui.Controls;

namespace Sample.Features.Docking;

public sealed class OutputPanel : ContentView
{
    public OutputPanel()
    {
        Content = new VerticalStackLayout
        {
            Padding = 12,
            Spacing = 4,
            Children =
            {
                new Label { Text = "Output", FontAttributes = FontAttributes.Bold },
                new Label { Text = "[info] Build succeeded.", FontFamily = "Courier" },
                new Label { Text = "[warn] Nothing else to report.", FontFamily = "Courier" }
            }
        };
    }
}
