using Shiny.Maui.Controls.Chat;

namespace Sample.Features.Chat;

/// <summary>
/// A fully in-memory <see cref="IChatSessionProvider"/> used by the demo. It seeds a "demo"
/// session (me + Alice + Bob) with history, then simulates a live conversation: outgoing sends are
/// echoed, replies arrive after a short typing burst, reactions/edits/deletes/read-receipts raise
/// the matching events, and a one-shot connection blip can be triggered on demand.
/// </summary>
public class InMemoryChatSessionProvider : IChatSessionProvider
{
    public const string DemoSessionId = "demo";

    readonly Dictionary<string, InMemoryChatStore> stores = new(StringComparer.Ordinal);

    public InMemoryChatSessionProvider()
    {
        var demo = InMemoryChatStore.CreateDemo(DemoSessionId);
        this.stores[demo.SessionId] = demo;
    }

    public Task<IChatSession> CreateSessionAsync(string[] userIds, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var store = InMemoryChatStore.CreateEmpty(id, userIds);
        this.stores[id] = store;
        return Task.FromResult<IChatSession>(new InMemoryChatSession(store));
    }

    public Task<IChatSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (!this.stores.TryGetValue(sessionId, out var store))
            throw new ChatSessionException($"Chat session '{sessionId}' was not found.");

        return Task.FromResult<IChatSession>(new InMemoryChatSession(store));
    }
}


/// <summary>
/// The persistent data behind a session. Lives in the provider so history survives the control
/// disposing and re-resolving its <see cref="IChatSession"/> across navigations.
/// </summary>
sealed class InMemoryChatStore
{
    public const string MeId = "me";

    readonly object sync = new();
    int idCounter;

    InMemoryChatStore(string sessionId, string sessionName, ChatSessionUserInfo[] users)
    {
        this.SessionId = sessionId;
        this.SessionName = sessionName;
        this.Users = users;
        this.Messages = new List<ChatMessage>();
    }

    public string SessionId { get; }
    public string SessionName { get; private set; }
    public ChatSessionUserInfo[] Users { get; private set; }
    public List<ChatMessage> Messages { get; }
    public object Sync => this.sync;

    public string[] OtherUserIds => this.Users
        .Where(u => u.UserId != MeId)
        .Select(u => u.UserId)
        .ToArray();

    public string NextMessageId() => $"m{Interlocked.Increment(ref this.idCounter)}";

    public void Rename(string name) => this.SessionName = name;

    /// <summary>Builds a store with custom users; callers seed history via <see cref="SeedMessage"/>.</summary>
    public static InMemoryChatStore Create(string sessionId, string sessionName, ChatSessionUserInfo[] users)
        => new(sessionId, sessionName, users);

    /// <summary>Appends a historical (already-read) message. Used by demos to seed conversations.</summary>
    public void SeedMessage(string senderId, string? body, string? imageUrl, int minutesAgo)
    {
        lock (this.sync)
        {
            this.Messages.Add(new ChatMessage(
                MessageId: this.NextMessageId(),
                ClientMessageId: null,
                SenderId: senderId,
                Body: body,
                ImageUrl: imageUrl,
                Status: MessageStatus.Read,
                StatusReason: null,
                Timestamp: DateTimeOffset.Now.AddMinutes(minutesAgo),
                EditedTimestamp: null,
                Reactions: Array.Empty<Reaction>(),
                ReadReceipts: Array.Empty<ReadReceipt>()
            ));
        }
    }

    public ChatSessionInfo BuildInfo() => new(
        this.SessionId,
        this.SessionName,
        this.Users,
        PermittedEmojis: null,                       // use the control's default emoji set
        BodyPermissions: MessageBodyPermissions.All,
        Permissions: ChatSessionPermissions.All,
        CreatedAt: DateTimeOffset.Now.AddDays(-1),
        LastReadDate: DateTimeOffset.Now,
        UnreadMessageCount: 0
    );

