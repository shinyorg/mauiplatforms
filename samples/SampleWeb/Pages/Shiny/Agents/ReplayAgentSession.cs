using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using AGUI.Abstractions;
using Shiny.Controls.AgentUI;

namespace Sample.Features.Agents;

/// <summary>
/// Plays back AG-UI event streams that were recorded off a real <c>AGUI.Server</c> endpoint (the
/// <c>.sse</c> fixtures <c>spikes/AgUiSpike</c> records), so the sample shows byte-for-byte what a
/// live server sends with no server running. Send a recording's name to play it. A run that follows
/// a frontend tool plays <c>&lt;name&gt;-followup</c>, and resuming an interrupt plays
/// <c>&lt;name&gt;-resume</c>, when those were recorded.
/// </summary>
/// <remarks>
/// The recordings reuse ids across files (each starts at <c>m1</c>), so every replay gets its ids
/// prefixed, and the thread id is swapped for this session's.
/// </remarks>
public sealed class ReplayAgentSession : IAgentSession
{
    /// <summary>The resource name prefix the samples embed the recordings under.</summary>
    public const string ResourcePrefix = "AgentRecordings/";

    static readonly string[] IdKeys = ["messageId", "toolCallId", "parentMessageId", "runId", "subagentRunId"];

    readonly IReadOnlyDictionary<string, string> recordings;
    string? last;
    int replays;

    /// <param name="recordings">Recording name to its SSE text.</param>
    public ReplayAgentSession(IReadOnlyDictionary<string, string> recordings)
    {
        this.recordings = new Dictionary<string, string>(recordings, StringComparer.OrdinalIgnoreCase);
        Suggestions = this.recordings.Keys
            .Where(k => !k.EndsWith("-followup", StringComparison.OrdinalIgnoreCase) && !k.EndsWith("-resume", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Loads every <c>AgentRecordings/*.sse</c> resource embedded in an assembly.</summary>
    public static ReplayAgentSession FromResources(Assembly assembly)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(".sse", StringComparison.Ordinal))
                continue;
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            map[name[ResourcePrefix.Length..^".sse".Length]] = reader.ReadToEnd();
        }
        return new ReplayAgentSession(map);
    }

    /// <summary>The recordings a user can ask for (follow-ups and resumes play on their own).</summary>
    public IReadOnlyList<string> Suggestions { get; }

    /// <summary>The pause between events, so a replay streams like the original. Zero for tests.</summary>
    public TimeSpan EventDelay { get; init; } = TimeSpan.FromMilliseconds(40);

    public string ThreadId { get; } = "replay-" + Guid.NewGuid().ToString("N")[..8];

    public async IAsyncEnumerable<BaseEvent> RunAsync(RunAgentInput input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var name = Pick(input);
        var prefix = $"r{++replays}-";

        if (name is null || !recordings.TryGetValue(name, out var sse))
        {
            foreach (var e in NoRecording(input, prefix))
                yield return e;
            yield break;
        }

        foreach (var json in Parse(sse))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (EventDelay > TimeSpan.Zero)
                await Task.Delay(EventDelay, cancellationToken);
            else
                await Task.Yield();
            yield return Rewrite(json, prefix);
        }
    }

    string? Pick(RunAgentInput input)
    {
        if (input.Resume is { Count: > 0 })
            return last is null ? null : last + "-resume";

        if (input.Messages?.LastOrDefault() is AGUIToolMessage)
            return last is null ? null : last + "-followup";

        var prompt = input.Messages?.OfType<AGUIUserMessage>().LastOrDefault()?.Content.ToString()?.Trim() ?? "";
        var name = Suggestions.FirstOrDefault(s => s.Equals(prompt, StringComparison.OrdinalIgnoreCase))
                   ?? Suggestions.FirstOrDefault(s => prompt.Contains(s, StringComparison.OrdinalIgnoreCase));
        if (name is not null)
            last = name;
        return name;
    }

    // one JSON object per SSE "data:" line; blank lines separate events
    static IEnumerable<JsonObject> Parse(string sse)
    {
        foreach (var line in sse.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith("data:", StringComparison.Ordinal) && JsonNode.Parse(trimmed[5..]) is JsonObject e)
                yield return e;
        }
    }

    BaseEvent Rewrite(JsonObject e, string prefix)
    {
        // the recordings carry the server's raw MEAI updates too; the client never reads them
        e.Remove("rawEvent");
        foreach (var key in IdKeys)
        {
            if (e[key] is JsonValue v && v.TryGetValue<string>(out var id))
                e[key] = prefix + id;
        }
        if (e.ContainsKey("threadId"))
            e["threadId"] = ThreadId;
        return JsonSerializer.Deserialize(e.ToJsonString(), AGUIJsonSerializerContext.Default.BaseEvent)!;
    }

    IEnumerable<BaseEvent> NoRecording(RunAgentInput input, string prefix)
    {
        var text = input.Resume is { Count: > 0 } || input.Messages?.LastOrDefault() is AGUIToolMessage
            ? "That was the end of the recording."
            : "There's no recording by that name. Try one of: " + string.Join(", ", Suggestions) + ".";
        var id = prefix + "msg";
        foreach (var json in new[]
        {
            $$"""{"type":"RUN_STARTED","threadId":"{{ThreadId}}","runId":"{{prefix}}run"}""",
            $$"""{"type":"TEXT_MESSAGE_START","messageId":"{{id}}","role":"assistant"}""",
            new JsonObject { ["type"] = "TEXT_MESSAGE_CONTENT", ["messageId"] = id, ["delta"] = text }.ToJsonString(),
            $$"""{"type":"TEXT_MESSAGE_END","messageId":"{{id}}"}""",
            $$"""{"type":"RUN_FINISHED","threadId":"{{ThreadId}}","runId":"{{prefix}}run"}"""
        })
            yield return JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent)!;
    }
}
