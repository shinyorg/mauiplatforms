# .NET MAUI Backends for Apple TV & the Web

[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.TvOS.svg?label=Platform.Maui.TvOS)](https://www.nuget.org/packages/Platform.Maui.TvOS/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.Essentials.TvOS.svg?label=Platform.Maui.Essentials.TvOS)](https://www.nuget.org/packages/Platform.Maui.Essentials.TvOS/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.Web.svg?label=Platform.Maui.Web)](https://www.nuget.org/packages/Platform.Maui.Web/)
[![NuGet](https://img.shields.io/nuget/v/Platform.Maui.Web.Essentials.svg?label=Platform.Maui.Web.Essentials)](https://www.nuget.org/packages/Platform.Maui.Web.Essentials/)

Custom .NET MAUI backends for platforms MAUI doesn't officially support: **Apple TV** (tvOS via UIKit) and
the **web** (WebAssembly, rendering real DOM elements, without Blazor).

Both backends use the platform-agnostic MAUI NuGet packages (`net10.0` fallback assemblies) and provide custom
handler implementations that bridge MAUI's layout/rendering system to the native UI of each platform.

> **macOS has moved.** The macOS (AppKit) backend that started here was merged into
> [dotnet/maui-labs](https://github.com/dotnet/maui-labs) and continues there as
> [`platforms/MacOS`](https://github.com/dotnet/maui-labs/tree/main/platforms/MacOS). Use that for new macOS work.
> The code and docs from this repository are kept, unbuilt, in [`archive/macos`](archive/macos).

https://github.com/user-attachments/assets/035de5fa-bda6-49d1-98f6-41cd4d6ff559

## Web (WebAssembly)

`Platform.Maui.Web` runs a MAUI app in the browser on WebAssembly. Every control is a real DOM element
(`<button>`, `<input>`, `<dialog>`, SVG for shapes, a 2D canvas for GraphicsView), positioned by MAUI's own
cross-platform layout engine. No Blazor, no Razor components, no canvas-drawn UI.

![The SampleWeb gallery running in the browser](docs/web/images/home.png)

| Controls | Dark theme | Dialogs |
|---|---|---|
| ![Controls](docs/web/images/controls.png) | ![Dark theme](docs/web/images/home-dark.png) | ![DisplayAlert as an HTML dialog](docs/web/images/dialog.png) |
| **CollectionView** | **GraphicsView** | **Essentials** |
| ![CollectionView](docs/web/images/collectionview.png) | ![GraphicsView](docs/web/images/graphics.png) | ![Essentials](docs/web/images/essentials.png) |

On a phone the flyout becomes a popover and tabs move to the bottom:

<p>
  <img src="docs/web/images/phone-home.png" width="200" alt="Home on a phone" />
  <img src="docs/web/images/phone-menu.png" width="200" alt="Flyout menu on a phone" />
  <img src="docs/web/images/phone-tabs.png" width="200" alt="TabbedPage on a phone" />
</p>

### Setting up a web app

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). No workload is required
(`wasm-tools` is only needed if you opt into AOT compilation). Any modern browser runs the result.

#### 1. Create the project

Create a folder with this project file (`MyApp.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk.WebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- Required by the [JSImport] interop the backend generates. -->
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>

    <!-- Read at runtime by AppInfo (Name, PackageName, VersionString, BuildString). -->
    <ApplicationTitle>My App</ApplicationTitle>
    <ApplicationId>com.example.myapp</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- The packages are prereleases, so float on prerelease versions (or pin an exact one). -->
    <PackageReference Include="Platform.Maui.Web" Version="*-*" />
    <PackageReference Include="Platform.Maui.Web.Essentials" Version="*-*" />
  </ItemGroup>
</Project>
```

The SDK is `Microsoft.NET.Sdk.WebAssembly`, not `Microsoft.NET.Sdk.BlazorWebAssembly`: there is no Blazor.
`Microsoft.Maui.Controls` comes in through the `Platform.Maui.Web` package.

> **Building from this repository instead of NuGet?** Replace the two package references with project
> references, and import the backend's build targets yourself (packages do this automatically):
>
> ```xml
> <ItemGroup>
>   <ProjectReference Include="path/to/mauiplatforms/src/Platform.Maui.Web/Platform.Maui.Web.csproj" />
>   <ProjectReference Include="path/to/mauiplatforms/src/Platform.Maui.Essentials.Web/Platform.Maui.Essentials.Web.csproj" />
> </ItemGroup>
> <Import Project="path/to/mauiplatforms/src/Platform.Maui.Web/buildTransitive/Platform.Maui.Web.targets" />
> ```

#### 2. Add the host page (`wwwroot/index.html`)

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
    <title>My App</title>
    <link rel="icon" href="favicon.svg" />
    <script type="module" src="./main.js"></script>
</head>
<body>
    <div id="loading">Loading…</div>
</body>
</html>
```

The app fills the whole page. To host it inside part of an existing page instead, override
`HostElementSelector` in your `WebMauiApplication` (step 4) with a CSS selector such as `"#app"`.

#### 3. Start .NET from JavaScript (`wwwroot/main.js`)

```js
import { dotnet } from './_framework/dotnet.js';

const { runMain } = await dotnet.create();
await runMain();
document.getElementById('loading')?.remove();
```

Use `runMain()`, not `dotnet.run()`: `run()` exits the runtime when `Main` returns, which would stop the app.

#### 4. Add the entry point (`Program.cs`)

```csharp
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web;

namespace MyApp;

public class MyWebApp : WebMauiApplication
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

public static class Program
{
    public static Task Main() => new MyWebApp().RunAsync();
}
```

#### 5. Configure MAUI (`MauiProgram.cs`)

```csharp
using Microsoft.Maui.Essentials.Web;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform.Web.Hosting;

namespace MyApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppWeb<App>()       // instead of UseMauiApp<App>()
            .UseWebEssentials()         // optional: AppInfo, Preferences, Geolocation, ...
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular")); // file in Resources/Fonts
        return builder.Build();
    }
}
```

#### 6. Write the app as usual (`App.cs`)

```csharp
using Microsoft.Maui.Controls;

namespace MyApp;

public class App : Application
{
    protected override Window CreateWindow(Microsoft.Maui.IActivationState? activationState)
        => new(new ContentPage
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Hello from .NET MAUI on the web!", FontSize = 24 },
                    new Image { Source = "dotnet_bot.png", HeightRequest = 160 },
                    new Button { Text = "Click me" },
                },
            },
        });
}
```

Everything else is standard MAUI: XAML or C# pages, `Shell`, `NavigationPage`, bindings, styles, `DisplayAlertAsync`.

#### 7. Add resources (optional)

The same folders as a MAUI single project are picked up automatically and served from the site root:

| Folder | Item | Notes |
|---|---|---|
| `Resources/Images/` | `MauiImage` | SVGs are served as SVG; `Source="name.png"` finds `name.svg` |
| `Resources/Fonts/` | `MauiFont` | Register with `ConfigureFonts`; variable fonts get their full weight range |
| `Resources/Raw/` | `MauiAsset` | Read with `FileSystem.OpenAppPackageFileAsync("file.txt")` |
| `Resources/AppIcon/appicon.svg` | `MauiIcon` | Served as `favicon.svg` (linked in step 2) |

Set `<EnableDefaultMauiResourceItems>false</EnableDefaultMauiResourceItems>` to declare these items yourself.

#### 8. Run it

```bash
dotnet run
```

Open the `App url` it prints. The dev server also prints a debugging URL for browser-based .NET debugging.

#### 9. Publish and host

```bash
dotnet publish -c Release -o publish
```

Upload `publish/wwwroot` to any static host: GitHub Pages, Azure Static Web Apps, Netlify, S3/CloudFront, nginx.
Things to check on the host:

- **`.wasm` must be served as `application/wasm`.** Most hosts already do this.
- **Compression:** the publish output includes `.br` and `.gz` copies of every file. Serve them with the
  matching `Content-Encoding` when your host supports it; otherwise the uncompressed files are used.
- **Hosting under a subpath** (for example `https://example.com/myapp/`): add `<base href="/myapp/" />` to
  `<head>` in `index.html`. The backend resolves its scripts and your resources against the document's base URL.
