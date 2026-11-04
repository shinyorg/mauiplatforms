using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Controls.Gauge;

namespace Sample.Features.Gauges;

public partial class GaugeViewModel : ObservableObject
{
    // A crude five-speed car: enough to make a tachometer climb, shift and fall the way a real one does.
    static readonly double[] GearRatios = [0, 3.6, 2.1, 1.4, 1.0, 0.8];
    const double IdleRpm = 0.8;
    const double RedlineRpm = 6.6;

    CancellationTokenSource? drive;
    int gear = 1;

    [ObservableProperty] double rpm = IdleRpm;
    [ObservableProperty] double speed;
    [ObservableProperty] bool isThrottleDown;
    [ObservableProperty] bool isDriving;
    [ObservableProperty] double fuel = 72;
    [ObservableProperty] double temperature = 88;
    [ObservableProperty] double cpu = 35;

    // The playground gauge
    [ObservableProperty] double playValue = 40;
    [ObservableProperty] double damping = 0.65;
    [ObservableProperty] double durationMs = 800;
    [ObservableProperty] GaugeAnimation animation = GaugeAnimation.Spring;
    [ObservableProperty] GaugeNeedleStyle needleStyle = GaugeNeedleStyle.Tapered;

    public string GearText => this.Speed < 1 && !this.IsThrottleDown ? "N" : this.gear.ToString();

    public TimeSpan Duration => TimeSpan.FromMilliseconds(this.DurationMs);

    partial void OnDurationMsChanged(double value) => this.OnPropertyChanged(nameof(this.Duration));


    [RelayCommand]
    void ToggleDrive()
    {
        if (this.IsDriving)
            this.Stop();
        else
            this.Start();
    }


    [RelayCommand]
    void Random()
    {
        this.PlayValue = System.Random.Shared.Next(0, 101);
        this.Cpu = System.Random.Shared.Next(5, 100);
    }


    [RelayCommand]
    void SetAnimation(string name) => this.Animation = Enum.Parse<GaugeAnimation>(name);


    [RelayCommand]
    void SetNeedle(string name) => this.NeedleStyle = Enum.Parse<GaugeNeedleStyle>(name);


    public void Start()
    {
        if (this.IsDriving)
            return;

        this.IsDriving = true;
        this.drive = new CancellationTokenSource();
        _ = this.DriveLoop(this.drive.Token);
    }


    public void Stop()
    {
        this.drive?.Cancel();
        this.drive = null;
        this.IsDriving = false;
        this.IsThrottleDown = false;
    }


    /// <summary>
    /// Ten readings a second, the way a real vehicle feed arrives. The gauges' springs turn the steps
    /// into one continuous sweep - which is the point of the demo.
    /// </summary>
    async Task DriveLoop(CancellationToken ct)
    {
        var autoThrottle = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(100, ct);

                // With nobody holding the pedal, the demo drives itself: floor it, lift, repeat.
                autoThrottle = (autoThrottle + 1) % 120;
                var throttle = this.IsThrottleDown || autoThrottle < 70;

                var ratio = GearRatios[this.gear];
                // Low gears rev quickly, tall ones slowly.
                var rpm = throttle
                    ? this.Rpm + 0.12 * ratio
                    : this.Rpm - 0.25;

                if (throttle && rpm >= RedlineRpm && this.gear < GearRatios.Length - 1)
                {
                    // Shift up: the needle drops sharply, then climbs again in the next gear.
                    this.gear++;
                    rpm *= GearRatios[this.gear] / ratio;
                }
                else if (!throttle && rpm < 2.2 && this.gear > 1)
                {
                    this.gear--;
                    rpm *= GearRatios[this.gear] / ratio;
                }

                this.Rpm = Math.Clamp(rpm, IdleRpm, 7.8);
                var target = (this.Rpm - IdleRpm) / GearRatios[this.gear] * 26;
                this.Speed = Math.Max(0, this.Speed + (target - this.Speed) * 0.2);
                this.Fuel = Math.Max(0, this.Fuel - (throttle ? 0.05 : 0.01));
                this.Temperature = Math.Clamp(this.Temperature + (throttle ? 0.15 : -0.1), 85, 118);
                this.OnPropertyChanged(nameof(this.GearText));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
