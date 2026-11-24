using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Shiny;
using Shiny.Infrastructure;
using IAppLinks = Shiny.IAppLinks;

namespace SampleWeb.Pages.ShinyShellDemo;

// The view models use plain properties rather than CommunityToolkit's [ObservableProperty]:
// Shiny's [ShellProperty] generator can't see properties another generator writes.

/// <summary>Navigate tab: every way of pushing a page, and the results that come back.</summary>
[ShellMap<NavigateTabPage>("ShellNavigate", registerRoute: false)]
public partial class NavigateTabViewModel(INavigator navigator, ShellDemoLog log)
	: ObservableObject, IQueryAttributable, IPageLifecycleAware
{
	string _lastResult = "Nothing has come back yet.";
	public string LastResult { get => _lastResult; set => SetProperty(ref _lastResult, value); }

	public Task ByRoute() => navigator.NavigateTo("ShellDetail", args: [("ItemId", "A-1"), ("Count", 1)]);

	public Task ByViewModel() => navigator.NavigateTo<DetailViewModel>(vm =>
	{
		vm.ItemId = "B-2";
		vm.Count = 2;
	});

	// Generated from [ShellMap<DetailPage>("ShellDetail")] and its [ShellProperty]s.
	public Task Generated() => navigator.NavigateToShellDetail("C-3", count: 3);

	public Task Modal() => navigator.NavigateTo<ModalViewModel>();

	public Task Chain() => navigator
		.CreateBuilder()
		.Add<DetailViewModel>(x => x.ItemId = "Chain 1")
		.Add<DetailViewModel>(x => x.ItemId = "Chain 2")
		.Add<DetailViewModel>(x => x.ItemId = "Chain 3")
		.Navigate();

	public Task ToEventsTab() => navigator.NavigateTo("ShellEvents", relativeNavigation: false);

	// GoBack(("Result", ...)) on the detail page lands here.
	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.Count == 0)
			return;
		LastResult = string.Join(", ", query.Select(x => $"{x.Key} = {x.Value}"));
		log.Write("Query", $"Navigate tab received {LastResult}");
	}

	public void OnAppearing() => log.Write("Lifecycle", "NavigateTab OnAppearing");
	public void OnDisappearing() => log.Write("Lifecycle", "NavigateTab OnDisappearing");
}

/// <summary>A pushed page with navigation parameters, every lifecycle hook, and its own way back.</summary>
[ShellMap<DetailPage>(
	"ShellDetail",
	description: "Use when the user wants to open or look at a specific item by its id or name",
	appLinks: ["item/{itemId}"]
)]
public partial class DetailViewModel(INavigator navigator, ShellDemoLog log)
	: ObservableObject, IPageLifecycleAware, INavigationAware, IQueryAttributable, IDisposable
{
	static int _instances;
	readonly int _instance = Interlocked.Increment(ref _instances);

	string _itemId = string.Empty;
	int _count;

	[ShellProperty("The item id or name the user mentioned")]
	public string ItemId
	{
		get => _itemId;
		set
		{
			if (SetProperty(ref _itemId, value))
				OnPropertyChanged(nameof(Summary));
		}
	}

	[ShellProperty("How many of the item the user asked about, if they said", required: false)]
	public int Count
	{
		get => _count;
		set
		{
			if (SetProperty(ref _count, value))
				OnPropertyChanged(nameof(Summary));
		}
	}

	public string Summary => $"ItemId = '{ItemId}', Count = {Count}  (view model #{_instance})";

	// Generated because exactly one app link scheme is configured (ShinyAppLinkSchemes in the csproj).
	public string AppLink => navigator.CreateShellDetailAppLink(ItemId).ToString();

	public Task PushAnother() => navigator.NavigateTo<DetailViewModel>(x => x.ItemId = ItemId + "+");
	public Task BackWithResult() => navigator.GoBack(("Result", $"picked '{ItemId}'"), ("At", DateTime.Now.ToString("HH:mm:ss")));
	public Task BackTwo() => navigator.GoBack(2);
	public Task Root() => navigator.PopToRoot(("Result", "popped to root"));
	// Replacing with the same route would leave Shell's stack as it is (Shell only re-applies the
	// query to the page already there), so the builder swaps in a different page.
	public Task Replace() => navigator.CreateBuilder()
		.PopBack(1)
		.Add<IssueViewModel>(x =>
		{
			x.Description = $"Replaced detail '{ItemId}'";
			x.Priority = IssuePriority.Low;
		})
		.Navigate();

	// Route-string navigation (NavigateTo(route, args), Navigate.Route) passes values as Shell query
	// parameters; the typed and generated methods set the [ShellProperty]s directly instead.
	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue(nameof(ItemId), out var id))
			ItemId = id?.ToString() ?? string.Empty;
		if (query.TryGetValue(nameof(Count), out var count) && int.TryParse(count?.ToString(), out var n))
			Count = n;
	}

	public void OnAppearing() => log.Write("Lifecycle", $"Detail #{_instance} OnAppearing ({ItemId})");
	public void OnDisappearing() => log.Write("Lifecycle", $"Detail #{_instance} OnDisappearing");

	public void OnNavigatingFrom(IDictionary<string, object> parameters)
	{
		parameters["LeftItem"] = ItemId;
		log.Write("Lifecycle", $"Detail #{_instance} OnNavigatingFrom, added LeftItem = {ItemId}");
	}

	public void Dispose() => log.Write("Lifecycle", $"Detail #{_instance} Dispose");
}

