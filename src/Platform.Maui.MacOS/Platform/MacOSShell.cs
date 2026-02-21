using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS;

/// <summary>
/// Attached properties for configuring Shell behavior on macOS.
/// </summary>
public static class MacOSShell
{
	/// <summary>
	/// When true, Shell uses a native NSOutlineView source list sidebar instead of custom views.
	/// </summary>
	public static readonly BindableProperty UseNativeSidebarProperty =
		BindableProperty.CreateAttached(
			"UseNativeSidebar",
			typeof(bool),
			typeof(MacOSShell),
			false);

	public static bool GetUseNativeSidebar(BindableObject obj)
		=> (bool)obj.GetValue(UseNativeSidebarProperty);

	public static void SetUseNativeSidebar(BindableObject obj, bool value)
		=> obj.SetValue(UseNativeSidebarProperty, value);
}
