namespace AvitoNet.Messenger.Models;

/// <summary>Filters for listing chats.</summary>
public sealed class AvitoChatQuery
{
    /// <summary>Max chats to return (1–100; default 100).</summary>
    public int? Limit { get; init; }

    /// <summary>Offset for paging (0–1000).</summary>
    public int? Offset { get; init; }

    /// <summary>Return only chats with unread messages.</summary>
    public bool? UnreadOnly { get; init; }

    /// <summary>Restrict to these chat types (see <see cref="AvitoChatType"/>); default <c>u2i</c>.</summary>
    public IReadOnlyList<string>? ChatTypes { get; init; }

    /// <summary>Restrict to chats about these listing ids.</summary>
    public IReadOnlyList<long>? ItemIds { get; init; }
}
