using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

public class WebDispatcherProvider : IDispatcherProvider
{
	static readonly WebDispatcher Instance = new();

	// The browser runtime is single-threaded: the JS event loop is the UI thread.
	public IDispatcher? GetForCurrentThread() => Instance;
}

/// <summary>
/// Dispatches onto the browser event loop. Dispatch never runs inline, matching the
/// queued semantics of the native MAUI dispatchers.
/// </summary>
public class WebDispatcher : IDispatcher
{
	public bool IsDispatchRequired => false;

	public IDispatcherTimer CreateTimer() => new WebDispatcherTimer();

	public bool Dispatch(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		Dom.Enqueue(action);
		return true;
	}

	public bool DispatchDelayed(TimeSpan delay, Action action)
	{
		ArgumentNullException.ThrowIfNull(action);
		Dom.StartTimeout(action, Math.Max(0, delay.TotalMilliseconds));
		return true;
	}
}

public class WebDispatcherTimer : IDispatcherTimer
{
	int _id;
	bool _repeating;

	public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(16);

	public bool IsRepeating { get; set; } = true;

	public bool IsRunning { get; private set; }

	public event EventHandler? Tick;

	public void Start()
	{
		if (IsRunning)
			return;

		IsRunning = true;
		_repeating = IsRepeating;
		var ms = Math.Max(0, Interval.TotalMilliseconds);
		_id = _repeating ? Dom.StartInterval(OnTick, ms) : Dom.StartTimeout(OnTick, ms);
	}

	public void Stop()
	{
		if (!IsRunning)
			return;

		IsRunning = false;
		if (_repeating)
			Dom.StopInterval(_id);
		else
			Dom.StopTimeout(_id);
		_id = 0;
	}

	void OnTick()
	{
		if (!IsRunning)
			return;
		if (!_repeating)
			IsRunning = false;
		Tick?.Invoke(this, EventArgs.Empty);
	}
}
