using System.Text.Json.Serialization;
using AvitoNet.Messenger.Models;

namespace AvitoNet.Messenger.Webhooks;

/// <summary>The JSON body Avito POSTs to a registered webhook URL for a new message (v3 shape).</summary>
public sealed class AvitoWebhookUpdate
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("version")] public string? Version { get; init; }
    [JsonPropertyName("timestamp")] public long Timestamp { get; init; }
    [JsonPropertyName("payload")] public AvitoWebhookPayload? Payload { get; init; }
}

/// <summary>The webhook payload envelope. <see cref="Type"/> is currently always <c>message</c>.</summary>
public sealed class AvitoWebhookPayload
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("value")] public AvitoWebhookMessage? Value { get; init; }
}

/// <summary>A message as delivered by the webhook.</summary>
public sealed class AvitoWebhookMessage
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    /// <summary>The account the webhook is registered to (the receiver) — always your account.</summary>
    [JsonPropertyName("user_id")] public long UserId { get; init; }

    /// <summary>Id of the user who sent the message.</summary>
    [JsonPropertyName("author_id")] public long AuthorId { get; init; }

    [JsonPropertyName("chat_id")] public string ChatId { get; init; } = "";

    /// <summary>One of <see cref="AvitoChatType"/>.</summary>
    [JsonPropertyName("chat_type")] public string? ChatType { get; init; }

    /// <summary>One of <see cref="AvitoMessageType"/>.</summary>
    [JsonPropertyName("type")] public string Type { get; init; } = "";

    [JsonPropertyName("content")] public AvitoMessageContent? Content { get; init; }

    /// <summary>The listing id, for <c>u2i</c> chats.</summary>
    [JsonPropertyName("item_id")] public long? ItemId { get; init; }

    /// <summary>Unix timestamp (seconds) the message was created.</summary>
    [JsonPropertyName("created")] public long Created { get; init; }

    /// <summary>Publish time in RFC3339 UTC.</summary>
    [JsonPropertyName("published_at")] public string? PublishedAt { get; init; }

    /// <summary>Unix timestamp the message was read, if already read.</summary>
    [JsonPropertyName("read")] public long? Read { get; init; }

    /// <summary>True when the message was sent by this account (author == receiver), i.e. an echo of an outbound send.</summary>
    [JsonIgnore] public bool IsOutgoing => AuthorId == UserId;

    [JsonIgnore] public DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Created);
}
