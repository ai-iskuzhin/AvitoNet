namespace AvitoNet.Auth;

/// <summary>Supplies (and caches) <c>client_credentials</c> access tokens, one cache entry per <c>client_id</c>.</summary>
public interface IAvitoTokenProvider
{
    /// <summary>Get a valid access token for the given credentials, fetching/refreshing as needed.</summary>
    Task<string> GetAccessTokenAsync(AvitoCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>Drop the cached token (e.g. Avito rejected it before its stated expiry); the next call fetches a new one.</summary>
    void Invalidate(AvitoCredentials credentials);
}

/// <summary>
/// The access token for ONE account, however it is obtained: <c>client_credentials</c> (built in, see
/// <see cref="IAvitoTokenProvider"/>) or a user's <c>authorization_code</c> tokens (<see cref="AvitoUserTokenSource"/>).
/// Implement it to plug in your own token storage.
/// </summary>
public interface IAvitoTokenSource
{
    /// <summary>A currently valid access token.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Avito rejected the token (401/403): forget it so the next call obtains a fresh one.</summary>
    void Invalidate();
}

internal sealed class ClientCredentialsTokenSource(IAvitoTokenProvider provider, AvitoCredentials credentials) : IAvitoTokenSource
{
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        provider.GetAccessTokenAsync(credentials, cancellationToken);

    public void Invalidate() => provider.Invalidate(credentials);
}
