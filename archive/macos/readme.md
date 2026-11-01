> **Archived.** The macOS (AppKit) backend was merged into [dotnet/maui-labs](https://github.com/dotnet/maui-labs),
> where it continues as [`platforms/MacOS`](https://github.com/dotnet/maui-labs/tree/main/platforms/MacOS).
> This folder keeps the code and documentation as they were at the time of the merge; it is no longer
> built or published from this repository. Paths below (`src/`, `samples/`, `docs/macos/`) now live under
> `archive/macos/`. The tvOS and web backends remain at the repository root.

# .NET MAUI Backends for Apple TV & macOS (AppKit)

[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.TvOS.svg?label=Platform.Maui.TvOS)](https://www.nuget.org/packages/Platform.Maui.TvOS/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.MacOS.svg?label=Platform.Maui.MacOS)](https://www.nuget.org/packages/Platform.Maui.MacOS/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.MacOS.svg?label=Platform.Maui.MacOS.BlazorWebView)](https://www.nuget.org/packages/Platform.Maui.MacOS.BlazorWebView/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.Essentials.TvOS.svg?label=Platform.Maui.Essentials.TvOS)](https://www.nuget.org/packages/Platform.Maui.Essentials.TvOS/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.Essentials.MacOS.svg?label=Platform.Maui.Essentials.MacOS)](https://www.nuget.org/packages/Platform.Maui.Essentials.MacOS/)

Custom .NET MAUI backends targeting platforms not officially supported by MAUI — Apple TV (tvOS via UIKit) and macOS (native AppKit, not Mac Catalyst).

Both backends use the platform-agnostic MAUI NuGet packages (`net10.0` fallback assemblies) and provide custom handler implementations that bridge MAUI's layout/rendering system to the native platform UI frameworks.

> **Inspiration:** The Xamarin.Forms project previously had a macOS AppKit backend ([Xamarin.Forms.Platform.MacOS](https://github.com/xamarin/Xamarin.Forms/tree/5.0.0/Xamarin.Forms.ControlGallery.MacOS)). While this project uses MAUI's handler architecture rather than the legacy renderer approach, some of that codebase is useful as a reference for mapping AppKit native controls.

https://github.com/user-attachments/assets/035de5fa-bda6-49d1-98f6-41cd4d6ff559


## Quick Start — Setting Up a macOS MAUI App

### 1. Create the project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-macos</TargetFramework>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <SupportedOSPlatformVersion>14.0</SupportedOSPlatformVersion>
    <ApplicationTitle>My macOS App</ApplicationTitle>
    <ApplicationId>com.example.myapp</ApplicationId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Platform.Maui.MacOS" Version="*" />
    <PackageReference Include="Platform.Maui.Essentials.MacOS" Version="*" />
  </ItemGroup>

  <!-- App icon (SVG or PNG source — .icns is generated automatically) -->
  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.png" />
  </ItemGroup>
</Project>
```

### 2. Entry point (`Main.cs`)

```csharp
using AppKit;

public class MainClass
{
    static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new MauiMacOSApp();
        NSApplication.Main(args);
    }
}
```

### 3. App delegate (`MauiMacOSApp.cs`)

```csharp
using Foundation;
using Microsoft.Maui.Platform.MacOS;

[Register("MauiMacOSApp")]
public class MauiMacOSApp : MacOSMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

### 4. MAUI program (`MauiProgram.cs`)

```csharp
using Microsoft.Maui.Platform.MacOS.Hosting;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppMacOS<App>()
            .AddMacOSEssentials()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        return builder.Build();
    }
}
```

### 5. App class (`App.cs`)

```csharp
public class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new MainPage());
}
```

## App Icons

The `Platform.Maui.MacOS` NuGet package includes MSBuild targets that automatically process `MauiIcon` items — the same item type used by MAUI's Resizetizer for iOS, Android, and Windows.

```xml
<ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.png" />
</ItemGroup>
```

At build time, the targets:
1. Use `sips` to resize the source image (SVG or PNG) to all 10 required macOS icon sizes (16×16 through 512×512@2x)
2. Use `iconutil` to create the `.icns` file
3. Inject `CFBundleIconFile` into the app manifest via `PartialAppManifest`
4. Add the `.icns` as a `BundleResource`

No manual `.icns` creation or `Info.plist` editing required.

## macOS Platform-Specific APIs

### Shell — Native Sidebar

The macOS backend supports Shell with a native macOS sidebar using `NSOutlineView` source list, providing a Finder/System Settings-like navigation experience. Configure it with attached properties on your `Shell`:

```csharp
using Microsoft.Maui.Platform.MacOS;

public class AppShell : Shell
{
    public AppShell()
    {
        FlyoutBehavior = FlyoutBehavior.Locked;

        // Enable native NSOutlineView sidebar
        MacOSShell.SetUseNativeSidebar(this, true);

        // Allow user to resize sidebar by dragging the divider (default: true)
        MacOSShell.SetIsSidebarResizable(this, true);

        var generalSection = new FlyoutItem { Title = "General" };

        var home = new ShellContent { Title = "Home", ContentTemplate = new DataTemplate(typeof(HomePage)) };
        MacOSShell.SetSystemImage(home, "house.fill");  // SF Symbol icon
        generalSection.Items.Add(home);

        var settings = new ShellContent { Title = "Settings", ContentTemplate = new DataTemplate(typeof(SettingsPage)) };
        MacOSShell.SetSystemImage(settings, "gear");
        generalSection.Items.Add(settings);

        Items.Add(generalSection);
    }
}
```

#### `MacOSShell` Attached Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UseNativeSidebar` | `bool` | `false` | Use native `NSOutlineView` source list sidebar instead of custom-drawn sidebar |
| `SystemImage` | `string?` | `null` | SF Symbol name for sidebar icon (e.g. `"house.fill"`, `"gear"`, `"star"`). Set on `ShellContent`, `ShellSection`, or `FlyoutItem` |
| `IsSidebarResizable` | `bool` | `true` | Allow sidebar resizing by dragging the divider |

### FlyoutPage — Native Sidebar

For `FlyoutPage`, you can also use a native sidebar with structured items:

```csharp
using Microsoft.Maui.Platform.MacOS;

var flyoutPage = new FlyoutPage();

MacOSFlyoutPage.SetSidebarItems(flyoutPage, new List<MacOSSidebarItem>
{
    new MacOSSidebarItem
    {
        Title = "Library",
        Children = new List<MacOSSidebarItem>
        {
            new MacOSSidebarItem { Title = "Music", SystemImage = "music.note" },
            new MacOSSidebarItem { Title = "Photos", SystemImage = "photo" },
        }
    }
});

MacOSFlyoutPage.SetSidebarSelectionChanged(flyoutPage, item =>
{
    // Handle selection
});
```

To use the native sidebar handler, register it in `MauiProgram.cs`:
```csharp
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<FlyoutPage, NativeSidebarFlyoutPageHandler>();
});
```

#### `MacOSFlyoutPage` Attached Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SidebarItems` | `IList<MacOSSidebarItem>?` | `null` | Structured sidebar items for native `NSOutlineView` |
| `SidebarSelectionChanged` | `Action<MacOSSidebarItem>?` | `null` | Callback when a sidebar item is selected |

#### `MacOSSidebarItem` Properties

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | Display title |
| `SystemImage` | `string?` | SF Symbol name (takes priority over `Icon`) |
| `Icon` | `ImageSource?` | MAUI ImageSource fallback |
| `Children` | `IList<MacOSSidebarItem>?` | Child items — makes this item a section header (group row) |
| `Tag` | `object?` | Developer-defined tag for identifying selected items |
| `IsGroup` | `bool` | Read-only: `true` when item has children |

## Project Structure

```
src/
  Platform.Maui.MacOS/              # macOS AppKit backend library (net10.0-macos)
  Platform.Maui.MacOS.BlazorWebView/ # Blazor Hybrid support for macOS
  Platform.Maui.TvOS/               # tvOS backend library (net10.0-tvos)
  Platform.Maui.Essentials.MacOS/   # macOS Essentials library
  Platform.Maui.Essentials.TvOS/    # tvOS Essentials library
samples/
  Sample/                           # Shared sample code (Pages, Platforms/)
  SampleTv/                         # tvOS sample app
  SampleMac/                        # macOS sample app
```

> **Note:** There is also a `Sample/Sample.csproj` that multitargets both `net10.0-tvos` and `net10.0-macos`, but it is **not yet working**. Use `SampleTv` and `SampleMac` to build and run the individual platform samples.

## Handlers Implemented

### Controls

| Control | tvOS (UIKit) | macOS (AppKit) |
|---------|-------------|----------------|
| Label | UILabel | NSTextField (non-editable) |
| Button | UIButton | NSButton |
| ImageButton | — | NSButton (image content) |
| Entry | UITextField | NSTextField (editable) |
| Editor | — | NSTextView (in NSScrollView) |
| Picker | UIButton + UIAlertController | NSPopUpButton |
| Slider | Custom TvOSSliderView | NSSlider |
| Stepper | — | NSStepper |
| Switch | UIButton (toggle) | NSSwitch |
| CheckBox | — | NSButton (checkbox style) |
| RadioButton | — | NSButton (radio style) |
| SearchBar | Custom TvOSSearchBarView | NSSearchField |
| ActivityIndicator | UIActivityIndicatorView | NSProgressIndicator |
| ProgressBar | UIProgressView | NSProgressIndicator (bar) |
| Image | UIImageView | NSImageView |
| DatePicker | — | NSDatePicker (date mode) |
| TimePicker | — | NSDatePicker (time mode) |
| WebView | — | WKWebView |
| MapView | MKMapView (display-only) | MKMapView (interactive) |

### Pages & Navigation

| Page | tvOS | macOS |
|------|------|-------|
| ContentPage | ✅ | ✅ |
| NavigationPage | ✅ Stack navigation | ✅ Stack + toolbar back button |
| TabbedPage | ✅ Custom tab bar | ✅ NSSegmentedControl |
| FlyoutPage | — | ✅ Native sidebar or custom |
| Shell | — | ✅ Full navigation, native sidebar, push/pop |
| Modal pages | — | ✅ NSAlert-backed |

### Collections

| Control | tvOS | macOS |
|---------|------|-------|
| CollectionView | ✅ Virtualized | ✅ Virtualized, grouping, selection, EmptyView, Header/Footer |
| CarouselView | ✅ Horizontal paging | ✅ Scroll snapping, indicators |
| ListView | — | ✅ (deprecated — use CollectionView) |
| TableView | — | ✅ |
| IndicatorView | — | ✅ |
| RefreshView | — | ✅ |
| SwipeView | — | ✅ |

### Layouts

All layouts work on both platforms: `VerticalStackLayout`, `HorizontalStackLayout`, `Grid`, `FlexLayout`, `AbsoluteLayout`, `ScrollView`, `ContentView`, `Border`, `Frame`.

### Graphics & Shapes

| Feature | tvOS | macOS |
|---------|------|-------|
| GraphicsView | — | ✅ CoreGraphics DirectRenderer |
| Shapes (Rectangle, Ellipse, Line, Polyline, Polygon, Path) | ✅ | ✅ |
| Shadow | ✅ | ✅ |
| Gradient Brushes (Linear, Radial) | — | ✅ via CAGradientLayer |

### Gestures

| Gesture | tvOS | macOS |
|---------|------|-------|
| Tap | ✅ | ✅ |
| Pan | — | ✅ |
| Swipe | — | ✅ |
| Pinch | — | ✅ |
| Pointer (Hover) | — | ✅ |

### Infrastructure

| Component | tvOS | macOS |
|-----------|------|-------|
| Application | NSObject | NSObject |
| Window | UIWindow + UIViewController | NSWindow + FlippedNSView |
| Dispatcher | GCD (DispatchQueue.MainQueue) | GCD (DispatchQueue.MainQueue) |
| DispatcherTimer | NSTimer | NSTimer |
| Dialogs (Alert, Confirm, Prompt) | — | ✅ NSAlert |
| Font Management | ✅ | ✅ CTFontManager |
| Dark/Light Mode | ✅ | ✅ Automatic theme detection |
| Animations | ✅ | ✅ MacOSTicker + MAUI animation system |
| FormattedText / Spans | — | ✅ NSAttributedString |
| InputTransparent | — | ✅ HitTest override |
| Toolbar | — | ✅ NSToolbar |
| Window Resize Relayout | — | ✅ |

## BlazorWebView (macOS only)

The BlazorWebView implementation uses a custom `MacOSBlazorWebView` control — the built-in MAUI `BlazorWebView` internally casts to the iOS/Catalyst handler, which fails on AppKit.

### Setup

```csharp
// In MauiProgram.cs
builder.AddMacOSBlazorWebView();
```

### Usage

```csharp
using Microsoft.Maui.Platform.MacOS.Controls;

var blazorView = new MacOSBlazorWebView
{
    HostPage = "wwwroot/index.html",
    HeightRequest = 400
};
blazorView.RootComponents.Add(new BlazorRootComponent
{
    Selector = "#app",
    ComponentType = typeof(MyApp.Components.Counter)
});
```

Static web assets (`wwwroot/`) must be included as `BundleResource` items in the project file:
```xml
<ItemGroup>
    <BundleResource Include="wwwroot\**" />
</ItemGroup>
```

## Lifecycle Events

Both platforms fire standard `IWindow` lifecycle methods and support MAUI's `ConfigureLifecycleEvents` builder pattern.

### IWindow Lifecycle

| IWindow Method | macOS Trigger | tvOS Trigger |
|---|---|---|
| `Created()` | App launch | App launch |
| `Activated()` | App becomes active | App becomes active |
| `Deactivated()` | App loses active status | App resigns activation |
| `Stopped()` | App hidden (Cmd+H) | App enters background |
| `Resumed()` | App unhidden | App enters foreground |
| `Destroying()` | App terminating | App terminating |

### Platform-Specific Lifecycle Events

```csharp
builder.ConfigureLifecycleEvents(events =>
{
    events.AddMacOS(macOS => macOS
        .DidFinishLaunching(notification => { /* NSNotification */ })
        .DidBecomeActive(notification => { })
        .DidResignActive(notification => { })
        .DidHide(notification => { })
        .DidUnhide(notification => { })
        .WillTerminate(notification => { })
    );
});
```

## Essentials

Platform-specific implementations of MAUI Essentials APIs.

### Setup

```csharp
// In MauiProgram.cs
builder.AddMacOSEssentials();
```

Then use the standard MAUI Essentials APIs:
```csharp
var appName = AppInfo.Name;
var version = AppInfo.VersionString;
var theme = AppInfo.RequestedTheme;
var model = DeviceInfo.Model;
```

### Supported APIs

| API | tvOS | macOS | Notes |
|-----|------|-------|-------|
| AppInfo | ✅ | ✅ | Package name, version, build, theme, layout direction |
| DeviceInfo | ✅ | ✅ | Model, manufacturer, device name, OS version, platform, idiom |
| Connectivity | ✅ | ✅ | Network access status, connection profiles, change events |
| Battery | — | ✅ | Charge level, state, power source (IOKit) |
| DeviceDisplay | ✅ | ✅ | Screen dimensions, density, orientation, refresh rate |
| FileSystem | ✅ | ✅ | Cache directory, app data directory, app package files |
| Preferences | ✅ | ✅ | Key/value storage via NSUserDefaults |
| SecureStorage | ✅ | ✅ | Encrypted storage via Keychain |
| FilePicker | — | ✅ | NSOpenPanel file selection |
| MediaPicker | — | ✅ | Photo/video picking via NSOpenPanel |
| TextToSpeech | ✅ | ✅ | macOS: NSSpeechSynthesizer, tvOS: AVSpeechSynthesizer |
| Clipboard | ✅ | ✅ | macOS: NSPasteboard, tvOS: in-process |
| Browser | — | ✅ | Open URLs via NSWorkspace |
| Share | — | ✅ | NSSharingServicePicker |
| Launcher | — | ✅ | Open files/URIs via NSWorkspace |

## Prerequisites

> **Important:** JetBrains Rider and Visual Studio will not compile these projects. The `net10.0-tvos` and `net10.0-macos` TFMs are not recognized by IDE build systems. All building and running **must be done through the CLI** using the `dotnet` command.

### .NET 10 SDK

Install the latest .NET 10 preview SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0).

### Workloads

```bash
dotnet workload install tvos
dotnet workload install macos
dotnet workload list   # verify
```

### Xcode

Xcode must be installed for Apple platform SDKs:

```bash
sudo xcode-select -s /Applications/Xcode.app
```

## Building & Running

```bash
# Build
dotnet build samples/SampleMac/SampleMac.csproj
dotnet build samples/SampleTv/SampleTv.csproj

# Run macOS
dotnet build samples/SampleMac/SampleMac.csproj -t:Run

# Run tvOS (simulator)
dotnet build samples/SampleTv/SampleTv.csproj -t:Run
```

> **Note:** Do not use `dotnet build "MAUI Platforms.slnx"` — build projects individually.

## Key Technical Notes

* MAUI NuGet packages resolve to the `net10.0` (platform-agnostic) assembly for unsupported TFMs. `ToPlatform()` returns `object` — custom `ViewExtensions` casts to the native view type.
* The platform-agnostic `ViewHandler` has no-op `PlatformArrange` and returns `Size.Zero`. Custom base handlers (`MacOSViewHandler`/`TvOSViewHandler`) override these to bridge MAUI layout to native view frames.
* macOS NSView uses bottom-left origin by default. All container views override `IsFlipped => true` for MAUI's top-left coordinate system.
* macOS NSView has no `SizeThatFits()` — the base handler uses `IntrinsicContentSize` and `FittingSize`, with a custom `SizeThatFits` method on `MacOSContainerView`.
* Sample apps use pure C# pages (no XAML) to avoid XAML compilation issues on unsupported platforms.
* The macOS window uses `FullSizeContentView` style with `TitlebarAppearsTransparent = true` so the Shell sidebar extends under the titlebar with proper macOS vibrancy.

## Dialogs

Dialogs (`DisplayAlertAsync`, `DisplayPromptAsync`) are supported on **macOS** via `NSAlert`, but are **not yet supported on tvOS**.

MAUI's dialog system uses an internal `AlertManager` with `IAlertManagerSubscription`. On macOS, we implement this via `DispatchProxy` reflection. tvOS uses AOT compilation which doesn't support `DispatchProxy`. Until `IAlertManagerSubscription` is made public in MAUI (see the [proposal](https://gist.github.com/Redth/fc07a982bcff79cf925168f241a12c95)), tvOS dialog support is blocked.

## TODO

### General
* Multitarget `Sample` project
* XAML Compilation
* Multi-window support
* Accessibility support

### tvOS Specific
* Focus Engine integration
* Top Shelf extensions
* TV remote menu button handling

### macOS Specific
* Touch Bar support
* Drag and drop
* Multiple windows

### Broader Goals
* NuGet packaging
* CI/CD pipeline

## Commercial Support

Shipping this to production? [ShinySoft Technologies](https://shinysoft.biz), the company that builds and maintains the Shiny libraries, offers priority issue response, support agreements, architecture reviews and private office hours. See [Commercial Support](https://shinylib.net/foundation/support/?utm_source=github&utm_medium=readme#commercial-support) or [get in touch](https://shinysoft.biz/contact?utm_source=github&utm_medium=readme). Community support through GitHub issues and discussions stays free.
