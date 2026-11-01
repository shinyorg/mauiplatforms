using System.Runtime.InteropServices.JavaScript;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// The platform view for every web handler: a thin wrapper over a DOM element.
/// </summary>
public class WebElement
{
	public WebElement(string tag, string? className = null)
		: this(Dom.CreateElement(tag, className))
	{
	}

	public WebElement(JSObject element)
	{
		Js = element;
	}

	public static WebElement CreateSvg(string tag) => new(Dom.CreateSvgElement(tag));

	/// <summary>The underlying DOM element.</summary>
	public JSObject Js { get; }

	public void SetStyle(string name, string? value) => Dom.SetStyle(Js, name, value);

	public void SetAttribute(string name, string? value) => Dom.SetAttribute(Js, name, value);

	public void SetProperty(string name, string? value) => Dom.SetPropString(Js, name, value);

	public void SetProperty(string name, bool value) => Dom.SetPropBool(Js, name, value);

	public void SetProperty(string name, double value) => Dom.SetPropNumber(Js, name, value);

	public string? GetString(string name) => Dom.GetPropString(Js, name);

	public double GetNumber(string name) => Dom.GetPropNumber(Js, name);

	public bool GetBool(string name) => Dom.GetPropBool(Js, name);

	public void SetText(string? text) => Dom.SetText(Js, text);

	public void SetHtml(string? html) => Dom.SetHtml(Js, html);

	public void ToggleClass(string name, bool on) => Dom.ToggleClass(Js, name, on);

	public void AddClass(string name) => Dom.ToggleClass(Js, name, true);

	public void RemoveClass(string name) => Dom.ToggleClass(Js, name, false);

	public void Append(WebElement child) => Dom.AppendChild(Js, child.Js);

	public void Insert(int index, WebElement child) => Dom.InsertChild(Js, child.Js, index);

	public void Remove(WebElement child) => Dom.RemoveChild(Js, child.Js);

	public void Detach() => Dom.Detach(Js);

	public void Clear() => Dom.ClearChildren(Js);

	public void SetFrame(Rect frame) => Dom.SetFrame(Js, frame.X, frame.Y, frame.Width, frame.Height);

	public void SetFrame(double x, double y, double width, double height) => Dom.SetFrame(Js, x, y, width, height);

	/// <summary>Measures the element's natural size. Infinite constraints are unbounded.</summary>
	public Size Measure(double widthConstraint, double heightConstraint)
	{
		var size = Dom.Measure(Js,
			double.IsFinite(widthConstraint) ? Math.Max(0, widthConstraint) : -1,
			double.IsFinite(heightConstraint) ? Math.Max(0, heightConstraint) : -1);
		return new Size(Math.Ceiling(size[0]), Math.Ceiling(size[1]));
	}

	public bool Focus() => Dom.Focus(Js);

	public void Blur() => Dom.Blur(Js);

	public bool HasFocus => Dom.HasFocus(Js);

	/// <summary>Subscribes to a DOM event; dispose to unsubscribe.</summary>
	public IDisposable On(string type, Action callback, bool preventDefault = false)
		=> new Subscription(Dom.Listen(Js, type, callback, preventDefault));

	/// <summary>Subscribes to a DOM event without letting it bubble to ancestor views.</summary>
	public IDisposable OnStopped(string type, Action callback)
		=> new Subscription(Dom.ListenStopped(Js, type, callback));

	/// <summary>Subscribes to an input event, receiving the element's value (or checked state).</summary>
	public IDisposable OnValue(string type, Action<string> callback)
		=> new Subscription(Dom.ListenValue(Js, type, callback));

	public IDisposable OnKey(string type, Action<string> callback)
		=> new Subscription(Dom.ListenKey(Js, type, callback));

	/// <summary>Pointer events report element-relative coordinates and packed flags (see <see cref="PointerFlags"/>).</summary>
	public IDisposable OnPointer(string type, Action<double, double, int> callback, bool capture = false)
		=> new Subscription(Dom.ListenPointer(Js, type, callback, capture));

	public IDisposable OnScroll(Action<double, double> callback)
		=> new Subscription(Dom.ListenScroll(Js, callback));

	public IDisposable OnResize(Action<double, double> callback)
		=> new Subscription(Dom.ObserveResize(Js, callback), observer: true);

	sealed class Subscription(int id, bool observer = false) : IDisposable
	{
		bool _disposed;

		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			if (observer)
				Dom.Unobserve(id);
			else
				Dom.Unlisten(id);
		}
	}
}

/// <summary>Decodes the flags passed to <see cref="WebElement.OnPointer"/>.</summary>
public static class PointerFlags
{
	public static int Button(int flags) => flags & 0xF;

	public static bool IsTouch(int flags) => (flags & 16) != 0;

	public static int ClickCount(int flags) => flags >> 5;
}
