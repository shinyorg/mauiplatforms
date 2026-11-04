using Shiny.Maui.Controls;

namespace Sample.Features.Confetti;

public partial class ConfettiPage : ContentPage
{
    readonly IConfettiService confetti;

    public ConfettiPage()
    {
        InitializeComponent();
        this.confetti = IPlatformApplication.Current!.Services.GetRequiredService<IConfettiService>();
    }

    void OnFireworks(object? sender, EventArgs e) => _ = this.confetti.FireAsync(ConfettiPreset.Fireworks);

    void OnSideCannons(object? sender, EventArgs e) => _ = this.confetti.FireAsync(ConfettiPreset.SideCannons);

    async void OnFromElement(object? sender, EventArgs e)
    {
        this.StatusLabel.Text = "In the air...";
        await this.confetti.FireFromAsync(this.FromElementButton, ConfettiPreset.Burst);
        this.StatusLabel.Text = "Landed.";
    }

    void OnClear(object? sender, EventArgs e) => this.confetti.Clear();
}
