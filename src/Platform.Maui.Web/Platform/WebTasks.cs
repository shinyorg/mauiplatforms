namespace Microsoft.Maui.Platform.Web;

internal static class WebTasks
{
	/// <summary>
	/// Observes a task started from a DOM event. Failures are written to the browser console
	/// instead of being lost, since there is no caller to rethrow to.
	/// </summary>
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
