using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AvitoNet.Messenger.Auth;
using AvitoNet.Messenger.Models;

namespace AvitoNet.Messenger;

/// <summary>Default <see cref="IAvitoMessengerClient"/>. Construct via <see cref="IAvitoMessengerClientFactory"/>.</summary>
public sealed class AvitoMessengerClient : IAvitoMessengerClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AvitoCredentials _credentials;
    private readonly IAvitoTokenProvider _tokens;
    private readonly AvitoMessengerOptions _options;

    public AvitoMessengerClient(HttpClient http, AvitoCredentials credentials, IAvitoTokenProvider tokens, AvitoMessengerOptions options)
    {
        _http = http;
        _credentials = credentials;
        _tokens = tokens;
        _options = options;
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

    public Task<AvitoMessage> SendTextAsync(long userId, string chatId, string text, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoMessage>(HttpMethod.Post, $"/messenger/v1/accounts/{userId}/chats/{Esc(chatId)}/messages",
            new SendTextRequest(new SendTextBody(text), "text"), cancellationToken);

    public Task<AvitoMessage> SendImageAsync(long userId, string chatId, string imageId, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoMessage>(HttpMethod.Post, $"/messenger/v1/accounts/{userId}/chats/{Esc(chatId)}/messages/image",
            new SendImageRequest(imageId), cancellationToken);

    public async Task<string> UploadImageAsync(long userId, Stream image, string fileName, string? contentType = null, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var file = new StreamContent(image);
        if (!string.IsNullOrEmpty(contentType))
        {
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
        content.Add(file, "uploadfile[0]", fileName);

        // The response is a { "<image_id>": { sizes… } } map; the key is the id.
        var map = await RequestJsonAsync<Dictionary<string, JsonElement>>(
            HttpMethod.Post, $"/messenger/v1/accounts/{userId}/uploadImages", content, cancellationToken).ConfigureAwait(false);
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

    public Task SubscribeWebhookAsync(string url, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "/messenger/v3/webhook", new UrlRequest(url), cancellationToken);

    public Task UnsubscribeWebhookAsync(string url, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "/messenger/v1/webhook/unsubscribe", new UrlRequest(url), cancellationToken);

    public Task<AvitoSubscriptionList> GetSubscriptionsAsync(CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoSubscriptionList>(HttpMethod.Post, "/messenger/v1/subscriptions", null, cancellationToken);

    // ---- transport ----

    private async Task<T> RequestJsonAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var content = body switch
        {
            null => null,
            HttpContent http => http,
            _ => JsonContent.Create(body, body.GetType(), options: Json),
        };
        using var response = await SendCoreAsync(method, path, content, ct).ConfigureAwait(false);
        var value = await response.Content.ReadFromJsonAsync<T>(Json, ct).ConfigureAwait(false);
        return value ?? throw new AvitoApiException((int)response.StatusCode, null, "Avito returned an empty response body.", null);
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var content = body is null ? null : JsonContent.Create(body, body.GetType(), options: Json);
        using var _ = await SendCoreAsync(method, path, content, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync(_credentials, ct).ConfigureAwait(false);
        using var request = new HttpRequestMessage(method, new Uri(_options.BaseAddress, path)) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                await ThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
            }
        }
        return response;
    }

    private static async Task ThrowApiExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        int? code = null;
        var message = $"Avito API request failed with status {status}.";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("code", out var c) && c.TryGetInt32(out var ci))
                {
                    code = ci;
                }
                if (error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                {
                    message = m.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            // non-JSON error body; keep the generic message
        }
        throw new AvitoApiException(status, code, message, body);
    }

    private static string Esc(string value) => Uri.EscapeDataString(value);
}

// Request bodies (explicit snake_case names, independent of the serializer's naming policy).
file sealed record SendTextRequest([property: JsonPropertyName("message")] SendTextBody Message, [property: JsonPropertyName("type")] string Type);
file sealed record SendTextBody([property: JsonPropertyName("text")] string Text);
file sealed record SendImageRequest([property: JsonPropertyName("image_id")] string ImageId);
file sealed record UrlRequest([property: JsonPropertyName("url")] string Url);
