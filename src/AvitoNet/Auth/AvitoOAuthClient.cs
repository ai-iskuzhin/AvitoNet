using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace AvitoNet.Auth;

/// <summary>
/// A user's tokens from the <c>authorization_code</c> flow. Avito access tokens live 24 hours, and every refresh
/// returns a NEW refresh token -- persist both (see <see cref="AvitoUserTokenSource"/>'s callback).
/// </summary>
public sealed record AvitoTokenSet(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, string? Scope = null);

/// <summary>
/// OAuth2 <c>authorization_code</c> for Avito partner apps ("Войти через Авито"): send the user to
/// <see cref="BuildAuthorizeUrl"/>, exchange the returned <c>code</c>, refresh as needed. Avito registers apps for this
/// flow for trusted partners only; without one, use <c>client_credentials</c> keys the account owner creates.
/// </summary>
public interface IAvitoOAuthClient
{
    /// <summary>
    /// The page to send the user to. Avito redirects back to the redirect URI registered for the app with
    /// <c>code</c> and <c>state</c>; always verify <paramref name="state"/> on return.
    /// </summary>
    Uri BuildAuthorizeUrl(string clientId, IEnumerable<string> scopes, string state);

    /// <summary>Exchange an authorization code for the user's tokens.</summary>
    Task<AvitoTokenSet> ExchangeCodeAsync(AvitoCredentials app, string code, CancellationToken cancellationToken = default);

    /// <summary>Refresh a user's tokens. The returned refresh token REPLACES the old one.</summary>
    Task<AvitoTokenSet> RefreshAsync(AvitoCredentials app, string refreshToken, CancellationToken cancellationToken = default);
}

internal sealed class AvitoOAuthClient(IHttpClientFactory httpClients, IOptions<AvitoClientOptions> options) : IAvitoOAuthClient
{
    private AvitoClientOptions O => options.Value;

    public Uri BuildAuthorizeUrl(string clientId, IEnumerable<string> scopes, string state)
    {
        var query = string.Join("&",
            "response_type=code",
            "pro_users_flow=true",
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"scope={Uri.EscapeDataString(string.Join(",", scopes))}",
            $"state={Uri.EscapeDataString(state)}");
        return new UriBuilder(O.AuthorizeUrl) { Query = query }.Uri;
    }

    public Task<AvitoTokenSet> ExchangeCodeAsync(AvitoCredentials app, string code, CancellationToken cancellationToken = default) =>
        RequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = app.ClientId,
            ["client_secret"] = app.ClientSecret,
            ["code"] = code,
        }, cancellationToken);

    public Task<AvitoTokenSet> RefreshAsync(AvitoCredentials app, string refreshToken, CancellationToken cancellationToken = default) =>
        RequestAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = app.ClientId,
            ["client_secret"] = app.ClientSecret,
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

    private async Task<AvitoTokenSet> RequestAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(O.BaseAddress, O.TokenPath)) { Content = new FormUrlEncodedContent(form) };
        using var response = await httpClients.CreateClient(AvitoHttp.ClientName).SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new AvitoAuthException((int)response.StatusCode, $"Avito token request ({form["grant_type"]}) failed with status {(int)response.StatusCode}.", body);
        }

        var token = System.Text.Json.JsonSerializer.Deserialize<AvitoTokenResponse>(body, AvitoJson.Web);
        if (token is not { AccessToken.Length: > 0, RefreshToken.Length: > 0 })
        {
            throw new AvitoAuthException((int)response.StatusCode, "Avito token response lacked access_token or refresh_token.", body);
        }

        return new AvitoTokenSet(token.AccessToken, token.RefreshToken!, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn), token.Scope);
    }
}

/// <summary>
/// An <see cref="IAvitoTokenSource"/> over a user's <c>authorization_code</c> tokens: refreshes ahead of expiry (and
/// when Avito rejects the token), serialized per instance, and hands every new token set to <paramref name="onRefreshed"/>
/// so you can persist it -- the old refresh token stops working after a refresh.
/// </summary>
public sealed class AvitoUserTokenSource(
    IAvitoOAuthClient oauth,
    AvitoCredentials app,
    AvitoTokenSet tokens,
    Func<AvitoTokenSet, CancellationToken, Task> onRefreshed,
    TimeSpan? refreshSkew = null) : IAvitoTokenSource
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _skew = refreshSkew ?? TimeSpan.FromMinutes(5);
    private AvitoTokenSet _tokens = tokens;
    private bool _invalid;

    /// <summary>The current token set (after any refresh).</summary>
    public AvitoTokenSet Current => _tokens;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_invalid && _tokens.ExpiresAt - _skew > DateTimeOffset.UtcNow)
        {
            return _tokens.AccessToken;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_invalid && _tokens.ExpiresAt - _skew > DateTimeOffset.UtcNow)
            {
                return _tokens.AccessToken; // another caller refreshed while we waited
            }

            var fresh = await oauth.RefreshAsync(app, _tokens.RefreshToken, cancellationToken).ConfigureAwait(false);
            _tokens = fresh;
            _invalid = false;
            await onRefreshed(fresh, cancellationToken).ConfigureAwait(false);
            return fresh.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => _invalid = true;
}
