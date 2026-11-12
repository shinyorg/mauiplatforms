using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace SampleWeb.Pages;

/// <summary>
/// A ScrollView with padding whose content overflows: the full-width cards should sit inside the padding on both
/// sides, and inside the viewport a classic (non-overlay) scrollbar leaves.
/// </summary>
public class ScrollingPage : ContentPage
{
	public ScrollingPage()
	{
		Title = "Scrolling";

		var stack = new VerticalStackLayout { Spacing = 10, AutomationId = "paddedContent" };
		stack.Children.Add(new Label
		{
			Text = "ScrollView with Padding = 20. Each card fills the width; its right edge should stay clear of the padding and of the scrollbar.",
			FontSize = 14,
		});
		// Text long enough to wrap measures as wide as the space it's offered, the case where padding was lost.
		stack.Children.Add(new Label
		{
			Text = string.Join(" ", Enumerable.Repeat("This paragraph wraps onto several lines at any window width, so it asks for all of the width it is offered.", 4)),
			FontSize = 14,
		});
		for (var i = 1; i <= 30; i++)
		{
			var row = new Grid
			{
				ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)),
			};
			row.Add(new Label { Text = $"Card {i}", FontSize = 15 }, 0, 0);
			row.Add(new Label { Text = "right edge ›", FontSize = 13, TextColor = Colors.Gray }, 1, 0);
			var card = new Border
			{
				Padding = new Thickness(14, 10),
				StrokeThickness = 1,
				StrokeShape = new RoundRectangle { CornerRadius = 10 },
				Content = row,
			};
			Theme.Bind(card, VisualElement.BackgroundColorProperty, Theme.Paper);
			Theme.BindBrush(card, Border.StrokeProperty, Theme.Line);
			stack.Children.Add(card);
		}

		Content = new ScrollView
		{
			AutomationId = "paddedScroll",
			Padding = new Thickness(20),
			Content = stack,
		};
	}
}
