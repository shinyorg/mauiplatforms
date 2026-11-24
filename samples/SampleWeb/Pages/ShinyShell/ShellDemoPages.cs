using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Shiny;

namespace SampleWeb.Pages.ShinyShellDemo;

/// <summary>
/// The demo's Shell. Its tabs are ShellContents (mapped with registerRoute: false); every other
/// page is pushed by route. Shiny sets each page's BindingContext to its mapped view model.
/// </summary>
public class ShellDemoShell : ShinyShell
{
	public ShellDemoShell()
	{
		Title = "Shiny Shell";
		FlyoutBehavior = FlyoutBehavior.Flyout;
		FlyoutHeader = new VerticalStackLayout
		{
			Padding = new Thickness(20, 24, 20, 12),
			Spacing = 4,
			Children =
			{
				new Label { Text = "Shiny Shell", FontFamily = Theme.Display, FontSize = 22, FontAttributes = FontAttributes.Bold },
				SpecimenPage.Muted("Shiny.Maui.Shell on the web backend", 13),
			},
		};
		Theme.Bind(this, FlyoutBackgroundColorProperty, Theme.Stage);

		var demo = new FlyoutItem { Title = "Demo", Route = "demo" };
		demo.Items.Add(Tab<NavigateTabPage>("Navigate", "ShellNavigate"));
		demo.Items.Add(Tab<DialogsTabPage>("Dialogs", "ShellDialogs"));
		demo.Items.Add(Tab<GuardsTabPage>("Guards", "ShellGuards"));
		demo.Items.Add(Tab<EventsTabPage>("Events", "ShellEvents"));
		demo.Items.Add(Tab<MoreTabPage>("More", "ShellMore"));
		Items.Add(demo);

		Items.Add(new MenuItem { Text = "Switch to the other Shell", Command = new Command(async () => await Navigator.SwitchShell<AltShell>()) });
		Items.Add(new MenuItem { Text = "Back to the gallery", Command = new Command(ShellLauncher.BackToGallery) });
	}

	static INavigator Navigator => IPlatformApplication.Current!.Services.GetRequiredService<INavigator>();

	static ShellSection Tab<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TPage>(string title, string route) where TPage : Page
	{
		var section = new ShellSection { Title = title };
		section.Items.Add(new ShellContent { Title = title, Route = route, ContentTemplate = new DataTemplate(typeof(TPage)) });
		return section;
	}
}

/// <summary>A second Shell, for SwitchShell.</summary>
public class AltShell : ShinyShell
{
	public AltShell()
	{
		Title = "Another Shell";
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Items.Add(new ShellContent { Title = "Another Shell", Route = "AltHome", ContentTemplate = new DataTemplate(typeof(AltHomePage)) });
	}
}

/// <summary>Opens the demo Shell from the gallery, and goes back.</summary>
public static class ShellLauncher
{
	public static async Task Launch(string? route)
	{
		var navigator = IPlatformApplication.Current!.Services.GetRequiredService<INavigator>();
		await navigator.SwitchShell<ShellDemoShell>();
		if (route is not null)
			await navigator.NavigateTo(route, relativeNavigation: false);
	}

	public static void BackToGallery()
	{
		if (Application.Current?.Windows.FirstOrDefault() is not { } window)
			return;
		var gallery = new MainShell();
		window.Page = gallery;
		gallery.SelectTab(MainShell.Tabs.Count - 1);
	}
}

/// <summary>The demo pages' common parts: buttons that call the view model, and bound readouts.</summary>
public abstract class ShellDemoPage<TViewModel>(string title, string intro) : SpecimenPage(title, intro) where TViewModel : class
{
	protected TViewModel? ViewModel => BindingContext as TViewModel;

	protected Button Action(string text, Func<TViewModel, Task> action, bool secondary = false)
	{
		var button = new Button { Text = text };
		if (secondary)
			button.StyleClass = ["secondary"];
		button.Clicked += async (_, _) =>
		{
			if (ViewModel is not { } vm)
				return;
			try
			{
				await action(vm);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
				await DisplayAlertAsync(ex.GetType().Name, ex.Message, "OK");
			}
		};
		return button;
	}

	protected Button Action(string text, Action<TViewModel> action, bool secondary = false)
		=> Action(text, vm =>
		{
			action(vm);
			return Task.CompletedTask;
		}, secondary);

	protected static Label Bound(string path, double size = 14)
	{
		var label = new Label { FontSize = size };
		label.SetBinding(Label.TextProperty, path);
		return label;
	}

	protected static Label Code(string path)
	{
		var label = new Label { FontFamily = "monospace", FontSize = 12, LineBreakMode = LineBreakMode.WordWrap };
		label.SetBinding(Label.TextProperty, path);
		return label;
	}

