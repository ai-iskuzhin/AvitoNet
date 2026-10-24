namespace AvitoNet;

/// <summary>
/// An Avito app's <c>client_id</c> / <c>client_secret</c>.
/// <list type="bullet">
/// <item>With <c>client_credentials</c> (the default): keys the account owner created under «Для профессионалов» →
/// «API» act on <em>their</em> account. Use the company's MAIN account — employee keys fail on most chat calls.</item>
/// <item>With <c>authorization_code</c> (partner apps, see <see cref="Auth.IAvitoOAuthClient"/>): the keys identify
/// your app; the user's tokens decide which account is acted on.</item>
/// </list>
/// </summary>
public sealed record AvitoCredentials(string ClientId, string ClientSecret);
