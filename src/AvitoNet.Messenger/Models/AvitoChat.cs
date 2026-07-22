using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvitoNet.Messenger.Models;

/// <summary>Wrapper for the chats-list response.</summary>
public sealed class AvitoChatList
{
    [JsonPropertyName("chats")] public IReadOnlyList<AvitoChat> Chats { get; init; } = [];
}

/// <summary>A chat (conversation).</summary>
public sealed class AvitoChat
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("created")] public long Created { get; init; }
    [JsonPropertyName("updated")] public long Updated { get; init; }

    /// <summary>Chat participants.</summary>
    [JsonPropertyName("users")] public IReadOnlyList<AvitoChatUser> Users { get; init; } = [];

    /// <summary>The most recent message (partial).</summary>
    [JsonPropertyName("last_message")] public AvitoMessage? LastMessage { get; init; }

    /// <summary>Context accompanying the chat (e.g. the listing for a <c>u2i</c> chat).</summary>
    [JsonPropertyName("context")] public AvitoChatContext? Context { get; init; }

    [JsonIgnore] public DateTimeOffset UpdatedAt => DateTimeOffset.FromUnixTimeSeconds(Updated);
}

/// <summary>A chat participant.</summary>
public sealed class AvitoChatUser
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

/// <summary>
/// Chat context. <see cref="Type"/> (e.g. <c>item</c>) determines the shape of <see cref="Value"/>, which is
/// left as raw JSON since its schema varies by type.
/// </summary>
public sealed class AvitoChatContext
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("value")] public JsonElement Value { get; init; }
}
