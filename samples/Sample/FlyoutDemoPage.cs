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

        var homeButton = CreateMenuButton("Home", Color.FromArgb("#4A90E2"));
        var settingsButton = CreateMenuButton("Settings", Color.FromArgb("#7B68EE"));
        var aboutButton = CreateMenuButton("About", Color.FromArgb("#2ECC71"));

        homeButton.Clicked += (s, e) => Detail = new NavigationPage(CreateDetailPage("Home", "Welcome home! Browse your content here.", "#4A90E2"));
        settingsButton.Clicked += (s, e) => Detail = new NavigationPage(CreateDetailPage("Settings", "Configure your preferences and options.", "#7B68EE"));
        aboutButton.Clicked += (s, e) => Detail = new NavigationPage(CreateDetailPage("About", "MAUI macOS FlyoutPage demo.\nSidebar powered by NSSplitView.", "#2ECC71"));

        Flyout = new ContentPage
        {
            Title = "Menu",
            BackgroundColor = Color.FromArgb("#1E1E3A"),
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
                            TextColor = Colors.White,
                        },
                        new BoxView
                        {
                            Color = Color.FromArgb("#4A90E2"),
                            HeightRequest = 2,
                        },
                        homeButton,
                        settingsButton,
                        aboutButton,
                    },
                },
            },
        };

        Detail = new NavigationPage(CreateDetailPage("Home", "Welcome home! Browse your content here.", "#4A90E2"));
    }

    static ContentPage CreateDetailPage(string title, string description, string accentColor) => new()
    {
        Title = title,
        BackgroundColor = Color.FromArgb("#1A1A2E"),
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
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                },
                new Label
                {
                    Text = description,
                    FontSize = 18,
                    TextColor = Color.FromArgb("#AAAAAA"),
                    HorizontalTextAlignment = TextAlignment.Center,
                },
            },
        },
    };

    static Button CreateMenuButton(string text, Color color) => new()
    {
        Text = text,
        BackgroundColor = color,
        TextColor = Colors.White,
    };
}
