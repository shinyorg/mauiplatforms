using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// <see cref="IAppInfo"/> for the browser. Lives in the core package because Controls reads
/// <c>AppInfo.Current.RequestedTheme</c> even when Essentials is not referenced.
/// Identity comes from the AssemblyMetadata the build targets generate from ApplicationId,
/// ApplicationTitle, ApplicationDisplayVersion and ApplicationVersion.
/// </summary>
public class WebAppInfo : IAppInfo
{
	readonly Assembly _assembly = Assembly.GetEntryAssembly() ?? typeof(WebAppInfo).Assembly;

	public string PackageName => Metadata("MauiApplicationId") ?? _assembly.GetName().Name ?? "app";

	public string Name => Metadata("MauiApplicationTitle")
		?? _assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
		?? _assembly.GetName().Name
		?? "App";

	public string VersionString => Metadata("MauiApplicationDisplayVersion")
		?? _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
		?? Version.ToString();

	public Version Version => Version.TryParse(Metadata("MauiApplicationDisplayVersion"), out var v)
		? v
		: _assembly.GetName().Version ?? new Version(1, 0);

	public string BuildString => Metadata("MauiApplicationVersion") ?? Version.Build.ToString();

	public AppTheme RequestedTheme => WebTheme.SystemTheme;

	public AppPackagingModel PackagingModel => AppPackagingModel.Unpackaged;

	public LayoutDirection RequestedLayoutDirection
		=> Dom.GetPropString(Dom.GetBody(), "dir") == "rtl" ? LayoutDirection.RightToLeft : LayoutDirection.LeftToRight;

	/// <summary>Browsers expose no app settings page.</summary>
	public void ShowSettingsUI()
	{
	}

	string? Metadata(string key)
	{
		foreach (var attribute in _assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
		{
			if (attribute.Key == key && !string.IsNullOrWhiteSpace(attribute.Value))
				return attribute.Value;
		}
		return null;
	}
}
