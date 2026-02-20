using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.MacOS;

namespace Sample;

public class FlyoutDemoPage : FlyoutPage
{
    public FlyoutDemoPage()
    {
        Title = "FlyoutPage Demo";
        FlyoutLayoutBehavior = FlyoutLayoutBehavior.Split;

        // Empty flyout page (required by FlyoutPage API, but native sidebar ignores it)
        Flyout = new ContentPage { Title = "Menu" };

        Detail = new NavigationPage(CreateDetailPage("Home", "Welcome home! Browse your content here.", "#4A90E2"));

        // Configure native sidebar items
        MacOSFlyoutPage.SetSidebarItems(this, new List<MacOSSidebarItem>
        {
            new MacOSSidebarItem
            {
                Title = "Favorites",
                Children = new List<MacOSSidebarItem>
                {
                    new() { Title = "Home", SystemImage = "house.fill", Tag = "home" },
                    new() { Title = "Settings", SystemImage = "gear", Tag = "settings" },
                    new() { Title = "About", SystemImage = "info.circle", Tag = "about" },
                }
            },
            new MacOSSidebarItem
            {
                Title = "More",
                Children = new List<MacOSSidebarItem>
                {
                    new() { Title = "Notifications", SystemImage = "bell.fill", Tag = "notifications" },
                    new() { Title = "Profile", SystemImage = "person.circle", Tag = "profile" },
                }
            },
        });

        MacOSFlyoutPage.SetSidebarSelectionChanged(this, item =>
        {
            Detail = item.Tag switch
            {
                "home" => new NavigationPage(CreateDetailPage("Home", "Welcome home! Browse your content here.", "#4A90E2")),
                "settings" => new NavigationPage(CreateDetailPage("Settings", "Configure your preferences and options.", "#7B68EE")),
                "about" => new NavigationPage(CreateDetailPage("About", "MAUI macOS FlyoutPage demo.\nSidebar powered by native NSOutlineView source list.", "#2ECC71")),
                "notifications" => new NavigationPage(CreateDetailPage("Notifications", "Stay up to date with alerts and messages.", "#F39C12")),
                "profile" => new NavigationPage(CreateDetailPage("Profile", "View and edit your profile information.", "#1ABC9C")),
                _ => Detail,
            };
        });
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
                    new Border
                    {
                        BackgroundColor = Color.FromArgb(accentColor),
                        HeightRequest = 4,
                        WidthRequest = 200,
                        HorizontalOptions = LayoutOptions.Center,
                        StrokeThickness = 0,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 2 },
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
}
