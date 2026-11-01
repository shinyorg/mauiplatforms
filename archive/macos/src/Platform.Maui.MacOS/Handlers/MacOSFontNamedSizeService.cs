using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

[assembly: Dependency(typeof(Microsoft.Maui.Platform.MacOS.Handlers.MacOSFontNamedSizeService))]

namespace Microsoft.Maui.Platform.MacOS.Handlers;

/// <summary>
/// Maps MAUI NamedSize enum values to macOS point sizes.
/// Required for XAML FontSize="Title" etc. — without it,
/// FontSizeConverter throws XamlParseException during DataTemplate.CreateContent().
/// Registered via DependencyService attribute (legacy pattern).
/// </summary>
public class MacOSFontNamedSizeService : IFontNamedSizeService
{
    public double GetNamedSize(NamedSize size, Type targetElementType, bool useOldSizes)
    {
        return size switch
        {
            NamedSize.Default => 13.0,
            NamedSize.Micro => 10.0,
            NamedSize.Small => 12.0,
            NamedSize.Medium => 17.0,
            NamedSize.Large => 22.0,
            NamedSize.Body => 13.0,
            NamedSize.Header => 17.0,
            NamedSize.Title => 28.0,
            NamedSize.Subtitle => 22.0,
            NamedSize.Caption => 12.0,
            _ => 13.0,
        };
    }
}
