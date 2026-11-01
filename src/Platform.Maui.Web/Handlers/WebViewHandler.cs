using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Handlers;

/// <summary>
/// MAUI WebView as an &lt;iframe&gt;. Script evaluation and history only work for same-origin
/// content (HTML sources and pages served from the app's own origin); cross-origin pages are
/// isolated by the browser.
/// </summary>
public class WebViewHandler : WebElementHandler<IWebView, WebElement>
{
	public static IPropertyMapper<IWebView, WebViewHandler> Mapper =
		new PropertyMapper<IWebView, WebViewHandler>(WebElementHandler.ViewMapper)
		{
			[nameof(IWebView.Source)] = MapSource,
		};

	public static CommandMapper<IWebView, WebViewHandler> CommandMapper = new(WebElementHandler.ViewCommandMapper)
	{
		[nameof(IWebView.GoBack)] = (h, v, _) => h.Navigate(-1),
		[nameof(IWebView.GoForward)] = (h, v, _) => h.Navigate(1),
		[nameof(IWebView.Reload)] = (h, v, _) => Dom.FrameHistory(h.PlatformView.Js, 0),
		[nameof(IWebView.Eval)] = MapEval,
		[nameof(IWebView.EvaluateJavaScriptAsync)] = MapEvaluateJavaScriptAsync,
	};

	public WebViewHandler() : base(Mapper, CommandMapper)
	{
	}

	int _history;
	int _position;

	protected override WebElement CreatePlatformView()
	{
		var frame = new WebElement("iframe", "maui-webview");
		frame.SetAttribute("allow", "clipboard-write; fullscreen");
		return frame;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		Track(platformView.On("load", OnLoaded));
	}

	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 300, double.IsFinite(heightConstraint) ? heightConstraint : 150);

	void OnLoaded()
	{
		var url = Dom.FrameUrl(PlatformView.Js) ?? string.Empty;
		if (_position == _history)
			_history = ++_position;
		VirtualView.CanGoBack = _position > 1;
		VirtualView.CanGoForward = _position < _history;
		VirtualView.Navigated(WebNavigationEvent.NewPage, url, WebNavigationResult.Success);
	}

	void Navigate(int delta)
	{
		if (Dom.FrameHistory(PlatformView.Js, delta))
			_position = Math.Clamp(_position + delta, 1, _history);
	}

	public static void MapSource(WebViewHandler handler, IWebView webView)
	{
		var url = (webView.Source as Microsoft.Maui.Controls.UrlWebViewSource)?.Url;
		var html = (webView.Source as Microsoft.Maui.Controls.HtmlWebViewSource)?.Html;
		if (webView.Navigating(WebNavigationEvent.NewPage, url ?? "about:srcdoc"))
			return;
		handler._history = handler._position;
		Dom.SetIframeSource(handler.PlatformView.Js, url, html);
	}

	public static void MapEval(WebViewHandler handler, IWebView webView, object? args)
	{
		if (args is string script)
			Dom.EvaluateInFrame(handler.PlatformView.Js, script);
	}

	public static void MapEvaluateJavaScriptAsync(WebViewHandler handler, IWebView webView, object? args)
	{
		if (args is EvaluateJavaScriptAsyncRequest request)
			request.TrySetResult(Dom.EvaluateInFrame(handler.PlatformView.Js, request.Script)!);
	}
}
