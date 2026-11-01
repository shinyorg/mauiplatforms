using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Devices;

/// <summary>
/// Battery Status API (Chromium only). Until the async API answers, and where it is missing,
/// the battery reads as full with an unknown state rather than guessing.
/// </summary>
public class WebBattery : IBattery
{
	EventHandler<BatteryInfoChangedEventArgs>? _changed;
	bool _started;
	double _level = 1;
	BatteryState _state = BatteryState.Unknown;
	BatteryPowerSource _source = BatteryPowerSource.Unknown;

	public WebBattery() => EnsureStarted();

	public double ChargeLevel => _level;

	public BatteryState State => _state;

	public BatteryPowerSource PowerSource => _source;

	public EnergySaverStatus EnergySaverStatus => EnergySaverStatus.Unknown;

	public event EventHandler<BatteryInfoChangedEventArgs> BatteryInfoChanged
	{
		add => _changed += value;
		remove => _changed -= value;
	}

	// Browsers expose no power-saver signal.
	public event EventHandler<EnergySaverStatusChangedEventArgs> EnergySaverStatusChanged
	{
		add { }
		remove { }
	}

	void EnsureStarted()
	{
		if (_started)
			return;
		_started = true;
		EssentialsJs.WhenLoaded(() => _ = EssentialsJs.StartBattery(OnBattery));
	}

	void OnBattery(double level, bool charging, double chargingTime)
	{
		_level = level;
		_state = charging ? (level >= 1 || chargingTime == 0 ? BatteryState.Full : BatteryState.Charging) : BatteryState.Discharging;
		_source = charging ? BatteryPowerSource.AC : BatteryPowerSource.Battery;
		_changed?.Invoke(this, new BatteryInfoChangedEventArgs(_level, _state, _source));
	}
}

public class WebVibration : IVibration
{
	public bool IsSupported => BrowserJs.Has(BrowserJs.Global("navigator"), "vibrate");

	public void Vibrate() => Vibrate(TimeSpan.FromMilliseconds(500));

	public void Vibrate(TimeSpan duration)
	{
		if (!IsSupported)
			throw new FeatureNotSupportedException("This browser does not support vibration.");
		var ms = (int)Math.Clamp(duration.TotalMilliseconds, 1, 5000);
		EssentialsJs.WhenLoaded(() => EssentialsJs.Vibrate(ms));
	}

	public void Cancel()
	{
		if (IsSupported)
			EssentialsJs.WhenLoaded(() => EssentialsJs.Vibrate(0));
	}
}

public class WebHapticFeedback : IHapticFeedback
{
	public bool IsSupported => BrowserJs.Has(BrowserJs.Global("navigator"), "vibrate");

	public void Perform(HapticFeedbackType type)
	{
		if (!IsSupported)
			throw new FeatureNotSupportedException("This browser does not support haptic feedback.");
		var ms = type == HapticFeedbackType.LongPress ? 50 : 10;
		EssentialsJs.WhenLoaded(() => EssentialsJs.Vibrate(ms));
	}
}

/// <summary>Browsers cannot drive the camera torch outside a live MediaStream.</summary>
public class WebFlashlight : IFlashlight
{
	public Task<bool> IsSupportedAsync() => Task.FromResult(false);

	public Task TurnOnAsync() => throw new FeatureNotSupportedException("The flashlight is not available in the browser.");

	public Task TurnOffAsync() => throw new FeatureNotSupportedException("The flashlight is not available in the browser.");
}
