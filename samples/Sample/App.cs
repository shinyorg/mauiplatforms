using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Sample;

public class App : Microsoft.Maui.Controls.Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(new MainPage { Title = "Controls" });
        tabbedPage.Children.Add(new CollectionViewPage { Title = "Collection" });
        tabbedPage.Children.Add(new EssentialsPage { Title = "Essentials" });
        return new Window(tabbedPage);
    }
}