    public static InMemoryChatStore CreateEmpty(string sessionId, string[] userIds)
    {
        var users = new List<ChatSessionUserInfo>
        {
            new(MeId, "Me", null, null, DateTimeOffset.Now.AddDays(-1))
        };
        foreach (var id in userIds.Where(x => x != MeId))
            users.Add(new ChatSessionUserInfo(id, id, null, null, DateTimeOffset.Now.AddDays(-1)));

        return new InMemoryChatStore(sessionId, "New Chat", users.ToArray());
    }

    public static InMemoryChatStore CreateDemo(string sessionId)
    {
        var joined = DateTimeOffset.Now.AddDays(-7);
        var users = new[]
        {
            new ChatSessionUserInfo(MeId, "Me", null, null, joined),
            new ChatSessionUserInfo(
                "alice",
                "Alice Johnson",
                ImageSource.FromFile("dotnet_bot.png"),
                Color.FromArgb("#E3F2FD"),
                joined),
            new ChatSessionUserInfo(
                "bob",
                "Bob Smith",
                null,
                Color.FromArgb("#FFF3E0"),
                joined)
        };

        var store = new InMemoryChatStore(sessionId, "Controls Crew", users);
        store.Seed();
        return store;
    }

    void Seed()
    {
        var now = DateTimeOffset.Now;

        void Add(string sender, string? body, string? imageUrl, int minutesAgo,
            IReadOnlyList<Reaction>? reactions = null, IReadOnlyList<ReadReceipt>? receipts = null)
        {
            var ts = now.AddMinutes(minutesAgo);
            this.Messages.Add(new ChatMessage(
                MessageId: this.NextMessageId(),
                ClientMessageId: null,
                SenderId: sender,
                Body: body,
                ImageUrl: imageUrl,
                Status: MessageStatus.Read,
                StatusReason: null,
                Timestamp: ts,
                EditedTimestamp: null,
                Reactions: reactions ?? Array.Empty<Reaction>(),
                ReadReceipts: receipts ?? Array.Empty<ReadReceipt>()
            ));
        }

        Add("alice", "Hey everyone! Has anyone tried the new controls library?", null, -240);
        Add("bob", "Yes! The TableView is really nice.", null, -239);
        Add(MeId, "I agree, the styling system is great.", null, -238,
            reactions:
            [
                new Reaction("alice", "\U0001F44D", now.AddMinutes(-237)),
                new Reaction("bob", "\U0001F44D", now.AddMinutes(-237)),
                new Reaction("alice", "❤️", now.AddMinutes(-236))
            ],
            receipts:
            [
                new ReadReceipt("alice", now.AddMinutes(-236)),
                new ReadReceipt("bob", now.AddMinutes(-235))
            ]);
        Add("alice", "Check out https://github.com/shinyorg/controls for the latest updates.", null, -120);
        Add("alice", "The scheduler component is my favorite so far.", null, -119,
            reactions: [new Reaction(MeId, "\U0001F4AF", now.AddMinutes(-118))]);
        Add("bob", "Same here! Really clean API.", null, -90);
        Add(MeId, "Here's a screenshot of what I built:", null, -30,
            receipts: [new ReadReceipt("alice", now.AddMinutes(-29))]);
        Add(MeId, null, "https://picsum.photos/300/200", -29);
    }
}


/// <summary>
/// A live session over an <see cref="InMemoryChatStore"/>. A fresh instance is handed to the control
/// each time it resolves the session; the underlying store (history) is shared and persistent.
/// </summary>
sealed class InMemoryChatSession : IChatSession
{
    static readonly string[] ReplyBodies =
    [
        "That's awesome!",
        "Tell me more!",
        "Ha, nice one \U0001F604",
        "Sounds good to me.",
        "Let's do it!"
    ];

    readonly InMemoryChatStore store;

    public InMemoryChatSession(InMemoryChatStore store)
        => this.store = store;

