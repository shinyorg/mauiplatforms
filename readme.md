# .NET MAUI Backends for Apple TV & macOS (AppKit)

Custom .NET MAUI backends targeting platforms not officially supported by MAUI — Apple TV (tvOS via UIKit) and macOS (native AppKit, not Mac Catalyst).

Both backends use the platform-agnostic MAUI NuGet packages (`net10.0` fallback assemblies) and provide custom handler implementations that bridge MAUI's layout/rendering system to the native platform UI frameworks.

## Samples

Videos are attached in the repo

## Project Structure

```
src/
  Microsoft.Maui.Platform.TvOS/     # tvOS backend library (net10.0-tvos)
  Microsoft.Maui.Platform.MacOS/    # macOS AppKit backend library (net10.0-macos)
  Microsoft.Maui.Essentials.TvOS/   # tvOS Essentials library
  Microsoft.Maui.Essentials.MacOS/  # macOS Essentials library
samples/
  Sample/                           # Shared sample code (App.cs, MainPage.cs, Platforms/)
  SampleTv/                         # tvOS sample app (links files from Sample/)
  SampleMac/                        # macOS sample app (links files from Sample/)
```

> **Note:** There is also a `Sample/Sample.csproj` that multitargets both `net10.0-tvos` and `net10.0-macos`, but it is **not yet working**. Use `SampleTv` and `SampleMac` to build and run the individual platform samples.

## Handlers Implemented

Both platforms share the same set of control handlers:

| Control | tvOS (UIKit) | macOS (AppKit) |
|---------|-------------|----------------|
| Label | UILabel | NSTextField (non-editable) |
| Button | UIButton | NSButton |
| Entry | UITextField | NSTextField (editable) |
| Editor | ❌ Not implemented | NSTextView (multiline, in NSScrollView) |
| Picker | UIButton + UIAlertController | NSPopUpButton |
| Slider | Custom TvOSSliderView | NSSlider |
| Switch | UIButton (toggle, no native UISwitch on tvOS) | NSSwitch |
| CheckBox | ❌ Not available on tvOS | NSButton (checkbox style) |
| ActivityIndicator | UIActivityIndicatorView | NSProgressIndicator |
| ProgressBar | UIProgressView | NSProgressIndicator (bar mode) |
| Image | UIImageView | NSImageView |
| ScrollView | UIScrollView | NSScrollView |
| ShapeView | UIView + CAShapeLayer | NSView + CAShapeLayer |
| Border | UIView + CAShapeLayer (stroke + mask) | NSView + CAShapeLayer (stroke + mask) |
| DatePicker | ❌ No UIDatePicker on tvOS | NSDatePicker (date mode) |
| TimePicker | ❌ No UIDatePicker on tvOS | NSDatePicker (time mode) |
| Shadow | CALayer shadow properties | CALayer shadow properties |
| Layout (Stack, Grid, etc.) | TvOSContainerView | MacOSContainerView |
| CollectionView | UIScrollView (item materialization) | NSScrollView (item materialization) |
| CarouselView | UIScrollView (horizontal paging, snap-to-item) | ❌ Not implemented |
| ContentPage | TvOSContainerView | MacOSContainerView |
| ContentView | TvOSContainerView | MacOSContainerView |
| FlyoutPage | ❌ Not available on tvOS | FlyoutContainerView (NSSplitView sidebar) |
| Toolbar | ❌ Not available on tvOS | NSToolbar (via Page.ToolbarItems) |
| BoxView | via ShapeView | via ShapeView |
| GraphicsView | ❌ Not implemented | MacOSGraphicsView (DirectRenderer + CoreGraphics) |
| NavigationPage | NavigationContainerView (stack navigation) | NavigationContainerView (stack navigation) |
| TabbedPage | TabbedContainerView (custom tab bar) | TabbedContainerView (NSSegmentedControl) |
| WebView | ❌ Not available on tvOS | WKWebView |
| BlazorWebView | ❌ Not available on tvOS | MacOSBlazorWebView + WKWebView |

### Infrastructure

| Component | tvOS | macOS |
|-----------|------|-------|
| Application | NSObject | NSObject |
| Window | UIWindow + UIViewController | NSWindow + FlippedNSView |
| Dispatcher | GCD (DispatchQueue.MainQueue) | GCD (DispatchQueue.MainQueue) |
| DispatcherTimer | NSTimer | NSTimer |
| Dialogs (Alert, Confirm, Prompt) | ❌ Not supported (see below) | NSAlert |

## Handler TODO

