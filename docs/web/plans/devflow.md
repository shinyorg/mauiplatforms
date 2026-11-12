# Plan: DevFlow support for MAUI Web

**Goal:** The `maui-devflow` CLI (and AI agents using it) can inspect and drive a MAUI Web app running in a browser, the same way it does for iOS, Android, macOS and GTK apps. That covers the visual tree, element properties, tap, fill, scroll, screenshots, logs, and listing in the broker.

## Where things stand

- The macOS sample already uses DevFlow (`samples/Sample/Platforms/macOS/MauiProgram.cs`: `AddMauiDevFlowAgent()`, `AddMauiBlazorDevFlowTools()`).
- DevFlow is split into `Microsoft.Maui.DevFlow.Agent.Core` (platform-neutral: `MauiDevFlowAgentService`, `VisualTreeWalker`, `AgentHttpServer`, the essentials handlers) and one agent per platform. `Microsoft.Maui.DevFlow.Agent.Gtk` is the closest model for us: a `GtkAgentService`, a `GtkVisualTreeWalker`, screenshot capture, and `AddMauiDevFlowAgent()` and `StartDevFlowAgent()` extensions.
- The catch: `AgentHttpServer` listens on a TCP port inside the app. A WebAssembly app in a browser can't listen on sockets. It can only open outbound `fetch` and `WebSocket` connections.
- We already drive the app from outside: `tests/browser/smoke.mjs` uses the Chrome DevTools protocol to evaluate JS, dispatch real mouse and keyboard input, and read the DOM. So input and screenshots are already proven to work through CDP.

## Approach: reverse the connection

The CLI and broker expect to make HTTP calls to an agent. In the browser, we flip that around:

1. **In the app**, `Platform.Maui.DevFlow.Web` (new package) hosts `MauiDevFlowAgentService` from Agent.Core. Instead of `AgentHttpServer`, it opens an outbound `ClientWebSocket` to a relay. Each request that arrives over the socket is dispatched to the same handlers the HTTP server would call, and the response goes back over the socket.
2. **On the dev machine**, a relay listens on a normal local port, speaks the regular agent HTTP API to the CLI and broker, and forwards each request over the WebSocket to the page. It's simplest to put the relay inside the dev server, as middleware on the `dotnet run` and `dotnet watch` host, so there's nothing extra to start. A standalone `maui-devflow web-relay` command can come later.
3. The relay registers the app with the broker just as a native agent registers itself, so `maui-devflow list` shows it.

## Platform pieces the web agent provides

| Capability | Web implementation |
| --- | --- |
| Visual tree walk | `WebVisualTreeWalker`: MAUI tree from Agent.Core, plus each handler's `WebElement` (tag, id, classes, bounding box via `getBoundingClientRect`) |
| Element bounds and occlusion | Bounding boxes through `Dom` interop, `elementFromPoint` for hit testing and occlusion |
| Tap, fill, scroll | First choice: real input dispatched in-page (`PointerEvent` and `InputEvent`) on the element's DOM node. Optional: the relay uses CDP `Input.dispatch*` when the browser was started with a debug port, which gives trusted events |
| Screenshot | The relay calls CDP `Page.captureScreenshot` when it's available. Fallback: canvases capture themselves, and other views get bounds only, with no screenshot |
| Logs | Forward `ILogger` and `Console` output over the socket |
| App and device info | `WebAppInfo`, `Platform.Maui.Essentials.Web` |
| Blazor tools | Not applicable (no BlazorWebView on web) |

## Work items

1. **Spike: can Agent.Core run without its HTTP server?** Check whether `MauiDevFlowAgentService` lets us plug in a transport, or whether the request routing is internal to `AgentHttpServer`. If it's internal, propose an `IAgentTransport` (or a public `HandleRequestAsync(method, path, body)`) upstream in `dotnet/maui-labs`, and ask them about a first-party web agent. This decides whether the package is a thin adapter or carries its own request router.
2. **Check that Agent.Core runs in the browser.** Its dependencies (Fizzler, SkiaSharp, Microsoft.Extensions.*) must load in WebAssembly and survive trimming. A WebAssembly build with the agent referenced and `TrimMode=full` must start.
3. **`Platform.Maui.DevFlow.Web` package:** the WebSocket transport, the web tree walker, interaction and bounds, and `builder.AddMauiDevFlowAgent()`. Debug only by default, the way the native agents are used.
4. **Relay:** dev-server middleware that accepts the page's WebSocket, exposes the agent HTTP port, registers with the broker, and optionally attaches CDP for screenshots and trusted input.
5. **Port and URL discovery.** The page has to find the relay. Use the dev server's own origin (`/_devflow` on the same host) so there's no configuration, with an override in `MauiAppBuilder` options.
6. **Sample:** wire it into SampleWeb in Debug, behind `#if DEBUG`.
7. **Docs and skill.** Add a section to `docs/web/readme.md`, and tell the `maui-ai-debugging` skill maintainers about the web target: `dotnet run`, open the URL, then `maui-devflow` commands.
8. **Test:** a script like `smoke.mjs` that starts the app plus the relay and runs `maui-devflow tree`, `tap` and `screenshot` against it.

## Risks and open questions

- Agent.Core may assume a listening server or native APIs. The spike decides how much code we own versus contribute upstream.
- Untrusted synthetic events: some browser behaviour (focus, file pickers, fullscreen) only responds to trusted input. Use the CDP path when it's present and document what doesn't work without it.
- Security: the relay must bind to loopback only and must not exist in published apps. The web agent package must not be referenced from Release builds.
- One relay with several tabs open: each page connection registers as its own app instance with a tab id.

## Done when

With SampleWeb running under `dotnet run` in Debug, `maui-devflow list` shows it. `tree`, `tap`, `fill` and `screenshot` work against it with no setup beyond `AddMauiDevFlowAgent()`.
