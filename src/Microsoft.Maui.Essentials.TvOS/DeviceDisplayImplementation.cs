using Foundation;
using UIKit;
using Microsoft.Maui.Devices;

namespace Microsoft.Maui.Essentials.TvOS;

class DeviceDisplayImplementation : IDeviceDisplay
{
    NSObject? _observer;
    event EventHandler<DisplayInfoChangedEventArgs>? _mainDisplayInfoChanged;
    DisplayInfo _currentMetrics;

    public bool KeepScreenOn
    {
        get => UIApplication.SharedApplication.IdleTimerDisabled;
        set => UIApplication.SharedApplication.IdleTimerDisabled = value;
    }

    public DisplayInfo MainDisplayInfo => GetMainDisplayInfo();

    public event EventHandler<DisplayInfoChangedEventArgs> MainDisplayInfoChanged
    {
        add
        {
            if (_mainDisplayInfoChanged is null)
            {
                _currentMetrics = MainDisplayInfo;
                StartListeners();
            }
            _mainDisplayInfoChanged += value;
        }
        remove
        {
            _mainDisplayInfoChanged -= value;
            if (_mainDisplayInfoChanged is null)
                StopListeners();
        }
    }

    static DisplayInfo GetMainDisplayInfo()
    {
        var bounds = UIScreen.MainScreen.Bounds;
        var scale = UIScreen.MainScreen.Scale;
        var rate = (float)UIScreen.MainScreen.MaximumFramesPerSecond;

        return new DisplayInfo(
            width: bounds.Width * scale,
            height: bounds.Height * scale,
            density: scale,
            orientation: bounds.Width >= bounds.Height
                ? DisplayOrientation.Landscape
                : DisplayOrientation.Portrait,
            rotation: DisplayRotation.Rotation0,
            rate: rate);
    }

    void StartListeners()
    {
        _observer = NSNotificationCenter.DefaultCenter.AddObserver(
            UIScreen.ModeDidChangeNotification,
            OnDisplayInfoChanged);
    }

    void StopListeners()
    {
        _observer?.Dispose();
        _observer = null;
    }

    void OnDisplayInfoChanged(NSNotification notification)
    {
        var info = MainDisplayInfo;
        if (!_currentMetrics.Equals(info))
        {
            _currentMetrics = info;
            _mainDisplayInfoChanged?.Invoke(null, new DisplayInfoChangedEventArgs(info));
        }
    }
}
