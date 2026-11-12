using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Maui.Platform.Web.Interop;

/// <summary>
/// [JSImport] bindings for wwwroot/maui-web.js. All DOM access goes through here.
/// </summary>
internal static partial class Dom
{
	public const string Module = "maui-web";

	[JSImport("init", Module)]
	public static partial string Init();

	[JSImport("createElement", Module)]
	public static partial JSObject CreateElement(string tag, string? className);

	[JSImport("createSvgElement", Module)]
	public static partial JSObject CreateSvgElement(string tag);

	[JSImport("getBody", Module)]
	public static partial JSObject GetBody();

	[JSImport("getDocument", Module)]
	public static partial JSObject GetDocument();

	[JSImport("querySelector", Module)]
	public static partial JSObject? QuerySelector(string selector);

	[JSImport("setStyle", Module)]
	public static partial void SetStyle(JSObject el, string name, string? value);

	[JSImport("setAttribute", Module)]
	public static partial void SetAttribute(JSObject el, string name, string? value);

	[JSImport("getAttribute", Module)]
	public static partial string? GetAttribute(JSObject el, string name);

	[JSImport("setPropString", Module)]
	public static partial void SetPropString(JSObject el, string name, string? value);

	[JSImport("setPropBool", Module)]
	public static partial void SetPropBool(JSObject el, string name, bool value);

	[JSImport("setPropNumber", Module)]
	public static partial void SetPropNumber(JSObject el, string name, double value);

	[JSImport("getPropString", Module)]
	public static partial string? GetPropString(JSObject el, string name);

	[JSImport("getPropNumber", Module)]
	public static partial double GetPropNumber(JSObject el, string name);

	[JSImport("getPropBool", Module)]
	public static partial bool GetPropBool(JSObject el, string name);

	[JSImport("setText", Module)]
	public static partial void SetText(JSObject el, string? text);

	[JSImport("setHtml", Module)]
	public static partial void SetHtml(JSObject el, string? html);

	[JSImport("toggleClass", Module)]
	public static partial void ToggleClass(JSObject el, string name, bool on);

	[JSImport("appendChild", Module)]
	public static partial void AppendChild(JSObject parent, JSObject child);

	[JSImport("insertChild", Module)]
	public static partial void InsertChild(JSObject parent, JSObject child, int index);

	[JSImport("removeChild", Module)]
	public static partial void RemoveChild(JSObject parent, JSObject child);

	[JSImport("detach", Module)]
	public static partial void Detach(JSObject el);

	[JSImport("clearChildren", Module)]
	public static partial void ClearChildren(JSObject el);

	[JSImport("setFrame", Module)]
	public static partial void SetFrame(JSObject el, double x, double y, double width, double height);

	[JSImport("measure", Module)]
	[return: JSMarshalAs<JSType.Array<JSType.Number>>]
	public static partial double[] Measure(JSObject el, double widthConstraint, double heightConstraint);

	[JSImport("measureScrollHeight", Module)]
	public static partial double MeasureScrollHeight(JSObject el, double width);

	[JSImport("listen", Module)]
	public static partial int Listen(JSObject el, string type, [JSMarshalAs<JSType.Function>] Action callback, bool preventDefault);

