using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using AGUI.Abstractions;
using AGUI.Server;
using Microsoft.Extensions.AI;
using OpenAI;
using Shiny.Controls.AgentUI;
using Shiny.Controls.AgentUI.A2UI;

namespace Sample.Features.Agents;

/// <summary>
/// A real model as an AG-UI agent, with no server: the official <c>AGUI.Server</c> SDK turns an
/// <see cref="IChatClient"/>'s stream into AG-UI events in-process, so it runs anywhere .NET does -
/// including the browser (Blazor WebAssembly). The model gets a <c>render_ui</c> tool that draws A2UI
/// surfaces from the Shiny catalog, and the app's frontend tools (the built-in UI prompts) arrive with
/// each run as usual.
/// </summary>
/// <remarks>
/// The sample builds it with the visitor's own OpenAI-compatible key; the key only lives in the
/// <see cref="IChatClient"/> and goes nowhere but the endpoint it was given.
/// </remarks>
public sealed class ChatClientAgentSession : IAgentSession
{
    /// <summary>The backend tool that draws A2UI surfaces.</summary>
    public const string RenderUiTool = "render_ui";

    static readonly JsonSerializerOptions Json = CreateJson();

    readonly IChatClient chat;

    /// <param name="chat">The model. Function invocation is added here, for <c>render_ui</c>.</param>
    /// <param name="instructions">The system prompt; <see cref="DefaultInstructions"/> when omitted.</param>
    public ChatClientAgentSession(IChatClient chat, string? instructions = null)
    {
        this.chat = new FunctionInvokingChatClient(chat);
        Instructions = instructions ?? DefaultInstructions;
    }

    /// <summary>An OpenAI-compatible endpoint (OpenAI itself when <paramref name="endpoint"/> is null).</summary>
    public static ChatClientAgentSession ForOpenAI(string apiKey, string model, Uri? endpoint = null)
    {
        var options = new OpenAIClientOptions();
        if (endpoint is not null)
            options.Endpoint = endpoint;
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return new ChatClientAgentSession(client.GetChatClient(model).AsIChatClient());
    }

    /// <summary>Prompts that show off what a real model can do here.</summary>
    public static readonly IReadOnlyList<string> Suggestions =
    [
        "Show me a dashboard of this week's support tickets",
        "Help me plan a team offsite",
        "Compare three laptops in a table",
        "Ask me a few questions to pick a pet"
    ];

    /// <summary>The system prompt this session sends.</summary>
    public string Instructions { get; }

    public string ThreadId { get; } = "chat-" + Guid.NewGuid().ToString("N")[..8];

    public async IAsyncEnumerable<BaseEvent> RunAsync(RunAgentInput input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var streamOptions = new AGUIStreamOptions()
            // the render_ui call's operations become an a2ui-surface activity, which AgentView draws
            .MapResult(RenderUiTool, r => r.Result is JsonElement { ValueKind: JsonValueKind.Array } ops
                ? [Surface("ui-" + r.CallId, JsonNode.Parse(ops.GetRawText())!)]
                : []);

        var context = input.ToChatRequestContext(Json, streamOptions);
        context.Messages.Insert(0, new ChatMessage(ChatRole.System, Instructions));
        context.ChatOptions.Tools = [.. context.ChatOptions.Tools ?? [], RenderUi];

        var events = chat
            .GetStreamingResponseAsync(context.Messages, context.ChatOptions, cancellationToken)
            .AsAGUIEventStreamAsync(context, cancellationToken);

        await foreach (var e in events.WithCancellation(cancellationToken))
            yield return e;
    }

    static ActivitySnapshotEvent Surface(string messageId, JsonNode operations) => new()
    {
        MessageId = messageId,
        ActivityType = A2UIAgentBinding.DefaultActivityType,
        Replace = true,
        // parsed rather than serialized, so nothing goes through the reflection serializer (trimming)
        Content = JsonElement.Parse(new JsonObject { [A2UIAgentBinding.OperationsKey] = operations }.ToJsonString())
    };

    static readonly AIFunction RenderUi = new RenderUiFunction();

