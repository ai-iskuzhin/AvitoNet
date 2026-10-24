namespace AvitoNet;

/// <summary>Transport-level configuration shared by every AvitoNet API surface (host, token endpoint, timeouts).</summary>
public class AvitoClientOptions
{
    /// <summary>API base address. Defaults to the production host.</summary>
    public Uri BaseAddress { get; set; } = new("https://api.avito.ru");

    /// <summary>OAuth2 token endpoint (relative to <see cref="BaseAddress"/>).</summary>
    public string TokenPath { get; set; } = "/token";

    /// <summary>OAuth2 authorization page for partner apps (<c>authorization_code</c>).</summary>
    public Uri AuthorizeUrl { get; set; } = new("https://avito.ru/oauth");

    /// <summary>Per-request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long before a token's real expiry it is treated as expired, so a fresh one is fetched ahead of time.
    /// </summary>
    public TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(60);
}
