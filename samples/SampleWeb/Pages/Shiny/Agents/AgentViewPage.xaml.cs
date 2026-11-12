using System.Text.Json.Nodes;
using Shiny.Controls.AgentUI;

namespace Sample.Features.Agents;

public partial class AgentViewPage : ContentPage
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public AgentViewPage()
    {
        InitializeComponent();

        // the built-in UI tools: the agent asks, a real control answers inline in the conversation
        var prompts = new AgentPromptQueue();
        Agent.Prompts = prompts;
        Agent.Tools = new AgentToolRegistry().AddAll(prompts);
        Agent.StateChanged += (_, state) => ShowReadingList(state);
        SourcePicker.SelectedIndex = 0; // Demo

        SizeChanged += (_, _) => PlaceStatePanel();
    }

    void OnSourceChanged(object? sender, EventArgs e)
    {
        var source = SourcePicker.SelectedIndex;
        DemoOptions.IsVisible = source == 0;
        RecordingsOptions.IsVisible = source == 1;
        EndpointOptions.IsVisible = source == 2;
        KeyOptions.IsVisible = source == 3;

        switch (source)
        {
            case 0:
                // a scripted agent: every scenario works offline
                Agent.Suggestions = DemoAgentSession.Suggestions;
                Agent.Session = new DemoAgentSession();
                break;

            case 1:
                // the AGUI.Server streams the engine tests replay, embedded in this app
                var replay = ReplayAgentSession.FromResources(typeof(AgentViewPage).Assembly);
                Agent.Suggestions = replay.Suggestions;
                Agent.Session = replay;
                break;

            default:
                // the endpoint and key sources wait for Connect
                Agent.Suggestions = null;
                Agent.Session = null;
                break;
        }
    }

    async void OnConnectEndpoint(object? sender, EventArgs e)
    {
        var text = EndpointEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text))
            text = EndpointEntry.Placeholder;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("http" or "https"))
        {
            await DisplayAlertAsync("Endpoint", "Enter an http(s) URL of an AG-UI endpoint.", "OK");
            return;
        }

        // the scripted test server answers these prompts; a real agent takes anything
        Agent.Suggestions = ["text", "reasoning", "frontend-tool", "a2ui", "interrupt", "error"];
        Agent.Session = new HttpAgentSession(Http, endpoint);
    }

    async void OnConnectKey(object? sender, EventArgs e)
    {
        // the key is read from the entry and handed to the session - never stored or logged
        var key = ApiKeyEntry.Text?.Trim();
        var model = ModelEntry.Text?.Trim();
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(model))
        {
            await DisplayAlertAsync("Your key", "Enter an API key and a model.", "OK");
            return;
        }
        if (!Uri.TryCreate(BaseUrlEntry.Text?.Trim(), UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("http" or "https"))
        {
            await DisplayAlertAsync("Your key", "Enter the http(s) base URL of an OpenAI-compatible API, such as https://api.openai.com/v1.", "OK");
            return;
        }

        Agent.Suggestions = ChatClientAgentSession.Suggestions;
        Agent.Session = ChatClientAgentSession.ForOpenAI(key, model, endpoint);
    }

    void OnClear(object? sender, EventArgs e) => Agent.Clear();

    // the panel is bound to the same shared state the agent edits; tapping a book edits it back
    void ShowReadingList(JsonNode? state)
    {
        BooksStack.Children.Clear();
        try
        {
            FillReadingList(state);
        }
        finally
        {
            PlaceStatePanel();
        }
    }

    void FillReadingList(JsonNode? state)
    {
        if (state?["books"] is not JsonArray books)
        {
            GoalLabel.Text = "Shared state - ask the agent to “track my reading list”.";
            return;
        }

        var done = books.Count(b => b?["done"]?.GetValue<bool>() == true);
        GoalLabel.Text = $"{done} of {state["goal"]?.ToString() ?? "?"} read this year";
        for (var i = 0; i < books.Count; i++)
        {
            var index = i;
            var book = books[i];
            var isDone = book?["done"]?.GetValue<bool>() == true;
            var row = new Label
            {
                Text = $"{(isDone ? "✓" : "○")}  {book?["title"]}",
                FontSize = 14,
                Padding = new Thickness(0, 4)
            };
            SemanticProperties.SetDescription(row, $"{book?["title"]}, {(isDone ? "read" : "not read")}. Tap to toggle.");
            row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => ToggleBook(index)) });
            BooksStack.Children.Add(row);
        }
    }

    void ToggleBook(int index)
    {
        if (Agent.State?.DeepClone() is not JsonObject state || state["books"]?[index] is not JsonObject book)
            return;
        book["done"] = !(book["done"]?.GetValue<bool>() ?? false);
        Agent.State = state; // goes to the agent with the next run
    }

    void PlaceStatePanel()
    {
        var wide = Width >= 760;
        Grid.SetRow(StatePanel, wide ? 2 : 3);
        Grid.SetColumn(StatePanel, wide ? 1 : 0);
        StatePanel.WidthRequest = wide ? 260 : -1;
        StatePanel.Margin = wide ? new Thickness(0, 0, 12, 12) : new Thickness(12, 0, 12, 12);
        // on a phone the panel only takes room once there is something in it
        StatePanel.IsVisible = wide || BooksStack.Children.Count > 0;
    }
}
