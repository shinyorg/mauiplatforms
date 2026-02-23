using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.MacOS;

namespace Sample.Pages;

class MultiWindowPage : ContentPage
{
	readonly Label _windowCountLabel;

	public MultiWindowPage()
	{
		Title = "Multi-Window";

		_windowCountLabel = new Label
		{
			Text = "Windows: 1",
			FontSize = 16,
			HorizontalTextAlignment = TextAlignment.Center,
		};

		var openBtn = new Button { Text = "Open New Window" };
		openBtn.Clicked += (s, e) =>
		{
			Application.Current?.OpenWindow(new Window(new SecondaryWindowPage()));
			Dispatcher.Dispatch(UpdateWindowCount);
		};

		var openCompactBtn = new Button { Text = "Open Compact Titlebar Window" };
		openCompactBtn.Clicked += (s, e) =>
		{
			var win = new Window(new SecondaryWindowPage { Title = "Compact Window" });
			MacOSWindow.SetTitlebarStyle(win, MacOSTitlebarStyle.UnifiedCompact);
			MacOSWindow.SetTitleVisibility(win, MacOSTitleVisibility.Visible);
			Application.Current?.OpenWindow(win);
			Dispatcher.Dispatch(UpdateWindowCount);
		};

		var openExpandedBtn = new Button { Text = "Open Expanded Titlebar Window" };
		openExpandedBtn.Clicked += (s, e) =>
		{
			var win = new Window(new SecondaryWindowPage { Title = "Expanded Window" });
			MacOSWindow.SetTitlebarStyle(win, MacOSTitlebarStyle.Expanded);
			MacOSWindow.SetTitleVisibility(win, MacOSTitleVisibility.Visible);
			MacOSWindow.SetTitlebarTransparent(win, false);
			Application.Current?.OpenWindow(win);
			Dispatcher.Dispatch(UpdateWindowCount);
		};

		var closeBtn = new Button { Text = "Close This Window" };
		closeBtn.Clicked += (s, e) =>
		{
			if (Window != null)
				Application.Current?.CloseWindow(Window);
		};

		Content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "Multi-Window Support",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold,
					HorizontalTextAlignment = TextAlignment.Center,
				},
				_windowCountLabel,
				openBtn,
				openCompactBtn,
				openExpandedBtn,
				closeBtn,
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		UpdateWindowCount();

		// Update count when this window regains focus (e.g. after closing another window)
		if (Window != null)
			Window.Activated += OnWindowActivated;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		if (Window != null)
			Window.Activated -= OnWindowActivated;
	}

	void OnWindowActivated(object? sender, EventArgs e) => UpdateWindowCount();

	void UpdateWindowCount()
	{
		var count = Application.Current?.Windows?.Count ?? 0;
		_windowCountLabel.Text = $"Windows: {count}";
	}
}

class SecondaryWindowPage : ContentPage
{
	public SecondaryWindowPage()
	{
		Title = "Secondary Window";

		var closeBtn = new Button { Text = "Close This Window" };
		closeBtn.Clicked += (s, e) =>
		{
			if (Window != null)
				Application.Current?.CloseWindow(Window);
		};

		Content = new VerticalStackLayout
		{
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			Spacing = 16,
			Children =
			{
				new Label
				{
					Text = "🪟 Secondary Window",
					FontSize = 24,
					FontAttributes = FontAttributes.Bold,
					HorizontalTextAlignment = TextAlignment.Center,
				},
				new Label
				{
					Text = "This is a new window opened via Application.OpenWindow().\nClose it with the red button or the button below.",
					HorizontalTextAlignment = TextAlignment.Center,
					MaximumWidthRequest = 400,
				},
				closeBtn,
			}
		};
	}
}