    // returns the operations it was given, so the result mapping above can carry them as a surface.
    // A subclass rather than AIFunctionFactory, so nothing is found by reflection (trimmed WebAssembly)
    sealed class RenderUiFunction : AIFunction
    {
        public override string Name => RenderUiTool;

        public override string Description =>
            "Shows interactive UI (cards, forms, tables, gauges, pickers) instead of describing it in text. " +
            "'operations' is a JSON array of A2UI v1.0 messages, applied in order. Buttons send the user's action back to you as the next message.";

        public override JsonElement JsonSchema { get; } = JsonElement.Parse("""
            {
              "type": "object",
              "properties": {
                "operations": { "type": "array", "description": "A2UI v1.0 messages, applied in order.", "items": { "type": "object" } }
              },
              "required": ["operations"]
            }
            """);

        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            object? result = arguments.TryGetValue("operations", out var ops) ? ops switch
            {
                JsonElement e => e,
                JsonNode n => JsonElement.Parse(n.ToJsonString()),
                string s => JsonElement.Parse(s),
                _ => null
            } : null;
            return ValueTask.FromResult<object?>(result ?? "render_ui needs an 'operations' array of A2UI messages.");
        }
    }

    static JsonSerializerOptions CreateJson()
    {
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                AGUIJsonUtilities.DefaultTypeInfoResolver,
                AIJsonUtilities.DefaultOptions.TypeInfoResolver!)
        };
        AGUIJsonUtilities.RegisterInterruptContentTypes(json);
        json.MakeReadOnly();
        return json;
    }

    /// <summary>
    /// What the model is told: how to answer, how to call <c>render_ui</c>, and a compact summary of
    /// the Shiny catalog (every component with its properties) read off the catalog itself.
    /// </summary>
    public static string DefaultInstructions { get; } = BuildInstructions();

    static string BuildInstructions()
    {
        var catalog = A2UICatalog.CreateShiny();
        var sb = new StringBuilder("""
            You are a helpful assistant inside an app that renders rich UI. Reply in concise markdown.

            When a table, dashboard, form, comparison or choice would help, call render_ui instead of writing it out.
            Each call creates a NEW surface. Its 'operations' array holds one message:
            {"version":"1.0","createSurface":{"surfaceId":"<unique id>","catalogId":"
            """);
        sb.Append(A2UICatalog.ShinyCatalogId);
        sb.Append("""
            ","dataModel":{...},"components":[...]}}

            Components are a FLAT list; exactly one has id "root". Each is {"id":"...","component":"<Name>", ...properties}.
            Containers name their children by id: "children":["a","b"] (Row, Column, List) or "child":"a" (Card, Button).
            A Button's label is a child Text component. Any value can be a literal, {"@path":"/pointer"} into dataModel,
            or {"@call":"<function>","args":{...}}. Bind inputs to dataModel paths so the user's edits are kept.
            A Button's action is {"event":{"name":"<action>","context":{"key":{"@path":"/x"}}}}; when the user presses it
            you get a message describing the action and its context - answer it (often with another surface).
            Keep text around a surface to one short sentence.

            The app may also give you UI tools (confirm, choose_one, choose_many, pick_date, pick_date_range, ask_text).
            Use them to ask the user something mid-task; their answer comes back as the tool result.

            Components:

            """);

        foreach (var (name, def) in catalog.Components.OrderBy(c => c.Key, StringComparer.Ordinal))
        {
            var schema = def.Schema;
            var props = def.Properties
                .Where(p => p is not ("id" or "component") && !p.StartsWith('@'))
                .Select(p => def.RequiredProperties.Contains(p) ? p + "*" : p);
            sb.Append("- ").Append(name).Append('(').Append(string.Join(", ", props)).Append(')');
            if (A2UIValues.AsString(schema?["description"]) is { } description)
                sb.Append(": ").Append(description);
            sb.Append('\n');
        }

        sb.Append("\n(* = required)\n\nFunctions for @call: ");
        sb.Append(string.Join(", ", catalog.Functions.Keys.Order(StringComparer.Ordinal)));
        sb.Append("\n\nExample operations for a small form:\n");
        sb.Append(JsonNode.Parse(DemoAgentSession.FormJson)!.ToJsonString());
        return sb.ToString();
    }
}
