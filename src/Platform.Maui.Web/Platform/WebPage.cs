using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.Web;

/// <summary>Controls how a page is presented when pushed with PushModalAsync.</summary>
public enum WebModalPresentationStyle
{
	/// <summary>The page covers the whole window (the default, as on phones).</summary>
	FullWindow,

	/// <summary>The page is shown as a centered dialog over a dimmed backdrop.</summary>
	Dialog,
}

/// <summary>
/// Attached properties for web-specific page behavior, mirroring the GTK backend's GtkPage.
/// </summary>
/// <example>
/// <code>
/// WebPage.SetModalPresentationStyle(page, WebModalPresentationStyle.Dialog);
/// WebPage.SetModalWidth(page, 480);
/// WebPage.SetModalHeight(page, 320);
/// await Navigation.PushModalAsync(page);
///
/// // A dialog sized to its content
/// WebPage.SetModalPresentationStyle(page, WebModalPresentationStyle.Dialog);
/// WebPage.SetModalSizesToContent(page, true);
/// </code>
/// </example>
public static class WebPage
{
	public static readonly BindableProperty ModalPresentationStyleProperty =
		BindableProperty.CreateAttached("ModalPresentationStyle", typeof(WebModalPresentationStyle), typeof(WebPage), WebModalPresentationStyle.FullWindow);

	public static readonly BindableProperty ModalWidthProperty =
		BindableProperty.CreateAttached("ModalWidth", typeof(double), typeof(WebPage), -1d);

	public static readonly BindableProperty ModalHeightProperty =
		BindableProperty.CreateAttached("ModalHeight", typeof(double), typeof(WebPage), -1d);

	public static readonly BindableProperty ModalMinWidthProperty =
		BindableProperty.CreateAttached("ModalMinWidth", typeof(double), typeof(WebPage), -1d);

	public static readonly BindableProperty ModalMinHeightProperty =
		BindableProperty.CreateAttached("ModalMinHeight", typeof(double), typeof(WebPage), -1d);

	public static readonly BindableProperty ModalSizesToContentProperty =
		BindableProperty.CreateAttached("ModalSizesToContent", typeof(bool), typeof(WebPage), false);

	/// <summary>Whether clicking the dialog backdrop pops the modal. Defaults to true.</summary>
	public static readonly BindableProperty ModalDismissOnBackdropProperty =
		BindableProperty.CreateAttached("ModalDismissOnBackdrop", typeof(bool), typeof(WebPage), true);

	public static WebModalPresentationStyle GetModalPresentationStyle(BindableObject obj) => (WebModalPresentationStyle)obj.GetValue(ModalPresentationStyleProperty);
	public static void SetModalPresentationStyle(BindableObject obj, WebModalPresentationStyle value) => obj.SetValue(ModalPresentationStyleProperty, value);

	public static double GetModalWidth(BindableObject obj) => (double)obj.GetValue(ModalWidthProperty);
	public static void SetModalWidth(BindableObject obj, double value) => obj.SetValue(ModalWidthProperty, value);

	public static double GetModalHeight(BindableObject obj) => (double)obj.GetValue(ModalHeightProperty);
	public static void SetModalHeight(BindableObject obj, double value) => obj.SetValue(ModalHeightProperty, value);

	public static double GetModalMinWidth(BindableObject obj) => (double)obj.GetValue(ModalMinWidthProperty);
	public static void SetModalMinWidth(BindableObject obj, double value) => obj.SetValue(ModalMinWidthProperty, value);

	public static double GetModalMinHeight(BindableObject obj) => (double)obj.GetValue(ModalMinHeightProperty);
	public static void SetModalMinHeight(BindableObject obj, double value) => obj.SetValue(ModalMinHeightProperty, value);

	public static bool GetModalSizesToContent(BindableObject obj) => (bool)obj.GetValue(ModalSizesToContentProperty);
	public static void SetModalSizesToContent(BindableObject obj, bool value) => obj.SetValue(ModalSizesToContentProperty, value);

	public static bool GetModalDismissOnBackdrop(BindableObject obj) => (bool)obj.GetValue(ModalDismissOnBackdropProperty);
	public static void SetModalDismissOnBackdrop(BindableObject obj, bool value) => obj.SetValue(ModalDismissOnBackdropProperty, value);
}
