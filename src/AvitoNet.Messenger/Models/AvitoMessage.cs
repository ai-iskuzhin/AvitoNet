using System.Text.Json.Serialization;

namespace AvitoNet.Messenger.Models;

/// <summary>A messenger message (from history, a send response, or a chat's last message).</summary>
public sealed class AvitoMessage
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";

    /// <summary>Id of the user who sent the message.</summary>
    [JsonPropertyName("author_id")] public long AuthorId { get; init; }

    /// <summary>One of <see cref="AvitoMessageType"/>.</summary>
    [JsonPropertyName("type")] public string Type { get; init; } = "";

    [JsonPropertyName("content")] public AvitoMessageContent? Content { get; init; }

    /// <summary>Unix timestamp (seconds) when the message was created.</summary>
    [JsonPropertyName("created")] public long Created { get; init; }

    /// <summary><c>in</c> or <c>out</c> (see <see cref="AvitoMessageDirection"/>); absent on send responses.</summary>
    [JsonPropertyName("direction")] public string? Direction { get; init; }

    /// <summary>True if the authenticated account has already read this message.</summary>
    [JsonPropertyName("is_read")] public bool IsRead { get; init; }

    /// <summary>Unix timestamp when the counterpart read the message, if read.</summary>
    [JsonPropertyName("read")] public long? Read { get; init; }

    /// <summary>The quoted message this one replies to, if any.</summary>
    [JsonPropertyName("quote")] public AvitoMessageQuote? Quote { get; init; }

    /// <summary><see cref="Created"/> as a <see cref="DateTimeOffset"/> (UTC).</summary>
    [JsonIgnore] public DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Created);
}

/// <summary>A quoted (replied-to) message.</summary>
public sealed class AvitoMessageQuote
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("author_id")] public long AuthorId { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; } = "";
    [JsonPropertyName("content")] public AvitoMessageContent? Content { get; init; }
    [JsonPropertyName("created")] public long Created { get; init; }
}

/// <summary>
/// Message content. Only the field matching <c>message.Type</c> is populated (e.g. <see cref="Text"/> for
/// <c>text</c>). Unsupported types (<c>appCall</c>, <c>file</c>, <c>video</c>) carry no content.
/// </summary>
public sealed class AvitoMessageContent
{
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("image")] public AvitoImageContent? Image { get; init; }
    [JsonPropertyName("item")] public AvitoItemContent? Item { get; init; }
    [JsonPropertyName("link")] public AvitoLinkContent? Link { get; init; }
    [JsonPropertyName("location")] public AvitoLocationContent? Location { get; init; }
    [JsonPropertyName("call")] public AvitoCallContent? Call { get; init; }
    [JsonPropertyName("voice")] public AvitoVoiceContent? Voice { get; init; }

    /// <summary>Chat-bot flow id, for <c>system</c> messages.</summary>
    [JsonPropertyName("flow_id")] public string? FlowId { get; init; }
}

/// <summary>Image content: a "WxH" → URL map of the available sizes.</summary>
public sealed class AvitoImageContent
{
    [JsonPropertyName("sizes")] public IReadOnlyDictionary<string, string>? Sizes { get; init; }
}

/// <summary>An item (listing) card attached to a message.</summary>
public sealed class AvitoItemContent
{
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("item_url")] public string? ItemUrl { get; init; }
    [JsonPropertyName("image_url")] public string? ImageUrl { get; init; }
    [JsonPropertyName("price_string")] public string? PriceString { get; init; }
}

public sealed class AvitoLinkContent
{
    [JsonPropertyName("url")] public string? Url { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("preview")] public AvitoLinkPreview? Preview { get; init; }
}

public sealed class AvitoLinkPreview
{
    [JsonPropertyName("url")] public string? Url { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("domain")] public string? Domain { get; init; }
    [JsonPropertyName("images")] public IReadOnlyDictionary<string, string>? Images { get; init; }
}

public sealed class AvitoLocationContent
{
    [JsonPropertyName("lat")] public double Lat { get; init; }
    [JsonPropertyName("lon")] public double Lon { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("kind")] public string? Kind { get; init; }
}

public sealed class AvitoCallContent
{
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("target_user_id")] public long TargetUserId { get; init; }
}

public sealed class AvitoVoiceContent
{
    /// <summary>Resolve to a playable URL via <c>GetVoiceFilesAsync</c>.</summary>
    [JsonPropertyName("voice_id")] public string? VoiceId { get; init; }
}
