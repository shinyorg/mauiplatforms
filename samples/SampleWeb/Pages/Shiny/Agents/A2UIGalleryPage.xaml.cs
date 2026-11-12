using Shiny.Controls.AgentUI.A2UI;

namespace Sample.Features.Agents;

public partial class A2UIGalleryPage : ContentPage
{
    readonly List<string> log = [];

    public A2UIGalleryPage()
    {
        InitializeComponent();
        Show(DemoAgentSession.DashboardJson);
    }

    void OnDashboard(object? sender, EventArgs e) => Show(DemoAgentSession.DashboardJson);

    void OnForm(object? sender, EventArgs e) => Show(DemoAgentSession.FormJson);

    void OnKitchenSink(object? sender, EventArgs e) => Show(KitchenSinkJson);

    // a fresh host per demo: surface ids are unique for a host's lifetime
    void Show(string json)
    {
        var host = new A2UISurfaceHost(new A2UICatalogRegistry().Add(A2UICatalog.CreateShiny()));
        host.Outbound += (_, message) => Log(message.Kind + " " + message.Body.ToJsonString());
        host.Error += (_, error) => Log($"error {error.Code}: {error.Message}");
        host.Apply(json);
        log.Clear();
        LogLabel.Text = "Tap a button or a grid row.";
        SurfaceView.Host = host;
    }

    void Log(string line)
    {
        log.Insert(0, line);
        if (log.Count > 6)
            log.RemoveAt(log.Count - 1);
        LogLabel.Text = string.Join("\n", log);
    }

