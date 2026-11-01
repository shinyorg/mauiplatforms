using AppKit;
using Foundation;
using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Essentials.MacOS;

class BrowserImplementation : IBrowser
{
	public Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options)
	{
		var nsUrl = new NSUrl(uri.AbsoluteUri);
		var result = NSWorkspace.SharedWorkspace.OpenUrl(nsUrl);
		return Task.FromResult(result);
	}
}
