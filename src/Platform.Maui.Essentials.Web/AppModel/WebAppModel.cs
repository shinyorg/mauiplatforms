using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Essentials.Web.Interop;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.Web.AppModel;

/// <summary>
/// Version history kept in Preferences. Nothing is read or written until first use, so
/// resolving the service (e.g. at MauiApp.Build) does not consume first-launch state.
/// </summary>
public class WebVersionTracking(IPreferences preferences, IAppInfo appInfo) : IVersionTracking
{
	const string SharedName = "Microsoft.Maui.Essentials.VersionTracking";
	const string VersionsKey = "VersionTracking.Versions";
	const string BuildsKey = "VersionTracking.Builds";

	State? _state;

	sealed record State(bool FirstEver, bool FirstForVersion, bool FirstForBuild, List<string> Versions, List<string> Builds);

	State Current => _state ??= Load();

	State Load()
	{
		var firstEver = !preferences.ContainsKey(VersionsKey, SharedName) || !preferences.ContainsKey(BuildsKey, SharedName);
		var versions = firstEver ? new List<string>() : Read(VersionsKey);
		var builds = firstEver ? new List<string>() : Read(BuildsKey);

		var firstForVersion = !versions.Contains(CurrentVersion);
		if (firstForVersion)
			versions.Add(CurrentVersion);
		var firstForBuild = !builds.Contains(CurrentBuild);
		if (firstForBuild)
			builds.Add(CurrentBuild);

		preferences.Set(VersionsKey, string.Join('|', versions), SharedName);
		preferences.Set(BuildsKey, string.Join('|', builds), SharedName);
		return new State(firstEver, firstForVersion, firstForBuild, versions, builds);
	}

	List<string> Read(string key)
		=> preferences.Get(key, string.Empty, SharedName).Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

	public void Track() => _ = Current;

	public bool IsFirstLaunchEver => Current.FirstEver;

	public bool IsFirstLaunchForCurrentVersion => Current.FirstForVersion;

	public bool IsFirstLaunchForCurrentBuild => Current.FirstForBuild;

	public string CurrentVersion => appInfo.VersionString;

	public string CurrentBuild => appInfo.BuildString;

	public string? PreviousVersion => Previous(Current.Versions);

	public string? PreviousBuild => Previous(Current.Builds);

	public string? FirstInstalledVersion => Current.Versions.FirstOrDefault();

	public string? FirstInstalledBuild => Current.Builds.FirstOrDefault();

	public IReadOnlyList<string> VersionHistory => Current.Versions.ToArray();

	public IReadOnlyList<string> BuildHistory => Current.Builds.ToArray();

	public bool IsFirstLaunchForVersion(string version) => CurrentVersion == version && IsFirstLaunchForCurrentVersion;

	public bool IsFirstLaunchForBuild(string build) => CurrentBuild == build && IsFirstLaunchForCurrentBuild;

	static string? Previous(List<string> history) => history.Count >= 2 ? history[^2] : null;
}

/// <summary>Opens URLs in a new tab. There is no in-app browser, so every launch mode opens a tab.</summary>
public class WebBrowser : IBrowser
{
	public async Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options)
	{
		ArgumentNullException.ThrowIfNull(uri);
		await EssentialsJs.EnsureLoadedAsync();
		EssentialsJs.OpenWindow(uri.AbsoluteUri);
		return true;
	}
}

public class WebLauncher : ILauncher
{
	static readonly HashSet<string> TabSchemes = new(StringComparer.OrdinalIgnoreCase) { "http", "https", "ftp", "blob", "data" };
	static readonly HashSet<string> HandlerSchemes = new(StringComparer.OrdinalIgnoreCase) { "mailto", "tel", "sms", "geo" };

	public Task<bool> CanOpenAsync(Uri uri) => Task.FromResult(uri is not null && (TabSchemes.Contains(uri.Scheme) || HandlerSchemes.Contains(uri.Scheme)));

