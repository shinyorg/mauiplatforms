using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Platform.MacOS.Controls;

/// <summary>
/// A custom BlazorWebView control for macOS (AppKit) that hosts Blazor components
/// in a WKWebView. This avoids the MAUI BlazorWebView control which internally
/// casts to its own handler type and fails on AppKit.
/// </summary>
public class MacOSBlazorWebView : View
{
    public static readonly BindableProperty HostPageProperty =
        BindableProperty.Create(nameof(HostPage), typeof(string), typeof(MacOSBlazorWebView));

    public static readonly BindableProperty StartPathProperty =
        BindableProperty.Create(nameof(StartPath), typeof(string), typeof(MacOSBlazorWebView), "/");

    public string? HostPage
    {
        get => (string?)GetValue(HostPageProperty);
        set => SetValue(HostPageProperty, value);
    }

    public string StartPath
    {
        get => (string)GetValue(StartPathProperty);
        set => SetValue(StartPathProperty, value);
    }

    public List<BlazorRootComponent> RootComponents { get; } = new();
}

public class BlazorRootComponent
{
    public string? Selector { get; set; }
    public Type? ComponentType { get; set; }
    public IDictionary<string, object?>? Parameters { get; set; }
}
