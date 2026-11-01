using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>Tracks the browser's prefers-color-scheme and applies Application.UserAppTheme.</summary>
public static class WebTheme
{
	static bool _started;

	public static AppTheme SystemTheme { get; private set; } = AppTheme.Light;

	internal static void Start()
	{
		if (_started)
			return;
		_started = true;
		SystemTheme = Parse(Dom.GetColorScheme());
		Dom.ObserveColorScheme(scheme =>
		{
			SystemTheme = Parse(scheme);
			if (IPlatformApplication.Current?.Application is { } app)
			{
				WebElementHandler.InvalidateAllMeasurements();
				app.ThemeChanged();
			}
		});
	}

	/// <summary>Forces the page theme, or follows the system when <see cref="AppTheme.Unspecified"/>.</summary>
	public static void SetUserTheme(AppTheme theme)
		=> Dom.SetRootTheme(theme switch
		{
			AppTheme.Dark => "dark",
			AppTheme.Light => "light",
			_ => null,
		});

	static AppTheme Parse(string scheme) => scheme == "dark" ? AppTheme.Dark : AppTheme.Light;
}
