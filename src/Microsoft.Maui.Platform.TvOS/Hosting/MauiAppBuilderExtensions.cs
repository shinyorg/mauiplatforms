using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.TvOS.Dispatching;
using Microsoft.Maui.Platform.TvOS.Handlers;

namespace Microsoft.Maui.Platform.TvOS.Hosting;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder UseTvOSMauiApp<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApp>(this MauiAppBuilder builder) where TApp : class, IApplication
    {
        builder.Services.TryAddSingleton<IApplication, TApp>();

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<IWindow, WindowHandler>();
            handlers.AddHandler<IContentView, ContentPageHandler>();
            handlers.AddHandler<ILayout, LayoutHandler>();
            handlers.AddHandler<ILabel, LabelHandler>();
            handlers.AddHandler<IButton, ButtonHandler>();
            handlers.AddHandler<IApplication, ApplicationHandler>();
            handlers.AddHandler<IScrollView, ScrollViewHandler>();
            handlers.AddHandler<IEntry, EntryHandler>();
            handlers.AddHandler<IPicker, PickerHandler>();
            handlers.AddHandler<ISlider, SliderHandler>();
            handlers.AddHandler<IActivityIndicator, ActivityIndicatorHandler>();
            handlers.AddHandler<IShapeView, ShapeViewHandler>();
            handlers.AddHandler<ISwitch, SwitchHandler>();
            handlers.AddHandler(typeof(Microsoft.Maui.Controls.Image), typeof(ImageHandler));
        });

        builder.Services.TryAddSingleton<IDispatcher, TvOSDispatcher>();

        return builder;
    }
}