	public async Task<bool> OpenAsync(Uri uri)
	{
		if (!await CanOpenAsync(uri))
			return false;
		await EssentialsJs.EnsureLoadedAsync();
		// Protocol handlers would leave an empty tab behind, so navigate in place for those.
		if (HandlerSchemes.Contains(uri.Scheme))
			EssentialsJs.Navigate(uri.AbsoluteUri);
		else
			EssentialsJs.OpenWindow(uri.AbsoluteUri);
		return true;
	}

	/// <summary>Files are offered as a download; the browser decides how to open them.</summary>
	public async Task<bool> OpenAsync(OpenFileRequest request)
	{
		ArgumentNullException.ThrowIfNull(request?.File);
		await EssentialsJs.EnsureLoadedAsync();
		var bytes = await File.ReadAllBytesAsync(request.File.FullPath);
		EssentialsJs.Download(new ArraySegment<byte>(bytes), request.File.FileName, WebContentTypes.Of(request.File));
		return true;
	}

	public async Task<bool> TryOpenAsync(Uri uri) => await OpenAsync(uri);
}

/// <summary>Maps open in OpenStreetMap, or Google Maps when directions are requested.</summary>
public class WebMap(IBrowser browser) : IMap
{
	public Task OpenAsync(double latitude, double longitude, MapLaunchOptions options)
		=> browser.OpenAsync(BuildUri(latitude, longitude, options), BrowserLaunchMode.External);

	public Task OpenAsync(Placemark placemark, MapLaunchOptions options)
		=> browser.OpenAsync(BuildUri(placemark, options), BrowserLaunchMode.External);

	public async Task<bool> TryOpenAsync(double latitude, double longitude, MapLaunchOptions options)
	{
		await OpenAsync(latitude, longitude, options);
		return true;
	}

	public async Task<bool> TryOpenAsync(Placemark placemark, MapLaunchOptions options)
	{
		await OpenAsync(placemark, options);
		return true;
	}

	static Uri BuildUri(double latitude, double longitude, MapLaunchOptions? options)
	{
		var lat = latitude.ToString(CultureInfo.InvariantCulture);
		var lon = longitude.ToString(CultureInfo.InvariantCulture);
		if (options is { NavigationMode: not NavigationMode.None })
			return new Uri($"https://www.google.com/maps/dir/?api=1&destination={lat},{lon}&travelmode={TravelMode(options.NavigationMode)}");
		return new Uri($"https://www.openstreetmap.org/?mlat={lat}&mlon={lon}#map=16/{lat}/{lon}");
	}

	static Uri BuildUri(Placemark placemark, MapLaunchOptions? options)
	{
		ArgumentNullException.ThrowIfNull(placemark);
		var address = string.Join(", ", new[]
		{
			placemark.Thoroughfare, placemark.Locality, placemark.AdminArea, placemark.PostalCode, placemark.CountryName,
		}.Where(s => !string.IsNullOrWhiteSpace(s)));
		var query = Uri.EscapeDataString(address);
		if (options is { NavigationMode: not NavigationMode.None })
			return new Uri($"https://www.google.com/maps/dir/?api=1&destination={query}&travelmode={TravelMode(options.NavigationMode)}");
		return new Uri($"https://www.openstreetmap.org/search?query={query}");
	}

	static string TravelMode(NavigationMode mode) => mode switch
	{
		NavigationMode.Walking => "walking",
		NavigationMode.Bicycling => "bicycling",
		NavigationMode.Transit => "transit",
		_ => "driving",
	};
}

/// <summary>Browsers have no app shortcut menu.</summary>
public class WebAppActions : IAppActions
{
	public bool IsSupported => false;

	public Task<IEnumerable<AppAction>> GetAsync() => throw new FeatureNotSupportedException("App actions are not available in the browser.");

	public Task SetAsync(IEnumerable<AppAction> actions) => throw new FeatureNotSupportedException("App actions are not available in the browser.");

#pragma warning disable CS0067 // Never raised: the browser has no app actions.
	public event EventHandler<AppActionEventArgs>? AppActionActivated;
#pragma warning restore CS0067
}
