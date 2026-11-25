# Plan: Shiny Office editors on MAUI Web

**Goal:** Add `DocumentEditorView` and `SlideEditorView` to SampleWeb's Office gallery group, next to the Spreadsheet and the two viewers that already work. Add `NotebookEditorView` too, since it has the same two dependencies. Don't add a Shiny web handler library: Skia (`Platform.Maui.Web.SkiaSharp`) and the ribbon already render, so the gaps are one controls fix and one generic backend hook.

## Where things stand

- `SpreadsheetView`, `DocumentView` and `SlideView` run on the web (`samples/SampleWeb/Pages/Shiny/Office`). The Spreadsheet already shows the full ribbon and the backstage.
- The editors are left out of the gallery (`samples/SampleWeb/Pages/Shiny/ShinyGallery.cs`). The comment there blames MAUI's `FilePickerFileType`. That diagnosis is still right, but it's incomplete: see blocker 2.
- Essentials already covers what the editors call: `WebFilePicker`, `WebMediaPicker`, `WebClipboard`, `WebLauncher` and `WebShare` are registered by `UseWebEssentials()`.

## Blocker 1: building an editor crashes (fix in controls)

**Cause.** `OfficeMenus` holds `static readonly FilePickerFileType ImageFileTypes = new(...)` (`controls/src/Shiny.Maui.Controls.Office/OfficeMenus.cs:288`). Touching `OfficeMenus` at all runs its type initializer, which creates a `FilePickerFileType`. MAUI's `net11.0` (reference-assembly) build fills `FilePickerFileType.Images`, `.Videos` and the others with `PlatformImageFileType()` and similar methods, which throw `NotImplementedInReferenceAssemblyException`. So `FilePickerFileType`'s own type initializer fails, and the error surfaces as a `TypeInitializationException`.

- On CoreCLR (checked with a `net11.0` console app on MAUI 11.0.10), `new FilePickerFileType(dict)` works, because the type is `beforefieldinit`. Only reading the built-in values (`FilePickerFileType.Videos`) throws.
- In the browser (Mono/WASM), `docs/web/readme.md` records that creating *any* `FilePickerFileType` throws. Re-check this in the browser while doing the fix. The fix below covers both cases.

**Where it fires:**

| Call site | When | Effect on web |
|---|---|---|
| `OfficeRibbonItems.cs:158` reads `OfficeMenus.Shapes` | While `DocumentEditorView` / `SlideEditorView` build their ribbon | The editor can't be constructed |
| `SpreadsheetToolbar.cs:1236` / `:1451` (`PickHighlightAsync`, `PickImageAsync`) | Highlight and Picture buttons | Probably broken on web **today**. Verify. |
| `MauiOfficeDocumentStore.FileTypesFor` (`MauiOfficeDocumentStore.cs:81`) | Backstage ▸ Open | Open fails |
| `SlideEditorView.PowerPoint.cs:1735` (`FilePickerFileType.Videos`) | Insert video | Fails |

**Fix (Shiny.Maui.Controls.Office):**

1. Make `OfficeMenus.ImageFileTypes` lazy (a method or a `Lazy<>`), so the `OfficeMenus` type initializer no longer creates a `FilePickerFileType`.
2. Wherever `PickOptions.FileTypes` is set, pass `null` when `OperatingSystem.IsBrowser()`. `WebFilePicker.Accept` treats a missing filter as "any file". `PickImageAsync` already re-checks the picked file's type through `ImageContentTypes.Resolve`, so a missing filter is safe. Do the same in `MauiOfficeDocumentStore.FileTypesFor` and for the slide editor's video pick. For the video pick, `MediaPicker.PickVideoAsync` is an option, since `WebMediaPicker` maps it to `accept="video/*"`.
3. Optional: an accept-string seam (`.docx`, `image/png,…`) that the web head could use to keep a filter. That needs either an extension point in controls or reflection over `WebFilePicker.PickAsync(string)`. Not needed for the first pass.

Then publish a new `ShinyControlsVersion` (or a local `3.0.0-local.*` package) for SampleWeb to pick up.

## Blocker 2: physical keys never reach the editors (generic hook in Platform.Maui.Web)

**Cause.** The editors take text through a hidden `OfficeTextInput : Entry` and clear it after every character. MAUI has no portable key-down event, so each editor exposes `HandleKey(EditorKey, shift, control)` and `HandleShortcut(...)` (`DocumentEditorView.cs`, `SlideEditorView.cs`, `NotebookEditorView.cs`) for a host to call. `SpreadsheetView.HandleKey(string, SheetKeyModifiers)` uses browser key names. Hosts wire these today as follows:

