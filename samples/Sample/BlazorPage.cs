#if MACAPP
using Microsoft.Maui.Platform.MacOS.Controls;

namespace Sample;

public class BlazorPage : ContentPage
{
    public BlazorPage()
    {
        Title = "Blazor WebView";
        this.WithPageBackground();

        var backButton = new Button
        {
            Text = "← Back",
            BackgroundColor = AppColors.AccentPink,
            TextColor = Colors.White,
            HeightRequest = 44,
            Margin = new Thickness(10, 10, 10, 0)
        };
        backButton.Clicked += async (s, e) => await Navigation.PopAsync();

        var blazorWebView = new MacOSBlazorWebView
        {
            HostPage = "wwwroot/index.html",
            HeightRequest = 400,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        }.WithPageBackground();

        blazorWebView.RootComponents.Add(new BlazorRootComponent
        {
            Selector = "#app",
            ComponentType = typeof(SampleMac.Components.Counter)
        });

        Content = new VerticalStackLayout
        {
            Children = { backButton, blazorWebView }
        };
    }
}
#endif
