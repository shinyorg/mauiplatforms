using Microsoft.Maui.Platform.MacOS;

namespace ControlGallery.Pages.Features;

public class ModalStackingPage : ContentPageBase
{
	protected override void Build()
	{
		Title = "Modal Stacking";

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Nested Modal Sheets",
						FontSize = 24,
						FontAttributes = FontAttributes.Bold,
					},
					new Label
					{
						Text = "Each button pushes a modal sheet. Sheets stack on top of each other using native AppKit sheet-on-sheet presentation.",
					},
					CreateButton("Push Sheet Modal", MacOSModalPresentationStyle.Sheet),
					CreateButton("Push Overlay Modal", MacOSModalPresentationStyle.Overlay),
					CreateButton("Push Window Modal", MacOSModalPresentationStyle.Window),
				}
			}
		};
	}

	Button CreateButton(string text, MacOSModalPresentationStyle style)
	{
		var btn = new Button { Text = text };
		btn.Clicked += async (s, e) =>
		{
			var page = CreateModalPage(1, style);
			await Navigation.PushModalAsync(page);
		};
		return btn;
	}

	static ContentPage CreateModalPage(int depth, MacOSModalPresentationStyle style)
	{
		var page = new ContentPage { Title = $"Modal #{depth}" };

		MacOSPage.SetModalPresentationStyle(page, style);
		MacOSPage.SetModalSheetWidth(page, Math.Max(300, 500 - (depth - 1) * 40));
		MacOSPage.SetModalSheetHeight(page, Math.Max(250, 400 - (depth - 1) * 30));
		MacOSPage.SetModalSheetMinWidth(page, 250);
		MacOSPage.SetModalSheetMinHeight(page, 200);

		var depthLabel = new Label
		{
			Text = $"Sheet Depth: {depth}",
			FontSize = 20,
			FontAttributes = FontAttributes.Bold,
			HorizontalOptions = LayoutOptions.Center,
		};

		var styleLabel = new Label
		{
			Text = $"Style: {style}",
			HorizontalOptions = LayoutOptions.Center,
			TextColor = Colors.Gray,
		};

		var pushSheetBtn = new Button { Text = $"Push Sheet #{depth + 1}" };
		pushSheetBtn.Clicked += async (s, e) =>
		{
			var next = CreateModalPage(depth + 1, MacOSModalPresentationStyle.Sheet);
			await page.Navigation.PushModalAsync(next);
		};

		var pushOverlayBtn = new Button { Text = $"Push Overlay #{depth + 1}" };
		pushOverlayBtn.Clicked += async (s, e) =>
		{
			var next = CreateModalPage(depth + 1, MacOSModalPresentationStyle.Overlay);
			await page.Navigation.PushModalAsync(next);
		};

		var pushWindowBtn = new Button { Text = $"Push Window #{depth + 1}" };
		pushWindowBtn.Clicked += async (s, e) =>
		{
			var next = CreateModalPage(depth + 1, MacOSModalPresentationStyle.Window);
			await page.Navigation.PushModalAsync(next);
		};

		var dismissBtn = new Button
		{
			Text = "Dismiss",
			BackgroundColor = Colors.OrangeRed,
			TextColor = Colors.White,
		};
		dismissBtn.Clicked += async (s, e) =>
		{
			await page.Navigation.PopModalAsync();
		};

		page.Content = new VerticalStackLayout
		{
			Padding = 20,
			Spacing = 12,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				depthLabel,
				styleLabel,
				pushSheetBtn,
				pushOverlayBtn,
				pushWindowBtn,
				dismissBtn,
			}
		};

		return page;
	}
}