- Android: `HiddenInputKeys` (Backspace / Delete / arrows / Home / End / Tab from `EditText.KeyPress`).
- Apple: `OfficeTextInputHandler` (`deleteBackward` → `EmptyBackspace`).
- Web: nothing. `EntryHandler.ConnectHandler` (`src/Platform.Maui.Web/Handlers/TextInputHandler.cs:172`) gets every keydown and only acts on Enter (raises `Completed`).

**What works on web without the hook:** typing (`TextChanged`), Enter (`Completed` → new paragraph), pointer selection, and every ribbon command.
**What doesn't:** Backspace on the empty hidden entry (it raises nothing, the same bug iOS had), Delete, arrow keys, Home/End, Tab, and the Ctrl/⌘ shortcuts (B/I/U, Z/Y, A …). The Spreadsheet has no keyboard navigation for the same reason.

**Fix (Platform.Maui.Web, generic, not Office-specific):**

1. Add a keydown listener that sends what the current `listenKey` drops: `key`, plus `shiftKey`, `ctrlKey`, `metaKey`, `altKey`, and the input's current value or an `isEmpty` flag. `listenKey` in `wwwroot` only sends `e.key` today (`WebElement.OnKey` → `Action<string>`).
2. Let the .NET callback return `bool handled` and call `preventDefault()` when it's true. JSImport callbacks run synchronously on the single WASM thread, so the return value is available inside the event. Without this, Backspace or ⌘B would also run the browser's default action.
3. Expose it the same way as `LabelHandler.SpanCreated`: a public static hook on `EntryHandler`, for example `Func<EntryHandler, IEntry, WebKeyEvent, bool>? KeyDown`. The hook belongs to the backend and any app can use it, so no Shiny handler library is needed.
4. Keep the existing Enter → `Completed` behaviour. If the hook handles Enter, skip `Completed`, so an Enter isn't acted on twice.

**Wiring (SampleWeb, in the Office pages or one helper next to `OfficeWebFonts`):** when the focused entry is an `OfficeTextInput`, find its editor view and call the right method:

- Map `Backspace` (only when the entry is empty), `Delete`, `ArrowLeft/Right/Up/Down`, `Home`, `End` and `Tab` → `HandleKey(EditorKey.*, shift, ctrl || meta)`.
- Map Ctrl/⌘ + key → `HandleShortcut(key, command: true, shift, alt)`.
- For the Spreadsheet, send the browser key name and modifiers straight to `SpreadsheetView.HandleKey`.

Use the input's own empty state for the Backspace / Left / Right checks, as `HiddenInputKeys` does on Android. That way an IME composition keeps its own Backspace.

## Also out of scope here

- **ImageEditor** stays blocked for a separate reason: MAUI's `net11.0` `PlatformImage` reads a PNG's size from its signature bytes, so the image never shows (see `readme.md`).
- **Spell checking**: `PlatformSpellChecker` has no web implementation. Check that the editors degrade quietly, with no squiggles and no crash.

## Work items

1. **Controls:** make `ImageFileTypes` lazy; skip `FileTypes` in the browser at the three pick sites; verify the Spreadsheet's Highlight/Picture buttons in the browser before and after; publish the package.
2. **Backend:** the keydown hook with modifiers and `handled` → `preventDefault`. Add unit coverage in `tests/Platform.Maui.Web.Tests` where it can run on the host.
3. **Sample:** `DocumentEditorPage`, `SlideEditorPage` and `NotebookEditorPage` under `Pages/Shiny/Office`, ported from `controls/samples/Sample/Features/Office`, each wrapped in `OfficeWebFonts.With(...)`. Add the key wiring, including the Spreadsheet. Bump `ShinyControlsVersion`.
4. **Gallery:** add the three editors to the Office group in `ShinyGallery.cs`, and replace its stale comment with a one-liner about ImageEditor.
5. **Playwright:** extend `tests/playwright/specs/shiny-chat-office.spec.ts`: open each editor; type, Backspace and arrow-key into the document; check the change lands (through the editor's own state, or a screenshot diff); Picture opens a file chooser.
6. **Docs:** update the Office paragraph in `readme.md` (it currently says the editors don't work) and document the keydown hook in `docs/web/readme.md`.

## Risks and open questions

- The browser behaviour of `FilePickerFileType` isn't re-verified (see blocker 1). The fix doesn't depend on it, but the gallery comment and the docs should describe the real behaviour.
- `preventDefault` on ⌘-shortcuts: some (⌘W, ⌘T, ⌘N) can't be cancelled by the page. Leave those to the browser.
- Mobile browsers' soft keyboards often send `key: "Unidentified"` (keyCode 229) for Backspace. The empty-entry check may need a `beforeinput` (`inputType: "deleteContentBackward"`) fallback.
- Ribbon hit-testing and popups on narrow viewports haven't been tried with the larger editor ribbons.

## Order

1 → publish → 3 and 4 (editors render, typing works). 2 can be done alongside 1, and the key wiring lands with 3. Then 5 and 6.
