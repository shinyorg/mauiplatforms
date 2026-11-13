using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web.Interop;

namespace Microsoft.Maui.Platform.Web;

/// <summary>
/// Connects the browser's Back and Forward buttons (and the back gesture of mobile browsers) to MAUI
/// navigation, so Back steps back through the app instead of leaving it.
/// </summary>
/// <remarks>
/// <para>
/// The window's "depth" is how many steps Back can take inside it: modal pages, pages pushed on a
/// NavigationPage or Shell section, and an open popover flyout. One browser history entry is kept per
/// step. Browser Back then does what Android's Back button does: an open popover flyout closes, and
/// otherwise <see cref="IWindow.BackButtonClicked"/> runs, so the top modal pops, NavigationPage and
/// Shell pop, and pages that override <c>OnBackButtonPressed</c> handle it first. In-app back (the
/// navigation bar's back button, <c>PopAsync</c>) removes the matching entries. Back on the root page
/// leaves the site as usual.
/// </para>
/// <para>
/// History entries are added as the app navigates, which is normally right after a tap. Browsers skip
/// entries a page adds without user activation (Chrome's back-button trap intervention), so an app
/// that pushes pages on its own, without a tap, may find Back skipping those entries.
/// </para>
/// </remarks>
public static class WebHistory
{
	/// <summary>
	/// Whether browser Back and Forward drive MAUI navigation. On by default; set to false before the
	/// window is created to leave browser history to the app.
	/// </summary>
	public static bool IsEnabled { get; set; } = true;

	static Window? _window;
	static bool _syncQueued;
	static bool _handlingBack;
	static int _pendingBackSteps;

	internal static void Attach(Window window)
	{
		if (!IsEnabled || !OperatingSystem.IsBrowser())
			return;
		// Only one window renders into the page.
		if (_window is not null && _window != window)
			return;
		_window = window;
		Dom.HistoryInit(OnBrowserBack);
		Invalidate();
	}

	internal static void Detach(Window window)
	{
		if (_window == window)
			_window = null;
	}

	/// <summary>Called by the handlers when what Back would do may have changed.</summary>
	internal static void Invalidate()
	{
		if (_window is null || _syncQueued || _handlingBack)
			return;
		_syncQueued = true;
		// Push, pop and flyout changes arrive in bursts (Navigated, then mappers): report once after them.
		_window.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(30), () =>
		{
			_syncQueued = false;
			if (!_handlingBack && _window is not null)
				Dom.HistorySync(Depth(_window));
		});
	}

	/// <summary>How many steps Back can take inside <paramref name="window"/>.</summary>
	internal static int Depth(Window window)
	{
		var depth = Depth(window.Page);
		foreach (var modal in window.Navigation.ModalStack)
			depth += 1 + Depth(modal);
		return depth;
	}

	static int Depth(Page? page)
	{
		var depth = 0;
		for (; page is not null; page = Visible(page))
		{
			if (IsPopoverFlyoutOpen(page))
				depth++;
			switch (page)
			{
				case NavigationPage navigation:
					depth += Math.Max(0, navigation.Navigation.NavigationStack.Count - 1);
					break;
				case Shell shell:
					depth += Math.Max(0, (shell.CurrentItem?.CurrentItem?.Stack.Count ?? 1) - 1);
					break;
			}
		}
		return depth;
	}

	/// <summary>The page shown inside <paramref name="page"/>, if it hosts one.</summary>
	static Page? Visible(Page page) => page switch
	{
		NavigationPage navigation => navigation.CurrentPage,
		FlyoutPage flyout => flyout.Detail,
		TabbedPage tabbed => tabbed.CurrentPage,
		_ => null,
	};

	static bool IsPopoverFlyoutOpen(Page page) => page switch
	{
		FlyoutPage flyout => FlyoutPageHandler.IsPopoverOpen(flyout),
		Shell shell => shell.FlyoutIsPresented && shell.FlyoutBehavior == FlyoutBehavior.Flyout,
		_ => false,
	};

	/// <summary>Browser Back moved <paramref name="steps"/> entries back; take that many steps back in the app.</summary>
	/// <remarks>
	/// Steps run one at a time: a quick second Back must wait for the first one's page to finish
	/// popping, or it would reach the page that's on its way out and be lost.
	/// </remarks>
	static async void OnBrowserBack(int steps)
	{
		_pendingBackSteps += steps;
		if (_handlingBack)
			return;

		_handlingBack = true;
		try
		{
			while (_pendingBackSteps > 0 && _window is { } window)
			{
				_pendingBackSteps--;
				var before = Depth(window);
				if (!GoBack(window))
					break;
				// Pops land after their animation. A page that handled Back itself (a wizard step) leaves
				// the depth alone; stop waiting after a moment and let the sync put its entry back.
				for (var wait = 0; wait < 15 && Depth(window) >= before; wait++)
					await Task.Delay(50);
			}
		}
		catch (Exception ex)
		{
			Dom.Log($"Browser back failed: {ex}");
		}
		finally
		{
			_pendingBackSteps = 0;
			_handlingBack = false;
			if (_window is { } window)
				Dom.HistorySync(Depth(window));
		}
	}

	/// <summary>One step of Back, as Android's Back button does it. False when there was nothing to go back from.</summary>
	static bool GoBack(Window window)
	{
		// A modal covers everything below it, flyouts included.
		if (window.Navigation.ModalStack.Count == 0)
		{
			// Android's drawer closes itself on Back before MAUI hears about it; do the same.
			for (var page = window.Page; page is not null; page = Visible(page))
			{
				if (!IsPopoverFlyoutOpen(page))
					continue;
				if (page is FlyoutPage flyout)
					flyout.IsPresented = false;
				else if (page is Shell shell)
					shell.FlyoutIsPresented = false;
				return true;
			}
		}

		return ((IWindow)window).BackButtonClicked();
	}
}