- **Trimming:** Release builds are trimmed. String-path bindings (`SetBinding(..., "Name")`) and
  `QueryPropertyAttribute` rely on reflection; prefer compiled bindings (`SetBinding(..., static (Item i) => i.Name)`),
  or keep your app assembly whole with `<ItemGroup><TrimmerRootAssembly Include="MyApp" /></ItemGroup>`.

#### Theming the built-in chrome

Your MAUI styles style your views. The pieces the backend draws itself (navigation bar, tab bar, flyout,
dialogs, native inputs) use CSS custom properties you can override in `index.html`:

```html
<style>
    html:root { --maui-accent: #0E7C66; --maui-font: "Inter", system-ui, sans-serif; }
</style>
```

See `--maui-*` in [`maui-web.css`](src/Platform.Maui.Web/wwwroot/maui-web.css) for the full list, including
dark-mode values.

For how the backend works (rendering, layout and measurement, threading), the lessons carried over from the
maui-labs backends, and known limitations, see **[docs/web](docs/web/readme.md)**.

[`samples/SampleWeb`](samples/SampleWeb) is a 32-page gallery whose pages are ported from the maui-labs
[Linux GTK4 sample](https://github.com/dotnet/maui-labs/tree/main/platforms/Linux.Gtk4/samples).

## Project Structure

```
src/
  Platform.Maui.TvOS/               # tvOS backend library (net10.0-tvos)
  Platform.Maui.Essentials.TvOS/    # tvOS Essentials library
  Platform.Maui.Web/                # Web backend library (net10.0, browser/WebAssembly)
  Platform.Maui.Essentials.Web/     # Web Essentials library
samples/
  Sample/                           # Shared sample code (Pages, Platforms/)
  SampleTv/                         # tvOS sample app
  SampleWeb/                        # Web gallery app
tests/
  Platform.Maui.Web.Tests/          # Web unit tests (xUnit)
  browser/                          # Web end-to-end smoke test (headless Chrome)
  playwright/                       # Web end-to-end suite (Playwright: Chromium, WebKit, phone)
docs/
  web/                              # Web backend guide
archive/
  macos/                            # The former macOS backend (merged into dotnet/maui-labs)
```

> **Note:** `Sample/Sample.csproj` multitargets `net10.0-tvos` and `net10.0-macos` (now referencing the archived
> macOS projects) and is **not yet working**. Use `SampleTv` and `SampleWeb`.

## Handlers Implemented

### Controls

| Control | tvOS (UIKit) | Web (DOM) |
|---------|-------------|-----------|
| Label | UILabel | `<div>` with spans (FormattedText, HTML) |
| Button | UIButton | `<button>` |
| ImageButton | — | `<button>` + `<img>` |
| Entry | UITextField | `<input>` |
| Editor | — | `<textarea>` (auto-size) |
| Picker | UIButton + UIAlertController | `<select>` |
| Slider | Custom TvOSSliderView | `<input type="range">` |
| Stepper | — | Button pair |
| Switch | UIButton (toggle) | Styled checkbox, `role="switch"` |
| CheckBox | — | `<input type="checkbox">` |
| RadioButton | — | `<input type="radio">`, or its ControlTemplate |
| SearchBar | Custom TvOSSearchBarView | `<input type="search">` |
| ActivityIndicator | UIActivityIndicatorView | CSS spinner |
| ProgressBar | UIProgressView | `<progress>` |
| Image | UIImageView | `<img>` (file, URI, stream, font glyph; SVG served as SVG) |
| DatePicker | — | `<input type="date">` |
| TimePicker | — | `<input type="time">` |
| WebView | — | `<iframe>` |
| MapView | MKMapView (display-only) | — |

### Pages & Navigation

| Page | tvOS | Web |
|------|------|-----|
| ContentPage | ✅ | ✅ |
| NavigationPage | ✅ Stack navigation | ✅ Navigation bar, back button, toolbar items |
| TabbedPage | ✅ Custom tab bar | ✅ Tab strip (bottom on phone widths) |
| FlyoutPage | — | ✅ Docked when split, otherwise an overlay |
| Shell | — | ✅ Flyout, tabs, routes, push/pop |
| Modal pages | — | ✅ Full-window or dialog (`WebPage.SetModalPresentationStyle`) |

### Collections

| Control | tvOS | Web |
|---------|------|-----|
| CollectionView | ✅ Virtualized | ✅ Templates, grouping, selection, EmptyView, Header/Footer (not virtualized) |
| CarouselView | ✅ Horizontal paging | ✅ CSS scroll snapping |
| ListView | — | — (placeholder; use CollectionView) |
| TableView | — | — (placeholder) |
| IndicatorView | — | ✅ |
| RefreshView | — | ✅ Spinner (no pull gesture) |
| SwipeView | — | Content only |

### Layouts

All layouts work on both platforms: `VerticalStackLayout`, `HorizontalStackLayout`, `Grid`, `FlexLayout`, `AbsoluteLayout`, `ScrollView`, `ContentView`, `Border`, `Frame`.

### Graphics & Shapes

| Feature | tvOS | Web |
|---------|------|-----|
| GraphicsView | — | ✅ Canvas 2D |
| Shapes (Rectangle, Ellipse, Line, Polyline, Polygon, Path) | ✅ | ✅ SVG |
| Shadow | ✅ | ✅ `box-shadow` |
| Gradient Brushes (Linear, Radial) | — | ✅ CSS gradients |

### Gestures

| Gesture | tvOS | Web |
|---------|------|-----|
| Tap | ✅ | ✅ |
| Pan | — | ✅ |
| Swipe | — | ✅ |
| Pinch | — | — |
| Pointer (Hover) | — | ✅ |

### Infrastructure

| Component | tvOS | Web |
|-----------|------|-----|
| Application | NSObject | `WebMauiApplication` |
| Window | UIWindow + UIViewController | Full-viewport element, `ResizeObserver` |
| Dispatcher | GCD (DispatchQueue.MainQueue) | JS event loop (`MessageChannel`) |
| DispatcherTimer | NSTimer | `setTimeout` / `setInterval` |
| Dialogs (Alert, Confirm, Prompt) | — | ✅ `<dialog>` |
| Font Management | ✅ | ✅ CSS Font Loading API (variable fonts) |
| Dark/Light Mode | ✅ | ✅ `prefers-color-scheme` and `UserAppTheme` |
| Animations | ✅ | ✅ `requestAnimationFrame` ticker |
| FormattedText / Spans | — | ✅ |
| InputTransparent | — | ✅ `pointer-events` |
| Toolbar | — | ✅ ToolbarItems in the navigation bar |
| Window Resize Relayout | — | ✅ |

## Lifecycle Events

Both platforms fire standard `IWindow` lifecycle methods and support MAUI's `ConfigureLifecycleEvents` builder pattern.

### IWindow Lifecycle

| IWindow Method | tvOS Trigger | Web Trigger |
|---|---|---|
| `Created()` | App launch | App start |
| `Activated()` | App becomes active | App start, tab shown |
| `Deactivated()` | App resigns activation | Tab hidden |
| `Stopped()` | App enters background | Tab hidden |
| `Resumed()` | App enters foreground | Tab shown |
| `Destroying()` | App terminating | `Application.CloseWindow` |

### Platform-Specific Lifecycle Events

```csharp
builder.ConfigureLifecycleEvents(events =>
{
    events.AddWeb(web => web
        .OnMauiApplicationCreated(app => { })
        .OnWindowCreated(window => { /* WebWindow */ })
        .OnVisibilityChanged(visible => { })
    );
});
```

## Essentials

Platform-specific implementations of MAUI Essentials APIs.

### Setup

```csharp
// tvOS
EssentialsExtensions.UseTvOSEssentials();

// Web (MauiProgram.cs)
builder.UseWebEssentials();
```

Then use the standard MAUI Essentials APIs:
```csharp
var appName = AppInfo.Name;
var version = AppInfo.VersionString;
var theme = AppInfo.RequestedTheme;
var model = DeviceInfo.Model;
```

### Supported APIs

| API | tvOS | Web | Notes |
|-----|------|-----|-------|
| AppInfo | ✅ | ✅ | Package name, version, build, theme, layout direction |
| DeviceInfo | ✅ | ✅ | Model, manufacturer, device name, OS version, platform, idiom |
| Connectivity | ✅ | ✅ | Network access status, connection profiles, change events |
| Battery | — | ✅ | Web: Battery API where the browser has it |
| DeviceDisplay | ✅ | ✅ | Web: also KeepScreenOn via the Wake Lock API |
| FileSystem | ✅ | ✅ | Web: in-memory directories; app package files fetched from the site |
| Preferences | ✅ | ✅ | tvOS: NSUserDefaults, Web: localStorage |
| SecureStorage | ✅ | ❌ | Web throws `PlatformNotSupportedException`: a browser has nowhere safe to keep secrets |
| FilePicker | — | ✅ | |
| MediaPicker | — | ✅ | |
| TextToSpeech | ✅ | ✅ | tvOS: AVSpeechSynthesizer, Web: `speechSynthesis` |
| Clipboard | ✅ | ✅ | tvOS: in-process, Web: async Clipboard API |
| Browser | — | ✅ | |
| Share | — | ✅ | Web Share API, with fallbacks |
| Launcher | — | ✅ | |
| Map, Email, PhoneDialer, Sms | — | ✅ | URL schemes |
| Geolocation | — | ✅ | |
| Accelerometer, Gyroscope, Compass, OrientationSensor | — | ✅ | Device motion and orientation events |
| Vibration, HapticFeedback | — | ✅ | `navigator.vibrate` |
| SemanticScreenReader | — | ✅ | ARIA live region |

## Prerequisites

> **Important:** JetBrains Rider and Visual Studio will not compile the tvOS projects. The `net10.0-tvos` TFM is not recognized by IDE build systems. Build tvOS **through the CLI** using the `dotnet` command.

### .NET 10 SDK

Install the latest .NET 10 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0).

