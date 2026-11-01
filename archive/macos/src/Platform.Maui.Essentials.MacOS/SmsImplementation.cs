using Microsoft.Maui.ApplicationModel.Communication;

namespace Microsoft.Maui.Essentials.MacOS;

class SmsImplementation : ISms
{
	public bool IsComposeSupported => false;

	public Task ComposeAsync(SmsMessage? message)
		=> throw new NotSupportedException("SMS is not available on macOS.");
}
