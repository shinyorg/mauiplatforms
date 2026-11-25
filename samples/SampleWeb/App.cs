using Microsoft.Maui.Controls;

namespace SampleWeb;

public class App : Application
{
	public App()
	{
		Resources = Theme.CreateResources();
		// UXDivers Popups' theme and styles: the Shiny Shell demo's UXDivers dialogs are built from them.
		Resources.MergedDictionaries.Add(new UXDivers.Popups.Maui.Controls.DarkTheme());
		Resources.MergedDictionaries.Add(new UXDivers.Popups.Maui.Controls.PopupStyles());
		Theme.Apply(Resources, RequestedTheme);
		RequestedThemeChanged += (_, e) => Theme.Apply(Resources, e.RequestedTheme);
	}

	protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
	{
		var window = new Window(new MainShell()) { Title = "MAUI Web Gallery" };
		// The animated app background picked on the Animated Backgrounds page, behind every page.
		AppBackground.Attach(window);
		return window;
	}
}
