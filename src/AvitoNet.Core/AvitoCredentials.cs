namespace AvitoNet;

/// <summary>
/// OAuth2 <c>client_credentials</c> for one Avito account (created by the account owner under
/// "Для профессионалов" → "API"). One set of credentials == one Avito account; the issued token carries every
/// scope granted to that app, so a single token is shared across all AvitoNet clients for that account.
/// </summary>
public sealed record AvitoCredentials(string ClientId, string ClientSecret);
