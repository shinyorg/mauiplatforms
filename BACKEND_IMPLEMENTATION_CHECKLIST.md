# .NET MAUI macOS (AppKit) Backend — Implementation Checklist

A comprehensive checklist for the Platform.Maui.MacOS backend targeting macOS via AppKit/Cocoa.
Items marked `[x]` have a handler or implementation present; items marked `[~]` are partially implemented.

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
- [ ] **Event System** — Native AppKit event model for user interactions (NSEvent, NSResponder chain)
- [x] **Handler Factory Integration** — All handlers registered via `AddMauiControlsHandlers()` in `AppHostBuilderExtensions`
- [x] **App Host Builder Extension** — `UseMauiAppMacOS<TApp>()` wires up handlers, dispatcher, alert manager

### Rendering Pipeline
- [x] **View Renderer** — `MacOSViewHandler<TVirtualView, TPlatformView>` base class bridges MAUI layout → NSView frames
- [x] **Property Change Propagation** — Property mappers re-apply when `IView` property changes fire
- [x] **Child Synchronization** — `MacOSContainerView` + `LayoutHandler` add/remove/reorder subviews
- [x] **Style/Attribute Application** — Opacity, IsVisible, IsEnabled, Background, FlowDirection, AutomationId, transforms, Clip, Shadow all mapped in base `MacOSViewHandler`

### AppKit Interop
- [ ] **NSResponder Chain** — First responder management for keyboard/mouse event routing
- [ ] **NSEvent Handling** — Mouse, keyboard, and trackpad event processing
- [x] **NSGestureRecognizer Integration** — `GestureManager` with `NSClickGestureRecognizer`, `NSPanGestureRecognizer`, `NSTrackingArea` for pointer
- [ ] **NSAccessibility** — VoiceOver and accessibility protocol conformance

---

## 2. Application & Window

| Control | Status | Notes |
|---------|--------|-------|
| [x] **Application** | ✅ | `MacOSMauiApplication : NSApplicationDelegate`, lifecycle events (DidFinishLaunching, DidBecomeActive, WillTerminate, etc.) |
| [~] **Window** | Partial | `WindowHandler` maps Title and Content; missing min/max size constraints, position tracking, fullscreen support |

---

## 3. Pages

| Page | Status | Notes |
|------|--------|-------|
| [x] **ContentPage** | ✅ | Maps Content, Background, Title, MenuBarItems (via `MenuBarManager` → `NSApp.MainMenu`) |
| [~] **NavigationPage** | Partial | Push/Pop via `RequestNavigation` command works; missing transition animations, back button customization |
| [~] **TabbedPage** | Partial | Tab switching via events; missing property mappers for tab appearance/placement |
| [x] **FlyoutPage** | ✅ | Maps Flyout, Detail, IsPresented, FlyoutBehavior, FlyoutWidth via `NSSplitView` |
| [ ] **Shell** | ❌ | Not implemented — no ShellHandler exists |

---

## 4. Layouts

| Layout | Status | Notes |
|--------|--------|-------|
| [x] **VerticalStackLayout** | ✅ | Handled by `LayoutHandler` — MAUI's cross-platform layout manager computes frames |
| [x] **HorizontalStackLayout** | ✅ | Same as above |
| [x] **Grid** | ✅ | Row/column definitions, spans, spacing — all computed by MAUI layout manager |
| [x] **FlexLayout** | ✅ | Direction, Wrap, JustifyContent, AlignItems — MAUI layout manager handles positioning |
| [x] **AbsoluteLayout** | ✅ | Absolute and proportional positioning — MAUI layout manager computes bounds |
| [~] **ScrollView** | Partial | Maps Content, Orientation, ScrollBarVisibility, ContentSize via `NSScrollView`; missing `ScrollToAsync` APIs, scroll position tracking, `Scrolled` event |
| [x] **ContentView** | ✅ | Simple content wrapper with Background support |
| [x] **Border** | ✅ | Full stroke/shape support — Stroke, StrokeThickness, StrokeShape, StrokeLineCap, StrokeLineJoin, StrokeDashPattern |
| [ ] **Frame** | ❌ | Legacy border container — no dedicated handler (may fall back to Border) |
| [x] **Layout (fallback)** | ✅ | Base `LayoutHandler` with Background; MAUI's layout manager handles custom layout subclasses |

