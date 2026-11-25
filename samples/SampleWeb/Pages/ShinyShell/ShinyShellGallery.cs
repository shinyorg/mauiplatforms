using Microsoft.Maui.Controls;

namespace SampleWeb.Pages.ShinyShellDemo;

/// <summary>
/// The menu's Shiny Shell tab. Shiny.Maui.Shell needs a Shell as the window's page, so each entry
/// describes a feature and opens the demo Shell (replacing this gallery) at the tab that shows it.
/// </summary>
public static class ShinyShellGallery
{
	public static IReadOnlyList<GalleryGroup> Groups { get; } =
	[
		new("Navigation",
		[
			Entry("🧭", "Navigation", "ShellNavigate",
				"NavigateTo by route and by view model, the source-generated NavigateTo methods, the Navigate.Route attached " +
				"properties, the navigation builder (push a chain, pop-and-push), modal pages, GoBack / PopToRoot with results " +
				"and absolute navigation. The detail page's view model logs IPageLifecycleAware, INavigationAware and IDisposable."),
			Entry("🛡️", "Guards", "ShellGuards",
				"An INavigationInterceptor that cancels, redirects to a login page, or asks with a dialog before opening the " +
				"detail page, bypassInterceptors, and INavigationConfirmation asking before you leave a tab with unsaved changes."),
			Entry("🔗", "App Links & AI", "ShellMore",
				"IAppLinks routing a URI to a page with its parameters, the generated AiMauiShellTools (prompt, tools and " +
				"NavigateToRoute), the IRouteRegistry, and SwitchShell between two Shells."),
		]),
		new("Dialogs & Events",
		[
			Entry("💬", "Dialogs", "ShellDialogs",
				"IDialogs (alert, confirm, prompt, action sheet) and ViewModel dialogs (IDialogAware<T> with the generated " +
				"Show…Dialog method), each switchable between MAUI's dialogs, Shiny.Maui.Shell.ShinyDialogs and " +
				"Shiny.Maui.Shell.UxDiversDialogs. UXDivers Popups has no browser build; this sample hosts its popups on a " +
				"web overlay (WebPopupManager)."),
			Entry("📜", "Events & Badges", "ShellEvents",
				"A live log of INavigator's Navigating / Navigated events, an INavigationObserver (which also sees tab taps and " +
				"back navigation, with each outcome), an IDialogObserver and the lifecycle hooks; and tab badges set by route or " +
				"by view model."),
		]),
	];

	/// <summary>Every entry, in menu order.</summary>
	public static IReadOnlyList<GalleryItem> Items { get; } = GalleryGroup.Flatten(Groups);

	static GalleryItem Entry(string icon, string title, string route, string description)
		=> new(icon, title, () => new ShellLauncherPage(title, route, description));
}

/// <summary>A gallery page that describes one Shiny Shell feature and opens the demo Shell at it.</summary>
public class ShellLauncherPage : SpecimenPage
{
	public ShellLauncherPage(string title, string route, string description)
		: base(title, "Shiny.Maui.Shell runs in its own ShinyShell, which replaces this gallery while you try it. Its flyout menu brings you back.")
	{
		var open = new Button { Text = "Open in the Shiny Shell demo" };
		open.Clicked += async (_, _) => await ShellLauncher.Launch(route);
		Specimen("What it shows", description, Wrap(10, open));
	}
}
