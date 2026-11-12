using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using AGUI.Abstractions;
using AGUI.Client;
using Shiny.Controls.AgentUI;

namespace Sample.Features.Agents;

/// <summary>
/// A live AG-UI endpoint over HTTP + SSE: POSTs each run and reads the event stream back with the
/// official SDK's reader. This is all a transport needs to be - production apps get one from
/// Shiny.AiConversation (or any other AG-UI client) instead.
/// </summary>
public sealed class HttpAgentSession(HttpClient http, Uri endpoint) : IAgentSession
{
    public string ThreadId { get; } = "thread-" + Guid.NewGuid().ToString("N")[..8];

    public async IAsyncEnumerable<BaseEvent> RunAsync(RunAgentInput input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(input, AGUIJsonSerializerContext.Default.RunAgentInput)
        };
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await foreach (var e in response.ReadAGUIEventStreamAsync(cancellationToken))
            yield return e;
    }
}
