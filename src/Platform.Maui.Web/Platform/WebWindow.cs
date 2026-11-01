using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// The platform window: a full-viewport element hosting a MAUI window's content and modal stack.
/// </summary>
public class WebWindow
{
	readonly List<Modal> _modals = new();

	sealed record Modal(IView Page, WebElement Layer, WebElement? Panel, IDisposable? BackdropClick);
	readonly IDisposable _resize;
	WebElement? _content;

	public WebWindow(WebElement host)
	{
		Root = new WebElement("div", "maui-window");
		host.Append(Root);
		_resize = Root.OnResize(OnResized);
		WebLayoutScheduler.Register(this);
	}

	public WebElement Root { get; }

	public IWindow? VirtualWindow { get; internal set; }

	public Size Size { get; private set; }

	internal event Action<Size>? SizeChanged;

	public void SetContent(WebElement? content)
	{
		if (_content == content)
			return;
		if (_content is not null)
			Root.Remove(_content);
		_content = content;
		if (content is not null)
			Root.Insert(0, content);
		WebLayoutScheduler.Request();
	}

	public void PushModal(IView page, WebElement platformPage)
	{
		var bindable = page as Microsoft.Maui.Controls.BindableObject;
		if (bindable is null || WebPage.GetModalPresentationStyle(bindable) == WebModalPresentationStyle.FullWindow)
		{
			var layer = new WebElement("div", "maui-modal-layer");
			layer.Append(platformPage);
			Root.Append(layer);
			_modals.Add(new Modal(page, layer, null, null));
		}
		else
		{
			var backdrop = new WebElement("div", "maui-modal-backdrop");
			var panel = new WebElement("div", "maui-view maui-modal-dialog");
			panel.SetAttribute("role", "dialog");
			panel.SetAttribute("aria-modal", "true");
			panel.Append(platformPage);
			backdrop.Append(panel);
			Root.Append(backdrop);
			// Only clicks on the backdrop itself dismiss; the panel stops propagation.
			var stop = panel.OnStopped("click", () => { });
			var click = backdrop.On("click", () =>
			{
				if (WebPage.GetModalDismissOnBackdrop(bindable) && _modals.Count > 0 && _modals[^1].Page == page &&
					VirtualWindow is Microsoft.Maui.Controls.Window window)
					window.Navigation.PopModalAsync().FireAndForget("Dismiss modal");
			});
			_modals.Add(new Modal(page, backdrop, panel, new Subscriptions(stop, click)));
		}
		WebLayoutScheduler.Request();
	}

	public void PopModal(IView page)
	{
		var index = _modals.FindLastIndex(m => m.Page == page);
		if (index < 0)
			return;
		var modal = _modals[index];
		modal.BackdropClick?.Dispose();
		Root.Remove(modal.Layer);
		_modals.RemoveAt(index);
		WebLayoutScheduler.Request();
	}

	sealed class Subscriptions(params IDisposable[] items) : IDisposable
	{
		public void Dispose()
		{
			foreach (var item in items)
				item.Dispose();
		}
	}

	public void Close()
	{
		_resize.Dispose();
		WebLayoutScheduler.Unregister(this);
		Root.Detach();
	}

	void OnResized(double width, double height)
	{
		Size = new Size(width, height);
		VirtualWindow?.FrameChanged(new Rect(0, 0, width, height));
		SizeChanged?.Invoke(Size);
		WebLayoutScheduler.Request();
	}

	/// <summary>
	/// Resolves a dialog's size from its WebPage properties: explicit size, its content's
	/// measured size, or a comfortable default; then minimums, then the viewport less a margin.
	/// </summary>
	static Size DialogSize(IView page, double viewportWidth, double viewportHeight)
	{
		const double Margin = 16;
		var bindable = (Microsoft.Maui.Controls.BindableObject)page;
		var maxWidth = Math.Max(0, viewportWidth - Margin * 2);
		var maxHeight = Math.Max(0, viewportHeight - Margin * 2);

		var width = WebPage.GetModalWidth(bindable);
		var height = WebPage.GetModalHeight(bindable);
		if (WebPage.GetModalSizesToContent(bindable) && page is Microsoft.Maui.Controls.ContentPage { Content: IView content } contentPage)
		{
			// A page always fills what it's given, so measure its content instead.
			var padding = contentPage.Padding;
			var measured = content.Measure(
				(width > 0 ? width : maxWidth) - padding.HorizontalThickness,
				(height > 0 ? height : maxHeight) - padding.VerticalThickness);
			if (width <= 0)
				width = measured.Width + padding.HorizontalThickness;
			if (height <= 0)
				height = measured.Height + padding.VerticalThickness;
		}
		if (width <= 0)
			width = Math.Min(640, maxWidth);
		if (height <= 0)
			height = Math.Min(Math.Max(320, maxHeight * 0.6), maxHeight);

		width = Math.Max(width, WebPage.GetModalMinWidth(bindable));
		height = Math.Max(height, WebPage.GetModalMinHeight(bindable));
		return new Size(Math.Min(width, maxWidth), Math.Min(height, maxHeight));
	}

	/// <summary>Measures and arranges the window's content and modal pages to the viewport.</summary>
	internal void Layout()
	{
		var (width, height) = (Size.Width, Size.Height);
		if (width <= 0 || height <= 0)
			return;

		var bounds = new Rect(0, 0, width, height);
		if (VirtualWindow?.Content is IView content && _content is not null)
		{
			content.Measure(width, height);
			content.Arrange(bounds);
		}

		foreach (var modal in _modals)
		{
			if (modal.Panel is null)
			{
				modal.Page.Measure(width, height);
				modal.Page.Arrange(bounds);
				continue;
			}

			var size = DialogSize(modal.Page, width, height);
			modal.Panel.SetFrame((width - size.Width) / 2, (height - size.Height) / 2, size.Width, size.Height);
			modal.Page.Measure(size.Width, size.Height);
			modal.Page.Arrange(new Rect(0, 0, size.Width, size.Height));
		}
	}
}

/// <summary>
/// Coalesces measure invalidations into one layout pass per animation frame.
/// </summary>
public static class WebLayoutScheduler
{
	static readonly List<WebWindow> Windows = new();
	static bool _pending;
	static bool _running;

	internal static void Register(WebWindow window) => Windows.Add(window);

	internal static void Unregister(WebWindow window) => Windows.Remove(window);

	/// <summary>Requests a layout pass for all windows before the next paint.</summary>
	public static void Request()
	{
		if (_pending)
			return;
		_pending = true;
		Dom.RequestFrame(Run);
	}

	/// <summary>Runs any pending layout immediately (e.g. before reading native geometry).</summary>
	public static void Flush()
	{
		if (_pending && !_running)
			Run();
	}

	static void Run()
	{
		_pending = false;
		if (_running)
			return;

		_running = true;
		try
		{
			foreach (var window in Windows.ToArray())
				window.Layout();
		}
		catch (Exception ex)
		{
			Dom.Log($"Layout pass failed: {ex}");
		}
		finally
		{
			// Invalidations raised while arranging are picked up by the next frame.
			_running = false;
		}
	}
}
