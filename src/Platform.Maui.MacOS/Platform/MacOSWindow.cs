using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS;

/// <summary>
/// Controls how the window toolbar/titlebar is rendered.
/// Maps to NSWindowToolbarStyle.
/// </summary>
public enum MacOSTitlebarStyle
{
	/// <summary>The system chooses the style automatically.</summary>
	Automatic = 0,

	/// <summary>Full-height toolbar with title below the toolbar.</summary>
	Expanded = 1,

	/// <summary>Standard unified toolbar with title inline. Default for MAUI windows.</summary>
	Unified = 2,

	/// <summary>Compact unified toolbar — shorter/narrower, ideal for tool windows.</summary>
	UnifiedCompact = 3,
}

/// <summary>
/// Controls whether the window title text is shown in the titlebar.
/// Maps to NSWindowTitleVisibility.
/// </summary>
public enum MacOSTitleVisibility
{
	/// <summary>The title text is visible in the titlebar.</summary>
	Visible = 0,

	/// <summary>The title text is hidden. Default for MAUI windows.</summary>
	Hidden = 1,
}

/// <summary>
/// Attached properties for configuring NSWindow titlebar appearance on macOS.
/// </summary>
/// <example>
/// <code>
/// var window = new Window(page);
/// MacOSWindow.SetTitlebarStyle(window, MacOSTitlebarStyle.UnifiedCompact);
/// MacOSWindow.SetTitlebarTransparent(window, false);
/// MacOSWindow.SetTitleVisibility(window, MacOSTitleVisibility.Visible);
/// </code>
/// </example>
public static class MacOSWindow
{
	/// <summary>
	/// The toolbar/titlebar style for the window. Defaults to <see cref="MacOSTitlebarStyle.Unified"/>.
	/// Use <see cref="MacOSTitlebarStyle.UnifiedCompact"/> for a shorter, tool-window style titlebar.
	/// </summary>
	public static readonly BindableProperty TitlebarStyleProperty =
		BindableProperty.CreateAttached(
			"TitlebarStyle",
			typeof(MacOSTitlebarStyle),
			typeof(MacOSWindow),
			MacOSTitlebarStyle.Unified);

	public static MacOSTitlebarStyle GetTitlebarStyle(BindableObject obj)
		=> (MacOSTitlebarStyle)obj.GetValue(TitlebarStyleProperty);

	public static void SetTitlebarStyle(BindableObject obj, MacOSTitlebarStyle value)
		=> obj.SetValue(TitlebarStyleProperty, value);

	/// <summary>
	/// When true, the titlebar blends with the window content. Defaults to true.
	/// </summary>
	public static readonly BindableProperty TitlebarTransparentProperty =
		BindableProperty.CreateAttached(
			"TitlebarTransparent",
			typeof(bool),
			typeof(MacOSWindow),
			true);

	public static bool GetTitlebarTransparent(BindableObject obj)
		=> (bool)obj.GetValue(TitlebarTransparentProperty);

	public static void SetTitlebarTransparent(BindableObject obj, bool value)
		=> obj.SetValue(TitlebarTransparentProperty, value);

	/// <summary>
	/// Controls whether the window title text is visible. Defaults to <see cref="MacOSTitleVisibility.Hidden"/>.
	/// </summary>
	public static readonly BindableProperty TitleVisibilityProperty =
		BindableProperty.CreateAttached(
			"TitleVisibility",
			typeof(MacOSTitleVisibility),
			typeof(MacOSWindow),
			MacOSTitleVisibility.Hidden);

	public static MacOSTitleVisibility GetTitleVisibility(BindableObject obj)
		=> (MacOSTitleVisibility)obj.GetValue(TitleVisibilityProperty);

	public static void SetTitleVisibility(BindableObject obj, MacOSTitleVisibility value)
		=> obj.SetValue(TitleVisibilityProperty, value);
}