	/// <summary>A picker over an enum, two-way with a property on <see cref="ShellDemoSettings"/>.</summary>
	protected static Picker EnumPicker<TEnum>(string title, string settingsPath) where TEnum : struct, Enum
	{
		var picker = new Picker { Title = title, ItemsSource = Enum.GetNames<TEnum>(), MinimumWidthRequest = 220 };
		picker.SetBinding(Picker.SelectedIndexProperty, new Binding(settingsPath, BindingMode.TwoWay, new EnumIndexConverter<TEnum>()));
		return picker;
	}

	sealed class EnumIndexConverter<TEnum> : IValueConverter where TEnum : struct, Enum
	{
		public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
			=> value is TEnum e ? Array.IndexOf(Enum.GetValues<TEnum>(), e) : -1;

		public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
			=> value is int i && i >= 0 ? Enum.GetValues<TEnum>()[i] : default(TEnum);
	}
}

public class NavigateTabPage : ShellDemoPage<NavigateTabViewModel>
{
	public NavigateTabPage() : base("Navigate", "Pushing pages with INavigator: by route, by view model, with the generated methods, from markup and with the navigation builder.")
	{
		Specimen("Push a page", "Each opens the detail page with different parameters. The detail page shows what arrived.",
			Wrap(10,
				Action("NavigateTo(route, args)", vm => vm.ByRoute()),
				Action("NavigateTo<DetailViewModel>", vm => vm.ByViewModel()),
				Action("Generated NavigateToShellDetail", vm => vm.Generated()),
				MarkupButton()));

		Specimen("Navigation builder", "One navigation that pushes three detail pages. Go back to walk down them.",
			Wrap(10, Action("Push Chain 1 › 2 › 3", vm => vm.Chain())));

		Specimen("Modal", "A page with Shell.PresentationMode = Modal.",
			Wrap(10, Action("Open the modal page", vm => vm.Modal())));

		Specimen("Absolute navigation", "NavigateTo(route, relativeNavigation: false) resets to a route: here, another tab.",
			Wrap(10, Action("Go to the Events tab", vm => vm.ToEventsTab(), secondary: true)));

		Specimen("Results", "GoBack and PopToRoot can carry parameters; this page receives them in IQueryAttributable.",
			Bound(nameof(NavigateTabViewModel.LastResult)));
	}

	// The Navigate.* attached properties, as XAML would set them: no view model code at all.
	static Button MarkupButton()
	{
		var button = new Button { Text = "Navigate.Route (markup)", StyleClass = ["secondary"] };
		Navigate.SetRoute(button, "ShellDetail");
		Navigate.SetParameterKey(button, "ItemId");
		Navigate.SetParameterValue(button, "D-4 from markup");
		return button;
	}
}

public class DetailPage : ShellDemoPage<DetailViewModel>
{
	public DetailPage() : base("Detail", "A pushed page. Its view model logs every lifecycle hook to the Events tab.")
	{
		Specimen("What arrived", "[ShellProperty] values set by the navigation that opened this page.",
			new VerticalStackLayout { Spacing = 8, Children = { Bound(nameof(DetailViewModel.Summary), 16), Code(nameof(DetailViewModel.AppLink)) } });

		Specimen("Go deeper", "Push another detail page, or replace this one with the issue page using the navigation builder (PopBack(1).Add).",
			Wrap(10,
				Action("Push another", vm => vm.PushAnother()),
				Action("Replace with the issue page", vm => vm.Replace(), secondary: true)));

		Specimen("Go back", "GoBack with result parameters, GoBack(2) and PopToRoot.",
			Wrap(10,
				Action("GoBack with a result", vm => vm.BackWithResult()),
				Action("GoBack(2)", vm => vm.BackTwo(), secondary: true),
				Action("PopToRoot", vm => vm.Root(), secondary: true)));
	}
}

public class ModalPage : ShellDemoPage<ModalViewModel>
{
	public ModalPage() : base("Modal", "Presented modally because the page sets Shell.PresentationMode.")
	{
		Shell.SetPresentationMode(this, PresentationMode.Modal);
		Specimen("Close", "GoBack pops the modal, with a result for the page underneath.",
			Wrap(10, Action("Close", vm => vm.Close())));
	}
}

public class LoginPage : ShellDemoPage<LoginViewModel>
{
	public LoginPage() : base("Sign in", "The Guards tab's interceptor redirected here because you aren't signed in.")
	{
		Specimen("Sign in", "Marks you signed in, then replaces this page with the detail page.",
			Wrap(10, Action("Sign in", vm => vm.SignIn())));
	}
}

