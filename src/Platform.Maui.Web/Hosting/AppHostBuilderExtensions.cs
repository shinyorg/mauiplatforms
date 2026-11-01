using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Animations;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Handlers;
using Microsoft.Maui.Platform.Web;

namespace Microsoft.Maui.Platform.Web.Hosting;

public static class AppHostBuilderExtensions
{
	/// <summary>
	/// Configures MAUI to render into the browser DOM. Use instead of <c>UseMauiApp</c>.
	/// </summary>
	public static MauiAppBuilder UseMauiAppWeb<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApp>(
		this MauiAppBuilder builder)
		where TApp : class, IApplication
	{
		builder.UseMauiApp<TApp>();
		builder.SetupWebDefaults();
		return builder;
	}

	static MauiAppBuilder SetupWebDefaults(this MauiAppBuilder builder)
	{
		// Controls reads AppInfo.Current (theme) during startup, before Essentials initializes.
		var appInfo = new WebAppInfo();
		SetAppInfo(appInfo);
		builder.Services.TryAddSingleton<IAppInfo>(appInfo);

		builder.Services.AddSingleton<IDispatcherProvider, WebDispatcherProvider>();
		builder.Services.AddScoped(svc =>
		{
			DispatcherProvider.SetCurrent(svc.GetRequiredService<IDispatcherProvider>());
			return Dispatcher.GetForCurrentThread()!;
		});

		WebAlertManager.Register(builder.Services);

		builder.Services.RemoveAll<IFontRegistrar>();
		builder.Services.RemoveAll<IFontManager>();
		builder.Services.RemoveAll<IEmbeddedFontLoader>();
		builder.Services.AddSingleton<IFontRegistrar, WebFontRegistrar>();
		builder.Services.AddSingleton<IFontManager>(svc => new WebFontManager(svc.GetRequiredService<IFontRegistrar>()));

#pragma warning disable CS0612, CS0618
		DependencyService.Register<Microsoft.Maui.Controls.Internals.IFontNamedSizeService, WebFontNamedSizeService>();
#pragma warning restore CS0612, CS0618

		builder.Services.RemoveAll<ITicker>();
		builder.Services.AddSingleton<ITicker, WebTicker>();

		builder.ConfigureMauiHandlers(handlers => handlers.AddWebHandlers());
		return builder;
	}

	public static IMauiHandlersCollection AddWebHandlers(this IMauiHandlersCollection handlers)
	{
		handlers.AddHandler<Application, ApplicationHandler>();
		handlers.AddHandler<Window, WindowHandler>();
		handlers.AddHandler<ContentPage, PageHandler>();
		handlers.AddHandler<Layout, LayoutHandler>();
		handlers.AddHandler<ContentView, ContentViewHandler>();
		// ContentPresenter and any other IContentView (custom templated controls).
		handlers.AddHandler<IContentView, ContentViewHandler>();
		handlers.AddHandler<Page, EmptyPageHandler>();
		handlers.AddHandler<TemplatedView, ContentViewHandler>();
		handlers.AddHandler<Label, LabelHandler>();
		handlers.AddHandler<Button, ButtonHandler>();
		handlers.AddHandler<Entry, EntryHandler>();
		handlers.AddHandler<Editor, EditorHandler>();
		handlers.AddHandler<SearchBar, SearchBarHandler>();
		handlers.AddHandler<CheckBox, CheckBoxHandler>();
		handlers.AddHandler<Switch, SwitchHandler>();
		handlers.AddHandler<RadioButton, RadioButtonHandler>();
		handlers.AddHandler<Slider, SliderHandler>();
		handlers.AddHandler<Stepper, StepperHandler>();
		handlers.AddHandler<ProgressBar, ProgressBarHandler>();
		handlers.AddHandler<ActivityIndicator, ActivityIndicatorHandler>();
		handlers.AddHandler<Image, ImageHandler>();
		handlers.AddHandler<ImageButton, ImageButtonHandler>();
		handlers.AddHandler<BoxView, ShapeViewHandler>();
		// Controls registers each concrete shape, so a registration on the Shape base type would lose.
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.Rectangle, ShapeViewHandler>();
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.RoundRectangle, ShapeViewHandler>();
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.Ellipse, ShapeViewHandler>();
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.Line, ShapeViewHandler>();
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.Path, ShapeViewHandler>();
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.Polygon, ShapeViewHandler>();
		handlers.AddHandler<Microsoft.Maui.Controls.Shapes.Polyline, ShapeViewHandler>();
		handlers.AddHandler<Border, BorderHandler>();
#pragma warning disable CS0618 // Frame is obsolete but still used
		handlers.AddHandler<Frame, FrameHandler>();
#pragma warning restore CS0618
		handlers.AddHandler<ScrollView, ScrollViewHandler>();
		handlers.AddHandler<Picker, PickerHandler>();
		handlers.AddHandler<DatePicker, DatePickerHandler>();
		handlers.AddHandler<TimePicker, TimePickerHandler>();
		handlers.AddHandler<NavigationPage, NavigationPageHandler>();
		handlers.AddHandler<TabbedPage, TabbedPageHandler>();
		handlers.AddHandler<FlyoutPage, FlyoutPageHandler>();
		handlers.AddHandler<Shell, ShellHandler>();
		handlers.AddHandler<CollectionView, CollectionViewHandler>();
		handlers.AddHandler<WebView, WebViewHandler>();
		handlers.AddHandler<GraphicsView, GraphicsViewHandler>();
		handlers.AddHandler<CarouselView, CarouselViewHandler>();
		handlers.AddHandler<IndicatorView, IndicatorViewHandler>();
		handlers.AddHandler<RefreshView, RefreshViewHandler>();
		handlers.AddHandler<SwipeView, SwipeViewHandler>();
#pragma warning disable CS0618 // ListView and TableView are obsolete; render a placeholder rather than crash
		handlers.AddHandler<ListView, UnsupportedViewHandler>();
		handlers.AddHandler<TableView, UnsupportedViewHandler>();
#pragma warning restore CS0618
		handlers.AddHandler<HybridWebView, UnsupportedViewHandler>();
		// Not rendered on the web yet; resolving them must not hit Controls' throwing stubs.
		handlers.AddHandler<MenuBar, NullElementHandler>();
		handlers.AddHandler<MenuBarItem, NullElementHandler>();
		handlers.AddHandler<MenuFlyoutItem, NullElementHandler>();
		handlers.AddHandler<MenuFlyoutSeparator, NullElementHandler>();
		handlers.AddHandler<MenuFlyoutSubItem, NullElementHandler>();
		handlers.AddHandler<SwipeItem, NullElementHandler>();
		return handlers;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "AppInfo.SetCurrent is preserved by the DynamicDependency.")]
	[DynamicDependency("SetCurrent", typeof(AppInfo))]
	static void SetAppInfo(IAppInfo appInfo)
	{
		typeof(AppInfo)
			.GetMethod("SetCurrent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, [typeof(IAppInfo)])
			?.Invoke(null, [appInfo]);
	}
}
