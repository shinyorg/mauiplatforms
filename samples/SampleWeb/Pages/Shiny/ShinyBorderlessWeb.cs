using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Handlers;

namespace SampleWeb.Pages.Shiny;

/// <summary>
/// Shiny.Maui.Controls builds its text fields (TextEntry, TagEntry, AutoCompleteEntry, the TableView
/// EntryCell, the chat and quick-entry boxes) from internal BorderlessEntry / BorderlessEditor types
/// inside a Border it draws itself. Its handler mappings strip the native field chrome on Android,
/// iOS, Mac Catalyst and Windows only; this is the browser's equivalent, so the &lt;input&gt; stops drawing
/// a second border, background and focus ring inside the control's.
/// </summary>
public static class ShinyBorderlessWeb
{
	const string BorderlessEntry = "Shiny.Maui.Controls.BorderlessEntry";
	const string BorderlessEditor = "Shiny.Maui.Controls.BorderlessEditor";

	public static MauiAppBuilder UseShinyBorderlessWeb(this MauiAppBuilder builder)
	{
		// The types are internal to Shiny.Maui.Controls, so they're matched by name.
		EntryHandler.Mapper.AppendToMapping("ShinyBorderless", (handler, view) =>
			handler.PlatformView.ToggleClass("maui-borderless", view.GetType().FullName == BorderlessEntry));
		EditorHandler.Mapper.AppendToMapping("ShinyBorderless", (handler, view) =>
			handler.PlatformView.ToggleClass("maui-borderless", view.GetType().FullName == BorderlessEditor));
		return builder;
	}
}
