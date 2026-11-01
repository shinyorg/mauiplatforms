using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Devices;

/// <summary>
/// Screen metrics in physical pixels (CSS pixels × devicePixelRatio). KeepScreenOn uses the
/// Screen Wake Lock API and is re-acquired when the tab becomes visible again.
/// </summary>
public class WebDeviceDisplay : IDeviceDisplay
{
	EventHandler<DisplayInfoChangedEventArgs>? _changed;
	bool _observing;
	bool _keepScreenOn;
	DisplayInfo? _last;

	public bool KeepScreenOn
	{
		get => _keepScreenOn;
		set
		{
			_keepScreenOn = value;
			EssentialsJs.WhenLoaded(() => EssentialsJs.SetKeepScreenOn(value));
		}
	}

	public DisplayInfo MainDisplayInfo
	{
		get
		{
			var screen = BrowserJs.Global("screen");
			var density = BrowserJs.Number(JSHost.GlobalThis, "devicePixelRatio", 1);
			var width = BrowserJs.Number(screen, "width", 0);
			var height = BrowserJs.Number(screen, "height", 0);

			var screenOrientation = BrowserJs.Has(screen, "orientation") ? screen!.GetPropertyAsJSObject("orientation") : null;
			var orientationType = screenOrientation?.GetPropertyAsString("type") ?? "";
			var angle = BrowserJs.Number(screenOrientation, "angle", 0);
			var orientation = orientationType.StartsWith("portrait", StringComparison.Ordinal) ? DisplayOrientation.Portrait
				: orientationType.StartsWith("landscape", StringComparison.Ordinal) ? DisplayOrientation.Landscape
				: width >= height ? DisplayOrientation.Landscape : DisplayOrientation.Portrait;
			var rotation = ((int)angle % 360) switch
			{
				90 => DisplayRotation.Rotation90,
				180 => DisplayRotation.Rotation180,
				270 => DisplayRotation.Rotation270,
				_ => DisplayRotation.Rotation0,
			};

			// Browsers do not report the refresh rate.
			return new DisplayInfo(width * density, height * density, density, orientation, rotation, 60f);
		}
	}

	public event EventHandler<DisplayInfoChangedEventArgs> MainDisplayInfoChanged
	{
		add
		{
			_changed += value;
			if (_observing)
				return;
			// The JS listener cannot be removed, so register once and filter on subscribers.
			_observing = true;
			_last = MainDisplayInfo;
			EssentialsJs.WhenLoaded(() => EssentialsJs.ObserveDisplay(OnDisplayChanged));
		}
		remove => _changed -= value;
	}

	void OnDisplayChanged()
	{
		if (_changed is null)
			return;
		var info = MainDisplayInfo;
		if (_last is { } last && last.Equals(info))
			return;
		_last = info;
		_changed.Invoke(this, new DisplayInfoChangedEventArgs(info));
	}
}
