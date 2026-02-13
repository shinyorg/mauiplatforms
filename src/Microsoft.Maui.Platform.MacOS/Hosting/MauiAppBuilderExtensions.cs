using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.MacOS.Dispatching;
using Microsoft.Maui.Platform.MacOS.Handlers;

namespace Microsoft.Maui.Platform.MacOS.Hosting;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseMacOSMauiApp<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApp>(this MauiAppBuilder builder) where TApp : class, IApplication
    {
        builder.Services.TryAddSingleton<IApplication, TApp>();

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<IWindow, WindowHandler>();
            handlers.AddHandler<IContentView, ContentPageHandler>();
            handlers.AddHandler(typeof(Microsoft.Maui.Controls.NavigationPage), typeof(NavigationPageHandler));
            handlers.AddHandler<ILayout, LayoutHandler>();
            handlers.AddHandler<ILabel, LabelHandler>();
            handlers.AddHandler<IButton, ButtonHandler>();
            handlers.AddHandler<IApplication, ApplicationHandler>();
            handlers.AddHandler<IScrollView, ScrollViewHandler>();
            handlers.AddHandler<IEntry, EntryHandler>();
            handlers.AddHandler<IEditor, EditorHandler>();
            handlers.AddHandler<IPicker, PickerHandler>();
            handlers.AddHandler<ISlider, SliderHandler>();
            handlers.AddHandler<IActivityIndicator, ActivityIndicatorHandler>();
            handlers.AddHandler<IProgress, ProgressBarHandler>();
            handlers.AddHandler<IShapeView, ShapeViewHandler>();
            handlers.AddHandler<IBorderView, BorderHandler>();
            handlers.AddHandler<ISwitch, SwitchHandler>();
            handlers.AddHandler<IDatePicker, DatePickerHandler>();
            handlers.AddHandler<ITimePicker, TimePickerHandler>();
            handlers.AddHandler<ICheckBox, CheckBoxHandler>();
            handlers.AddHandler(typeof(Microsoft.Maui.Controls.Image), typeof(ImageHandler));
            handlers.AddHandler(typeof(Microsoft.Maui.Controls.CollectionView), typeof(CollectionViewHandler));
            handlers.AddHandler(typeof(Microsoft.Maui.Controls.TabbedPage), typeof(TabbedPageHandler));
            handlers.AddHandler<IWebView, WebViewHandler>();
            handlers.AddHandler<Controls.MacOSBlazorWebView, BlazorWebViewHandler>();
        });

        builder.Services.TryAddSingleton<IDispatcher, MacOSDispatcher>();

        AlertManagerSubscription.Register(builder.Services);

        return builder;
    }
}
