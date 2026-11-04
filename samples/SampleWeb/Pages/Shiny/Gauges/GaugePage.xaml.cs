namespace Sample.Features.Gauges;

public partial class GaugePage : ContentPage
{
    public GaugePage()
    {
        InitializeComponent();
    }

    GaugeViewModel? ViewModel => BindingContext as GaugeViewModel;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel?.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel?.Stop();
    }

    void OnThrottlePressed(object? sender, EventArgs e)
    {
        if (ViewModel is { } vm)
            vm.IsThrottleDown = true;
    }

    void OnThrottleReleased(object? sender, EventArgs e)
    {
        if (ViewModel is { } vm)
            vm.IsThrottleDown = false;
    }

    void OnSweep(object? sender, EventArgs e) => PlayGauge.Sweep();

    void OnResetPeak(object? sender, EventArgs e) => PlayGauge.ResetPeak();
}
