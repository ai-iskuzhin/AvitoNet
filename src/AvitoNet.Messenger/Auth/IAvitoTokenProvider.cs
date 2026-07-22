namespace AvitoNet.Messenger.Auth;

/// <summary>Supplies (and caches) OAuth2 access tokens for a set of Avito credentials.</summary>
public interface IAvitoTokenProvider
{
    /// <summary>Get a valid access token for the given credentials, fetching/refreshing as needed.</summary>
    Task<string> GetAccessTokenAsync(AvitoCredentials credentials, CancellationToken cancellationToken = default);
}