---

## 5. Basic Controls

| Control | Status | Notes |
|---------|--------|-------|
| [x] **Label** | ✅ | Text, TextColor, Font (family/size/bold), HorizontalTextAlignment, LineBreakMode, MaxLines, TextDecorations, CharacterSpacing, FormattedText/Spans (via `NSAttributedString`) |
| [~] **Button** | Partial | Maps Text, TextColor, Font, CharacterSpacing, Background, CornerRadius, StrokeColor, StrokeThickness, Padding, ImageSource, Clicked event |
| [ ] **ImageButton** | ❌ | Not implemented — no ImageButtonHandler |
| [~] **Entry** | Partial | Maps Text, TextColor, Font, CharacterSpacing, Placeholder, PlaceholderColor, IsPassword (NSSecureTextField swap), IsReadOnly, HorizontalTextAlignment, MaxLength, ReturnType, CursorPosition, SelectionLength, IsTextPredictionEnabled |
| [~] **Editor** | Partial | Maps Text, TextColor, Font (family/size/bold), IsReadOnly, HorizontalTextAlignment, MaxLength, CharacterSpacing, Placeholder (accessibility); missing AutoSize |
| [~] **Switch** | Partial | Maps IsOn via `NSSwitch`; TrackColor/ThumbColor limited by AppKit control |
| [x] **CheckBox** | ✅ | Maps IsChecked, Foreground via `NSButton` with checkbox style |
| [~] **RadioButton** | Partial | Maps IsChecked, TextColor, Content text; missing GroupName mutual exclusion, ControlTemplate support |
| [~] **Slider** | Partial | Maps Value, Minimum, Maximum via `NSSlider`; MinimumTrackColor, MaximumTrackColor, ThumbColor limited by AppKit |
| [x] **Stepper** | ✅ | Maps Value, Minimum, Maximum, Interval via `NSStepper` |
| [~] **ProgressBar** | Partial | Maps Progress via `NSProgressIndicator`; missing ProgressColor |
| [~] **ActivityIndicator** | Partial | Maps IsRunning (StartAnimation/StopAnimation) via `NSProgressIndicator`; missing Color |
| [x] **BoxView** | ✅ | Mapped via `ShapeViewHandler` |
| [~] **Image** | Partial | Maps Source (file/URI/stream), Aspect, IsOpaque via `NSImageView`; missing error/loading callback handling |

---

## 6. Input & Selection Controls

| Control | Status | Notes |
|---------|--------|-------|
| [~] **Picker** | Partial | Maps Title, SelectedIndex, Items, TextColor, Background via `NSPopUpButton`; missing TitleColor |
| [~] **DatePicker** | Partial | Maps Date, MinimumDate, MaximumDate, TextColor via `NSDatePicker`; missing custom Format |
| [~] **TimePicker** | Partial | Maps Time, TextColor via `NSDatePicker`; missing custom Format |
| [x] **SearchBar** | ✅ | Maps Text, TextColor, Placeholder, IsReadOnly, MaxLength via `NSSearchField` |

---

## 7. Collection Controls

| Control | Status | Notes |
|---------|--------|-------|
| [~] **CollectionView** | Partial | Maps ItemsSource, ItemTemplate via `NSScrollView`; missing SelectionMode, ScrollTo, grouping, virtualization, item spacing, layout modes, incremental loading |
| [ ] **ListView** | ❌ | Not implemented |
| [ ] **CarouselView** | ❌ | Not implemented |
| [ ] **IndicatorView** | ❌ | Not implemented |
| [ ] **TableView** | ❌ | Not implemented |
| [ ] **SwipeView** | ❌ | Not implemented |
| [ ] **RefreshView** | ❌ | Not implemented |

---

## 8. Navigation & Routing

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **NavigationPage stack** | ✅ | PushAsync, PopAsync via `RequestNavigation` command |
| [ ] **Shell navigation** | ❌ | Shell not implemented |
| [ ] **Deep linking** | ❌ | macOS URL scheme / `NSAppleEventManager` `GetUrl` handler |
| [ ] **Back button** | ❌ | No platform back button handling |
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
| [ ] **SwipeGestureRecognizer** | ❌ | Needs trackpad swipe detection or custom mouse delta tracking |
| [ ] **PinchGestureRecognizer** | ❌ | Needs `NSMagnificationGestureRecognizer` for trackpad pinch |
| [x] **PointerGestureRecognizer** | ✅ | `MacOSPointerTrackingArea` using `NSTrackingArea` for mouseEntered/mouseExited/mouseMoved |

