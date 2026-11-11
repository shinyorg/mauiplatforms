using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;
using Shiny.Maui.Controls.Chat;

namespace Sample.Features.Chat;

public partial class ChatViewModel : ObservableObject
{
    public ChatViewModel(InMemoryChatSessionProvider provider)
    {
        this.Provider = provider;

        // Example consumer-supplied input-bar actions (surfaced via the overflow button). The controls
        // sample also adds the speech add-ins' tools here; the web sample leaves them out.
        this.InputActions =
        [
            new ChatInputAction
            {
                Text = "Insert Greeting",
                Handler = view =>
                {
                    var existing = view.EntryText?.Trim();
                    view.EntryText = string.IsNullOrEmpty(existing) ? "Hello \U0001F44B" : $"{existing} Hello \U0001F44B";
                    return Task.CompletedTask;
                }
            }
        ];

        // Example consumer-supplied bubble actions (appended to the built-in action set).
        this.CustomBubbleActions =
        [
            new ChatBubbleAction
            {
                Text = "Translate",
                Handler = async msg =>
                    await Application.Current!.Windows[0].Page!.DisplayAlert("Translate", $"Translating: {msg.Body}", "OK")
            }
        ];
    }

    public IChatSessionProvider Provider { get; }
    public string SessionId => InMemoryChatSessionProvider.DemoSessionId;
    public IList<ChatInputAction> InputActions { get; }
    public IList<ChatBubbleAction> CustomBubbleActions { get; }
}
