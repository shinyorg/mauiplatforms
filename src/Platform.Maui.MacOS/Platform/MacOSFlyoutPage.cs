using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS;

/// <summary>
/// Attached properties for configuring a native macOS sidebar on FlyoutPage.
/// Used with <see cref="Microsoft.Maui.Platform.MacOS.Handlers.NativeSidebarFlyoutPageHandler"/>.
/// </summary>
public static class MacOSFlyoutPage
{
	/// <summary>
	/// Provides structured sidebar items for the native NSOutlineView source list.
	/// </summary>
	public static readonly BindableProperty SidebarItemsProperty =
		BindableProperty.CreateAttached(
			"SidebarItems",
			typeof(IList<MacOSSidebarItem>),
			typeof(MacOSFlyoutPage),
			null);

	public static IList<MacOSSidebarItem>? GetSidebarItems(BindableObject obj)
		=> (IList<MacOSSidebarItem>?)obj.GetValue(SidebarItemsProperty);

	public static void SetSidebarItems(BindableObject obj, IList<MacOSSidebarItem>? value)
		=> obj.SetValue(SidebarItemsProperty, value);

	/// <summary>
	/// Callback invoked when a sidebar item is selected.
	/// </summary>
	public static readonly BindableProperty SidebarSelectionChangedProperty =
		BindableProperty.CreateAttached(
			"SidebarSelectionChanged",
			typeof(Action<MacOSSidebarItem>),
			typeof(MacOSFlyoutPage),
			null);

	public static Action<MacOSSidebarItem>? GetSidebarSelectionChanged(BindableObject obj)
		=> (Action<MacOSSidebarItem>?)obj.GetValue(SidebarSelectionChangedProperty);

	public static void SetSidebarSelectionChanged(BindableObject obj, Action<MacOSSidebarItem>? value)
		=> obj.SetValue(SidebarSelectionChangedProperty, value);
}