---

## 11. Graphics & Shapes

### Microsoft.Maui.Graphics
| Feature | Status | Notes |
|---------|--------|-------|
| [x] **GraphicsView** | ✅ | `MacOSGraphicsView : NSView` with `IDrawable` rendering via `DirectRenderer` + CoreGraphics |
| [x] **Canvas Operations** | ✅ | CoreGraphics (`CGContext`) provides DrawLine, DrawRect, DrawEllipse, DrawPath, DrawString, Fill operations |
| [x] **Canvas State** | ✅ | CGContext supports SaveState/RestoreState, affine transforms |
| [~] **Brushes** | Partial | SolidColorBrush mapped on several handlers; LinearGradientBrush/RadialGradientBrush via `CAGradientLayer` — needs verification |

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
- [~] InputTransparent → mapped but needs hit-test override in container `NSView`

### Sizing
- [x] WidthRequest / HeightRequest — respected during `GetDesiredSize` measurement
- [x] MinimumWidthRequest / MinimumHeightRequest — used as floor in measurement
- [x] MaximumWidthRequest / MaximumHeightRequest

### Layout
- [ ] HorizontalOptions (Start, Center, End, Fill)
- [ ] VerticalOptions (Start, Center, End, Fill)
- [ ] Margin
- [ ] Padding (for views implementing IPadding)
- [x] FlowDirection (LTR, RTL) → `NSView.UserInterfaceLayoutDirection`
- [ ] ZIndex → `NSView` subview ordering or `layer.zPosition`

### Appearance
- [x] BackgroundColor — mapped in base `MacOSViewHandler.MapBackground` via `CALayer.BackgroundColor`
- [~] Background (LinearGradientBrush, RadialGradientBrush) → needs `CAGradientLayer` (only SolidPaint supported currently)

### Interactivity Attachments
- [ ] **ToolTip** — `ToolTipProperties.Text` → `NSView.ToolTip`
- [ ] **ContextFlyout** — `FlyoutBase.GetContextFlyout()` → `NSMenu` on right-click

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
- [ ] Semantic properties → `NSAccessibility` protocol

### Animations
- [ ] Core Animation (`CABasicAnimation`, `CAKeyframeAnimation`) or `NSAnimationContext` for smooth property transitions

---

## 13. VisualStateManager & Triggers

| Feature | Status | Notes |
|---------|--------|-------|
| [ ] **VisualStateManager** | ❌ | GoToState for "Normal", "Focused", "PointerOver", "Disabled", custom states |
| [ ] **PropertyTrigger** | ❌ | React to property value changes |
| [ ] **DataTrigger** | ❌ | React to binding value changes with conditions |
| [ ] **MultiTrigger** | ❌ | Multiple conditions combined |
| [ ] **EventTrigger** | ❌ | React to events (begin animations, etc.) |
| [ ] **Behaviors** | ❌ | Attach custom behaviors to views |

---

## 14. Font Management

