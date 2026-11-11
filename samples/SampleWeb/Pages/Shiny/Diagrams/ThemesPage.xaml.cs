using Shiny.Maui.Controls.MermaidDiagrams.Theming;

namespace Sample.Features.Diagrams;

public partial class ThemesPage : ContentPage
{
    public ThemesPage()
    {
        InitializeComponent();
        ThemePicker.SelectedIndex = 0;
    }

    void OnThemePickerSelectedIndexChanged(object sender, EventArgs e)
    {
        DiagramControl.Theme = ThemePicker.SelectedIndex switch
        {
            0 => new DefaultTheme(),
            1 => new DarkTheme(),
            2 => new ForestTheme(),
            3 => new NeutralTheme(),
            _ => new DefaultTheme()
        };
    }
}
