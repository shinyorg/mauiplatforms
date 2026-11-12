using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using AGUI.Abstractions;
using Shiny.Controls.AgentUI;

namespace Sample.Features.Agents;

/// <summary>
/// A scripted AG-UI agent, so the agent pages work with no API key and no server. It answers by
/// keyword and emits real AG-UI events - streamed markdown, reasoning, backend tool calls, A2UI
/// surfaces (a Shiny dashboard, a basic form and a media card), a frontend tool, interrupts of every
/// shape (approval, one choice, several choices, free text), shared state, a sub-agent and a run error - exactly as a live endpoint would. Shared by the MAUI and
/// Blazor samples (and driven end to end by the engine's tests).
/// </summary>
public sealed class DemoAgentSession : IAgentSession
{
    /// <summary>Prompts that show each scenario, for the sample's suggestion chips.</summary>
    public static readonly IReadOnlyList<string> Suggestions =
    [
        "Show me the October sales dashboard",
        "Book a trip",
        "Sign me up for the newsletter",
        "Delete my old drafts",
        "Order lunch",
        "Pick pizza toppings",
        "Rename my project",
        "Show me some media",
        "Research AG-UI for me",
        "Track my reading list",
        "What can you do?",
        "Fail on purpose"
    ];

    /// <summary>The frontend tool the "book a trip" scenario calls. The app registers it.</summary>
    public const string PickDateRangeTool = "pick_date_range";

    int ids;
    // what each pending interrupt asked, so a resume is answered in kind
    readonly Dictionary<string, string> interrupts = new(StringComparer.Ordinal);

    /// <summary>The delay between streamed tokens. Zero for tests.</summary>
    public TimeSpan TokenDelay { get; init; } = TimeSpan.FromMilliseconds(18);

    public string ThreadId { get; } = "demo-" + Guid.NewGuid().ToString("N")[..8];