    public ChatSessionInfo Info => this.store.BuildInfo();
    public string CurrentUserId => InMemoryChatStore.MeId;

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<MessageChanged>? MessageUpdated;
    public event EventHandler<string>? MessageDeleted;
    public event EventHandler<UserTypingEvent>? UserTyping;
    public event EventHandler<ChatSessionUserInfo>? UserJoined;
    public event EventHandler<ChatSessionUserInfo>? UserLeft;
    public event EventHandler<ChatSessionInfo>? SessionUpdated;
    public event EventHandler<ChatConnectionState>? ConnectionStateChanged;


    // ---- paging ----

    public Task<MessagePage> GetMessagesAsync(
        string? cursorMessageId,
        MessagePageDirection direction,
        int count,
        CancellationToken cancellationToken = default)
    {
        lock (this.store.Sync)
        {
            var all = this.store.Messages;

            if (direction == MessagePageDirection.Older)
            {
                var end = all.Count;
                if (cursorMessageId is not null)
                {
                    var i = all.FindIndex(m => m.MessageId == cursorMessageId);
                    if (i >= 0)
                        end = i; // strictly older than the cursor
                }

                var start = Math.Max(0, end - count);
                var slice = all.GetRange(start, end - start);
                return Task.FromResult(new MessagePage(slice, start > 0));
            }
            else
            {
                var start = 0;
                if (cursorMessageId is not null)
                {
                    var i = all.FindIndex(m => m.MessageId == cursorMessageId);
                    if (i >= 0)
                        start = i + 1; // strictly newer than the cursor
                }

                var take = Math.Min(count, all.Count - start);
                if (take <= 0)
                    return Task.FromResult(new MessagePage(Array.Empty<ChatMessage>(), false));

                var slice = all.GetRange(start, take);
                return Task.FromResult(new MessagePage(slice, start + take < all.Count));
            }
        }
    }


    // ---- outgoing ----

    public Task<ChatMessage> SendMessageAsync(OutgoingMessage message, CancellationToken cancellationToken = default)
    {
        // Attachment-only sends simulate a hosted image; the provider owns + disposes the stream.
        string? imageUrl = null;
        if (message.Attachment is not null)
        {
            imageUrl = "https://picsum.photos/seed/" + Guid.NewGuid().ToString("N") + "/300/200";
            message.Attachment.Content.Dispose();
        }

        ChatMessage stored;
        lock (this.store.Sync)
        {
            stored = new ChatMessage(
                MessageId: this.store.NextMessageId(),
                ClientMessageId: string.IsNullOrEmpty(message.ClientMessageId) ? null : message.ClientMessageId,
                SenderId: this.CurrentUserId,
                Body: message.Body,
                ImageUrl: imageUrl,
                Status: MessageStatus.Sent,
                StatusReason: null,
                Timestamp: DateTimeOffset.Now,
                EditedTimestamp: null,
                Reactions: Array.Empty<Reaction>(),
                ReadReceipts: Array.Empty<ReadReceipt>()
            );
            this.store.Messages.Add(stored);
        }

        _ = this.SimulateReplyAsync();
        return Task.FromResult(stored);
    }

    public Task<ChatMessage> ResendMessageAsync(string clientMessageId, CancellationToken cancellationToken = default)
    {
        lock (this.store.Sync)
        {
            var idx = this.store.Messages.FindIndex(m => m.ClientMessageId == clientMessageId);
            if (idx < 0)
                throw new ChatSessionException("Message to resend was not found.");

            var resent = this.store.Messages[idx] with { Status = MessageStatus.Sent, StatusReason = null };
            this.store.Messages[idx] = resent;
            return Task.FromResult(resent);
        }
    }

    public Task EditMessageAsync(string messageId, string body, CancellationToken cancellationToken = default)
    {
        ChatMessage? edited = null;
        lock (this.store.Sync)
        {
            var idx = this.store.Messages.FindIndex(m => m.MessageId == messageId);
            if (idx >= 0)
            {
                edited = this.store.Messages[idx] with { Body = body, EditedTimestamp = DateTimeOffset.Now };
                this.store.Messages[idx] = edited;
            }
        }
        if (edited is not null)
            this.MessageUpdated?.Invoke(this, new MessageChanged(edited, MessageChangeKind.Edited));

        return Task.CompletedTask;
    }

