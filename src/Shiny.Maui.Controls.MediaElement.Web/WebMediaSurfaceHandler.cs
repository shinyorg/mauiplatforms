using System.Runtime.CompilerServices;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform.Web;
using Microsoft.Maui.Platform.Web.Handlers;

namespace Shiny.Maui.Controls.Media.Web;

/// <summary>
/// Hosts a <see cref="MediaSurface"/> as a clipping <c>&lt;div&gt;</c> that the backend moves its
/// <c>&lt;video&gt;</c> into. The surface has no state of its own; the backend drives everything.
/// </summary>
public class WebMediaSurfaceHandler : WebElementHandler<MediaSurface, WebElement>
{
	public static IPropertyMapper<MediaSurface, WebMediaSurfaceHandler> Mapper =
		new PropertyMapper<MediaSurface, WebMediaSurfaceHandler>(WebElementHandler.ViewMapper);

	public static CommandMapper<MediaSurface, WebMediaSurfaceHandler> CommandMapper =
		new(WebElementHandler.ViewCommandMapper);

	public WebMediaSurfaceHandler() : base(Mapper, CommandMapper)
	{
	}

	protected override WebElement CreatePlatformView()
	{
		var host = new WebElement("div", "shiny-media-surface");
		host.SetStyle("overflow", "hidden");
		return host;
	}

	protected override void ConnectHandler(WebElement platformView)
	{
		base.ConnectHandler(platformView);
		AttachOutput(this.VirtualView, platformView);
	}

	protected override void DisconnectHandler(WebElement platformView)
	{
		if (this.CurrentView is { } surface)
			DetachOutput(surface);
		base.DisconnectHandler(platformView);
	}

	// A video surface has no natural size; it takes whatever the layout offers.
	protected override Size MeasureNative(double widthConstraint, double heightConstraint)
		=> new(double.IsFinite(widthConstraint) ? widthConstraint : 320, double.IsFinite(heightConstraint) ? heightConstraint : 180);

	// MediaSurface's binding hooks are internal to the MediaElement package (which grants access only
	// to its own GTK backend). UnsafeAccessor reaches them without reflection, so trimming and AOT are fine.
	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AttachOutput")]
	static extern void AttachOutput(MediaSurface surface, object nativeView);

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DetachOutput")]
	static extern void DetachOutput(MediaSurface surface);
}
