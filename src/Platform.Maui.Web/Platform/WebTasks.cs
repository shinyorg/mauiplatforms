namespace Microsoft.Maui.Platform.Web;

internal static class WebTasks
{
	/// <summary>
	/// Observes a task started from a DOM event. Failures are written to the browser console
	/// instead of being lost, since there is no caller to rethrow to.
	/// </summary>
	/// <summary>
	/// Wraps a callback the browser runs (dispatcher queue, timer, animation frame) so an exception
	/// is logged with its .NET stack before it propagates; otherwise it reaches the page as a bare
	/// script error. It is still rethrown, as an unhandled exception would be on other platforms.
	/// </summary>
	public static Action Logged(string context, Action action) => () =>
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] {context} failed: {ex}");
			throw;
		}
	};

	public static async void FireAndForget(this Task task, string context)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"[maui-web] {context} failed: {ex}");
		}
	}
}