/// <summary>A page whose XAML would say Shell.PresentationMode="Modal".</summary>
[ShellMap<ModalPage>("ShellModal")]
public partial class ModalViewModel(INavigator navigator) : ObservableObject
{
	public Task Close() => navigator.GoBack(("Result", "closed the modal"));
}

/// <summary>Where the interceptor sends the user when they aren't signed in.</summary>
[ShellMap<LoginPage>("ShellLogin")]
public partial class LoginViewModel(INavigator navigator, ShellDemoSettings settings) : ObservableObject
{
	public async Task SignIn()
	{
		settings.SignedIn = true;
		// Replace the login page with the page that was guarded. The guard would pass now anyway,
		// but navigation from a guarded flow usually bypasses interceptors.
		await navigator.CreateBuilder().PopBack(1).Add<DetailViewModel>(x => x.ItemId = "after sign-in").Navigate(bypassInterceptors: true);
	}
}

public enum IssuePriority { Low, Medium, High, Urgent }

/// <summary>An AI-applicable route: a description and parameters the model fills in from what the user said.</summary>
[ShellMap<IssuePage>("ShellIssue", description: "Use when the user reports something broken or needing repair")]
public partial class IssueViewModel : ObservableObject
{
	string _description = string.Empty;
	IssuePriority _priority = IssuePriority.Medium;

	[ShellProperty("Summarize what is broken from what the user said")]
	public string Description { get => _description; set => SetProperty(ref _description, value); }

	[ShellProperty("Infer urgency from the user's tone. Must be Low, Medium, High or Urgent")]
	public IssuePriority Priority { get => _priority; set => SetProperty(ref _priority, value); }
}

/// <summary>A ViewModel dialog: awaited with the generated ShowShellPickColorDialog(), returns a colour.</summary>
[ShellMap<PickColorPage>("ShellPickColor")]
public partial class PickColorViewModel(ShellDemoLog log) : ObservableObject, IDialogAware<string>, IPageLifecycleAware, IDisposable
{
	public event EventHandler<string>? Completed;
	public event EventHandler? Cancelled;

	[ShellProperty(required: false)]
	public string Preset { get; set; } = "Violet";

	public static IReadOnlyList<string> Colours { get; } = ["Violet", "Lagoon", "Coral", "Amber", "Slate"];

	public void Pick(string colour) => Completed?.Invoke(this, colour);
	public void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

	public void OnAppearing() => log.Write("Lifecycle", "PickColor dialog OnAppearing");
	public void OnDisappearing() => log.Write("Lifecycle", "PickColor dialog OnDisappearing");
	public void Dispose() => log.Write("Lifecycle", "PickColor dialog Dispose");
}