### Workloads

```bash
dotnet workload install tvos
dotnet workload list   # verify
```

The web backend needs no workload.

### Xcode

Xcode must be installed for the tvOS SDK:

```bash
sudo xcode-select -s /Applications/Xcode.app
```

## Building & Running

```bash
# tvOS (simulator)
dotnet build samples/SampleTv/SampleTv.csproj -t:Run

# Web: dev server with debugging (open the printed URL)
dotnet run --project samples/SampleWeb

# Web: tests
dotnet test tests/Platform.Maui.Web.Tests
dotnet publish samples/SampleWeb -c Release -o artifacts/SampleWeb
node tests/browser/smoke.mjs artifacts/SampleWeb/wwwroot
cd tests/playwright && npm ci && npx playwright install chromium webkit && npx playwright test
```

## Key Technical Notes

* MAUI NuGet packages resolve to the `net10.0` (platform-agnostic) assembly for unsupported TFMs. `ToPlatform()` returns `object` — custom `ViewExtensions` casts to the native view type.
* The platform-agnostic `ViewHandler` has no-op `PlatformArrange` and returns `Size.Zero`. Custom base handlers (`TvOSViewHandler`, `WebElementHandler`) override these to bridge MAUI layout to native view frames.
* Controls registers stub handlers (which throw) for every control type on unsupported platforms, so each backend registers its own handler for every concrete type. The web backend has a unit test that fails if any type still resolves to a stub.
* The tvOS sample uses pure C# pages (no XAML); the web sample also compiles XAML pages.