	[JSImport("listenStopped", Module)]
	public static partial int ListenStopped(JSObject el, string type, [JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("listenValue", Module)]
	public static partial int ListenValue(JSObject el, string type, [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

	[JSImport("listenKey", Module)]
	public static partial int ListenKey(JSObject el, string type, [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

	[JSImport("listenPointer", Module)]
	public static partial int ListenPointer(JSObject el, string type,
		[JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>] Action<double, double, int> callback, bool capture);

	[JSImport("listenScroll", Module)]
	public static partial int ListenScroll(JSObject el, [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<double, double> callback);

	[JSImport("listenWheel", Module)]
	public static partial int ListenWheel(JSObject el, [JSMarshalAs<JSType.Function<JSType.Number>>] Action<double> callback);

	[JSImport("unlisten", Module)]
	public static partial void Unlisten(int id);

	[JSImport("observeResize", Module)]
	public static partial int ObserveResize(JSObject el, [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<double, double> callback);

	[JSImport("observeVisibility", Module)]
	public static partial int ObserveVisibility(JSObject el, [JSMarshalAs<JSType.Function<JSType.Boolean>>] Action<bool> callback);

	[JSImport("unobserve", Module)]
	public static partial void Unobserve(int id);

	[JSImport("focus", Module)]
	public static partial bool Focus(JSObject el);

	[JSImport("blur", Module)]
	public static partial void Blur(JSObject el);

	[JSImport("hasFocus", Module)]
	public static partial bool HasFocus(JSObject el);

	[JSImport("enqueue", Module)]
	public static partial void Enqueue([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("requestFrame", Module)]
	public static partial int RequestFrame([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("startTimeout", Module)]
	public static partial int StartTimeout([JSMarshalAs<JSType.Function>] Action callback, double ms);

	[JSImport("stopTimeout", Module)]
	public static partial void StopTimeout(int id);

	[JSImport("startInterval", Module)]
	public static partial int StartInterval([JSMarshalAs<JSType.Function>] Action callback, double ms);

	[JSImport("stopInterval", Module)]
	public static partial void StopInterval(int id);

	[JSImport("startTicker", Module)]
	public static partial void StartTicker([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("stopTicker", Module)]
	public static partial void StopTicker();

	[JSImport("prefersReducedMotion", Module)]
	public static partial bool PrefersReducedMotion();

	[JSImport("setDocumentTitle", Module)]
	public static partial void SetDocumentTitle(string? title);

	[JSImport("getColorScheme", Module)]
	public static partial string GetColorScheme();

	[JSImport("observeColorScheme", Module)]
	public static partial void ObserveColorScheme([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> callback);

	[JSImport("setRootTheme", Module)]
	public static partial void SetRootTheme(string? theme);

	[JSImport("viewportWidth", Module)]
	public static partial double ViewportWidth();

	[JSImport("loadFontUrl", Module)]
	public static partial Task LoadFontUrl(string family, string url);

	[JSImport("loadFontBytes", Module)]
	public static partial Task LoadFontBytes(string family, [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> bytes);

	[JSImport("observeFonts", Module)]
	public static partial void ObserveFonts([JSMarshalAs<JSType.Function>] Action callback);

	[JSImport("createObjectUrl", Module)]
	public static partial string CreateObjectUrl([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> bytes, string? mime);

	[JSImport("revokeObjectUrl", Module)]
	public static partial void RevokeObjectUrl(string? url);

	[JSImport("naturalSize", Module)]
	[return: JSMarshalAs<JSType.Array<JSType.Number>>]
	public static partial double[] NaturalSize(JSObject img);

	[JSImport("renderGlyph", Module)]
	public static partial Task<string> RenderGlyph(string glyph, string fontFamily, double size, string? color);

	[JSImport("setSelectOptions", Module)]
	public static partial void SetSelectOptions(JSObject select, string[] items, string? title);

	[JSImport("setSelectedOption", Module)]
	public static partial void SetSelectedOption(JSObject select, int index);

	[JSImport("setInputSelection", Module)]
	public static partial void SetInputSelection(JSObject el, int start, int end);

	[JSImport("getInputSelection", Module)]
	[return: JSMarshalAs<JSType.Array<JSType.Number>>]
	public static partial double[] GetInputSelection(JSObject el);

	[JSImport("scrollTo", Module)]
	public static partial void ScrollTo(JSObject el, double x, double y, bool smooth);

	/// <summary>Width a classic scrollbar takes from <paramref name="el"/>'s content box; 0 for overlay or hidden scrollbars.</summary>
	[JSImport("scrollbarThickness", Module)]
	public static partial double ScrollbarThickness(JSObject el);

	[JSImport("getScrollExtent", Module)]
	[return: JSMarshalAs<JSType.Array<JSType.Number>>]
	public static partial double[] GetScrollExtent(JSObject el);

	[JSImport("setIframeSource", Module)]
	public static partial void SetIframeSource(JSObject frame, string? url, string? html);

	[JSImport("evaluateInFrame", Module)]
	public static partial string? EvaluateInFrame(JSObject frame, string script);

	[JSImport("frameHistory", Module)]
	public static partial bool FrameHistory(JSObject frame, int delta);

	[JSImport("frameUrl", Module)]
	public static partial string? FrameUrl(JSObject frame);

	[JSImport("drawCommands", Module)]
	public static partial void DrawCommands(JSObject canvas, double width, double height, string commands);

	[JSImport("registerCanvasImage", Module)]
	public static partial void RegisterCanvasImage(int id, byte[] bytes);

	[JSImport("measureText", Module)]
	[return: JSMarshalAs<JSType.Array<JSType.Number>>]
	public static partial double[] MeasureText(string font, string text);

	[JSImport("showAlert", Module)]
	public static partial Task<bool> ShowAlert(string? title, string? message, string? accept, string? cancel);

	[JSImport("showActionSheet", Module)]
	public static partial Task<string?> ShowActionSheet(string? title, string? cancel, string? destruction, string[] buttons);

	[JSImport("showPrompt", Module)]
	public static partial Task<string?> ShowPrompt(string? title, string? message, string? accept, string? cancel,
		string? placeholder, string? initialValue, int maxLength, string? inputMode);

	[JSImport("log", Module)]
	public static partial void Log(string message);
}
