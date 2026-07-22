namespace AvitoNet.Messenger;

/// <summary>
/// OAuth2 <c>client_credentials</c> for one Avito account (obtained by the account owner under
/// "Для профессионалов" → "API"). One set of credentials == one Avito account == one messenger inbox.
/// </summary>
public sealed record AvitoCredentials(string ClientId, string ClientSecret);
