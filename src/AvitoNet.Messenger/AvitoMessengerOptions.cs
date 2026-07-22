namespace AvitoNet.Messenger;

/// <summary>Configuration for the Avito Messenger client.</summary>
public sealed class AvitoMessengerOptions
{
    /// <summary>API base address. Defaults to the production host.</summary>
    public Uri BaseAddress { get; set; } = new("https://api.avito.ru");

    /// <summary>OAuth2 token endpoint (relative to <see cref="BaseAddress"/>).</summary>
    public string TokenPath { get; set; } = "/token";

    /// <summary>Per-request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long before a token's real expiry it is treated as expired, so a fresh one is fetched ahead of time.
    /// </summary>
    public TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(60);
}
