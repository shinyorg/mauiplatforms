using Microsoft.Maui.Animations;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Drives MAUI animations from requestAnimationFrame so they stay in step with paint.
/// </summary>
public class WebTicker : Ticker
{
	bool _running;

	public override bool IsRunning => _running;

	public override bool SystemEnabled => !Dom.PrefersReducedMotion();

	public override void Start()
	{
		if (_running)
			return;
		_running = true;
		Dom.StartTicker(() => Fire?.Invoke());
	}

	public override void Stop()
	{
		if (!_running)
			return;
		_running = false;
		Dom.StopTicker();
	}
}
