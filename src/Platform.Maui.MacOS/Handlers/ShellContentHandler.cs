using AppKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Minimal ShellContent handler for macOS. Required for Shell's internal navigation
/// system (GoToAsync) to work. Creates the content page via IShellContentController
/// so that Shell.CurrentPage is non-null (required for navigation to complete).
/// </summary>
public partial class ShellContentHandler : ElementHandler<ShellContent, NSView>
{
	public static readonly IPropertyMapper<ShellContent, ShellContentHandler> Mapper =
		new PropertyMapper<ShellContent, ShellContentHandler>(ElementMapper)
		{
		};

	public ShellContentHandler() : base(Mapper) { }

	protected override NSView CreatePlatformElement()
	{
		return new NSView();
	}

	protected override void ConnectHandler(NSView platformElement)
	{
		base.ConnectHandler(platformElement);

		// Ensure the content page is created so Shell.CurrentPage is non-null.
		// Shell's HandleNavigated waits for CurrentPage != null before firing
		// the Navigated event, so this must happen during handler connection.
		if (VirtualView is IShellContentController controller)
		{
			controller.GetOrCreateContent();
		}
	}
}