public class IssuePage : ShellDemoPage<IssueViewModel>
{
	public IssuePage() : base("Issue", "Opened by the AI navigate tool with values it inferred from what the user said.")
	{
		Specimen("Inferred values", "Description and Priority, converted from the tool call's strings.",
			new VerticalStackLayout
			{
				Spacing = 8,
				Children = { Bound(nameof(IssueViewModel.Description), 16), Bound(nameof(IssueViewModel.Priority), 16) },
			});
	}
}

/// <summary>
/// A ViewModel dialog's page. The overlay presenters take its Content, so it is a ContentPage
/// with content, kept compact.
/// </summary>
public class PickColorPage : ContentPage
{
	static readonly Dictionary<string, Color> Swatches = new()
	{
		["Violet"] = Color.FromArgb("#5B3FE0"),
		["Lagoon"] = Color.FromArgb("#0E9F9A"),
		["Coral"] = Color.FromArgb("#E5484D"),
		["Amber"] = Color.FromArgb("#F5A524"),
		["Slate"] = Color.FromArgb("#475569"),
	};

	public PickColorPage()
	{
		Title = "Pick a colour";
		var buttons = new VerticalStackLayout { Spacing = 8 };
		foreach (var (name, color) in Swatches)
		{
			var button = new Button { Text = name, BackgroundColor = color, TextColor = Colors.White };
			button.Clicked += (_, _) => (BindingContext as PickColorViewModel)?.Pick(name);
			buttons.Add(button);
		}
		var cancel = new Button { Text = "Cancel", StyleClass = ["secondary"] };
		cancel.Clicked += (_, _) => (BindingContext as PickColorViewModel)?.Cancel();

		var preset = SpecimenPage.Muted(string.Empty, 13);
		preset.SetBinding(Label.TextProperty, new Binding(nameof(PickColorViewModel.Preset), stringFormat: "Preset: {0}"));

		// The overlay presenters lift this content onto their own card, whose colour comes from their
		// theme (UXDivers' is dark); a background of its own keeps the gallery's text readable on all three.
		var content = new VerticalStackLayout
		{
			Padding = new Thickness(24),
			Spacing = 14,
			WidthRequest = 320,
			Children =
			{
				new Label { Text = "Pick a colour", FontFamily = Theme.Display, FontSize = 22, FontAttributes = FontAttributes.Bold },
				preset,
				buttons,
				cancel,
			},
		};
		Theme.Bind(content, VisualElement.BackgroundColorProperty, Theme.Paper);
		Content = content;
	}
}

public class DialogsTabPage : ShellDemoPage<DialogsTabViewModel>
{
	public DialogsTabPage() : base("Dialogs", "IDialogs and ViewModel dialogs, with each provider Shiny Shell ships: MAUI's own, Shiny.Maui.Controls and UXDivers Popups.")
	{
		Specimen("Dialog provider", "Shiny Shell registers one IDialogs. This demo's forwards to the one picked here.",
			EnumPicker<DialogProvider>("Provider", "Settings.Dialogs"));

		Specimen("IDialogs", "Alert, confirm, prompt and action sheet through the selected provider.",
			Wrap(10,
				Action("Alert", vm => vm.Alert()),
				Action("Confirm", vm => vm.Confirm()),
				Action("Prompt", vm => vm.Prompt()),
				Action("Numeric prompt", vm => vm.NumericPrompt(), secondary: true),
				Action("Action sheet", vm => vm.ActionSheet())));

		Specimen("ViewModel dialog", "ShowShellPickColorDialog() awaits a page whose view model implements IDialogAware<string>. The presenter decides how it appears.",
			new VerticalStackLayout
			{
				Spacing = 14,
				Children =
				{
					EnumPicker<DialogPresenterKind>("Presenter", "Settings.Presenter"),
					Wrap(10, Action("Pick a colour", vm => vm.PickColour())),
				},
			});

		Specimen("Result", "What the last dialog returned.", Bound(nameof(DialogsTabViewModel.Result)));
	}
}

public class GuardsTabPage : ShellDemoPage<GuardsTabViewModel>
{
	public GuardsTabPage() : base("Guards", "An INavigationInterceptor guarding the detail page, and INavigationConfirmation on this tab.")
	{
		Specimen("Interceptor", "Off, Cancel, RedirectToLogin (until you sign in) or Ask (an action sheet from the selected dialog provider).",
			new VerticalStackLayout
			{
				Spacing = 14,
				Children =
				{
					EnumPicker<GuardMode>("Guard", "Settings.Guard"),
					Wrap(10,
						Action("Open the detail page", vm => vm.TryDetail()),
						Action("Open it, bypassing interceptors", vm => vm.Bypass(), secondary: true),
						Action("Sign out", vm => vm.SignOut(), secondary: true)),
				},
			});

		var unsaved = new Switch();
		unsaved.SetBinding(Switch.IsToggledProperty, new Binding(nameof(GuardsTabViewModel.HasUnsavedChanges), BindingMode.TwoWay));
		Specimen("Unsaved changes", "Turn this on, then tap another tab or a flyout item: the view model's CanNavigate asks first.",
			Labeled(unsaved, "This tab has unsaved changes"));

		Specimen("Result", "What the last navigation returned.", Bound(nameof(GuardsTabViewModel.Result)));
	}
}