    /// <summary>Every basic catalog component on one surface, plus one the catalog doesn't have.</summary>
    const string KitchenSinkJson = """
        [
          {
            "version": "1.0",
            "createSurface": {
              "surfaceId": "kitchen-sink",
              "catalogId": "https://a2ui.org/specification/v1_0/catalogs/basic/catalog.json",
              "dataModel": {
                "name": "Ada", "email": "", "age": "36", "secret": "", "bio": "",
                "agree": false, "plan": ["pro"], "extras": ["backup"], "volume": 40,
                "when": "2026-12-01", "at": "09:30", "meeting": "2026-12-01T14:00:00",
                "people": [
                  { "name": "Grace Hopper", "role": "Admiral" },
                  { "name": "Alan Turing", "role": "Mathematician" },
                  { "name": "Katherine Johnson", "role": "Mathematician" }
                ]
              },
              "components": [
                { "id": "root", "component": "Column", "children": ["title", "intro", "tabs", "divider", "unknown"] },
                { "id": "title", "component": "Text", "variant": "h2", "text": "Kitchen sink" },
                { "id": "intro", "component": "Text", "variant": "caption",
                  "text": { "@call": "formatString", "args": { "value": "Hello **${/name}** - every basic component, bound to one data model." } } },

                { "id": "tabs", "component": "Tabs", "tabs": [
                  { "title": "Display", "child": "displayTab" },
                  { "title": "Layout", "child": "layoutTab" },
                  { "title": "Inputs", "child": "inputsTab" }
                ] },

                { "id": "displayTab", "component": "Column", "children": ["icons", "image", "video", "audio", "blocked"] },
                { "id": "icons", "component": "Row", "align": "center", "children": ["i1", "i2", "i3", "i4", "i5", "i6"] },
                { "id": "i1", "component": "Icon", "name": "home" },
                { "id": "i2", "component": "Icon", "name": "star" },
                { "id": "i3", "component": "Icon", "name": "favorite" },
                { "id": "i4", "component": "Icon", "name": "settings" },
                { "id": "i5", "component": "Icon", "name": "warning" },
                { "id": "i6", "component": "Icon", "name": { "svgPath": "M12 2 L22 22 L2 22 Z" } },
                { "id": "image", "component": "Image", "url": "https://picsum.photos/seed/shiny/800/400", "fit": "cover", "variant": "header",
                  "description": "A random landscape photo" },
                { "id": "video", "component": "Video", "url": "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                  "posterUrl": "https://picsum.photos/seed/bunny/800/400" },
                { "id": "audio", "component": "AudioPlayer", "url": "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3", "description": "SoundHelix song 1" },
                { "id": "blocked", "component": "Image", "url": "javascript:alert(1)", "variant": "icon", "description": "Blocked by the URL policy" },

                { "id": "layoutTab", "component": "Column", "children": ["weighted", "card", "people", "modal"] },
                { "id": "weighted", "component": "Row", "children": ["w1", "w2"] },
                { "id": "w1", "component": "Text", "text": "weight 2", "weight": 2 },
                { "id": "w2", "component": "Text", "text": "weight 1", "weight": 1 },
                { "id": "card", "component": "Card", "child": "cardBody" },
                { "id": "cardBody", "component": "Column", "children": ["cardTitle", "cardText"] },
                { "id": "cardTitle", "component": "Text", "variant": "h4", "text": "A card" },
                { "id": "cardText", "component": "Text", "text": "Cards hold one child; use a Column for more." },
                { "id": "people", "component": "List", "children": { "componentId": "person", "path": "/people" } },
                { "id": "person", "component": "Row", "align": "center", "children": ["personIcon", "personName", "personRole"] },
                { "id": "personIcon", "component": "Icon", "name": "person" },
                { "id": "personName", "component": "Text", "text": { "@path": "name" }, "weight": 1 },
                { "id": "personRole", "component": "Text", "variant": "caption", "text": { "@path": "role" } },
                { "id": "modal", "component": "Modal", "trigger": "openModal", "content": "modalBody" },
                { "id": "openModalLabel", "component": "Text", "text": "Open a modal" },
                { "id": "openModal", "component": "Button", "child": "openModalLabel", "action": { "event": { "name": "modalOpened" } } },
                { "id": "modalBody", "component": "Column", "children": ["modalTitle", "modalText"] },
                { "id": "modalTitle", "component": "Text", "variant": "h3", "text": "Modal" },
                { "id": "modalText", "component": "Text", "text": { "@call": "formatString", "args": { "value": "Still bound: the name is ${/name}." } } },

                { "id": "inputsTab", "component": "Column", "children": ["name", "email", "age", "secret", "bio", "plan", "extras", "volume", "when", "at", "meeting", "agree", "buttons"] },
                { "id": "name", "component": "TextField", "label": "Name", "value": { "@path": "/name" }, "placeholder": "Your name",
                  "@checks": [ { "condition": { "@call": "length", "args": { "value": { "@path": "/name" }, "min": 2 } }, "message": "At least two letters" } ] },
                { "id": "email", "component": "TextField", "label": "Email", "value": { "@path": "/email" },
                  "@checks": [ { "condition": { "@call": "email", "args": { "value": { "@path": "/email" } } }, "message": "Enter a valid email" } ] },
                { "id": "age", "component": "TextField", "label": "Age", "variant": "number", "value": { "@path": "/age" },
                  "@checks": [ { "condition": { "@call": "numeric", "args": { "value": { "@path": "/age" }, "min": 0, "max": 130 } }, "message": "0 to 130" } ] },
                { "id": "secret", "component": "TextField", "label": "Password", "variant": "obscured", "value": { "@path": "/secret" } },
                { "id": "bio", "component": "TextField", "label": "Bio", "variant": "longText", "value": { "@path": "/bio" } },
                { "id": "plan", "component": "ChoicePicker", "label": "Plan", "value": { "@path": "/plan" },
                  "options": [ { "label": "Free", "value": "free" }, { "label": "Pro", "value": "pro" }, { "label": "Team", "value": "team" } ] },
                { "id": "extras", "component": "ChoicePicker", "label": "Extras", "variant": "multipleSelection", "displayStyle": "chips", "value": { "@path": "/extras" },
                  "options": [ { "label": "Backup", "value": "backup" }, { "label": "Support", "value": "support" }, { "label": "Analytics", "value": "analytics" } ] },
                { "id": "volume", "component": "Slider", "label": "Volume", "min": 0, "max": 100, "steps": 20, "value": { "@path": "/volume" } },
                { "id": "when", "component": "DateTimeInput", "label": "Date", "enableDate": true, "value": { "@path": "/when" }, "min": "2026-01-01" },
                { "id": "at", "component": "DateTimeInput", "label": "Time", "enableTime": true, "value": { "@path": "/at" } },
                { "id": "meeting", "component": "DateTimeInput", "label": "Meeting", "enableDate": true, "enableTime": true, "value": { "@path": "/meeting" } },
                { "id": "agree", "component": "CheckBox", "label": "I agree to the terms", "value": { "@path": "/agree" } },
                { "id": "buttons", "component": "Row", "children": ["save", "docs", "reset"] },
                { "id": "saveLabel", "component": "Text", "text": "Save" },
                { "id": "save", "component": "Button", "child": "saveLabel", "variant": "primary",
                  "@checks": [ { "condition": { "@path": "/agree" }, "message": "Agree to the terms first" } ],
                  "action": { "event": { "name": "save", "context": { "name": { "@path": "/name" }, "plan": { "@path": "/plan" }, "volume": { "@path": "/volume" } } } } },
                { "id": "docsIcon", "component": "Icon", "name": "share" },
                { "id": "docs", "component": "Button", "child": "docsIcon", "variant": "borderless",
                  "action": { "functionCall": { "@call": "openUrl", "args": { "url": "https://shinylib.net" } } } },
                { "id": "resetLabel", "component": "Text", "text": "Reset" },
                { "id": "reset", "component": "Button", "child": "resetLabel", "action": { "event": { "name": "reset" } } },

                { "id": "divider", "component": "Divider" },
                { "id": "unknown", "component": "Sparkline", "values": [1, 3, 2] }
              ]
            }
          }
        ]
        """;
}
