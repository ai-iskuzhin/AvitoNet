using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AvitoNet.Auth;
using AvitoNet.Messenger.Models;

namespace AvitoNet.Messenger;

/// <summary>Default <see cref="IAvitoMessengerClient"/>. Obtain via <see cref="IAvitoClientFactory"/>.</summary>
internal sealed class AvitoMessengerClient : AvitoApiClientBase, IAvitoMessengerClient
{
    /// <summary>Avito's limit for one text message.</summary>
    public const int MaxTextLength = 1000;

    public AvitoMessengerClient(HttpClient http, IAvitoTokenSource tokens, AvitoClientOptions options)
        : base(http, tokens, options)
    {
    }

    public Task<AvitoAccount> GetSelfAsync(CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoAccount>(HttpMethod.Get, "/core/v1/accounts/self", null, cancellationToken);

    public Task<AvitoChatList> GetChatsAsync(long userId, AvitoChatQuery? query = null, CancellationToken cancellationToken = default)
    {
        var qs = new List<string>();
        if (query?.Limit is int limit) qs.Add($"limit={limit}");
        if (query?.Offset is int offset) qs.Add($"offset={offset}");
        if (query?.UnreadOnly is bool unread) qs.Add($"unread_only={(unread ? "true" : "false")}");
        if (query?.ChatTypes is { Count: > 0 } types) qs.Add($"chat_types={string.Join(",", types)}");
        if (query?.ItemIds is { Count: > 0 } items) qs.Add($"item_ids={string.Join(",", items)}");
        var suffix = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        return RequestJsonAsync<AvitoChatList>(HttpMethod.Get, $"/messenger/v2/accounts/{userId}/chats{suffix}", null, cancellationToken);
    }

    public Task<AvitoChat> GetChatAsync(long userId, string chatId, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoChat>(HttpMethod.Get, $"/messenger/v2/accounts/{userId}/chats/{Esc(chatId)}", null, cancellationToken);

    public async Task<IReadOnlyList<AvitoMessage>> GetMessagesAsync(long userId, string chatId, int? limit = null, int? offset = null, CancellationToken cancellationToken = default)
    {
        var qs = new List<string>();
        if (limit is int l) qs.Add($"limit={l}");
        if (offset is int o) qs.Add($"offset={o}");
        var suffix = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        var messages = await RequestJsonAsync<List<AvitoMessage>>(
            HttpMethod.Get, $"/messenger/v3/accounts/{userId}/chats/{Esc(chatId)}/messages/{suffix}", null, cancellationToken).ConfigureAwait(false);
        return messages;
    }

    public Task<AvitoMessage> SendTextAsync(long userId, string chatId, string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > MaxTextLength)
        {
            // Fail fast with a clear message instead of Avito's opaque 400; split with AvitoText.Split first.
            throw new ArgumentOutOfRangeException(nameof(text), text.Length, $"Avito accepts at most {MaxTextLength} characters per message; split it with AvitoText.Split.");
        }

        return RequestJsonAsync<AvitoMessage>(HttpMethod.Post, $"/messenger/v1/accounts/{userId}/chats/{Esc(chatId)}/messages",
            new SendTextRequest(new SendTextBody(text), "text"), cancellationToken);
    }

    public Task<AvitoMessage> SendImageAsync(long userId, string chatId, string imageId, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoMessage>(HttpMethod.Post, $"/messenger/v1/accounts/{userId}/chats/{Esc(chatId)}/messages/image",
            new SendImageRequest(imageId), cancellationToken);

    public async Task<string> UploadImageAsync(long userId, Stream image, string fileName, string? contentType = null, CancellationToken cancellationToken = default)
    {
        // Buffered so the request can be resent if the token has to be refreshed (Avito caps images at 24 MB).
        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var bytes = buffer.ToArray();

        HttpContent Content()
        {
            var file = new ByteArrayContent(bytes);
            if (!string.IsNullOrEmpty(contentType))
            {
                file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            // Field name per Avito's spec: one file per call as "uploadfile[]".
            return new MultipartFormDataContent { { file, "uploadfile[]", fileName } };
        }

        // The response is a { "<image_id>": { sizes… } } map; the key is the id.
        var map = await RequestContentAsync<Dictionary<string, JsonElement>>(
            HttpMethod.Post, $"/messenger/v1/accounts/{userId}/uploadImages", Content, cancellationToken).ConfigureAwait(false);
        foreach (var key in map.Keys)
        {
            return key;
        }
        throw new AvitoApiException(200, null, "Avito uploadImages returned no image id.", null);
    }

    public Task MarkChatReadAsync(long userId, string chatId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"/messenger/v1/accounts/{userId}/chats/{Esc(chatId)}/read", null, cancellationToken);

    public Task DeleteMessageAsync(long userId, string chatId, string messageId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"/messenger/v1/accounts/{userId}/chats/{Esc(chatId)}/messages/{Esc(messageId)}", null, cancellationToken);

    public Task<AvitoVoiceFiles> GetVoiceFilesAsync(long userId, IEnumerable<string> voiceIds, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoVoiceFiles>(HttpMethod.Get,
            $"/messenger/v1/accounts/{userId}/getVoiceFiles?voice_ids={string.Join(",", voiceIds.Select(Esc))}", null, cancellationToken);

    public Task BlacklistAsync(long userId, IEnumerable<AvitoBlacklistEntry> users, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"/messenger/v2/accounts/{userId}/blacklist",
            new BlacklistRequest(users.Select(u => new BlacklistUser(u.UserId, new BlacklistContext(u.ItemId, u.ReasonId))).ToList()), cancellationToken);

    public Task SubscribeWebhookAsync(string url, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "/messenger/v3/webhook", new UrlRequest(url), cancellationToken);

    public Task UnsubscribeWebhookAsync(string url, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "/messenger/v1/webhook/unsubscribe", new UrlRequest(url), cancellationToken);

    public Task<AvitoSubscriptionList> GetSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoSubscriptionList>(HttpMethod.Post, "/messenger/v1/subscriptions", null, cancellationToken);
}

// Request bodies (explicit snake_case names, independent of the serializer's naming policy).
file sealed record SendTextRequest([property: JsonPropertyName("message")] SendTextBody Message, [property: JsonPropertyName("type")] string Type);
file sealed record SendTextBody([property: JsonPropertyName("text")] string Text);
file sealed record SendImageRequest([property: JsonPropertyName("image_id")] string ImageId);
file sealed record UrlRequest([property: JsonPropertyName("url")] string Url);
file sealed record BlacklistRequest([property: JsonPropertyName("users")] IReadOnlyList<BlacklistUser> Users);
file sealed record BlacklistUser([property: JsonPropertyName("user_id")] long UserId, [property: JsonPropertyName("context")] BlacklistContext Context);
file sealed record BlacklistContext(
    [property: JsonPropertyName("item_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? ItemId,
    [property: JsonPropertyName("reason_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ReasonId);