/// <summary>Dialogs tab: IDialogs through the selected provider, and ViewModel dialogs through the selected presenter.</summary>
[ShellMap<DialogsTabPage>("ShellDialogs", registerRoute: false)]
public partial class DialogsTabViewModel(IDialogs dialogs, INavigator navigator, ShellDemoSettings settings) : ObservableObject
{
	string _result = string.Empty;
	public string Result { get => _result; set => SetProperty(ref _result, value); }

	public ShellDemoSettings Settings => settings;

	public async Task Alert()
	{
		await dialogs.Alert("Alert", $"This alert comes from the {settings.Dialogs} provider.");
		Result = "Alert dismissed";
	}

	public async Task Confirm()
		=> Result = "Confirm: " + await dialogs.Confirm("Confirm", "Do you like this provider?", "Yes", "Not really");

	public async Task Prompt()
	{
		var name = await dialogs.Prompt("Prompt", "What should we call you?", placeholder: "Your name", initialValue: "", maxLength: 30);
		Result = name is null ? "Prompt cancelled" : $"Prompt: '{name}'";
	}

	public async Task NumericPrompt()
	{
		var age = await dialogs.Prompt("Numeric prompt", "How old are you?", keyboard: Keyboard.Numeric);
		Result = age is null ? "Prompt cancelled" : $"Numeric prompt: '{age}'";
	}

	public async Task ActionSheet()
		=> Result = "Action sheet: " + await dialogs.ActionSheet("Share to", "Cancel", "Delete", "Email", "Messages", "Copy link");

	public async Task PickColour()
	{
		// Generated from [ShellMap<PickColorPage>("ShellPickColor")] + IDialogAware<string>.
		var result = await navigator.ShowShellPickColorDialog(preset: "Lagoon");
		Result = result.TryGetValue(out var colour) ? $"Picked {colour} with the {settings.Presenter} presenter" : $"Dialog cancelled ({settings.Presenter})";
	}
}

/// <summary>Guards tab: the demo interceptor's mode, and INavigationConfirmation on user-driven navigation.</summary>
[ShellMap<GuardsTabPage>("ShellGuards", registerRoute: false)]
public partial class GuardsTabViewModel(INavigator navigator, IDialogs dialogs, ShellDemoSettings settings, ShellDemoLog log)
	: ObservableObject, INavigationConfirmation
{
	bool _unsaved;
	string _result = string.Empty;

	public ShellDemoSettings Settings => settings;

	public bool HasUnsavedChanges { get => _unsaved; set => SetProperty(ref _unsaved, value); }
	public string Result { get => _result; set => SetProperty(ref _result, value); }

	public async Task TryDetail()
	{
		var navigated = await navigator.NavigateTo<DetailViewModel>(x => x.ItemId = "guarded");
		Result = navigated ? "NavigateTo returned true (navigated or redirected)" : "NavigateTo returned false: an interceptor cancelled it";
	}

	public async Task Bypass()
	{
		await navigator.NavigateTo<DetailViewModel>(x => x.ItemId = "bypassed", bypassInterceptors: true);
		Result = "Navigated with bypassInterceptors: true";
	}

	public void SignOut()
	{
		settings.SignedIn = false;
		Result = "Signed out";
	}

	// Asked for user-driven Shell navigation only: tab taps, flyout items, the back button.
	public async Task<bool> CanNavigate()
	{
		if (!HasUnsavedChanges)
			return true;
		var leave = await dialogs.Confirm("Unsaved changes", "Leave this tab and discard them?", "Discard", "Stay");
		log.Write("Confirmation", leave ? "Left the Guards tab, discarding changes" : "Stayed on the Guards tab");
		if (leave)
			HasUnsavedChanges = false;
		return leave;
	}
}

/// <summary>Events tab: the live log, and tab badges.</summary>
[ShellMap<EventsTabPage>("ShellEvents", registerRoute: false)]
public partial class EventsTabViewModel(INavigator navigator, ShellDemoLog log, IServiceProvider services) : ObservableObject
{
	int _count;
	string _badges = string.Empty;

	public ObservableCollection<string> Entries => log.Entries;
	public string Badges { get => _badges; set => SetProperty(ref _badges, value); }