| Feature | Status | Notes |
|---------|--------|-------|
| [x] **IFontManager** | ✅ | `MacOSFontManager` resolves `Font` → `NSFont` with family, size, weight (Bold/Light), slant (Italic/Oblique). Registered as singleton via `Services.Replace()` |
| [ ] **IFontRegistrar** | ❌ | Register embedded fonts with aliases |
| [ ] **IEmbeddedFontLoader** | ❌ | Extract fonts from assembly resources, register via `CTFontManager` |
| [ ] **Native Font Loading** | ❌ | Register fonts with CoreText (`CTFontManagerRegisterFontsForURL`) |
| [x] **IFontNamedSizeService** | ✅ | `MacOSFontNamedSizeService` maps NamedSize enum to macOS point sizes. Registered via `[assembly: Dependency]` attribute. Prevents `XamlParseException` for `FontSize="Title"`. |
| [~] **Font properties** | Partial | Font mapped on Label, Entry, Editor with family/size/bold via `FontExtensions.ToNSFont()`; not all controls wire up FontFamily/FontAttributes |

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
| [ ] **Geolocation** | `IGeolocation` | ❌ | Needs `CLLocationManager` implementation |
| [x] **Launcher** | `ILauncher` | ✅ | `LauncherImplementation` — `NSWorkspace.OpenUrl` |
| [ ] **Map** | `IMap` | ❌ | Needs to open Apple Maps via URL scheme |
| [x] **Media Picker** | `IMediaPicker` | ✅ | `MediaPickerImplementation` |
| [x] **Preferences** | `IPreferences` | ✅ | `PreferencesImplementation` — `NSUserDefaults` |
| [x] **Secure Storage** | `ISecureStorage` | ✅ | `SecureStorageImplementation` — Keychain |
| [ ] **Semantic Screen Reader** | `ISemanticScreenReader` | ❌ | Needs `NSAccessibility` announce support |
| [x] **Share** | `IShare` | ✅ | `ShareImplementation` — `NSSharingServicePicker` |
| [x] **Text-to-Speech** | `ITextToSpeech` | ✅ | `TextToSpeechImplementation` — `NSSpeechSynthesizer` / `AVSpeechSynthesizer` |
| [ ] **Version Tracking** | `IVersionTracking` | ❌ | Needs `NSUserDefaults`-based version history tracking |
| [ ] **Vibration** | `IVibration` | ❌ | Not typically available on macOS (no haptic motor); consider `NSHapticFeedbackManager` on supported hardware |

---

## 16. Styling Infrastructure

| Feature | Status | Notes |
|---------|--------|-------|
| [~] **Border style mapping** | Partial | `BorderHandler` maps Stroke/StrokeShape/StrokeThickness via CoreGraphics |
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
| [ ] **ControlTemplate** | ❌ | Custom control templates defined in XAML — need to render the visual tree from the template |
| [ ] **ContentPresenter** | ❌ | Placeholder in ControlTemplate that inserts the actual content |
| [ ] **RadioButton ControlTemplate** | ❌ | RadioButton uses ControlTemplate by default for its visual — without this, RadioButton `Content` renders as `Grid.ToString()` instead of the visual content |
| [ ] **TemplatedView** | ❌ | Base class for controls with ControlTemplate support |

---

## Summary Statistics

| Category | Implemented | Total | Notes |
|----------|-------------|-------|-------|
| **Core Infrastructure** | 6 of 6 | 6 | All core abstractions in place including gesture integration |
| **Pages** | 4 of 5 | 5 | Missing: Shell |
| **Layouts** | 9 of 10 | 10 | Missing: Frame (dedicated handler) |
| **Basic Controls** | 11 of 14 | 14 | Missing: ImageButton; Label now fully implemented |
| **Collection Controls** | 1 of 7 | 7 | Only CollectionView (basic); missing 6 controls |
| **Input Controls** | 4 of 4 | 4 | All present; Entry/Editor improved with font/spacing |
| **Gesture Recognizers** | 3 of 5 | 5 | Tap, Pan, Pointer implemented; missing Swipe, Pinch |
| **Shapes** | 1 handler | 6 types | Single ShapeViewHandler covers all shape types |
| **Essentials** | 15 of 20 | 20 | Missing: Geolocation, Map, SemanticScreenReader, VersionTracking, Vibration |
| **Dialog Types** | 3 of 3 | 3 | All implemented via NSAlert |
| **Font Services** | 2 of 5 | 5 | IFontNamedSizeService + IFontManager implemented; missing IFontRegistrar, IEmbeddedFontLoader |
| **Animations** | 9 of 9 | 9 | ✅ Full: MacOSTicker + MAUI's cross-platform animation system handles all animation types |
| **MenuBar** | 4 of 4 | 4 | ✅ Full: MenuBarItem, MenuFlyoutItem, MenuFlyoutSeparator, MenuFlyoutSubItem |
| **FormattedText** | 9 of 9 | 9 | ✅ Full: All span properties mapped via NSAttributedString |
| **Base View Properties** | ~15 of 20+ | 20+ | Opacity, IsVisible, IsEnabled, Background, FlowDirection, AutomationId, Transforms, Clip, Shadow, MaxWidth/MaxHeight |