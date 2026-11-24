using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Shiny;
using Shiny.Infrastructure;
using UXDivers.Popups.Maui;

namespace SampleWeb.Pages.ShinyShellDemo;

/// <summary>Which IDialogs implementation answers alert / confirm / prompt / action sheet.</summary>
public enum DialogProvider
{
	/// <summary>Shiny.Maui.Shell's default: the page's DisplayAlertAsync and friends.</summary>
	Maui,
	/// <summary>Shiny.Maui.Shell.ShinyDialogs: the Shiny.Maui.Controls dialog service.</summary>
	ShinyControls,
	/// <summary>Shiny.Maui.Shell.UxDiversDialogs: UXDivers popups.</summary>
	UXDivers,
}

/// <summary>Which IDialogPresenter shows ViewModel dialogs (ShowDialog).</summary>
public enum DialogPresenterKind
{
	/// <summary>Shiny.Maui.Shell's default: a page on Shell's modal stack.</summary>
	ShellModal,
	/// <summary>Shiny.Maui.Shell.ShinyDialogs: a themed card over the current page.</summary>
	ShinyOverlay,
	/// <summary>Shiny.Maui.Shell.UxDiversDialogs: a UXDivers PopupPage.</summary>
	UXDivers,
}

/// <summary>What the demo interceptor does to navigation into the detail page.</summary>
public enum GuardMode
{
	Off,
	Cancel,
	RedirectToLogin,
	Ask,
}

/// <summary>The demo's switches, shared by the tabs and the services that read them.</summary>
public sealed class ShellDemoSettings : ObservableObject
{
	DialogProvider _dialogs = DialogProvider.Maui;
	DialogPresenterKind _presenter = DialogPresenterKind.ShellModal;
	GuardMode _guard = GuardMode.Off;
	bool _signedIn;

	public DialogProvider Dialogs { get => _dialogs; set => SetProperty(ref _dialogs, value); }
	public DialogPresenterKind Presenter { get => _presenter; set => SetProperty(ref _presenter, value); }
	public GuardMode Guard { get => _guard; set => SetProperty(ref _guard, value); }
	public bool SignedIn { get => _signedIn; set => SetProperty(ref _signedIn, value); }
}

/// <summary>A running log of navigation, lifecycle and dialog events, shown on the Events tab.</summary>
public sealed class ShellDemoLog
{
	public ObservableCollection<string> Entries { get; } = new();

	public void Write(string category, string message)
	{
		var line = $"{DateTime.Now:HH:mm:ss.fff}  [{category}] {message}";
		Console.WriteLine("[ShinyShell] " + line);
		if (MainThread.IsMainThread)
			Add(line);
		else
			MainThread.BeginInvokeOnMainThread(() => Add(line));
	}

	void Add(string line)
	{
		Entries.Insert(0, line);
		while (Entries.Count > 300)
			Entries.RemoveAt(Entries.Count - 1);
	}
}

/// <summary>
/// The registered IDialogs. Shiny Shell takes one provider; this one forwards to whichever the
/// Dialogs tab picked, so all three can be tried in one app run.
/// </summary>
public sealed class DemoDialogs(ShellDemoSettings settings, IServiceProvider services) : IDialogs
{
	readonly ShellDialogs _maui = new();
	readonly UxDiversDialogs _uxDivers = new();
	ShinyDialogs? _shiny;

	IDialogs Current => settings.Dialogs switch
	{
		DialogProvider.ShinyControls => _shiny ??= new ShinyDialogs(services.GetRequiredService<global::Shiny.Maui.Controls.Dialogs.IDialogService>()),
		DialogProvider.UXDivers => _uxDivers,
		_ => _maui,
	};

	public Task Alert(string? title, string message, string acceptText = "OK")
		=> Current.Alert(title, message, acceptText);

	public Task<bool> Confirm(string? title, string message, string acceptText = "Yes", string cancelText = "No")
		=> Current.Confirm(title, message, acceptText, cancelText);

	public Task<string?> Prompt(string? title, string message, string acceptText = "OK", string cancelText = "Cancel", string? placeholder = null, string initialValue = "", int maxLength = -1, Keyboard? keyboard = null)
		=> Current.Prompt(title, message, acceptText, cancelText, placeholder, initialValue, maxLength, keyboard);

	public Task<string> ActionSheet(string? title, string? cancel, string? destruction, params string[] buttons)
		=> Current.ActionSheet(title, cancel, destruction, buttons);
}

/// <summary>The registered IDialogPresenter, forwarding to the one the Dialogs tab picked.</summary>
public sealed class DemoDialogPresenter(ShellDemoSettings settings) : IDialogPresenter
{
	readonly ShellModalDialogPresenter _modal = new();
	readonly ShinyOverlayDialogPresenter _shiny = new(new ShinyDialogPresenterOptions());
	readonly UxDiversDialogPresenter _uxDivers = new(new UxDiversDialogPresenterOptions());

	public Task Present(Page page, object viewModel, CancellationToken dismiss) => settings.Presenter switch
	{
		DialogPresenterKind.ShinyOverlay => _shiny.Present(page, viewModel, dismiss),
		DialogPresenterKind.UXDivers => _uxDivers.Present(page, viewModel, dismiss),
		_ => _modal.Present(page, viewModel, dismiss),
	};
}