## Dialogs

Dialogs (`DisplayAlertAsync`, `DisplayActionSheetAsync`, `DisplayPromptAsync`) are supported on the **web** as HTML `<dialog>` elements, but are **not yet supported on tvOS**.

MAUI's dialog system uses an internal `AlertManager` with `IAlertManagerSubscription`. The web backend implements it with `DispatchProxy` (which works on WebAssembly, including trimmed builds). tvOS uses AOT compilation, which doesn't support `DispatchProxy`. Until `IAlertManagerSubscription` is made public in MAUI (see the [proposal](https://gist.github.com/Redth/fc07a982bcff79cf925168f241a12c95)), tvOS dialog support is blocked.

## TODO

### General
* Multitarget `Sample` project
* Multi-window support
* Accessibility support

### tvOS Specific
* Focus Engine integration
* Top Shelf extensions
* TV remote menu button handling

### Web Specific
* CollectionView virtualization
* Pull-to-refresh and swipe-item gestures, pinch, drag and drop
* MenuBar and context menus
* Browser history integration for navigation

## Commercial Support

Shipping this to production? [ShinySoft Technologies](https://shinysoft.biz), the company that builds and maintains the Shiny libraries, offers priority issue response, support agreements, architecture reviews and private office hours. See [Commercial Support](https://shinylib.net/foundation/support/?utm_source=github&utm_medium=readme#commercial-support) or [get in touch](https://shinysoft.biz/contact?utm_source=github&utm_medium=readme). Community support through GitHub issues and discussions stays free.
