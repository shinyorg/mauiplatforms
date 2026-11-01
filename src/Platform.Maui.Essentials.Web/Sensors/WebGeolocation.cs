using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Sensors;

/// <summary>
/// Geolocation API. Requires a secure context (https or localhost); the browser prompts for
/// permission on first use and a denial surfaces as <see cref="PermissionException"/>.
/// </summary>
public class WebGeolocation : IGeolocation
{
	// GeolocationPositionError codes.
	const int PermissionDenied = 1;
	const int Timeout = 3;

	Location? _last;
	int _watchId;

	public bool IsEnabled => BrowserJs.Has(BrowserJs.Global("navigator"), "geolocation");

	public bool IsListeningForeground { get; private set; }

	public event EventHandler<GeolocationLocationChangedEventArgs>? LocationChanged;

	public event EventHandler<GeolocationListeningFailedEventArgs>? ListeningFailed;

	public Task<Location?> GetLastKnownLocationAsync() => Task.FromResult(_last);

	public async Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancelToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		EnsureEnabled();
		await EssentialsJs.EnsureLoadedAsync();

		var timeout = request.Timeout > TimeSpan.Zero ? request.Timeout.TotalMilliseconds : 0;
		var position = await EssentialsJs.GetLocation(HighAccuracy(request.DesiredAccuracy), timeout).WaitAsync(cancelToken);
		var error = position.GetPropertyAsInt32("error");
		if (error == PermissionDenied)
			throw new PermissionException("Location permission was denied.");
		if (error is Timeout or not 0)
			return null;

		return _last = ToLocation(position);
	}

	public async Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);
		EnsureEnabled();
		if (IsListeningForeground)
			throw new InvalidOperationException("Already listening to location changes.");

		await EssentialsJs.EnsureLoadedAsync();
		IsListeningForeground = true;
		_watchId = EssentialsJs.WatchLocation(HighAccuracy(request.DesiredAccuracy), OnPosition);
		return true;
	}

	public void StopListeningForeground()
	{
		if (!IsListeningForeground)
			return;
		IsListeningForeground = false;
		EssentialsJs.ClearWatch(_watchId);
		_watchId = 0;
	}

	void OnPosition(JSObject position)
	{
		var error = position.GetPropertyAsInt32("error");
		if (error != 0)
		{
			// Timeouts and transient unavailability keep the watch alive; denial ends it.
			if (error == PermissionDenied)
			{
				StopListeningForeground();
				ListeningFailed?.Invoke(this, new GeolocationListeningFailedEventArgs(GeolocationError.Unauthorized));
			}
			return;
		}

		_last = ToLocation(position);
		LocationChanged?.Invoke(this, new GeolocationLocationChangedEventArgs(_last));
	}

	void EnsureEnabled()
	{
		if (!IsEnabled)
			throw new FeatureNotSupportedException("This browser does not support geolocation.");
	}

	static bool HighAccuracy(GeolocationAccuracy accuracy) => accuracy >= GeolocationAccuracy.High;

	static Location ToLocation(JSObject p)
	{
		static double? Optional(JSObject p, string name)
		{
			var value = p.GetPropertyAsDouble(name);
			return double.IsNaN(value) ? null : value;
		}

		return new Location(p.GetPropertyAsDouble("latitude"), p.GetPropertyAsDouble("longitude"),
			DateTimeOffset.FromUnixTimeMilliseconds((long)p.GetPropertyAsDouble("timestamp")))
		{
			Altitude = Optional(p, "altitude"),
			Accuracy = Optional(p, "accuracy"),
			VerticalAccuracy = Optional(p, "altitudeAccuracy"),
			Course = Optional(p, "heading"),
			Speed = Optional(p, "speed"),
		};
	}
}

/// <summary>Browsers have no geocoder; use a web geocoding service instead.</summary>
public class WebGeocoding : IGeocoding
{
	public Task<IEnumerable<Placemark>> GetPlacemarksAsync(double latitude, double longitude)
		=> throw new FeatureNotSupportedException("Geocoding is not available in the browser.");

	public Task<IEnumerable<Location>> GetLocationsAsync(string address)
		=> throw new FeatureNotSupportedException("Geocoding is not available in the browser.");
}
