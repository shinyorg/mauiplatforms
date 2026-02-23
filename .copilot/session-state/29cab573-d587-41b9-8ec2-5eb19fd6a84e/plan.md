# Documentation Plan

## Goal
Write comprehensive macOS platform docs covering all undocumented features and a detailed getting-started guide for setting up a new macOS app head project.

## Docs to Create

### New docs in `docs/macos/`:
- [ ] **getting-started.md** — Complete guide to creating a macOS app head project: project file setup, linking shared code/pages/resources, platform bootstrap files (Main.cs, MauiMacOSApp.cs, MauiProgram.cs, App.cs), building and running
- [ ] **blazor-hybrid.md** — BlazorWebView support: adding the NuGet, registering handler, wwwroot linking, Blazor component hosting in WKWebView
- [ ] **menu-bar.md** — Application menu bar: default menus (App/Edit/Window), configuring via MacOSMenuBarOptions, Page.MenuBarItems integration
- [ ] **lifecycle.md** — App lifecycle events: DidFinishLaunching, DidBecomeActive, DidResignActive, DidHide, DidUnhide, WillTerminate
- [ ] **theming.md** — Light/dark mode support, AppTheme mapping to NSAppearance, automatic theme change detection
- [ ] **controls.md** — Platform-specific control notes: MapView, gestures, modal pages, app icons (MauiIcon → .icns auto-generation)

### Update existing:
- [ ] **index.md** — Add links to all new docs
