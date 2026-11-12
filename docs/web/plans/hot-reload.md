# Plan: Hot Reload for MAUI Web

**Goal:** `dotnet watch` on a MAUI Web app applies C# edits to the running page in the browser without a reload, and the page on screen re-renders to show them. XAML edits fall back to a fast rebuild and refresh.

## Where things stand

- Apps use `Microsoft.NET.Sdk.WebAssembly` and boot through `wwwroot/main.js` (`dotnet.create()` then `runMain()`); there is no Blazor.
- `Platform.Maui.Web.targets` already patches the dev loop: it refreshes computed `_framework` assets before compression, so incremental `dotnet run` builds don't serve a stale `dotnet.js.gz`. Hot reload depends on that loop staying healthy.
- MAUI 10.0.120 already ships the hooks we need: `Microsoft.Maui.HotReload.MauiHotReloadHelper`, `IHotReloadableView` (Controls implements it), `[OnHotReload]` and `HotReloadExtensions`. Nothing in `Platform.Maui.Web` calls them yet.
- Release builds AOT-compile (`RunAOTCompilation`). Hot reload only works in Debug on the interpreter, which is what `dotnet run` and `dotnet watch` use.

## How it has to work

1. `dotnet watch` builds a Debug app, serves it, and injects its browser refresh script, which opens a WebSocket back to the watcher.
2. On a C# edit, the watcher compiles a delta and sends it over that socket. The runtime applies it with `System.Reflection.Metadata.MetadataUpdater`. This needs `DOTNET_MODIFIABLE_ASSEMBLIES=debug`, which the WebAssembly SDK sets for Debug builds.
3. After the runtime applies a delta, it calls every `[assembly: MetadataUpdateHandler]` type's `ClearCache` and `UpdateApplication`. That is where we re-render.
4. The web backend rebuilds what's on screen: it re-creates the current page (or just the affected views) and lets the existing handlers diff it into the DOM.

Edits the runtime can't apply ("rude edits") make the watcher rebuild and reload the page. That has to work cleanly too.

## Work items

1. **Spike: does `dotnet watch` hot reload a non-Blazor WebAssembly app today?** Run `dotnet watch` on `samples/SampleWeb`. Confirm the refresh script is injected into our `index.html`, deltas arrive, and `MetadataUpdater.IsSupported` is true in the browser. If the watcher only wires the delta client for Blazor, we need our own small JS client (step 2) that receives deltas and calls the runtime's apply API.
2. **Delta client (only if the spike says so).** A `maui-web-hotreload.js` loaded only in Debug, from the targets, that hands deltas to the runtime the way the Blazor client does.
3. **`WebHotReloadHandler`** in `Platform.Maui.Web`, registered with `[assembly: MetadataUpdateHandler(typeof(WebHotReloadHandler))]`:
    - `ClearCache`: drop anything the backend caches by type, such as handler mappers built from reflection, `WebCss` style caches and template caches.
    - `UpdateApplication(Type[]? types)`: dispatch to the UI thread (`WebDispatcher`). Then call `MauiHotReloadHelper.UpdateApplication(types)` so `IHotReloadableView` and `[OnHotReload]` code runs. After that, re-create the current page.
4. **Re-creating the page.** For Shell, re-navigate to the current route with animation off. For a `NavigationPage` stack, replace the top page with a new instance of its type. For the root page, swap `Window.Page`. Keep the binding context when the new page's constructor didn't set one, so view model state survives.
5. **Keep the dev loop solid.** Make sure a rude-edit rebuild under `dotnet watch` still serves fresh `_framework` assets (the stale `.gz` fix in the targets), and that browser refresh doesn't race the loading screen in `main.js`.
6. **XAML.** Visual Studio's XAML Hot Reload is proprietary and isn't available through `dotnet watch`. For now, a `.xaml` edit triggers a rebuild and refresh. If MAUI's runtime XAML inflation (`ResourceLoader`) turns out to be reachable, a later step could reload just the page's XAML.
7. **Sample and docs.** Add a "Hot reload" section to `docs/web/readme.md` covering the command, what reloads in place, what forces a refresh, and that it's Debug only.
8. **Test.** A Playwright spec under `tests/playwright` that starts `dotnet watch`, edits a label's text in a fixture page, and waits for the new text without the page reloading. Keep it local-only like the other Playwright specs.

## Risks and open questions

- The watcher may not support plain WebAssembly SDK apps. The spike answers this first, and the fallback is our own delta client.
- Re-creating a page loses state held only in the page, such as scroll positions and Entry text not bound to anything. That is acceptable for a first version.
- Trimming is off in Debug, but trimmed Release builds must not keep the handler. Add `[DynamicDependency]` only where it's needed, and confirm the handler is removed from Release output.
- If deltas are applied while a layout pass is running, the handlers could see a half-updated tree. Always run the update through the dispatcher, after the current frame.

## Done when

`dotnet watch` on SampleWeb shows a changed label, colour or layout on the current page within about 2 seconds, without a page reload. A rude edit falls back to a working rebuild and refresh.
