using System.Runtime.CompilerServices;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Platform.Web;
using UXDivers.Popups;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Services;

namespace SampleWeb.Pages.ShinyShellDemo;

/// <summary>
/// Shows UXDivers popups in the browser. UXDivers.Popups.Maui has no browser build: its neutral
/// <c>net10.0</c> library throws PlatformNotSupportedException from the one platform piece,
/// <see cref="INativePopupManager"/>. This replacement puts the <see cref="PopupPage"/> (a full-window
/// ContentView that draws its own backdrop and card) on a <see cref="WebWindow"/> overlay layer.
/// Everything else (the popup stack, animations, the Shiny dialogs built on it) runs unchanged.
/// </summary>
public sealed class WebPopupManager : INativePopupManager
{
	/// <summary>Swaps the browser popup manager in. Call after <c>UseUXDiversPopups()</c>.</summary>
	public static void Install()
	{
		// Keep the dispatcher and view model strategy UseUXDiversPopups set up; they are internal types.
		var core = PopupServiceCore.Instance;
		core.Initialize(new WebPopupManager(), Dispatcher(core)!, Strategy(core));
	}

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_uiThreadDispatcher")]
	static extern ref IUIThreadDispatcher? Dispatcher(PopupServiceCore core);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_viewModelAssignmentStrategy")]
	static extern ref IViewModelAssignmentStrategy? Strategy(PopupServiceCore core);

	// PopupPage.OnBackgroundTapped is internal; it raises PopupBackgroundClicked, runs the command
	// and closes the popup when CloseWhenBackgroundIsClicked, as the native platforms do.
	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "OnBackgroundTapped")]
	static extern Task OnBackgroundTapped(PopupPage popup);

	sealed record Shown(PopupPage Popup, WebWindow Window, IDisposable[] Subscriptions);

	public Task<object> ShowNativeViewAsync(IPopupPage popupPage)
	{
		if (popupPage is not PopupPage popup)
			throw new ArgumentException("The popup does not inherit from PopupPage", nameof(popupPage));

		var window = Application.Current?.Windows.FirstOrDefault()
			?? throw new InvalidOperationException("There is no window to show the popup in");
		var context = window.Handler?.MauiContext
			?? throw new InvalidOperationException("The window has no handler yet");
		var webWindow = (WebWindow)window.Handler.PlatformView!;

		popup.Parent = window;
		var element = (WebElement)popup.ToPlatform(context);
		var layer = webWindow.AddOverlay(popup, element);

		// A click that reaches the layer missed the card: the card's element stops propagation.
		var subscriptions = new List<IDisposable>
		{
			layer.On("click", () => OnBackgroundTapped(popup).FireAndForget()),
		};
		if (popup.ActualContent?.Handler?.PlatformView is WebElement card)
			subscriptions.Add(card.OnStopped("click", () => { }));
		if (popup.BackgroundInputTransparent)
			layer.SetStyle("pointer-events", "none");

		return Task.FromResult<object>(new Shown(popup, webWindow, [.. subscriptions]));
	}

	public Task CloseNativeViewAsync(object nativePopup)
	{
		if (nativePopup is Shown shown)
		{
			foreach (var subscription in shown.Subscriptions)
				subscription.Dispose();
			shown.Window.RemoveOverlay(shown.Popup);
			shown.Popup.Handler?.DisconnectHandler();
			shown.Popup.Parent = null;
		}
		return Task.CompletedTask;
	}
}

static class TaskExtensions
{
	public static async void FireAndForget(this Task task)
	{
		try
		{
			await task;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
		}
	}
}
