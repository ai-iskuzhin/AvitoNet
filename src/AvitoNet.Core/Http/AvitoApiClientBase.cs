using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AvitoNet.Auth;

namespace AvitoNet;

/// <summary>
/// Base for typed AvitoNet API clients: acquires a bearer token per request, sends JSON, and turns non-success
/// responses into <see cref="AvitoApiException"/>. One instance is bound to one account's credentials.
/// </summary>
public abstract class AvitoApiClientBase
{
    private readonly HttpClient _http;
    private readonly AvitoCredentials _credentials;
    private readonly IAvitoTokenProvider _tokens;

    /// <summary>Transport options (base address, timeouts).</summary>
    protected AvitoClientOptions Options { get; }

    protected AvitoApiClientBase(HttpClient http, AvitoCredentials credentials, IAvitoTokenProvider tokens, AvitoClientOptions options)
    {
        _http = http;
        _credentials = credentials;
        _tokens = tokens;
        Options = options;
    }

    /// <summary>Send a request and deserialize the JSON response body into <typeparamref name="T"/>.</summary>
    protected async Task<T> RequestJsonAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var content = body switch
        {
            null => null,
            HttpContent http => http,
            _ => JsonContent.Create(body, body.GetType(), options: AvitoJson.Web),
        };
        using var response = await SendCoreAsync(method, path, content, ct).ConfigureAwait(false);
        var value = await response.Content.ReadFromJsonAsync<T>(AvitoJson.Web, ct).ConfigureAwait(false);
        return value ?? throw new AvitoApiException((int)response.StatusCode, null, "Avito returned an empty response body.", null);
    }

    /// <summary>Send a request and ignore the response body (for fire-and-forget actions).</summary>
    protected async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var content = body is null ? null : JsonContent.Create(body, body.GetType(), options: AvitoJson.Web);
        using var _ = await SendCoreAsync(method, path, content, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync(_credentials, ct).ConfigureAwait(false);
        using var request = new HttpRequestMessage(method, new Uri(Options.BaseAddress, path)) { Content = content };
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
        string? codeText = null;
        var message = $"Avito API request failed with status {status}.";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
            {
                if (error.TryGetProperty("code", out var c))
                {
                    if (c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var ci))
                    {
                        code = ci;
                        codeText = ci.ToString();
                    }
                    else if (c.ValueKind == JsonValueKind.String)
                    {
                        codeText = c.GetString();
                    }
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
        throw new AvitoApiException(status, code, message, body, codeText);
    }

    /// <summary>Percent-encode a value for safe inclusion in a URL path/query.</summary>
    protected static string Esc(string value) => Uri.EscapeDataString(value);
}
