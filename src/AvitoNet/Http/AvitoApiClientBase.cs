using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AvitoNet.Auth;

namespace AvitoNet;

/// <summary>
/// Transport for the API surfaces: a bearer token per request from the account's <see cref="IAvitoTokenSource"/>,
/// JSON in/out, and non-success responses turned into <see cref="AvitoApiException"/> (402 -> subscription,
/// 429 -> rate limit). If Avito rejects the token (401/403 -- it answers 403 to an expired one), the token is
/// invalidated and the request retried ONCE with a fresh token; request bodies are built by a factory so they can
/// be resent.
/// </summary>
internal abstract class AvitoApiClientBase
{
    private readonly HttpClient _http;
    private readonly IAvitoTokenSource _tokens;

    protected AvitoClientOptions Options { get; }

    protected AvitoApiClientBase(HttpClient http, IAvitoTokenSource tokens, AvitoClientOptions options)
    {
        _http = http;
        _tokens = tokens;
        Options = options;
    }

    /// <summary>Send a request and deserialize the JSON response body into <typeparamref name="T"/>.</summary>
    protected async Task<T> RequestJsonAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct) =>
        await RequestContentAsync<T>(method, path, JsonBody(body), ct).ConfigureAwait(false);

    /// <summary>Like <see cref="RequestJsonAsync{T}(HttpMethod,string,object?,CancellationToken)"/> for non-JSON bodies
    /// (multipart); <paramref name="content"/> builds a fresh body per attempt.</summary>
    protected async Task<T> RequestContentAsync<T>(HttpMethod method, string path, Func<HttpContent?> content, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, path, content, ct).ConfigureAwait(false);
        var value = await response.Content.ReadFromJsonAsync<T>(AvitoJson.Web, ct).ConfigureAwait(false);
        return value ?? throw new AvitoApiException((int)response.StatusCode, null, "Avito returned an empty response body.", null);
    }

    /// <summary>Send a request and ignore the response body.</summary>
    protected async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var _ = await SendCoreAsync(method, path, JsonBody(body), ct).ConfigureAwait(false);
    }

    private static Func<HttpContent?> JsonBody(object? body) =>
        body is null ? static () => null : () => JsonContent.Create(body, body.GetType(), options: AvitoJson.Web);

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, Func<HttpContent?> content, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var token = await _tokens.GetAccessTokenAsync(ct).ConfigureAwait(false);
            using var request = new HttpRequestMessage(method, new Uri(Options.BaseAddress, path)) { Content = content() };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (attempt == 1 && response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                response.Dispose();
                _tokens.Invalidate(); // expired/revoked before its stated lifetime -- one retry with a fresh token
                continue;
            }

            try
            {
                throw await ToExceptionAsync(response, ct).ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
            }
        }
    }

    private static async Task<AvitoApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
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

        return status switch
        {
            402 => new AvitoSubscriptionRequiredException(code, message, body, codeText),
            429 => new AvitoRateLimitException(RetryAfter(response), code, message, body, codeText),
            _ => new AvitoApiException(status, code, message, body, codeText),
        };
    }

    private static TimeSpan? RetryAfter(HttpResponseMessage response) =>
        response.Headers.RetryAfter is { } r
            ? r.Delta ?? (r.Date is { } at ? at - DateTimeOffset.UtcNow : null)
            : null;

    /// <summary>Percent-encode a value for safe inclusion in a URL path/query.</summary>
    protected static string Esc(string value) => Uri.EscapeDataString(value);
}
