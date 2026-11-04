using Shiny.Controls.Media;
using Shiny.Maui.Controls;

namespace Sample.Features.Media;

public partial class VideoTextPage : ShinyContentPage
{
    public VideoTextPage()
    {
        this.InitializeComponent();

        this.FontPicker.SelectedIndex = 0;
        this.OverlayPicker.SelectedIndex = 0;
        this.MarqueePicker.SelectedIndex = 0;
    }

    void OnFontChanged(object? sender, EventArgs e)
        => this.Video.FontFamily = this.FontPicker.SelectedIndex <= 0 ? null : (string)this.FontPicker.SelectedItem;

    void OnOverlayChanged(object? sender, EventArgs e)
        => this.Video.OverlayColor = this.OverlayPicker.SelectedIndex switch
        {
            1 => Colors.White,
            2 => Color.FromArgb("#312E81"),
            3 => null,
            _ => Colors.Black
        };

    void OnMarqueeChanged(object? sender, EventArgs e)
        => this.Video.Marquee = (MarqueeDirection)Math.Max(0, this.MarqueePicker.SelectedIndex);

    void OnItalicToggled(object? sender, ToggledEventArgs e)
        => this.Video.FontAttributes = e.Value
            ? FontAttributes.Bold | FontAttributes.Italic
            : FontAttributes.Bold;
}
