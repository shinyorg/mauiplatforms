using System.Text.RegularExpressions;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Essentials.Web.Interop;

namespace Microsoft.Maui.Essentials.Web.Devices;

/// <summary>The <see cref="DevicePlatform"/> reported by apps running in a browser.</summary>
public static class WebDevicePlatform
{
	public static DevicePlatform Web { get; } = DevicePlatform.Create("Web");
}

/// <summary>
/// Device information from the user agent. Browsers do not expose hardware identity, so
/// Model is the browser, Name is the operating system and Version is the OS version.
/// </summary>
public partial class WebDeviceInfo : IDeviceInfo
{
	readonly Lazy<(string Os, string OsVersion, string Browser)> _agent = new(ParseUserAgent);

	public string Model => _agent.Value.Browser;

	public string Manufacturer => BrowserJs.NavigatorString("vendor") is { Length: > 0 } vendor ? vendor : "Unknown";

	public string Name => _agent.Value.Os;

	public string VersionString => _agent.Value.OsVersion;

	public Version Version => Version.TryParse(VersionString, out var v) ? v
		: int.TryParse(VersionString, out var major) ? new Version(major, 0) : new Version(0, 0);

	public DevicePlatform Platform => WebDevicePlatform.Web;

	public DeviceIdiom Idiom
	{
		get
		{
			if (!BrowserJs.MatchMedia("(pointer: coarse)"))
				return DeviceIdiom.Desktop;
			var screen = BrowserJs.Global("screen");
			var shortSide = Math.Min(BrowserJs.Number(screen, "width", 0), BrowserJs.Number(screen, "height", 0));
			return shortSide >= 600 ? DeviceIdiom.Tablet : DeviceIdiom.Phone;
		}
	}

	public DeviceType DeviceType => DeviceType.Physical;

	static (string, string, string) ParseUserAgent()
	{
		var ua = BrowserJs.NavigatorString("userAgent") ?? string.Empty;

		var (os, version) = ua switch
		{
			_ when Match(OsPattern.Iphone(), ua) is { } v => ("iOS", v),
			_ when Match(OsPattern.Android(), ua) is { } v => ("Android", v),
			_ when Match(OsPattern.Windows(), ua) is { } v => ("Windows", v),
			_ when Match(OsPattern.Mac(), ua) is { } v => ("macOS", v),
			_ when ua.Contains("CrOS", StringComparison.Ordinal) => ("ChromeOS", ""),
			_ when ua.Contains("Linux", StringComparison.Ordinal) => ("Linux", ""),
			_ => ("Unknown", ""),
		};

		var browser = Match(OsPattern.Edge(), ua) is { } edge ? $"Edge {edge}"
			: Match(OsPattern.Firefox(), ua) is { } firefox ? $"Firefox {firefox}"
			: Match(OsPattern.Chrome(), ua) is { } chrome ? $"Chrome {chrome}"
			: Match(OsPattern.Safari(), ua) is { } safari ? $"Safari {safari}"
			: "Browser";

		return (os, version.Replace('_', '.'), browser);
	}

	static string? Match(Regex regex, string input) => regex.Match(input) is { Success: true } m ? m.Groups[1].Value : null;

	static partial class OsPattern
	{
		[GeneratedRegex(@"(?:iPhone|iPad|iPod).*? OS (\d+(?:_\d+)*)")]
		public static partial Regex Iphone();

		[GeneratedRegex(@"Android (\d+(?:\.\d+)*)")]
		public static partial Regex Android();

		[GeneratedRegex(@"Windows NT (\d+\.\d+)")]
		public static partial Regex Windows();

		[GeneratedRegex(@"Mac OS X (\d+(?:[_.]\d+)*)")]
		public static partial Regex Mac();

		[GeneratedRegex(@"Edg/(\d+)")]
		public static partial Regex Edge();

		[GeneratedRegex(@"Firefox/(\d+)")]
		public static partial Regex Firefox();

		[GeneratedRegex(@"Chrome/(\d+)")]
		public static partial Regex Chrome();

		[GeneratedRegex(@"Version/(\d+(?:\.\d+)?).*Safari")]
		public static partial Regex Safari();
	}
}
