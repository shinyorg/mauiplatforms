using Microsoft.Maui;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace SampleWeb.Pages;

/// <summary>MAUI's accessibility properties and what the web backend turns them into.</summary>
public class AccessibilityPage : SpecimenPage
{
	public AccessibilityPage() : base("Accessibility", "SemanticProperties, AutomationProperties and SemanticScreenReader, written as in any MAUI app and rendered as ARIA. Turn on VoiceOver, NVDA or Narrator, or inspect the accessibility tree in your browser's developer tools.")
	{
		var heading = new Label { Text = "A level 2 heading", FontSize = 20, FontAttributes = FontAttributes.Bold, AutomationId = "a11y-heading" };
		SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level2);
		var toggleHeading = Secondary("Toggle heading");
		toggleHeading.Clicked += (_, _) => SemanticProperties.SetHeadingLevel(heading,
			SemanticProperties.GetHeadingLevel(heading) == SemanticHeadingLevel.None ? SemanticHeadingLevel.Level2 : SemanticHeadingLevel.None);
		Specimen("Headings", "SemanticProperties.HeadingLevel gives a view the heading role, so screen reader users can jump between sections.",
			new VerticalStackLayout { Spacing = 12, Children = { heading, toggleHeading } });

		var icon = new Button { Text = "✕", WidthRequest = 44, AutomationId = "a11y-close" };
		SemanticProperties.SetDescription(icon, "Close");
		SemanticProperties.SetHint(icon, "Closes the current panel");
		var price = new Label { Text = "$4.99/mo", AutomationId = "a11y-price" };
		SemanticProperties.SetDescription(price, "4 dollars 99 per month");
		var bot = new Image { Source = "dotnet_bot.png", HeightRequest = 64, HorizontalOptions = LayoutOptions.Start, AutomationId = "a11y-bot" };
		SemanticProperties.SetDescription(bot, "The .NET bot waving");
		var decorative = new Image { Source = "dotnet_bot.png", HeightRequest = 32, HorizontalOptions = LayoutOptions.Start, AutomationId = "a11y-decorative" };
		var swatch = new BoxView { Color = Colors.MediumSeaGreen, WidthRequest = 44, HeightRequest = 44, CornerRadius = 8, HorizontalOptions = LayoutOptions.Start, AutomationId = "a11y-swatch" };
		SemanticProperties.SetDescription(swatch, "Sea green");
		var wifi = new Switch { AutomationId = "a11y-wifi" };
		SemanticProperties.SetDescription(wifi, "Wi-Fi");
		var name = new Entry { Placeholder = "Jane Doe", AutomationId = "a11y-name" };
		SemanticProperties.SetDescription(name, "Full name");
		SemanticProperties.SetHint(name, "As it appears on your card");
		var plan = new Picker { Title = "Plan", ItemsSource = new[] { "Free", "Pro", "Team" }, AutomationId = "a11y-plan" };
		var card = new Border
		{
			Padding = 12,
			StrokeShape = new RoundRectangle { CornerRadius = 10 },
			AutomationId = "a11y-card",
			Content = new VerticalStackLayout { Children = { new Label { Text = "Pro plan" }, Muted("Everything in Free, plus sync", 13) } },
		};
		SemanticProperties.SetDescription(card, "Pro plan card");
		Specimen("Descriptions & hints", "SemanticProperties.Description names a view and Hint adds what it does. Labels read their description instead of their text, images use it as alt text (undescribed images are decorative), and a Picker falls back to its Title.",
			new VerticalStackLayout
			{
				Spacing = 14,
				Children =
				{
					Wrap(16, icon, price, swatch, wifi),
					Wrap(16, bot, decorative),
					name,
					plan,
					card,
				},
			});

		var nameCaption = new Label { Text = "Email address", AutomationId = "a11y-email-caption" };
		var email = new Entry { Placeholder = "you@example.com", AutomationId = "a11y-email" };
#pragma warning disable CS0618 // Still honored by MAUI on every platform; shown here because existing apps use them.
		AutomationProperties.SetLabeledBy(email, nameCaption);
		var legacy = new Button { Text = "⚙", WidthRequest = 44, AutomationId = "a11y-legacy" };
		AutomationProperties.SetName(legacy, "Settings");
		AutomationProperties.SetHelpText(legacy, "Opens app settings");
#pragma warning restore CS0618
		Specimen("Labeled by, Name & HelpText", "The older AutomationProperties still work: LabeledBy points a control at the label that names it, Name and HelpText act like Description and Hint.",
			new VerticalStackLayout { Spacing = 8, Children = { nameCaption, email, legacy } });

		var hiddenDecoration = new Label { Text = "★ ★ ★", AutomationId = "a11y-stars" };
		AutomationProperties.SetIsInAccessibleTree(hiddenDecoration, false);
		var groupLayout = new HorizontalStackLayout { Spacing = 8, AutomationId = "a11y-unwrapped", Children = { new Label { Text = "Still read:" }, new Label { Text = "children of a layout outside the tree" } } };
		AutomationProperties.SetIsInAccessibleTree(groupLayout, false);
		var excludedLayout = new HorizontalStackLayout { Spacing = 8, AutomationId = "a11y-excluded", Children = { new Label { Text = "Never read:" }, new Button { Text = "Hidden button" } } };
		AutomationProperties.SetExcludedWithChildren(excludedLayout, true);
		var toggleExcluded = Secondary("Include the hidden row");
		toggleExcluded.Clicked += (_, _) =>
		{
			var excluded = AutomationProperties.GetExcludedWithChildren(excludedLayout) == true;
			AutomationProperties.SetExcludedWithChildren(excludedLayout, !excluded);
			toggleExcluded.Text = excluded ? "Exclude the row again" : "Include the hidden row";
		};
		Specimen("Hiding from assistive technology", "AutomationProperties.IsInAccessibleTree=false removes a view; on a layout its children stay readable. ExcludedWithChildren removes a view and everything inside it.",
			new VerticalStackLayout { Spacing = 10, Children = { hiddenDecoration, groupLayout, excludedLayout, toggleExcluded } });

		var announce = new Button { Text = "Announce", AutomationId = "a11y-announce" };
		announce.Clicked += (_, _) => SemanticScreenReader.Default.Announce("Saved. Your changes are live.");
		var scaled = new Label { Text = "This text follows your browser's text size.", FontSize = 16, AutomationId = "a11y-scaled" };
		var fixedSize = new Label { Text = "This text stays at 16 pixels.", FontSize = 16, FontAutoScalingEnabled = false, AutomationId = "a11y-fixed" };
		Specimen("Announcements & text size", "SemanticScreenReader.Announce speaks through a live region. Font sizes use rem so they follow the browser's text size setting, unless FontAutoScalingEnabled is false.",
			new VerticalStackLayout { Spacing = 12, Children = { announce, scaled, fixedSize } });
	}
}
