# Plan: `net10.0-browser` target framework for MAUI Web apps

**Goal:** MAUI Web apps and libraries target `net10.0-browser` instead of plain `net10.0`. An existing MAUI single project can then add the web the way it adds a platform: `<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-browser</TargetFrameworks>`, with a `Platforms/Browser` folder.

## Where things stand

- Everything targets `$(BaseTargetFramework)` = `net10.0` (`Directory.build.props`). SampleWeb uses `Microsoft.NET.Sdk.WebAssembly`. `Platform.Maui.Web` uses `Microsoft.NET.Sdk.Razor` (only for static web assets) with `<SupportedPlatform Include="browser" />`.
- `Platform.Maui.Web.targets` defines `WEB;BROWSER` itself, and adds `[assembly: SupportedOSPlatform("browser")]` when `RuntimeIdentifier == browser-wasm`. The TFM gives us both of these for free.
- Checked on this machine: a `Microsoft.NET.Sdk.WebAssembly` project with `<TargetFramework>net10.0-browser</TargetFramework>` builds with SDK 11.0.100. It gives `TargetPlatformIdentifier=browser`, `TargetPlatformVersion=1.0`, `RuntimeIdentifier=browser-wasm`, and defines `BROWSER`, `BROWSER1_0` and `BROWSER1_0_OR_GREATER`.

## What it buys

- **Single project.** One MAUI app builds for phones, desktop and the web. Today the web needs its own project.
- **NuGet asset selection.** Packages ship `lib/net10.0-browser/` next to `lib/net10.0-ios/` and so on. `Platform.Maui.Essentials.Web`, `Platform.Maui.Web.SkiaSharp` and the Shiny `*.Web` backends can become browser assets of multi-targeted packages instead of separate packages, the same way MAUI library authors already work.
- **Analyzers.** The platform compatibility analyzer understands `browser` from the TFM, so we drop our manual `SupportedOSPlatform` attribute.

## Work items

1. **Move the libraries to `net10.0-browser`.** Change `Platform.Maui.Web`, `Platform.Maui.Essentials.Web`, `Platform.Maui.Web.SkiaSharp`, `Shiny.Maui.Controls.Camera.Web` and `Shiny.Maui.Controls.MediaElement.Web`. Add a `WebTargetFramework` property (`net10.0-browser`) next to `BaseTargetFramework`. Check that the Razor SDK's static web assets (`_content/Platform.Maui.Web/`) still pack and flow with a platform TFM.
2. **Decide on `net10.0` compatibility.** Should the packages keep a `net10.0` asset, so apps on plain `net10.0` and the unit tests (`tests/Platform.Maui.Web.Tests`, which run on the host) keep working? Recommended: multi-target `net10.0;net10.0-browser` for one release, with a build warning for `net10.0` app projects, then drop `net10.0` later. Unit tests keep targeting `net10.0`.
3. **Move SampleWeb to `net10.0-browser`.** In the targets, key off `'$(TargetPlatformIdentifier)' == 'browser'` instead of `RuntimeIdentifier`. Keep `WEB` defined for back-compat, and stop adding `BROWSER` ourselves because the SDK now defines it.
4. **Single project support (the big one).** A project on `Microsoft.NET.Sdk` with `<UseMaui>true</UseMaui>` and `net10.0-browser` among its TFMs must:
    - Bring in the WebAssembly SDK's props and targets only for the browser inner build: `<Import Sdk="Microsoft.NET.Sdk.WebAssembly" Project="Sdk.props" Condition="'$(TargetPlatformIdentifier)' == 'browser'" />`, or the equivalent from our package. **Spike first:** the platform identifier isn't known when SDK props are evaluated, so this may need a check on `$(TargetFramework)` in the inner build, or a `Platform.Maui.Web.Sdk` MSBuild SDK.
    - Stop MAUI's own workload targets from running for `browser`. Resizetizer, the platform manifest steps and the XAML build tasks must either support it or be skipped. Our targets already handle `MauiImage`, `MauiFont`, `MauiAsset` and `MauiIcon` for the web.
    - Pick up `Platforms/Browser/**` for the browser TFM only (with `Program.cs` and the `WebMauiApplication` subclass), and leave out the other `Platforms/*` folders, following MAUI's existing convention.
    - Find `wwwroot/index.html` and `main.js`: supply defaults from the package when the app has none, so a single project doesn't need its own `wwwroot`.
5. **Template.** Ship a `dotnet new` template (or update the existing one) for a single project with the browser TFM included, plus a stand-alone web-only variant.
6. **CI.** `build.slnf` keeps building. Add a job that builds a single-project sample for `net10.0-browser` alongside one other platform TFM, to prove the multi-target path.
7. **Docs.** Add a migration section to `docs/web/readme.md` (change the TFM, delete the `SupportedOSPlatform` workaround, `BROWSER` now comes from the SDK) and a single-project guide.

## Risks and open questions

- MAUI's workload targets may reject an unknown platform. That needs a spike on the `UseMaui` + `net10.0-browser` combination before we commit to single project in this release.
- `dotnet run -f net10.0-browser` from a multi-targeted project must launch the WebAssembly dev server, not try to run the app as a native process.
- Switching TFMs breaks consumers who reference the packages from `net10.0` projects. The transition release in step 2 softens that.
- `TargetPlatformVersion` is `1.0`. We have no need for versioned browser APIs, but if that ever changes, we'll have to choose between our own versioning and the SDK's.

## Order

Steps 1–3 are mechanical and can ship together. Step 4 starts with its spike. If the spike fails, ship 1–3 with a separate web project, and treat single project as follow-up work.

## Done when

SampleWeb and all web packages build on `net10.0-browser` with no manual `SupportedOSPlatform` attribute. A sample single project builds and runs for both `net10.0-browser` and at least one native TFM.