	public Task Increment() => Run(() => navigator.SetTabBadge("ShellDialogs", ++_count));
	public Task Dot() => Run(() => navigator.SetTabBadge<GuardsTabViewModel>(string.Empty));
	public Task Text() => Run(() => navigator.SetTabBadge("ShellMore", "new"));

	public Task ClearAll() => Run(async () =>
	{
		_count = 0;
		await navigator.ClearTabBadge("ShellDialogs");
		await navigator.ClearTabBadge<GuardsTabViewModel>();
		await navigator.ClearTabBadge("ShellMore");
	});

	public void ClearLog() => log.Entries.Clear();

	async Task Run(Func<Task> action)
	{
		try
		{
			await action();
			var values = services.GetService<ShellTabBadgeManager>()?.Values;
			Badges = values is null or { Count: 0 } ? "No badges" : string.Join(", ", values.Select(x => $"{x.Key}: '{x.Value}'"));
		}
		catch (Exception ex)
		{
			Badges = $"{ex.GetType().Name}: {ex.Message}";
		}
	}
}

/// <summary>More tab: app links, the generated AI tools, the route registry and Shell switching.</summary>
[ShellMap<MoreTabPage>("ShellMore", registerRoute: false)]
public partial class MoreTabViewModel(INavigator navigator, IAppLinks appLinks, IRouteRegistry routes, AiMauiShellTools ai)
	: ObservableObject
{
	string _appLink = "shinyweb://item/42";
	string _appLinkResult = string.Empty;
	string _aiRoute = "ShellIssue";
	string _aiArgs = "Description=The kitchen sink is leaking everywhere; Priority=Urgent";
	string _aiResult = string.Empty;

	public string AppLink { get => _appLink; set => SetProperty(ref _appLink, value); }
	public string AppLinkResult { get => _appLinkResult; set => SetProperty(ref _appLinkResult, value); }
	public string AiRoute { get => _aiRoute; set => SetProperty(ref _aiRoute, value); }
	public string AiArgs { get => _aiArgs; set => SetProperty(ref _aiArgs, value); }
	public string AiResult { get => _aiResult; set => SetProperty(ref _aiResult, value); }

	public string AiPrompt => ai.Prompt;

	public string AiTools => string.Join("\n", ai.Tools.Select(t => $"• {t.Name}: {t.Description}"));

	public string Routes => string.Join("\n", routes.Routes.OrderBy(r => r.Route).Select(r =>
		$"• {r.Route}  →  {r.PageType.Name} / {r.ViewModelType.Name}{(r.IsRegisteredWithShell ? "" : "  (ShellContent)")}" +
		(r.Parameters is { Count: > 0 } p ? $"\n    {string.Join(", ", p.Select(x => x.ParameterName + (x.IsRequired ? "" : "?")))}" : "")));

	public async Task OpenAppLink()
	{
		if (!Uri.TryCreate(AppLink, UriKind.Absolute, out var uri))
		{
			AppLinkResult = "That isn't an absolute URI";
			return;
		}
		var matched = appLinks.TryResolve(uri, out var match);
		var result = await appLinks.Handle(uri);
		AppLinkResult = matched ? $"{result}: route {match.Route}" : result.ToString();
	}

	// What a chat client does when the model calls the navigate tool.
	public async Task RunAiNavigation()
	{
		try
		{
			var args = AiArgs
				.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries))
				.Where(x => x.Length == 2)
				.ToDictionary(x => x[0], x => x[1]);
			var result = await ai.NavigateToRoute(AiRoute, args);
			AiResult = $"NavigateToRoute returned: {result}";
		}
		catch (Exception ex)
		{
			AiResult = $"{ex.GetType().Name}: {ex.Message}";
		}
	}

	public Task SwitchToAlt() => navigator.SwitchShell<AltShell>();
}

/// <summary>The only page of the second Shell, to switch back from.</summary>
[ShellMap<AltHomePage>("AltHome", registerRoute: false)]
public partial class AltHomeViewModel(INavigator navigator) : ObservableObject
{
	public Task SwitchBack() => navigator.SwitchShell<ShellDemoShell>();
}