### Controls
* ImageButton
* Stepper
* RadioButton
* SearchBar
* Dialogs (Confirm, Prompt, Alert) - macOS ✅, tvOS ❌ (see [Dialogs](#dialogs) below)

### Pages
* IndicatorView

### Collections
* RefreshView
* SwipeView

## Platform TODO

### General
* Multitarget `Sample` project (`net10.0-tvos;net10.0-macos` in a single csproj)
* XAML Compilation (currently C#-only pages work reliably)
* Multi-window support
* Modal page presentation
* Keyboard/focus management
* Accessibility support
* Light/dark mode support (theme detection and dynamic colors) ✅
* Font management (custom fonts, font families)
* Gesture recognizers (Tap, Swipe, Pan, Pinch)

### tvOS Specific
* Focus Engine integration (visual focus states on controls)
* Top Shelf extensions
* TV remote menu button handling
* TVUIKit integration (TVPosterView, TVMonogramView)

### macOS Specific
* Menu bar integration (NSMenu)
* Touch Bar support
* NSSecureTextField for Entry.IsPassword (currently no-op)
* File dialogs (Open/Save panels)
* Drag and drop
* Multiple windows
* Window lifecycle (minimize, fullscreen, close)

### Broader Goals
* ~~WebView~~ — macOS ✅ (WKWebView), tvOS ❌ (not supported by platform)
* ~~BlazorWebView~~ — macOS ✅ (custom MacOSBlazorWebView control), tvOS ❌ (no WebView support)
* App Icons (ideally via MAUI build tools / `MauiIcon`)
* ~~Essentials (platform-specific API wrappers)~~ — AppInfo ✅, DeviceInfo ✅, Connectivity ✅, Battery ✅ (macOS only), DeviceDisplay ✅, FileSystem ✅, Preferences ✅, SecureStorage ✅, FilePicker ✅ (macOS only), MediaPicker ✅ (macOS only), TextToSpeech ✅ (see [Essentials](#essentials) below)
* NuGet packaging
* CI/CD pipeline

## Prerequisites

> **Important:** JetBrains Rider and Visual Studio will not compile these projects. The `net10.0-tvos` and `net10.0-macos` TFMs are not recognized by IDE build systems. All building and running **must be done through the CLI** using the `dotnet` command.

### .NET 10 SDK

Install the latest .NET 10 preview SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0).

### Workloads (macOS only)

The tvOS and macOS workloads must be installed. These are only available on macOS (requires Xcode).

```bash
# Install both workloads
dotnet workload install tvos
dotnet workload install macos

# Verify they are installed
dotnet workload list
```

You should see output similar to:

```
Installed Workload Id    Manifest Version    Installation Source
-----------------------------------------------------------------
macos                    26.2.10197/10.0.100 SDK 10.0.100
tvos                     26.2.10197/10.0.100 SDK 10.0.100
```

### Xcode

Xcode must be installed (for Apple platform SDKs and the tvOS simulator). Ensure the command-line tools are selected:

```bash
sudo xcode-select -s /Applications/Xcode.app
```

## Building

All builds must be done via the CLI:

```bash
# Build the backend libraries
dotnet build src/Microsoft.Maui.Platform.TvOS/Microsoft.Maui.Platform.TvOS.csproj
dotnet build src/Microsoft.Maui.Platform.MacOS/Microsoft.Maui.Platform.MacOS.csproj

# Build the sample apps (use SampleTv / SampleMac, not Sample)
dotnet build samples/SampleTv/SampleTv.csproj
dotnet build samples/SampleMac/SampleMac.csproj
```

> **Note:** Do not use `dotnet build "MAUI Platforms.slnx"` — the solution-level build may fail due to multitarget issues. Build projects individually instead. The `Sample` multitarget project is also not yet working — use `SampleTv` and `SampleMac` instead.

## Running

### tvOS (Simulator)

```bash
dotnet build samples/SampleTv/SampleTv.csproj -t:Run
```

This builds, deploys to the tvOS simulator, and launches the app in one step.

### macOS

```bash
dotnet build samples/SampleMac/SampleMac.csproj
open samples/SampleMac/bin/Debug/net10.0-macos/osx-arm64/MAUI\ macOS.app
```

## Key Technical Notes

* MAUI NuGet packages resolve to the `net10.0` (platform-agnostic) assembly for unsupported TFMs. This means `ToPlatform()` returns `object` — custom `ViewExtensions` casts to the native view type (UIView/NSView).
* The platform-agnostic `ViewHandler` has no-op `PlatformArrange` and returns `Size.Zero` from `GetDesiredSize`. Custom base handlers (`TvOSViewHandler`/`MacOSViewHandler`) override these to bridge MAUI layout to native view frames.
* macOS NSView uses bottom-left origin by default. All container views override `IsFlipped => true` for MAUI's top-left coordinate system.
* macOS NSView has no `SizeThatFits()` — the base handler uses `IntrinsicContentSize` and `FittingSize` for native controls, and a custom `SizeThatFits` method on `MacOSContainerView`.
* `IButton` does not have `Text` or `TextColor` directly — those are on `IText` and `ITextStyle`. Handlers cast via `if (button is IText textButton)`.
* Sample apps use pure C# pages (no XAML) to avoid XAML compilation issues on unsupported platforms.
* `NavigationPage` dispatches navigation requests via `Handler.Invoke()` (the MAUI command mapper pattern), not direct method calls. The handler must register a `CommandMapper` entry for `RequestNavigation` and call `NavigationFinished()` on the view after completing navigation. The initial page is pushed automatically by MAUI's `OnHandlerChangedCore`.

## BlazorWebView (macOS only)

The BlazorWebView implementation uses a custom `MacOSBlazorWebView` control instead of the MAUI package's `BlazorWebView` — the built-in control internally casts its handler to the iOS/Catalyst `BlazorWebViewHandler`, which fails on AppKit.

**Architecture:**
- `MacOSBlazorWebView` — a simple `View` subclass with `HostPage`, `StartPath`, and `RootComponents` properties
- `BlazorWebViewHandler` — creates a WKWebView with a custom `app://` URL scheme handler and injects the Blazor interop script
- `MacOSWebViewManager` — bridges `WebViewManager` (from `Microsoft.AspNetCore.Components.WebView`) to the native WKWebView
- `MacOSMauiAssetFileProvider` — serves static content from the macOS app bundle's `Resources/` directory
- `MacOSBlazorDispatcher` — bridges MAUI's `IDispatcher` to Blazor's abstract `Dispatcher`

**Usage:**
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

Static web assets (`wwwroot/`) must be included as `BundleResource` items in the project file, and `blazor.modules.json` (an empty `[]` array) plus `blazor.webview.js` must be present under `wwwroot/_framework/`.

## Essentials

Platform-specific implementations of MAUI Essentials APIs for both tvOS and macOS.

### Supported APIs

| API | tvOS | macOS | Notes |
|-----|------|-------|-------|
| AppInfo | ✅ | ✅ | Package name, version, build, theme, layout direction |
| DeviceInfo | ✅ | ✅ | Model, manufacturer, device name, OS version, platform, idiom, device type |
| Connectivity | ✅ | ✅ | Network access status, connection profiles (WiFi), change events |
| Battery | ❌ | ✅ | Charge level, state, power source, change events (IOKit). Not available on tvOS. |
| DeviceDisplay | ✅ | ✅ | Screen dimensions, density, orientation, rotation, refresh rate, keep screen on |
| FileSystem | ✅ | ✅ | Cache directory, app data directory, app package file access |
| Preferences | ✅ | ✅ | Key/value storage via NSUserDefaults |
| SecureStorage | ✅ | ✅ | Encrypted key/value storage via Keychain |
| FilePicker | ❌ | ✅ | Single/multiple file picking via NSOpenPanel. Not available on tvOS. |
| MediaPicker | ❌ | ✅ | Photo/video picking via NSOpenPanel (no capture). Not available on tvOS. |
| TextToSpeech | ✅ | ✅ | macOS: NSSpeechSynthesizer, tvOS: AVSpeechSynthesizer. GetLocalesAsync unavailable on tvOS (AOT). |

### Usage

Call the registration method early in your app startup (before `MauiApp.CreateBuilder()`):

```csharp
// macOS
Microsoft.Maui.Essentials.MacOS.EssentialsExtensions.UseMacOSEssentials();

// tvOS
Microsoft.Maui.Essentials.TvOS.EssentialsExtensions.UseTvOSEssentials();
```

Then use the standard MAUI Essentials APIs:
```csharp
var appName = AppInfo.Name;
var version = AppInfo.VersionString;
var theme = AppInfo.RequestedTheme;
var model = DeviceInfo.Model;
var platform = DeviceInfo.Platform;
```

> **Note:** MAUI's `AppInfo.SetCurrent()` and `DeviceInfo.SetCurrent()` are `internal`, so the Essentials libraries use reflection to set the backing field. This works with the `net10.0` fallback assemblies.

### Essentials TODO
* Clipboard
* VersionTracking
* MainThread

## Dialogs

Dialogs (`DisplayAlertAsync`, `DisplayPromptAsync`) are supported on **macOS** via `NSAlert`, but are **not yet supported on tvOS**.

MAUI's dialog system is driven by an internal `AlertManager` class that resolves an `IAlertManagerSubscription` implementation from DI. On macOS, we register a custom implementation using `DispatchProxy` to implement the internal interface via reflection. However, **tvOS uses AOT compilation which does not support dynamic code generation** — `DispatchProxy` throws `PlatformNotSupportedException` at runtime.

Until `IAlertManagerSubscription` is made public in MAUI (see the [proposal](https://gist.github.com/Redth/fc07a982bcff79cf925168f241a12c95)), tvOS dialog support is blocked. The implementation is commented out in `src/Microsoft.Maui.Platform.TvOS/Platform/AlertManagerSubscription.cs` and the sample uses `#if !TVOS` compiler directives to exclude dialog UI on Apple TV.
