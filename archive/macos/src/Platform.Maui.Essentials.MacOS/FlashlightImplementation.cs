using Microsoft.Maui.Devices;

namespace Microsoft.Maui.Essentials.MacOS;

class FlashlightImplementation : IFlashlight
{
	public bool IsSupported => false;

	public Task<bool> IsSupportedAsync() => Task.FromResult(false);

	public Task TurnOnAsync()
		=> throw new NotSupportedException("Flashlight is not available on macOS.");

	public Task TurnOffAsync()
		=> throw new NotSupportedException("Flashlight is not available on macOS.");
}
