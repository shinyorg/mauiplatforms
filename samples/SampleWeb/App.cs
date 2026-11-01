using Microsoft.Maui.Controls;

namespace SampleWeb;

public class App : Application
{
	public App()
	{
		Resources = Theme.CreateResources();
		Theme.Apply(Resources, RequestedTheme);
		RequestedThemeChanged += (_, e) => Theme.Apply(Resources, e.RequestedTheme);
	}

	protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
		=> new(new MainShell()) { Title = "MAUI Web Gallery" };
}