    public Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        bool removed;
        lock (this.store.Sync)
            removed = this.store.Messages.RemoveAll(m => m.MessageId == messageId) > 0;

        if (removed)
            this.MessageDeleted?.Invoke(this, messageId);

        return Task.CompletedTask;
    }

    public Task ReactToMessageAsync(string messageId, string emoji, bool add, CancellationToken cancellationToken = default)
    {
        ChatMessage? changed = null;
        lock (this.store.Sync)
        {
            var idx = this.store.Messages.FindIndex(m => m.MessageId == messageId);
            if (idx >= 0)
            {
                var msg = this.store.Messages[idx];
                var reactions = msg.Reactions.ToList();
                reactions.RemoveAll(r => r.UserId == this.CurrentUserId && r.Emoji == emoji);
                if (add)
                    reactions.Add(new Reaction(this.CurrentUserId, emoji, DateTimeOffset.Now));

                changed = msg with { Reactions = reactions };
                this.store.Messages[idx] = changed;
            }
        }
        if (changed is not null)
            this.MessageUpdated?.Invoke(this, new MessageChanged(changed, MessageChangeKind.ReactionChanged));

        return Task.CompletedTask;
    }

    public Task MarkReadAsync(string[] messageIds, CancellationToken cancellationToken = default)
    {
        var changes = new List<ChatMessage>();
        lock (this.store.Sync)
        {
            foreach (var id in messageIds)
            {
                var idx = this.store.Messages.FindIndex(m => m.MessageId == id);
                if (idx < 0)
                    continue;

                var msg = this.store.Messages[idx];
                if (msg.ReadReceipts.Any(r => r.UserId == this.CurrentUserId))
                    continue;

                var receipts = msg.ReadReceipts.ToList();
                receipts.Add(new ReadReceipt(this.CurrentUserId, DateTimeOffset.Now));
                var updated = msg with { ReadReceipts = receipts };
                this.store.Messages[idx] = updated;
                changes.Add(updated);
            }
        }
        foreach (var c in changes)
            this.MessageUpdated?.Invoke(this, new MessageChanged(c, MessageChangeKind.ReadReceiptChanged));

        return Task.CompletedTask;
    }


    // ---- session management ----

    public Task ToggleTypingAsync(bool isTyping, CancellationToken cancellationToken = default)
        => Task.CompletedTask; // local user typing — nothing to broadcast in the demo

    public Task InviteUserAsync(string userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task LeaveAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RenameAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        this.store.Rename(sessionName);
        this.SessionUpdated?.Invoke(this, this.Info);
        return Task.CompletedTask;
    }


    // ---- live simulation ----

    async Task SimulateReplyAsync()
    {
        var others = this.store.OtherUserIds;
        if (others.Length == 0)
            return;

        var replier = others[Random.Shared.Next(others.Length)];

        await Task.Delay(700).ConfigureAwait(false);
        this.UserTyping?.Invoke(this, new UserTypingEvent(replier, true, DateTimeOffset.Now));

        await Task.Delay(1600).ConfigureAwait(false);
        this.UserTyping?.Invoke(this, new UserTypingEvent(replier, false, DateTimeOffset.Now));

        ChatMessage reply;
        lock (this.store.Sync)
        {
            reply = new ChatMessage(
                MessageId: this.store.NextMessageId(),
                ClientMessageId: null,
                SenderId: replier,
                Body: ReplyBodies[Random.Shared.Next(ReplyBodies.Length)],
                ImageUrl: null,
                Status: MessageStatus.Sent,
                StatusReason: null,
                Timestamp: DateTimeOffset.Now,
                EditedTimestamp: null,
                Reactions: Array.Empty<Reaction>(),
                ReadReceipts: Array.Empty<ReadReceipt>()
            );
            this.store.Messages.Add(reply);
        }
        this.MessageReceived?.Invoke(this, reply);
    }


    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
