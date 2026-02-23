using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS;

/// <summary>
/// Controls where a toolbar item appears relative to the sidebar tracking separator.
/// Sidebar placements (Leading/Center/Trailing) are separated by flexible spaces
/// so items distribute across the sidebar titlebar area like:
/// <c>[Leading...] ←flex→ [Center...] ←flex→ [Trailing...]  |  [Content...]</c>
/// </summary>
public enum MacOSToolbarItemPlacement
{
	/// <summary>Standard content area placement (right of the sidebar divider).</summary>
	Content = 0,

	/// <summary>Sidebar titlebar, left-aligned (equivalent to <see cref="SidebarLeading"/>).</summary>
	Sidebar = 1,

	/// <summary>Sidebar titlebar, left-aligned.</summary>
	SidebarLeading = 1,

	/// <summary>Sidebar titlebar, centered between flexible spaces.</summary>
	SidebarCenter = 2,

	/// <summary>Sidebar titlebar, right-aligned (pushed right by a flexible space).</summary>
	SidebarTrailing = 3,
}

/// <summary>
/// Attached properties for configuring macOS-specific toolbar item behavior.
/// </summary>
public static class MacOSToolbarItem
{
	/// <summary>
	/// Controls where this toolbar item appears: in the content toolbar area,
	/// or in the sidebar titlebar area (leading, center, or trailing).
	/// </summary>
	public static readonly BindableProperty PlacementProperty =
		BindableProperty.CreateAttached(
			"Placement",
			typeof(MacOSToolbarItemPlacement),
			typeof(MacOSToolbarItem),
			MacOSToolbarItemPlacement.Content);

	public static MacOSToolbarItemPlacement GetPlacement(BindableObject obj)
		=> (MacOSToolbarItemPlacement)obj.GetValue(PlacementProperty);

	public static void SetPlacement(BindableObject obj, MacOSToolbarItemPlacement value)
		=> obj.SetValue(PlacementProperty, value);
}
