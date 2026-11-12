using Microsoft.Maui.Controls;

namespace Sample.Features.Docking;

public sealed class SolutionExplorerPanel : ContentView
{
    public SolutionExplorerPanel()
    {
        Content = new VerticalStackLayout
        {
            Padding = 12,
            Spacing = 6,
            Children =
            {
                new Label { Text = "Solution Explorer", FontAttributes = FontAttributes.Bold },
                new Label { Text = "▸ src" },
                new Label { Text = "▸ samples" },
                new Label { Text = "▸ tests" }
            }
        };
    }
}
