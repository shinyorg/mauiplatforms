using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.MacOS.Controls;
using Microsoft.Maui.Platform.MacOS.Handlers;

namespace Microsoft.Maui.Platform.MacOS.Hosting;

public static class BlazorWebViewExtensions
{
    /// <summary>
    /// Adds Blazor Hybrid support for macOS AppKit. Registers the BlazorWebView handler
    /// that hosts Blazor components in a WKWebView.
    /// </summary>
    public static MauiAppBuilder AddMacOSBlazorWebView(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<MacOSBlazorWebView, BlazorWebViewHandler>();
        });
        return builder;
    }
}
