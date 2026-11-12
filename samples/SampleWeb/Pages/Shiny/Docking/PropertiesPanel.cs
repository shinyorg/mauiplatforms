using Microsoft.Maui.Controls;

namespace Sample.Features.Docking;

public sealed class PropertiesPanel : ContentView
{
    public PropertiesPanel()
    {
        var grid = new Grid
        {
            Padding = 12,
            RowSpacing = 6,
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };

        var rows = new (string Key, string Value)[]
        {
            ("Name", "DockHostView"),
            ("IsLocked", "False"),
            ("Panels", "4"),
            ("SchemaVersion", "1")
        };
        for (var i = 0; i < rows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(new Label { Text = rows[i].Key, TextColor = Colors.Gray, FontSize = 12 }, 0, i);
            grid.Add(new Label { Text = rows[i].Value, FontSize = 12 }, 1, i);
        }
        Content = grid;
    }
}
