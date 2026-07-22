using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AvitoNet.Messenger.Auth;

/// <summary>
/// OAuth2 <c>client_credentials</c> token provider. Tokens are cached per <c>client_id</c> and refreshed a
/// little before expiry; concurrent callers for the same account share a single token fetch. Thread-safe and
/// safe to register as a singleton.
/// </summary>
public sealed class ClientCredentialsTokenProvider : IAvitoTokenProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly AvitoMessengerOptions _options;
    private readonly ConcurrentDictionary<string, CachedToken> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public ClientCredentialsTokenProvider(HttpClient http, IOptions<AvitoMessengerOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(AvitoCredentials credentials, CancellationToken cancellationToken = default)
    {
        if (TryGetFresh(credentials.ClientId, out var token))
        {
            return token;
        }

        var gate = _locks.GetOrAdd(credentials.ClientId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check: another caller may have refreshed while we waited.
            if (TryGetFresh(credentials.ClientId, out token))
            {
                return token;
            }
            return await FetchAsync(credentials, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetFresh(string clientId, out string token)
    {
        if (_cache.TryGetValue(clientId, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            token = cached.Token;
            return true;
        }
        token = "";
        return false;
    }

    private async Task<string> FetchAsync(AvitoCredentials credentials, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseAddress, _options.TokenPath))
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = credentials.ClientId,
                ["client_secret"] = credentials.ClientSecret,
            }),
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new AvitoAuthException((int)response.StatusCode,
                $"Avito token request failed with status {(int)response.StatusCode}.", body);
        }

        var token = await response.Content.ReadFromJsonAsync<AvitoTokenResponse>(Json, cancellationToken).ConfigureAwait(false);
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            throw new AvitoAuthException((int)response.StatusCode, "Avito token response contained no access_token.", null);
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) - _options.TokenRefreshSkew;
        _cache[credentials.ClientId] = new CachedToken(token.AccessToken, expiresAt);
        return token.AccessToken;
    }

    private readonly record struct CachedToken(string Token, DateTimeOffset ExpiresAt);
}
