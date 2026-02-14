using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Sample;

public class FlyoutDemoPage : FlyoutPage
{
    public FlyoutDemoPage()
    {
        Title = "FlyoutPage Demo";
        FlyoutLayoutBehavior = FlyoutLayoutBehavior.Split;

        var homeButton = CreateMenuButton("Home", AppColors.AccentBlue);
        var settingsButton = CreateMenuButton("Settings", AppColors.AccentPurple);
        var aboutButton = CreateMenuButton("About", AppColors.AccentGreen);

        homeButton.Clicked += (s, e) => Detail = new NavigationPage(CreateDetailPage("Home", "Welcome home! Browse your content here.", "#4A90E2"));
        settingsButton.Clicked += (s, e) => Detail = new NavigationPage(CreateDetailPage("Settings", "Configure your preferences and options.", "#7B68EE"));
        aboutButton.Clicked += (s, e) => Detail = new NavigationPage(CreateDetailPage("About", "MAUI macOS FlyoutPage demo.\nSidebar powered by NSSplitView.", "#2ECC71"));

        var flyoutPage = new ContentPage
        {
            Title = "Menu",
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(20),
                    Spacing = 12,
                    Children =
                    {
                        new Label
                        {
                            Text = "Sidebar",
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                        }.WithPrimaryText(),
                        new BoxView
                        {
                            Color = AppColors.AccentBlue,
                            HeightRequest = 2,
                        },
                        homeButton,
                        settingsButton,
                        aboutButton,
                    },
                },
            },
        };
        flyoutPage.WithSidebarBackground();
        Flyout = flyoutPage;

        Detail = new NavigationPage(CreateDetailPage("Home", "Welcome home! Browse your content here.", "#4A90E2"));
    }

    static ContentPage CreateDetailPage(string title, string description, string accentColor)
    {
        var page = new ContentPage
        {
            Title = title,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(40),
                Spacing = 20,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new BoxView
                    {
                        Color = Color.FromArgb(accentColor),
                        HeightRequest = 4,
                        WidthRequest = 200,
                        HorizontalOptions = LayoutOptions.Center,
                    },
                    new Label
                    {
                        Text = title,
                        FontSize = 32,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                    }.WithPrimaryText(),
                    new Label
                    {
                        Text = description,
                        FontSize = 18,
                        HorizontalTextAlignment = TextAlignment.Center,
                    }.WithSecondaryText(),
                },
            },
        };
        return page.WithPageBackground();
    }

    static Button CreateMenuButton(string text, Color color) => new()
    {
        Text = text,
        BackgroundColor = color,
        TextColor = Colors.White,
    };
}