/// <summary>
/// Guards the detail page as the Guards tab says: let it through, cancel it, send the user to the
/// login page first, or ask with a dialog (through whichever dialog provider is selected).
/// </summary>
public sealed class DemoInterceptor(ShellDemoSettings settings, IDialogs dialogs, ShellDemoLog log) : INavigationInterceptor
{
	const string LetItThrough = "Let it through";
	const string SendToLogin = "Go to login instead";
	const string Stop = "Stop navigation";

	public int Order => -100;

	public async Task<NavigationInterceptorResult> InterceptNavigationAsync(string uri, object? viewModel, CancellationToken cancellationToken)
	{
		// Always narrow to the destination being guarded, or tab taps and back navigation are caught too.
		if (viewModel is not DetailViewModel detail || settings.Guard == GuardMode.Off)
			return NavigationInterceptorResult.Continue;

		switch (settings.Guard)
		{
			case GuardMode.Cancel:
				log.Write("Interceptor", $"Cancelled navigation to {uri}");
				return NavigationInterceptorResult.Cancel();

			case GuardMode.RedirectToLogin when !settings.SignedIn:
				log.Write("Interceptor", $"Not signed in, redirecting {uri} to the login page");
				return NavigationInterceptorResult.Redirect<LoginViewModel>(relativeNavigation: true);

			case GuardMode.Ask:
				var choice = await dialogs.ActionSheet($"Open item '{detail.ItemId}'?", Stop, null, LetItThrough, SendToLogin);
				log.Write("Interceptor", $"Asked about {uri}: {choice}");
				return choice switch
				{
					LetItThrough => NavigationInterceptorResult.Continue,
					SendToLogin => NavigationInterceptorResult.Redirect<LoginViewModel>(relativeNavigation: true),
					_ => NavigationInterceptorResult.Cancel(),
				};

			default:
				return NavigationInterceptorResult.Continue;
		}
	}
}

/// <summary>Sees every navigation, including the ones Shell runs itself (tab taps, back), and how it ended.</summary>
public sealed class DemoNavigationObserver(ShellDemoLog log) : INavigationObserver
{
	public void OnNavigationStarted(NavigationStarted navigation)
		=> log.Write("Observer", $"#{navigation.Id} {navigation.Source} {navigation.NavigationType} → {navigation.Uri}");

	public void OnNavigationFinished(NavigationFinished result)
	{
		var detail = result.Outcome switch
		{
			NavigationOutcome.Cancelled => $" by {result.CancelledBy?.GetType().Name}",
			NavigationOutcome.Redirected => $" to {result.Uri}",
			NavigationOutcome.Failed => $": {result.Exception?.Message}",
			_ => string.Empty,
		};
		log.Write("Observer", $"#{result.Navigation.Id} {result.Outcome}{detail} in {result.Duration.TotalMilliseconds:0}ms");
	}
}

/// <summary>Sees ViewModel dialogs open and close.</summary>
public sealed class DemoDialogObserver(ShellDemoLog log) : IDialogObserver
{
	public void OnDialogShown(PresentedDialog dialog)
		=> log.Write("Dialog", $"#{dialog.Id} {dialog.ViewModelType.Name} shown");

	public void OnDialogClosed(PresentedDialog dialog, DialogClosed result)
		=> log.Write("Dialog", $"#{dialog.Id} closed: {(result.IsCancelled ? "cancelled" : $"'{result.Value}'")}");
}

/// <summary>INavigator's Navigating / Navigated events: they fire for navigation Shiny performs.</summary>
sealed class NavigatorEventLogger(INavigator navigator, ShellDemoLog log) : IMauiInitializeService
{
	public void Initialize(IServiceProvider services)
	{
		navigator.Navigating += (_, e) => log.Write("Navigating", $"{e.FromUri ?? "(none)"} → {e.ToUri} ({e.NavigationType}, from {e.FromViewModel?.GetType().Name ?? "-"})");
		navigator.Navigated += (_, e) => log.Write("Navigated", $"{e.ToUri} ({e.NavigationType}, to {e.ToViewModel?.GetType().Name ?? "-"})");
	}
}

public static class ShellDemoRegistration
{
	/// <summary>Shiny Shell and everything the demo plugs into it.</summary>
	public static MauiAppBuilder UseShinyShellDemo(this MauiAppBuilder builder)
	{
		builder.UseShinyShell(x => x
			// Every [ShellMap] in the sample: routes, pages, view models, app links and route info.
			.AddGeneratedMaps()
			// The generated AiMauiShellTools (Microsoft.Extensions.AI is referenced, so it is generated).
			.AddAiTools()
			.UseDialogs<DemoDialogs>()
			.UseDialogPresenter<DemoDialogPresenter>()
			.AddNavigationInterceptor<DemoInterceptor>()
			.AddNavigationObserver<DemoNavigationObserver>()
			.AddDialogObserver<DemoDialogObserver>()
		);

		// The UXDivers popup infrastructure, which UseUxDiversDialogs() would set up, then the browser
		// popup manager in place of the one that throws PlatformNotSupportedException.
		builder.UseUXDiversPopups();
		WebPopupManager.Install();

		builder.Services.AddSingleton<ShellDemoSettings>();
		builder.Services.AddSingleton<ShellDemoLog>();
		builder.Services.AddSingleton<IMauiInitializeService, NavigatorEventLogger>();
		// SwitchShell<TShell>() resolves the Shell from DI.
		builder.Services.AddTransient<ShellDemoShell>();
		builder.Services.AddTransient<AltShell>();
		return builder;
	}
}