    public async IAsyncEnumerable<BaseEvent> RunAsync(RunAgentInput input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var runId = input.RunId ?? Id("run");
        yield return Event(new { type = "RUN_STARTED", threadId = ThreadId, runId });

        foreach (var json in Script(input))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (json is null)
            {
                // a pause between tokens, so the stream reads like a live model
                if (TokenDelay > TimeSpan.Zero)
                    await Task.Delay(TokenDelay, cancellationToken);
                continue;
            }
            if (TokenDelay == TimeSpan.Zero)
                await Task.Yield();
            yield return Event(json);
        }
    }

    // null entries are pacing delays
    IEnumerable<object?> Script(RunAgentInput input)
    {
        var forwarded = input.ForwardedProperties is { } fp ? JsonNode.Parse(fp.GetRawText()) as JsonObject : null;

        // 1. an A2UI action from a surface the agent drew
        if (forwarded?["a2uiAction"]?["userAction"] is JsonObject action)
        {
            foreach (var e in OnAction(action)) yield return e;
            yield break;
        }

        // 2. resuming an interrupt
        if (input.Resume is { Count: > 0 } resume)
        {
            foreach (var e in Text(OnResume(resume))) yield return e;
            yield return Finished(input);
            yield break;
        }

        // 3. a frontend tool's result came back
        if (input.Messages?.LastOrDefault() is AGUIToolMessage tool)
        {
            if (tool.Error is { } error)
            {
                foreach (var e in Text($"No dates, no trip - the picker said: *{error}*. Ask again when you're ready.")) yield return e;
            }
            else
            {
                var range = JsonNode.Parse(tool.Content.ToString() ?? "{}");
                foreach (var e in Text($"""
                    Booked! Here's the plan:

                    | | |
                    |---|---|
                    | **Out** | {range?["start"]} |
                    | **Back** | {range?["end"]} |

                    I'll hold the fare for 24 hours.
                    """))
                    yield return e;
            }
            yield return Finished(input);
            yield break;
        }

        var prompt = (input.Messages?.OfType<AGUIUserMessage>().LastOrDefault()?.Content.ToString() ?? "").ToLowerInvariant();

        if (Has(prompt, "dashboard", "sales", "report"))
        {
            foreach (var e in Dashboard()) yield return e;
        }
        else if (Has(prompt, "trip", "book", "date", "holiday", "vacation"))
        {
            foreach (var e in Text("Sure - pick your dates and I'll find flights.")) yield return e;
            var call = Id("call");
            yield return new { type = "TOOL_CALL_START", toolCallId = call, toolCallName = PickDateRangeTool };
            yield return new { type = "TOOL_CALL_ARGS", toolCallId = call, delta = """{"title":"When are you travelling?","""};
            yield return null;
            yield return new { type = "TOOL_CALL_ARGS", toolCallId = call, delta = $$"""
                "min":"{{DateTime.Today:yyyy-MM-dd}}"}
                """.Trim() };
            yield return new { type = "TOOL_CALL_END", toolCallId = call };
            // the run ends here; the app runs the tool and AgentRunner starts the follow-up run
        }
        else if (Has(prompt, "newsletter", "sign", "form", "subscribe"))
        {
            foreach (var e in Form()) yield return e;
        }
        else if (Has(prompt, "delete", "remove", "approve"))
        {
            foreach (var e in Text("That deletes 3 drafts permanently, so I need your OK first.")) yield return e;
            yield return Interrupt(input, "approval", "Delete 3 drafts from before October?",
                responseSchema: new { type = "object", properties = new { approved = new { type = "boolean" } } });
            yield break;
        }
        else if (Has(prompt, "lunch", "order food"))
        {
            foreach (var e in Text("The canteen closes orders at 11:30. What would you like?")) yield return e;
            yield return Interrupt(input, "lunch", "Pick today's lunch",
                metadata: new { choices = LunchChoices.Select(c => new { id = c.Id, label = c.Label }).ToArray() });
            yield break;
        }
        else if (Has(prompt, "topping", "pizza"))
        {
            foreach (var e in Text("Build your pizza - pick as many as you like.")) yield return e;
            yield return Interrupt(input, "toppings", "Which toppings?",
                metadata: new { allowMultiple = true, choices = ToppingChoices.Select(c => new { id = c.Id, label = c.Label }).ToArray() });
            yield break;
        }
        else if (Has(prompt, "rename", "name my", "call it"))
        {
            foreach (var e in Text("Sure. I'll update the repo, the docs and the package ids.")) yield return e;
            yield return Interrupt(input, "rename", "What should the project be called?",
                responseSchema: new { type = "object", properties = new { text = new { type = "string" } }, required = new[] { "text" } });
            yield break;
        }
        else if (Has(prompt, "media", "video", "audio", "music", "link"))
        {
            foreach (var e in Media()) yield return e;
        }
        else if (Has(prompt, "research", "investigate"))
        {
            foreach (var e in Research()) yield return e;
        }
        else if (Has(prompt, "reading", "track", "list", "state"))
        {
            foreach (var e in State(input)) yield return e;
        }
        else if (Has(prompt, "fail", "error", "crash"))
        {
            foreach (var e in Text("Trying something that won't work...")) yield return e;
            yield return new { type = "RUN_ERROR", message = "The demo agent failed on purpose.", code = "DEMO_FAILURE" };
            yield break;
        }
        else
        {
            foreach (var e in Reasoning("The user wants to know what I can do. I'll list the scenarios this demo scripts, with a prompt for each.")) yield return e;
            foreach (var e in Text("""
                I'm a **scripted demo agent** - no model, no API key - but every event I send is real AG-UI. Try:

                - **"Show me the sales dashboard"** - a backend tool call, then an A2UI surface built from Shiny controls (DataGrid, Gauge, DateRangePicker, ChipGroup)
                - **"Book a trip"** - a *frontend tool*: I ask, a date range picker answers
                - **"Sign me up for the newsletter"** - an A2UI form whose button sends an action back to me
                - **"Delete my old drafts"** - an approval *interrupt*
                - **"Order lunch"**, **"Pick pizza toppings"**, **"Rename my project"** - interrupts that ask for one choice, several, or free text
                - **"Show me some media"** - an A2UI card with video, audio and a link that opens in the browser
                - **"Research AG-UI"** - a sub-agent working inside my turn
                - **"Track my reading list"** - shared state the page can bind to
                - **"Fail on purpose"** - a run error with Retry
                """)) yield return e;
        }

        yield return Finished(input);
    }

    static readonly (string Id, string Label)[] LunchChoices = [("soup", "Tomato soup"), ("salad", "Greek salad"), ("wrap", "Falafel wrap")];
    static readonly (string Id, string Label)[] ToppingChoices = [("cheese", "Extra cheese"), ("mushroom", "Mushroom"), ("olive", "Olive"), ("pepper", "Pepper"), ("basil", "Basil")];

    object Interrupt(RunAgentInput input, string kind, string message, object? metadata = null, object? responseSchema = null)
    {
        var id = Id("int");
        interrupts[id] = kind;
        return new
        {
            type = "RUN_FINISHED",
            threadId = ThreadId,
            runId = input.RunId,
            outcome = new
            {
                type = "interrupt",
                interrupts = new[] { new { id, reason = kind, message, metadata, responseSchema } }
            }
        };
    }

    string OnResume(IEnumerable<AGUIResume> resume)
    {
        var answers = new List<string>();
        foreach (var r in resume)
        {
            var kind = interrupts.GetValueOrDefault(r.InterruptId ?? "") ?? "approval";
            interrupts.Remove(r.InterruptId ?? "");
            var payload = r.Payload is { } p ? JsonNode.Parse(p.GetRawText()) : null;
            var resolved = r.Status == ResumeStatus.Resolved;
            var picked = (payload?["choiceIds"] as JsonArray)?.Select(x => x?.GetValue<string>()).OfType<string>().ToList() ?? [];

            answers.Add(kind switch
            {
                _ when !resolved && kind == "approval" => "No problem, I left your drafts alone.",
                _ when !resolved => "Okay, never mind.",
                "approval" => "Done - I deleted **3 drafts** from before October. They're in the bin for 30 days if you change your mind.",
                "lunch" when picked.Count > 0 => $"Ordered: **{Label(LunchChoices, picked[0])}**. It'll be at reception at 12:15.",
                "toppings" when picked.Count > 0 => $"One pizza with **{string.Join(", ", picked.Select(id => Label(ToppingChoices, id)))}** coming up.",
                "toppings" => "A plain margherita it is.",
                "rename" when payload?["text"]?.GetValue<string>() is { Length: > 0 } name => $"Renamed to **{name}**. I opened a PR with the changes.",
                _ => "I didn't get an answer, so I'll leave it."
            });
        }
        return string.Join("\n\n", answers);
    }

    static string Label((string Id, string Label)[] choices, string id) => choices.FirstOrDefault(c => c.Id == id).Label ?? id;

    IEnumerable<object?> Media()
    {
        var messages = JsonNode.Parse(MediaJson)!;
        yield return new
        {
            type = "ACTIVITY_SNAPSHOT",
            messageId = Id("surface"),
            activityType = "a2ui-surface",
            content = new JsonObject { ["a2ui_operations"] = messages },
            replace = true
        };
        foreach (var e in Text("A short film, a song, and the A2UI spec if you want to read up.")) yield return e;
    }

    IEnumerable<object?> OnAction(JsonObject action)
    {
        var name = action["name"]?.GetValue<string>();
        var context = action["context"] as JsonObject;
        switch (name)
        {
            case "subscribe":
                foreach (var e in Text($"Thanks **{context?["name"]}** - you're on the list. The first issue goes to `{context?["email"]}` on Friday.")) yield return e;
                break;
            case "openOrder":
                foreach (var e in Text($"Order **{context?["orderId"]}** shipped on time; the courier has it now.")) yield return e;
                break;
            case "refresh":
                foreach (var e in ToolCall("query_sales", $$"""{"start":"{{context?["start"]}}","end":"{{context?["end"]}}"}""", """{"rows":3}""")) yield return e;
                foreach (var e in Text($"Refreshed for **{context?["start"]} to {context?["end"]}** in {(context?["regions"] as JsonArray)?.Count ?? 0} region(s). Same three orders - October was quiet.")) yield return e;
                break;
            default:
                foreach (var e in Text($"I got the `{name}` action.")) yield return e;
                break;
        }
        yield return new { type = "RUN_FINISHED", threadId = ThreadId };
    }

    IEnumerable<object?> Dashboard()
    {
        foreach (var e in Reasoning("I'll pull October's orders, then lay them out with a gauge for the target and a grid for the orders.")) yield return e;
        foreach (var e in ToolCall("query_sales", """{"month":"2026-10"}""", """{"orders":3,"total":6640.75}""")) yield return e;

        var messages = JsonNode.Parse(DashboardJson)!;
        yield return new
        {
            type = "ACTIVITY_SNAPSHOT",
            messageId = Id("surface"),
            activityType = "a2ui-surface",
            content = new JsonObject { ["a2ui_operations"] = messages },
            replace = true
        };
        foreach (var e in Text("Here's October. Tap a row for the order, or change the period and **Refresh**.")) yield return e;
    }

    IEnumerable<object?> Form()
    {
        var messages = JsonNode.Parse(FormJson)!;
        yield return new
        {
            type = "ACTIVITY_SNAPSHOT",
            messageId = Id("surface"),
            activityType = "a2ui-surface",
            content = new JsonObject { ["a2ui_operations"] = messages },
            replace = true
        };
        foreach (var e in Text("Fill this in and I'll add you.")) yield return e;
    }

    IEnumerable<object?> Research()
    {
        var call = Id("call");
        var sub = Id("sub");
        yield return new { type = "TOOL_CALL_START", toolCallId = call, toolCallName = "delegate_research" };
        yield return new { type = "TOOL_CALL_ARGS", toolCallId = call, delta = """{"topic":"AG-UI"}""" };
        yield return new { type = "TOOL_CALL_END", toolCallId = call };
        yield return new { type = "SUBAGENT_STARTED", subagentRunId = sub, name = "researcher", parentToolCallId = call };
        yield return new { type = "STEP_STARTED", stepName = "search", subagentRunId = sub };
        foreach (var e in Text("Reading docs.ag-ui.com... AG-UI is an open, event-based protocol between agents and user-facing apps.", sub)) yield return e;
        yield return new { type = "STEP_FINISHED", stepName = "search", subagentRunId = sub };
        yield return new { type = "SUBAGENT_FINISHED", subagentRunId = sub, outcome = new { type = "success" } };
        yield return new { type = "TOOL_CALL_RESULT", messageId = Id("tool"), toolCallId = call, content = """{"summary":"AG-UI: ~16 event types over SSE"}""" };
        foreach (var e in Text("""
            My researcher says **AG-UI** standardises the *agent ↔ app* hop:

            1. The app POSTs a run (`RunAgentInput`).
            2. The agent streams events back - text, tool calls, state, activity.
            3. Frontend tools and interrupts put a human in the loop.
            """)) yield return e;
    }

    IEnumerable<object?> State(RunAgentInput input)
    {
        var existing = input.State is { ValueKind: JsonValueKind.Object } s ? JsonNode.Parse(s.GetRawText()) : null;
        if (existing?["books"] is not JsonArray)
        {
            yield return new { type = "STATE_SNAPSHOT", snapshot = JsonNode.Parse("""{"books":[{"title":"Dune","done":true},{"title":"Piranesi","done":false}],"goal":12}""") };
            foreach (var e in Text("I started your reading list with two books and a goal of **12** this year. Ask again and I'll add another.")) yield return e;
        }
        else
        {
            yield return new { type = "STATE_DELTA", delta = JsonNode.Parse("""[{"op":"add","path":"/books/-","value":{"title":"The Left Hand of Darkness","done":false}}]""") };
            foreach (var e in Text("Added *The Left Hand of Darkness*. The page's panel is bound to the same state, so it updated too.")) yield return e;
        }
    }

    IEnumerable<object?> ToolCall(string name, string args, string result)
    {
        var call = Id("call");
        yield return new { type = "TOOL_CALL_START", toolCallId = call, toolCallName = name };
        foreach (var chunk in Chunk(args, 12))
        {
            yield return new { type = "TOOL_CALL_ARGS", toolCallId = call, delta = chunk };
            yield return null;
        }
        yield return new { type = "TOOL_CALL_END", toolCallId = call };
        yield return null;
        yield return null;
        yield return new { type = "TOOL_CALL_RESULT", messageId = Id("tool"), toolCallId = call, content = result };
    }

    IEnumerable<object?> Reasoning(string text)
    {
        var id = Id("rsn");
        yield return new { type = "REASONING_START", messageId = id };
        yield return new { type = "REASONING_MESSAGE_START", messageId = id, role = "reasoning" };
        foreach (var chunk in Chunk(text, 6))
        {
            yield return new { type = "REASONING_MESSAGE_CONTENT", messageId = id, delta = chunk };
            yield return null;
        }
        yield return new { type = "REASONING_MESSAGE_END", messageId = id };
        yield return new { type = "REASONING_END", messageId = id };
    }

    IEnumerable<object?> Text(string text, string? subagentRunId = null)
    {
        var id = Id("msg");
        yield return new { type = "TEXT_MESSAGE_START", messageId = id, role = "assistant", subagentRunId };
        foreach (var chunk in Chunk(text, 4))
        {
            yield return new { type = "TEXT_MESSAGE_CONTENT", messageId = id, delta = chunk, subagentRunId };
            yield return null;
        }
        yield return new { type = "TEXT_MESSAGE_END", messageId = id, subagentRunId };
    }

    object Finished(RunAgentInput input) => new { type = "RUN_FINISHED", threadId = ThreadId, runId = input.RunId };

    // splits on word boundaries, a few words per delta
    static IEnumerable<string> Chunk(string text, int words)
    {
        var start = 0;
        var count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ' && ++count == words)
            {
                yield return text[start..(i + 1)];
                start = i + 1;
                count = 0;
            }
        }
        if (start < text.Length)
            yield return text[start..];
    }

    static bool Has(string prompt, params string[] words) => words.Any(prompt.Contains);

    string Id(string prefix) => $"{prefix}-{++ids}";

    static BaseEvent Event(object e) => e as BaseEvent ?? JsonSerializer.Deserialize(
        JsonSerializer.SerializeToNode(e, e.GetType(), JsonOptions)!.ToJsonString(),
        AGUIJsonSerializerContext.Default.BaseEvent)!;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>The A2UI dashboard the agent draws: basic layout plus Shiny catalog components.</summary>
    public const string DashboardJson = """
        [
          {
            "version": "1.0",
            "createSurface": {
              "surfaceId": "dashboard",
              "catalogId": "https://shinylib.net/a2ui/shiny.controls/v1.json",
              "sendDataModel": true,
              "dataModel": {
                "range": { "start": "2026-10-01", "end": "2026-10-31" },
                "regions": ["eu", "na"],
                "target": { "percent": 72 },
                "orders": [
                  { "id": "A-1001", "customer": "Northwind", "total": 1250.5, "margin": 0.31, "date": "2026-10-03", "status": "Shipped" },
                  { "id": "A-1002", "customer": "Contoso", "total": 980, "margin": 0.22, "date": "2026-10-09", "status": "Pending" },
                  { "id": "A-1003", "customer": "Fabrikam", "total": 4410.25, "margin": 0.4, "date": "2026-10-17", "status": "Shipped" }
                ]
              },
              "components": [
                { "id": "root", "component": "Column", "children": ["header", "filters", "kpis", "grid", "notes", "refresh"] },
                { "id": "header", "component": "Row", "align": "center", "children": ["title", "status"] },
                { "id": "title", "component": "Text", "variant": "h3", "text": "October sales", "weight": 1 },
                { "id": "status", "component": "Pill", "text": "Live", "tone": "success" },
                { "id": "filters", "component": "Column", "children": ["range", "regions"] },
                { "id": "range", "component": "DateRangePicker", "label": "Period", "start": { "@path": "/range/start" }, "end": { "@path": "/range/end" } },
                { "id": "regions", "component": "ChipGroup", "label": "Regions", "variant": "multiple", "value": { "@path": "/regions" },
                  "options": [ { "label": "Europe", "value": "eu" }, { "label": "North America", "value": "na" }, { "label": "Asia", "value": "apac" } ] },
                { "id": "kpis", "component": "Row", "align": "center", "children": ["targetGauge", "leader"] },
                { "id": "targetGauge", "component": "Gauge", "label": "Target", "units": "%", "min": 0, "max": 100, "value": { "@path": "/target/percent" },
                  "ranges": [ { "from": 0, "to": 50, "tone": "danger" }, { "from": 50, "to": 80, "tone": "warning" }, { "from": 80, "to": 100, "tone": "good" } ] },
                { "id": "leader", "component": "Text", "weight": 1,
                  "text": { "@call": "formatString", "args": { "value": "${/orders/2/customer} leads the month" } } },
                { "id": "grid", "component": "DataGrid", "title": "Orders", "rows": { "@path": "/orders" },
                  "columns": [
                    { "key": "id", "header": "Order" },
                    { "key": "customer", "header": "Customer" },
                    { "key": "date", "header": "Date", "format": "date" },
                    { "key": "total", "header": "Total", "format": "currency", "currency": "EUR" },
                    { "key": "margin", "header": "Margin", "format": "percent" },
                    { "key": "status", "header": "Status" }
                  ],
                  "rowAction": { "event": { "name": "openOrder", "context": { "orderId": { "@path": "id" } } } } },
                { "id": "notes", "component": "Expander", "header": "How this is calculated", "child": "notesText" },
                { "id": "notesText", "component": "Markdown", "text": "Totals are **net of tax**. Margin is `(total - cost) / total`." },
                { "id": "refreshLabel", "component": "Text", "text": "Refresh" },
                { "id": "refresh", "component": "Button", "child": "refreshLabel", "variant": "primary",
                  "action": { "event": { "name": "refresh", "context": { "start": { "@path": "/range/start" }, "end": { "@path": "/range/end" }, "regions": { "@path": "/regions" } } } } }
              ]
            }
          }
        ]
        """;

    /// <summary>A basic-catalog form with checks; its button sends a <c>subscribe</c> action.</summary>
    public const string FormJson = """
        [
          {
            "version": "1.0",
            "createSurface": {
              "surfaceId": "newsletter",
              "catalogId": "https://a2ui.org/specification/v1_0/catalogs/basic/catalog.json",
              "dataModel": { "name": "", "email": "", "topics": ["dotnet"], "weekly": true },
              "components": [
                { "id": "root", "component": "Card", "child": "body" },
                { "id": "body", "component": "Column", "children": ["heading", "name", "email", "topics", "weekly", "submit"] },
                { "id": "heading", "component": "Text", "variant": "h4", "text": "Shiny newsletter" },
                { "id": "name", "component": "TextField", "label": "Name", "value": { "@path": "/name" },
                  "@checks": [ { "condition": { "@call": "required", "args": { "value": { "@path": "/name" } } }, "message": "Tell me your name" } ] },
                { "id": "email", "component": "TextField", "label": "Email", "value": { "@path": "/email" },
                  "@checks": [ { "condition": { "@call": "email", "args": { "value": { "@path": "/email" } } }, "message": "That doesn't look like an email address" } ] },
                { "id": "topics", "component": "ChoicePicker", "label": "Topics", "variant": "multipleSelection", "displayStyle": "chips", "value": { "@path": "/topics" },
                  "options": [ { "label": ".NET MAUI", "value": "dotnet" }, { "label": "Blazor", "value": "blazor" }, { "label": "AI agents", "value": "ai" } ] },
                { "id": "weekly", "component": "CheckBox", "label": "Weekly digest", "value": { "@path": "/weekly" } },
                { "id": "submitLabel", "component": "Text", "text": "Subscribe" },
                { "id": "submit", "component": "Button", "child": "submitLabel", "variant": "primary",
                  "action": { "event": { "name": "subscribe", "context": { "name": { "@path": "/name" }, "email": { "@path": "/email" }, "topics": { "@path": "/topics" } } } } }
              ]
            }
          }
        ]
        """;

    /// <summary>A basic-catalog card with Video, AudioPlayer and a Button whose action opens a URL (<c>openUrl</c>).</summary>
    public const string MediaJson = """
        [
          {
            "version": "1.0",
            "createSurface": {
              "surfaceId": "media",
              "catalogId": "https://a2ui.org/specification/v1_0/catalogs/basic/catalog.json",
              "dataModel": { "spec": "https://a2ui.org" },
              "components": [
                { "id": "root", "component": "Card", "child": "body" },
                { "id": "body", "component": "Column", "children": ["heading", "video", "audio", "divider", "open"] },
                { "id": "heading", "component": "Text", "variant": "h4", "text": "Media" },
                { "id": "video", "component": "Video", "url": "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4" },
                { "id": "audio", "component": "AudioPlayer", "url": "https://interactive-examples.mdn.mozilla.net/media/cc0-audio/t-rex-roar.mp3", "description": "T-Rex roar (CC0)" },
                { "id": "divider", "component": "Divider" },
                { "id": "openLabel", "component": "Text", "text": "Read the A2UI spec" },
                { "id": "open", "component": "Button", "child": "openLabel",
                  "action": { "functionCall": { "@call": "openUrl", "args": { "url": { "@path": "/spec" } } } } }
              ]
            }
          }
        ]
        """;
}
