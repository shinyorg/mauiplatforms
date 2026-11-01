using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Sensors;

/// <summary>
/// Shared start/stop plumbing for sensors backed by DeviceMotion/DeviceOrientation events or the
/// Generic Sensor API. Desktop browsers expose the event types but never fire them, so
/// IsSupported means "the API exists", not "readings will arrive". On iOS, Start must be
/// called from a user gesture so the motion permission prompt can appear.
/// </summary>
public abstract class WebSensor
{
	readonly string _kind;
	int _id;
	int _generation;

	protected WebSensor(string kind) => _kind = kind;

	public bool IsMonitoring { get; private set; }

	protected abstract string SupportGlobal { get; }

	public bool IsSupported => JSHost.GlobalThis.HasProperty(SupportGlobal);

	public void Start(SensorSpeed sensorSpeed)
	{
		if (!IsSupported)
			throw new FeatureNotSupportedException($"The {_kind} sensor is not available in this browser.");
		if (IsMonitoring)
			throw new InvalidOperationException($"The {_kind} sensor is already being monitored.");

		IsMonitoring = true;
		var generation = ++_generation;
		var interval = sensorSpeed switch
		{
			SensorSpeed.Fastest => 0,
			SensorSpeed.Game => 20,
			SensorSpeed.UI => 60,
			_ => 200,
		};
		StartAsync(generation, interval);
	}

	async void StartAsync(int generation, double interval)
	{
		try
		{
			await EssentialsJs.EnsureLoadedAsync();
			var id = await EssentialsJs.StartSensor(_kind, interval, reading => OnReading(
				reading.GetPropertyAsDouble("x"), reading.GetPropertyAsDouble("y"),
				reading.GetPropertyAsDouble("z"), reading.GetPropertyAsDouble("w")));

			// Stop() ran (or a new Start began) while the permission prompt was open.
			if (generation != _generation)
			{
				if (id != 0)
					EssentialsJs.StopSensor(id);
				return;
			}
			_id = id;
			if (id == 0)
				IsMonitoring = false;
		}
		catch (Exception ex)
		{
			IsMonitoring = false;
			Console.Error.WriteLine($"[maui-web-essentials] {_kind} failed to start: {ex.Message}");
		}
	}

	public void Stop()
	{
		if (!IsMonitoring)
			return;
		IsMonitoring = false;
		_generation++;
		if (_id != 0)
			EssentialsJs.StopSensor(_id);
		_id = 0;
	}

	protected abstract void OnReading(double x, double y, double z, double w);
}

public class WebAccelerometer() : WebSensor("accelerometer"), IAccelerometer
{
	// Shake: total acceleration well beyond gravity, debounced.
	const double ShakeThresholdG = 2.5;
	DateTime _lastShake;

	protected override string SupportGlobal => "DeviceMotionEvent";

	public event EventHandler<AccelerometerChangedEventArgs>? ReadingChanged;

	public event EventHandler? ShakeDetected;

	protected override void OnReading(double x, double y, double z, double w)
	{
		ReadingChanged?.Invoke(this, new AccelerometerChangedEventArgs(new AccelerometerData(x, y, z)));
		if (Math.Sqrt(x * x + y * y + z * z) > ShakeThresholdG && DateTime.UtcNow - _lastShake > TimeSpan.FromMilliseconds(500))
		{
			_lastShake = DateTime.UtcNow;
			ShakeDetected?.Invoke(this, EventArgs.Empty);
		}
	}
}

public class WebGyroscope() : WebSensor("gyroscope"), IGyroscope
{
	protected override string SupportGlobal => "DeviceMotionEvent";

	public event EventHandler<GyroscopeChangedEventArgs>? ReadingChanged;

	protected override void OnReading(double x, double y, double z, double w)
		=> ReadingChanged?.Invoke(this, new GyroscopeChangedEventArgs(new GyroscopeData(x, y, z)));
}

/// <summary>Generic Sensor API Magnetometer (Chromium, behind a flag on most platforms).</summary>
public class WebMagnetometer() : WebSensor("magnetometer"), IMagnetometer
{
	protected override string SupportGlobal => "Magnetometer";

	public event EventHandler<MagnetometerChangedEventArgs>? ReadingChanged;

	protected override void OnReading(double x, double y, double z, double w)
		=> ReadingChanged?.Invoke(this, new MagnetometerChangedEventArgs(new MagnetometerData(x, y, z)));
}

public class WebOrientationSensor() : WebSensor("orientation"), IOrientationSensor
{
	protected override string SupportGlobal => "DeviceOrientationEvent";

	public event EventHandler<OrientationSensorChangedEventArgs>? ReadingChanged;

	protected override void OnReading(double x, double y, double z, double w)
		=> ReadingChanged?.Invoke(this, new OrientationSensorChangedEventArgs(new OrientationSensorData(x, y, z, w)));
}

public class WebCompass() : WebSensor("compass"), ICompass
{
	const double Alpha = 0.25;
	bool _lowPass;
	double? _filtered;

	protected override string SupportGlobal => "DeviceOrientationEvent";

	public event EventHandler<CompassChangedEventArgs>? ReadingChanged;

	public void Start(SensorSpeed sensorSpeed, bool applyLowPassFilter)
	{
		_lowPass = applyLowPassFilter;
		_filtered = null;
		Start(sensorSpeed);
	}

	protected override void OnReading(double x, double y, double z, double w)
	{
		var heading = x;
		if (_lowPass)
		{
			// Filter on the circle so 359° → 1° does not swing through 180°.
			if (_filtered is { } previous)
			{
				var delta = ((heading - previous + 540) % 360) - 180;
				heading = (previous + Alpha * delta + 360) % 360;
			}
			_filtered = heading;
		}
		ReadingChanged?.Invoke(this, new CompassChangedEventArgs(new CompassData(heading)));
	}
}

/// <summary>Browsers expose no pressure sensor.</summary>
public class WebBarometer : IBarometer
{
	public bool IsSupported => false;

	public bool IsMonitoring => false;

	public void Start(SensorSpeed sensorSpeed) => throw new FeatureNotSupportedException("The barometer is not available in the browser.");

	public void Stop()
	{
	}

#pragma warning disable CS0067 // Never raised: the browser has no barometer.
	public event EventHandler<BarometerChangedEventArgs>? ReadingChanged;
#pragma warning restore CS0067
}
