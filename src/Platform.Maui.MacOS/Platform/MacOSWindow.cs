using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS;

/// <summary>
/// Attached properties for configuring Window behavior on macOS.
/// </summary>
public static class MacOSWindow
{
	/// <summary>
	/// When true, the titlebar is transparent and content extends behind it
	/// (modern full-bleed look). When false, the native macOS titlebar gradient
	/// material is shown. Defaults to true.
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
	/// When true, content extends under the titlebar (FullSizeContentView).
	/// Defaults to true.
	/// </summary>
	public static readonly BindableProperty FullSizeContentProperty =
		BindableProperty.CreateAttached(
			"FullSizeContent",
			typeof(bool),
			typeof(MacOSWindow),
			true);

	public static bool GetFullSizeContent(BindableObject obj)
		=> (bool)obj.GetValue(FullSizeContentProperty);

	public static void SetFullSizeContent(BindableObject obj, bool value)
		=> obj.SetValue(FullSizeContentProperty, value);

	/// <summary>
	/// Controls the visibility of the window title text. Defaults to Hidden.
	/// Set to Visible to show the title in the titlebar alongside the gradient.
	/// </summary>
	public static readonly BindableProperty TitleVisibleProperty =
		BindableProperty.CreateAttached(
			"TitleVisible",
			typeof(bool),
			typeof(MacOSWindow),
			false);

	public static bool GetTitleVisible(BindableObject obj)
		=> (bool)obj.GetValue(TitleVisibleProperty);

	public static void SetTitleVisible(BindableObject obj, bool value)
		=> obj.SetValue(TitleVisibleProperty, value);
}
