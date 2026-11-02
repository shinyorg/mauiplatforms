namespace Sample;

public class App : Microsoft.Maui.Controls.Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var browse = new BrowsePage();
        var controls = new ControlsPage();

        var tabs = new TabbedPage { BackgroundColor = Theme.Background };

        var home = new NavigationPage(new HomePage(
            browseGenre: genre =>
            {
                browse.ShowGenre(genre);
                tabs.CurrentPage = browse;
            },
            showControls: () => tabs.CurrentPage = controls))
        {
            Title = "Home",
            BackgroundColor = Theme.Background,
        };

        tabs.Children.Add(home);
        tabs.Children.Add(browse);
        tabs.Children.Add(controls);
        tabs.Children.Add(new DevicePage());
        tabs.Children.Add(new MapPage());

        var window = new Window(tabs);
        window.Activated += (s, e) => Console.WriteLine("[Lifecycle] Window Activated");
        window.Deactivated += (s, e) => Console.WriteLine("[Lifecycle] Window Deactivated");
        window.Stopped += (s, e) => Console.WriteLine("[Lifecycle] Window Stopped");
        window.Resumed += (s, e) => Console.WriteLine("[Lifecycle] Window Resumed");
        return window;
    }
}