public class EventsTabPage : ShellDemoPage<EventsTabViewModel>
{
	public EventsTabPage() : base("Events", "Tab badges, and a live log of navigation events, observers, lifecycle hooks and dialogs.")
	{
		Specimen("Tab badges", "SetTabBadge by route or by view model: a count on Dialogs, a dot on Guards, text on More.",
			new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					Wrap(10,
						Action("Dialogs +1", vm => vm.Increment()),
						Action("Dot on Guards", vm => vm.Dot()),
						Action("'new' on More", vm => vm.Text()),
						Action("Clear all", vm => vm.ClearAll(), secondary: true)),
					Bound(nameof(EventsTabViewModel.Badges), 13),
				},
			});

		var list = new CollectionView
		{
			HeightRequest = 420,
			ItemTemplate = new DataTemplate(() =>
			{
				var line = new Label { FontFamily = "monospace", FontSize = 12, Padding = new Thickness(0, 3) };
				line.SetBinding(Label.TextProperty, ".");
				return line;
			}),
		};
		list.SetBinding(ItemsView.ItemsSourceProperty, nameof(EventsTabViewModel.Entries));
		Specimen("Log", "Newest first. Navigating/Navigated come from INavigator's events; Observer from an INavigationObserver, which also sees tab taps and back navigation.",
			new VerticalStackLayout { Spacing = 12, Children = { Wrap(10, Action("Clear the log", vm => vm.ClearLog(), secondary: true)), list } });
	}
}

public class MoreTabPage : ShellDemoPage<MoreTabViewModel>
{
	public MoreTabPage() : base("More", "App links, the generated AI tools, the route registry, and switching Shells.")
	{
		var link = new Entry { Placeholder = "shinyweb://item/42" };
		link.SetBinding(Entry.TextProperty, new Binding(nameof(MoreTabViewModel.AppLink), BindingMode.TwoWay));
		Specimen("App links", "[ShellMap(appLinks: [\"item/{itemId}\"])] on the detail page. IAppLinks.Handle routes a URI the way an inbound link would.",
			new VerticalStackLayout
			{
				Spacing = 12,
				Children = { link, Wrap(10, Action("Handle", vm => vm.OpenAppLink())), Bound(nameof(MoreTabViewModel.AppLinkResult), 13) },
			});

		var route = new Entry { Placeholder = "Route" };
		route.SetBinding(Entry.TextProperty, new Binding(nameof(MoreTabViewModel.AiRoute), BindingMode.TwoWay));
		var args = new Entry { Placeholder = "Key=Value; Key=Value" };
		args.SetBinding(Entry.TextProperty, new Binding(nameof(MoreTabViewModel.AiArgs), BindingMode.TwoWay));
		Specimen("AI tools", "The generated AiMauiShellTools: a system prompt, tools for a chat client, and NavigateToRoute, called here as a model's tool call would.",
			new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					Code(nameof(MoreTabViewModel.AiTools)),
					route,
					args,
					Wrap(10, Action("NavigateToRoute", vm => vm.RunAiNavigation())),
					Bound(nameof(MoreTabViewModel.AiResult), 13),
					new Label { Text = "Prompt", FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) },
					Code(nameof(MoreTabViewModel.AiPrompt)),
				},
			});

		Specimen("Route registry", "IRouteRegistry: every mapped route, its page and view model, and its [ShellProperty] parameters.",
			Code(nameof(MoreTabViewModel.Routes)));

		Specimen("Switch Shell", "SwitchShell<AltShell>() swaps the whole Shell; the other Shell switches back.",
			Wrap(10,
				Action("Switch to the other Shell", vm => vm.SwitchToAlt()),
				Action("Back to the gallery", _ => ShellLauncher.BackToGallery(), secondary: true)));
	}
}

public class AltHomePage : ShellDemoPage<AltHomeViewModel>
{
	public AltHomePage() : base("Another Shell", "A different ShinyShell with a single page, swapped in by INavigator.SwitchShell.")
	{
		Specimen("Switch back", "SwitchShell<ShellDemoShell>() brings the demo Shell back.",
			Wrap(10, Action("Switch back", vm => vm.SwitchBack())));
	}
}
