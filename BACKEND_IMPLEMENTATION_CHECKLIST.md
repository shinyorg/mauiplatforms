# .NET MAUI macOS (AppKit) Backend — Implementation Checklist

A comprehensive checklist for the Platform.Maui.MacOS backend targeting macOS via AppKit/Cocoa.
Items marked `[x]` have a handler or implementation present.

> **Reference:** Xamarin.Forms previously had a macOS/AppKit backend. The control gallery and platform renderers at
> [Xamarin.Forms 5.0 macOS](https://github.com/xamarin/Xamarin.Forms/tree/5.0.0/Xamarin.Forms.ControlGallery.MacOS)
> (and the corresponding `Xamarin.Forms.Platform.MacOS` renderers) can serve as useful inspiration for how to map
> MAUI virtual view properties to native AppKit controls — even though this project uses the newer handler architecture
> rather than the legacy renderer pattern.

---

## 1. Core Infrastructure

### Platform Abstractions
- [x] **Platform View Type** — `NSView` (via `MacOSContainerView`, a flipped `NSView` subclass with layer backing)
- [x] **Platform Context** — `MacOSMauiContext : IMauiContext` with scoped services, handler factory, window/app scope
- [x] **Dispatcher** — `MacOSDispatcher : IDispatcher` + `MacOSDispatcherProvider` + `MacOSDispatcherTimer`
- [x] **Event System** — NSEvent/NSResponder chain used by gesture recognizers, mouse tracking areas, keyboard events
- [x] **Handler Factory Integration** — All handlers registered via `AddMauiControlsHandlers()` in `AppHostBuilderExtensions`
- [x] **App Host Builder Extension** — `UseMauiAppMacOS<TApp>()` wires up handlers, dispatcher, alert manager

### Rendering Pipeline
- [x] **View Renderer** — `MacOSViewHandler<TVirtualView, TPlatformView>` base class bridges MAUI layout → NSView frames
- [x] **Property Change Propagation** — Property mappers re-apply when `IView` property changes fire
- [x] **Child Synchronization** — `MacOSContainerView` + `LayoutHandler` add/remove/reorder subviews
- [x] **Style/Attribute Application** — Opacity, IsVisible, IsEnabled, Background, FlowDirection, AutomationId, transforms, Clip, Shadow all mapped in base `MacOSViewHandler`

### AppKit Interop
- [x] **NSResponder Chain** — Used by gesture recognizers, mouse events in handlers, first responder for Entry/Editor
- [x] **NSEvent Handling** — Mouse events (click, hover, drag), keyboard events via NSTextField/NSTextView
- [x] **NSGestureRecognizer Integration** — `GestureManager` with `NSClickGestureRecognizer`, `NSPanGestureRecognizer`, `NSTrackingArea` for pointer
- [x] **NSAccessibility** — VoiceOver via SemanticScreenReader, semantic properties mapped to AccessibilityLabel/Help/Role

---

## 2. Application & Window

| Control | Status | Notes |
|---------|--------|-------|
| [x] **Application** | ✅ | `MacOSMauiApplication : NSApplicationDelegate`, lifecycle events (DidFinishLaunching, DidBecomeActive, WillTerminate, etc.) |
| [x] **Window** | ✅ | `WindowHandler` maps Title, Content, Width, Height, X, Y, MinWidth/MinHeight, MaxWidth/MaxHeight; content re-layouts on resize |

---

## 3. Pages

| Page | Status | Notes |
|------|--------|-------|
| [x] **ContentPage** | ✅ | Maps Content, Background, Title, MenuBarItems (via `MenuBarManager` → `NSApp.MainMenu`) |
| [x] **NavigationPage** | ✅ | Push/Pop via `RequestNavigation`; back button in toolbar; title updates on navigation |
| [x] **TabbedPage** | ✅ | Tab switching, BarBackgroundColor (layer), SelectedTabColor (bezel); BarTextColor/UnselectedTabColor not available in AppKit |
| [x] **FlyoutPage** | ✅ | Maps Flyout, Detail, IsPresented, FlyoutBehavior, FlyoutWidth via `NSSplitView` |
| [x] **Shell** | ✅ | `ShellHandler` — NSSplitView with sidebar flyout, content area, selection, flyout behavior |

---

## 4. Layouts

| Layout | Status | Notes |
|--------|--------|-------|
| [x] **VerticalStackLayout** | ✅ | Handled by `LayoutHandler` — MAUI's cross-platform layout manager computes frames |
| [x] **HorizontalStackLayout** | ✅ | Same as above |
| [x] **Grid** | ✅ | Row/column definitions, spans, spacing — all computed by MAUI layout manager |
| [x] **FlexLayout** | ✅ | Direction, Wrap, JustifyContent, AlignItems — MAUI layout manager handles positioning |
| [x] **AbsoluteLayout** | ✅ | Absolute and proportional positioning — MAUI layout manager computes bounds |
| [x] **ScrollView** | ✅ | Maps Content, Orientation, ScrollBarVisibility, ContentSize via `NSScrollView`; ScrollToAsync with animated scrolling via `NSAnimationContext`; `Scrolled` event fires via `SetScrolledPosition` on `BoundsChangedNotification` |
| [x] **ContentView** | ✅ | Simple content wrapper with Background support |
| [x] **Border** | ✅ | Full stroke/shape support — Stroke, StrokeThickness, StrokeShape, StrokeLineCap, StrokeLineJoin, StrokeDashPattern |
| [x] **Frame** | ✅ | Registered to `BorderHandler` — Frame extends ContentView with IBorderView, handled by Border's stroke/shape rendering |
| [x] **Layout (fallback)** | ✅ | Base `LayoutHandler` with Background; MAUI's layout manager handles custom layout subclasses |

---

## 5. Basic Controls

| Control | Status | Notes |
|---------|--------|-------|
| [x] **Label** | ✅ | Text, TextColor, Font (family/size/bold), HorizontalTextAlignment, LineBreakMode, MaxLines, TextDecorations, CharacterSpacing, Padding (via `MauiNSTextField`/`MauiNSTextFieldCell` with TextInsets), FormattedText/Spans (via `NSAttributedString`) |
| [x] **Button** | ✅ | Maps Text, TextColor, Font, CharacterSpacing, Background, CornerRadius, StrokeColor, StrokeThickness, Padding, ImageSource, Clicked event |
| [x] **ImageButton** | ✅ | `ImageButtonHandler` maps Source (file/URI), Clicked, Background, CornerRadius, StrokeColor, StrokeThickness via `NSButton` with ImageOnly position |
| [x] **Entry** | ✅ | Maps Text, TextColor, Font, CharacterSpacing, Placeholder, PlaceholderColor, IsPassword (NSSecureTextField swap), IsReadOnly, HorizontalTextAlignment, MaxLength, ReturnType, CursorPosition, SelectionLength, IsTextPredictionEnabled |
| [x] **Editor** | ✅ | Maps Text, TextColor, Font (family/size/bold), IsReadOnly, HorizontalTextAlignment, MaxLength, CharacterSpacing, Placeholder (accessibility); AutoSize is Controls-level |
| [x] **Switch** | ✅ | Maps IsOn via `NSSwitch`; TrackColor/ThumbColor not available in AppKit |
| [x] **CheckBox** | ✅ | Maps IsChecked, Foreground via `NSButton` with checkbox style |
| [x] **RadioButton** | ✅ | Maps IsChecked, TextColor, Content text; GroupName mutual exclusion handled by MAUI's cross-platform `RadioButtonGroup` |
| [x] **Slider** | ✅ | Maps Value, Minimum, Maximum via `NSSlider`; MinimumTrackColor/MaximumTrackColor/ThumbColor not available in AppKit |
| [x] **Stepper** | ✅ | Maps Value, Minimum, Maximum, Interval via `NSStepper` |
| [x] **ProgressBar** | ✅ | Maps Progress via `NSProgressIndicator`; ProgressColor via `CIColorMonochrome` content filter |
| [x] **ActivityIndicator** | ✅ | Maps IsRunning (StartAnimation/StopAnimation) via `NSProgressIndicator`; Color via `CIColorMonochrome` content filter |
| [x] **BoxView** | ✅ | Mapped via `ShapeViewHandler` |
| [x] **Image** | ✅ | Maps Source (file/URI/stream), Aspect, IsOpaque via `NSImageView`; loading state callbacks via `UpdateIsLoading` |

---

## 6. Input & Selection Controls

| Control | Status | Notes |
|---------|--------|-------|
| [x] **Picker** | Done | Maps Title, SelectedIndex, Items, TextColor, TitleColor, Background via `NSPopUpButton` |
| [x] **DatePicker** | Done | Maps Date, MinimumDate, MaximumDate, TextColor, Format via `NSDatePicker` |
| [x] **TimePicker** | Done | Maps Time, TextColor, Format via `NSDatePicker` |
| [x] **SearchBar** | ✅ | Maps Text, TextColor, Placeholder, IsReadOnly, MaxLength via `NSSearchField` |

---

## 7. Collection Controls

| Control | Status | Notes |
|---------|--------|-------|
| [x] **CollectionView** | ✅ | `CollectionViewHandler` — NSScrollView-based with full virtualization, ItemTemplate, SelectionMode (single/multiple), LinearItemsLayout, GridItemsLayout (vertical/horizontal), grouping with headers/footers, EmptyView, Header/Footer (as flat items via DataTemplate), ScrollTo with Start/Center/End/MakeVisible positions, RemainingItemsThreshold for incremental loading, item spacing |
| [x] **ListView** | ✅ | NSScrollView-based with DataTemplate, ViewCell, TextCell, ImageCell, SwitchCell, EntryCell, selection, header/footer, grouping |
| [x] **CarouselView** | ✅ | `CarouselViewHandler` — horizontal paging with snap, position tracking, swipe |
| [x] **IndicatorView** | ✅ | Page indicator dots with configurable size, color, and shape |
| [x] **TableView** | ✅ | NSScrollView-based with TableRoot/TableSection, TextCell, SwitchCell, EntryCell, ViewCell |
| [x] **SwipeView** | ✅ | Swipe-to-reveal actions via horizontal pan gesture with left/right items |
| [x] **RefreshView** | ✅ | Content wrapper with NSProgressIndicator spinner overlay (no pull-to-refresh on macOS) |

---

## 8. Navigation & Routing

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **NavigationPage stack** | ✅ | PushAsync, PopAsync via `RequestNavigation` command |
| [x] **Shell navigation** | ✅ | Shell.CurrentItem navigation, sidebar selection, flyout behavior, push/pop within ShellSection |
| [x] **Deep linking** | ⚠️ | Partial — requires app-level Info.plist URL scheme + NSApplicationDelegate.OpenUrls override; framework supports it via lifecycle events |
| [x] **Back button** | ✅ | Toolbar back button via `MacOSToolbarManager` for both NavigationPage and Shell navigation stacks; pops via Navigation.PopAsync or ShellSection.Navigation.PopAsync |
| [x] **ToolbarItems** | ✅ | `MacOSToolbarManager` manages `NSToolbar` items from `Page.ToolbarItems` |

---

## 9. Alerts & Dialogs

| Dialog | Status | Notes |
|--------|--------|-------|
| [x] **DisplayAlert** | ✅ | Title, message, accept/cancel buttons via `NSAlert` + `RunModal()` |
| [x] **DisplayActionSheet** | ✅ | Multi-button action sheet via `NSAlert` with button mapping |
| [x] **DisplayPromptAsync** | ✅ | Text input dialog via `NSAlert` with `NSTextField` accessory view |

---

## 10. Gesture Recognizers

| Gesture | Status | Notes |
|---------|--------|-------|
| [x] **TapGestureRecognizer** | ✅ | `MacOSTapGestureRecognizer` wrapping `NSClickGestureRecognizer` with NumberOfTapsRequired, Command |
| [x] **PanGestureRecognizer** | ✅ | `MacOSPanGestureRecognizer` wrapping `NSPanGestureRecognizer` with translation tracking |
| [x] **SwipeGestureRecognizer** | ✅ | `MacOSSwipeGestureRecognizer` using `NSPanGestureRecognizer` with velocity threshold for swipe detection |
| [x] **PinchGestureRecognizer** | ✅ | `MacOSPinchGestureRecognizer` wrapping `NSMagnificationGestureRecognizer` for trackpad pinch-to-zoom |
| [x] **PointerGestureRecognizer** | ✅ | `MacOSPointerTrackingArea` using `NSTrackingArea` for mouseEntered/mouseExited/mouseMoved |

---

## 11. Graphics & Shapes

### Microsoft.Maui.Graphics
| Feature | Status | Notes |
|---------|--------|-------|
| [x] **GraphicsView** | ✅ | `MacOSGraphicsView : NSView` with `IDrawable` rendering via `DirectRenderer` + CoreGraphics |
| [x] **Canvas Operations** | ✅ | CoreGraphics (`CGContext`) provides DrawLine, DrawRect, DrawEllipse, DrawPath, DrawString, Fill operations |
| [x] **Canvas State** | ✅ | CGContext supports SaveState/RestoreState, affine transforms |
| [x] **Brushes** | Done | SolidColorBrush, LinearGradientBrush, RadialGradientBrush via `CAGradientLayer` in MapBackground |

### Shapes
| Shape | Status | Notes |
|-------|--------|-------|
| [x] **All Shapes** | ✅ | `ShapeViewHandler` renders via `IShapeView.Shape` + CoreGraphics paths |
| [x] **Fill & Stroke** | ✅ | Fill brush and Stroke mapped in `ShapeViewHandler` |

> **Note:** Shapes are rendered using CoreGraphics (`CGPath`) in the `ShapeNSView` custom `NSView` subclass. Individual shape types (Rectangle, Ellipse, Line, Path, Polygon, Polyline) are handled by the MAUI cross-platform shape geometry — the handler draws whatever `IShape` provides.

---

## 12. Common View Properties (Base Handler)

Every handler must support these properties mapped from the base `IView` in `MacOSViewHandler`. Currently only Shadow is mapped in the base.

### Visibility & State
- [x] Opacity → `NSView.AlphaValue`
- [x] IsVisible → `NSView.Hidden`
- [x] IsEnabled → `NSControl.Enabled`
- [x] InputTransparent → returns null from HitTest in MacOSContainerView

### Sizing
- [x] WidthRequest / HeightRequest — respected during `GetDesiredSize` measurement
- [x] MinimumWidthRequest / MinimumHeightRequest — used as floor in measurement
- [x] MaximumWidthRequest / MaximumHeightRequest

### Layout
- [x] HorizontalOptions (Start, Center, End, Fill)
- [x] VerticalOptions (Start, Center, End, Fill)
- [x] Margin
- [x] Padding (for views implementing IPadding)
- [x] FlowDirection (LTR, RTL) → `NSView.UserInterfaceLayoutDirection`
- [x] ZIndex → `NSView` `layer.zPosition`

### Appearance
- [x] BackgroundColor — mapped in base `MacOSViewHandler.MapBackground` via `CALayer.BackgroundColor`
- [x] Background (LinearGradientBrush, RadialGradientBrush) → via `CAGradientLayer`

### Interactivity Attachments
- [x] **ToolTip** — `ToolTipProperties.Text` → `NSView.ToolTip`
- [x] **ContextFlyout** — `FlyoutBase.GetContextFlyout()` → `NSMenu` on right-click

### Transforms
- [x] TranslationX / TranslationY → `CATransform3D` via `MapTransform`
- [x] Rotation / RotationX / RotationY → `CATransform3D` via `MapTransform`
- [x] Scale / ScaleX / ScaleY → `CATransform3D` via `MapTransform`
- [x] AnchorX / AnchorY → `CALayer.AnchorPoint` via `MapTransform`

### Effects
- [x] Shadow → `CALayer` shadow properties (shadowColor, shadowOffset, shadowRadius, shadowOpacity)
- [x] Clip → `CAShapeLayer` mask via `MapClip` with `PathFToCGPath` conversion

### Automation
- [x] AutomationId → `NSView.AccessibilityIdentifier`
- [x] Semantic properties → `NSAccessibility` protocol (AccessibilityLabel, AccessibilityHelp, AccessibilityRole)

### Animations
- [x] Core Animation — NSAnimationContext used for scroll, swipe, carousel transitions; MacOSTicker drives MAUI animation system

---

## 13. VisualStateManager & Triggers

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **VisualStateManager** | ✅ | Cross-platform MAUI feature — works without platform code via binding/property system |
| [x] **PropertyTrigger** | ✅ | Cross-platform MAUI feature — no platform handler needed |
| [x] **DataTrigger** | ✅ | Cross-platform MAUI feature — no platform handler needed |
| [x] **MultiTrigger** | ✅ | Cross-platform MAUI feature — no platform handler needed |
| [x] **EventTrigger** | ✅ | Cross-platform MAUI feature — no platform handler needed |
| [x] **Behaviors** | ✅ | Cross-platform MAUI feature — no platform handler needed |

---

## 14. Font Management

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **IFontManager** | ✅ | `MacOSFontManager` resolves `Font` → `NSFont` with family, size, weight (Bold/Light), slant (Italic/Oblique). Registered as singleton via `Services.Replace()` |
| [x] **IFontRegistrar** | ✅ | Font registration handled by MAUI's built-in `FontRegistrar` — our `MacOSEmbeddedFontLoader` provides the platform loading |
| [x] **IEmbeddedFontLoader** | ✅ | `MacOSEmbeddedFontLoader` loads fonts from streams via `CGDataProvider` → `CGFont.CreateFromProvider()` → `CTFontManager.RegisterGraphicsFont()`, returns PostScript name for `NSFont.FromFontName()` |
| [x] **Native Font Loading** | ✅ | Registered via CoreText `CTFontManager.RegisterGraphicsFont()` |
| [x] **IFontNamedSizeService** | ✅ | `MacOSFontNamedSizeService` maps NamedSize enum to macOS point sizes. Registered via `[assembly: Dependency]` attribute. Prevents `XamlParseException` for `FontSize="Title"`. |
| [x] **Font properties** | ✅ | Font mapped on Label, Entry, Editor, Button with family/size/bold via `FontExtensions.ToNSFont()` |

---

## 15. Essentials / Platform Services

| Service | Interface | Status | Notes |
|---------|-----------|--------|-------|
| [x] **App Info** | `IAppInfo` | ✅ | `AppInfoImplementation` |
| [x] **Battery** | `IBattery` | ✅ | `BatteryImplementation` |
| [x] **Browser** | `IBrowser` | ✅ | `BrowserImplementation` — opens URLs via `NSWorkspace` |
| [x] **Clipboard** | `IClipboard` | ✅ | `ClipboardImplementation` — `NSPasteboard` |
| [x] **Connectivity** | `IConnectivity` | ✅ | `ConnectivityImplementation` |
| [x] **Device Display** | `IDeviceDisplay` | ✅ | `DeviceDisplayImplementation` — `NSScreen` info |
| [x] **Device Info** | `IDeviceInfo` | ✅ | `DeviceInfoImplementation` |
| [x] **File Picker** | `IFilePicker` | ✅ | `FilePickerImplementation` — `NSOpenPanel` |
| [x] **File System** | `IFileSystem` | ✅ | `FileSystemImplementation` |
| [x] **Geolocation** | `IGeolocation` | ✅ | `GeolocationImplementation` — CLLocationManager with foreground listening |
| [x] **Launcher** | `ILauncher` | ✅ | `LauncherImplementation` — `NSWorkspace.OpenUrl` |
| [x] **Map** | `IMap` | ✅ | `MapImplementation` — Opens Apple Maps via URL scheme |
| [x] **Media Picker** | `IMediaPicker` | ✅ | `MediaPickerImplementation` |
| [x] **Preferences** | `IPreferences` | ✅ | `PreferencesImplementation` — `NSUserDefaults` |
| [x] **Secure Storage** | `ISecureStorage` | ✅ | `SecureStorageImplementation` — Keychain |
| [x] **Semantic Screen Reader** | `ISemanticScreenReader` | ✅ | `SemanticScreenReaderImplementation` — VoiceOver via NSAccessibility |
| [x] **Share** | `IShare` | ✅ | `ShareImplementation` — `NSSharingServicePicker` |
| [x] **Text-to-Speech** | `ITextToSpeech` | ✅ | `TextToSpeechImplementation` — `NSSpeechSynthesizer` / `AVSpeechSynthesizer` |
| [x] **Version Tracking** | `IVersionTracking` | ✅ | Cross-platform `VersionTrackingImplementation` uses `IPreferences` + `IAppInfo` (both implemented) |
| [x] **Vibration** | `IVibration` | ✅ | No-op (IsSupported=false) — macOS lacks vibration hardware |

---

## 16. Styling Infrastructure

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **Border style mapping** | ✅ | `BorderHandler` maps Stroke, StrokeShape, StrokeThickness, StrokeLineCap, StrokeLineJoin, StrokeDashPattern, StrokeDashOffset, StrokeMiterLimit via CoreGraphics |
| [x] **View state mapping** | ✅ | IsVisible → `Hidden`, IsEnabled → `Enabled`, Opacity → `AlphaValue` — all mapped in base `MacOSViewHandler` |
| [x] **Automation mapping** | ✅ | AutomationId → `AccessibilityIdentifier` mapped in base `MacOSViewHandler` |

---

## 17. WebView

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **URL loading** | ✅ | Navigate to URLs via `WKWebView.LoadRequest` |
| [x] **HTML content** | ✅ | Display raw HTML via `WKWebView.LoadHtmlString` |
| [x] **JavaScript execution** | ✅ | `EvaluateJavaScriptAsync` via `WKWebView.EvaluateJavaScript` |
| [x] **Navigation commands** | ✅ | GoBack, GoForward, Reload commands mapped |
| [x] **User Agent** | ✅ | Custom user agent string support |

---

## 18. Label — FormattedText Detail

FormattedText requires special handling as a compound property using `NSAttributedString`:

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **FormattedText rendering** | ✅ | `LabelHandler.MapFormattedText` builds `NSAttributedString` from `FormattedString.Spans` → `NSTextField.AttributedStringValue` |
| [x] **Span.Text** | ✅ | Text content per span |
| [x] **Span.TextColor** | ✅ | `NSAttributedString` foreground color attribute |
| [x] **Span.BackgroundColor** | ✅ | `NSAttributedString` background color attribute |
| [x] **Span.FontSize** | ✅ | `NSFont` size attribute per span |
| [x] **Span.FontFamily** | ✅ | `NSFont` family attribute per span via `NSFont.FromFontName` |
| [x] **Span.FontAttributes** | ✅ | Bold/Italic via `NSFontManager.ConvertFont` |
| [x] **Span.TextDecorations** | ✅ | `NSUnderlineStyle` / `NSStrikethroughStyle` attributes |
| [x] **Span.CharacterSpacing** | ✅ | `NSKern` attribute |

---

## 19. MenuBar (Desktop)

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **MenuBarItem** | ✅ | Top-level menu items → `NSMenuItem` with submenu, added to `NSApp.MainMenu` via `MenuBarManager` |
| [x] **MenuFlyoutItem** | ✅ | Submenu items with Text, Command, CommandParameter, KeyboardAccelerators, enabled/disabled state |
| [x] **MenuFlyoutSeparator** | ✅ | `NSMenuItem.SeparatorItem` |
| [x] **Integration** | ✅ | `ContentPageHandler.MapMenuBarItems` wires `Page.MenuBarItems` to `MenuBarManager.UpdateMenuBar()` |

> **Note:** macOS has a global menu bar (not per-window). `MenuBar` does NOT implement `IView` — it must be managed by the `ContentPage` handler or `Window` handler via `NSApp.MainMenu`.

---

## 20. Animations

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **MacOSTicker** | ✅ | Custom `ITicker` using `NSTimer` on main run loop; replaces default `System.Timers.Timer` which fires on threadpool (unsafe for AppKit) |
| [x] **TranslateTo** | ✅ | Works via MAUI's built-in animation system — `MacOSTicker` drives frames, property mapper applies `CATransform3D` |
| [x] **FadeTo** | ✅ | Works via MAUI's built-in animation system — `MacOSTicker` drives frames, `MapOpacity` applies `AlphaValue` |
| [x] **ScaleTo** | ✅ | Works via MAUI's built-in animation system — `MacOSTicker` drives frames, `MapTransform` applies scale |
| [x] **RotateTo** | ✅ | Works via MAUI's built-in animation system — `MacOSTicker` drives frames, `MapTransform` applies rotation |
| [x] **LayoutTo** | ✅ | Works via MAUI's built-in animation system |
| [x] **Easing functions** | ✅ | Handled by MAUI's `Animation` class — easing is applied during value interpolation, not at platform level |
| [x] **Animation class** | ✅ | `new Animation(...)` with child animations, `Commit()`, `AbortAnimation()` — all cross-platform in MAUI Controls |
| [x] **AnimationExtensions** | ✅ | Extension methods on `VisualElement` — cross-platform in MAUI Controls |

> **Note:** MAUI's animation system is fully cross-platform. It uses `IAnimationManager` + `ITicker` to drive frame updates
> that set virtual view properties (e.g., `Opacity`, `TranslationX`). The handler property mappers then apply changes to native views.
> The only platform-specific requirement is providing a main-thread-safe `ITicker` — our `MacOSTicker` uses `NSTimer` on the main run loop.

---

## 21. ControlTemplate & ContentPresenter

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **ControlTemplate** | ✅ | Cross-platform MAUI feature — template inflation via ContentPresenter, no platform code needed |
| [x] **ContentPresenter** | ✅ | Cross-platform MAUI feature — dynamically instantiates template content |
| [x] **RadioButton ControlTemplate** | ✅ | Cross-platform MAUI feature — works via ControlTemplate system |
| [x] **TemplatedView** | ✅ | Cross-platform MAUI feature — base class for controls with ControlTemplate support |

---

## Summary Statistics

| Category | Implemented | Total | Notes |
|----------|-------------|-------|-------|
| **Core Infrastructure** | 6 of 6 | 6 | All core abstractions in place including gesture integration |
| **Pages** | 5 of 5 | 5 | ✅ All page types implemented including Shell |
| **Layouts** | 10 of 10 | 10 | ✅ All layouts implemented including Frame via BorderHandler |
| **Basic Controls** | 12 of 14 | 14 | ImageButton now implemented; Label has full Padding support |
| **Collection Controls** | 7 of 7 | 7 | ✅ All collection controls implemented |
| **Input Controls** | 4 of 4 | 4 | All present; Entry/Editor improved with font/spacing |
| **Gesture Recognizers** | 5 of 5 | 5 | ✅ All: Tap, Pan, Swipe, Pinch, Pointer |
| **Shapes** | 1 handler | 6 types | Single ShapeViewHandler covers all shape types |
| **Essentials** | 20 of 20 | 20 | ✅ All essentials implemented |
| **Dialog Types** | 3 of 3 | 3 | All implemented via NSAlert |
| **Font Services** | 5 of 5 | 5 | ✅ Full: IFontManager, IFontRegistrar, IEmbeddedFontLoader, Native Loading, IFontNamedSizeService |
| **Animations** | 9 of 9 | 9 | ✅ Full: MacOSTicker + MAUI's cross-platform animation system handles all animation types |
| **MenuBar** | 4 of 4 | 4 | ✅ Full: MenuBarItem, MenuFlyoutItem, MenuFlyoutSeparator, MenuFlyoutSubItem |
| **FormattedText** | 9 of 9 | 9 | ✅ Full: All span properties mapped via NSAttributedString |
| **Base View Properties** | ~18 of 20+ | 20+ | Opacity, IsVisible, IsEnabled, Background, FlowDirection, AutomationId, Transforms, Clip, Shadow, ToolTip, HorizontalOptions, VerticalOptions, Margin, Padding |
| **VSM & Triggers** | 6 of 6 | 6 | ✅ All cross-platform MAUI features — no platform code needed |
| **ControlTemplate** | 4 of 4 | 4 | ✅ All cross-platform MAUI features — ControlTemplate, ContentPresenter, TemplatedView, RadioButton |