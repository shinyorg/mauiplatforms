using System.Collections.Specialized;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// The title bar used by NavigationPage and Shell: flyout toggle, back button, title and toolbar items.
/// </summary>
public sealed class WebNavigationBar : IDisposable
{
	public const double Height = 52;

	readonly WebElement _menu;
	readonly WebElement _back;
	readonly WebElement _backText;
	readonly WebElement _title;
	readonly WebElement _toolbar;
	readonly List<IDisposable> _subscriptions = new();
	readonly List<IDisposable> _toolbarSubscriptions = new();
	readonly IFontManager? _fonts;
	Page? _toolbarPage;

	public WebNavigationBar(IFontManager? fonts)
	{
		_fonts = fonts;
		Element = new WebElement("div", "maui-view maui-navbar");
		Element.SetAttribute("role", "toolbar");

		_menu = Button("☰", "Open navigation");
		_back = Button("‹", "Back");
		_back.SetStyle("font-size", "17px");
		_backText = new WebElement("span");
		_back.Append(_backText);
		_title = new WebElement("div", "title");
		_title.SetAttribute("role", "heading");
		_title.SetAttribute("aria-level", "1");
		_toolbar = new WebElement("div");
		_toolbar.SetStyle("display", "flex");

		Element.Append(_menu);
		Element.Append(_back);
		Element.Append(_title);
		Element.Append(_toolbar);

		_subscriptions.Add(_menu.On("click", () => MenuClicked?.Invoke()));
		_subscriptions.Add(_back.On("click", () => BackClicked?.Invoke()));
		ShowMenu(false);
		ShowBack(false, null);
	}

	public WebElement Element { get; }

	public event Action? MenuClicked;

	public event Action? BackClicked;

	public bool IsVisible { get; private set; } = true;

	public void SetVisible(bool visible)
	{
		IsVisible = visible;
		Element.ToggleClass("maui-collapsed", !visible);
	}

	public void SetTitle(string? title) => _title.SetText(title);

	public void ShowMenu(bool visible) => _menu.SetProperty("hidden", !visible);

	public void ShowBack(bool visible, string? text)
	{
		_back.SetProperty("hidden", !visible);
		_backText.SetText(text);
	}

	public void SetColors(Color? background, Color? foreground)
	{
		Element.SetStyle("--maui-navbar-bg", WebCss.Color(background));
		Element.SetStyle("--maui-navbar-fg", WebCss.Color(foreground));
	}

	public void SetBackground(Brush? brush)
	{
		if (!Brush.IsNullOrEmpty(brush))
			Element.SetStyle("background", WebCss.Paint(brush));
		else
			Element.SetStyle("background", null);
	}

	public void Arrange(double width) => Element.SetFrame(0, 0, width, Height);

	/// <summary>Shows a page's primary toolbar items and tracks changes to the collection.</summary>
	public void SetToolbarItems(Page? page)
	{
		if (_toolbarPage?.ToolbarItems is INotifyCollectionChanged old)
			old.CollectionChanged -= OnToolbarItemsChanged;
		_toolbarPage = page;
		if (page?.ToolbarItems is INotifyCollectionChanged items)
			items.CollectionChanged += OnToolbarItemsChanged;
		RebuildToolbar();
	}

	void OnToolbarItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildToolbar();

	void RebuildToolbar()
	{
		foreach (var s in _toolbarSubscriptions)
			s.Dispose();
		_toolbarSubscriptions.Clear();
		_toolbar.Clear();

		if (_toolbarPage is null)
			return;

		foreach (var item in _toolbarPage.ToolbarItems.OrderBy(i => i.Priority))
		{
			var button = Button(null, item.Text);
			if (item.IconImageSource is { } icon && !icon.IsEmpty)
			{
				var img = new WebElement("img");
				img.SetAttribute("alt", "");
				button.Append(img);
				LoadIcon(img, icon).FireAndForget("Toolbar icon");
			}
			else
			{
				button.SetText(item.Text);
			}
			button.SetProperty("disabled", !item.IsEnabled);
			var captured = item;
			_toolbarSubscriptions.Add(button.On("click", () => ((IMenuItemController)captured).Activate()));
			_toolbar.Append(button);
		}
	}

	async Task LoadIcon(WebElement img, ImageSource source)
		=> img.SetAttribute("src", await WebImageSource.ResolveAsync(source, _fonts));

	static WebElement Button(string? text, string? label)
	{
		var button = new WebElement("button");
		button.SetAttribute("type", "button");
		if (label is not null)
			button.SetAttribute("aria-label", label);
		if (text is not null)
			button.SetText(text);
		return button;
	}

	public void Dispose()
	{
		SetToolbarItems(null);
		foreach (var s in _subscriptions)
			s.Dispose();
		_subscriptions.Clear();
	}
}
