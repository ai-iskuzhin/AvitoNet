using AvitoNet.Auth;
using AvitoNet.Messenger;
using AvitoNet.Reviews;
using Microsoft.Extensions.Options;

namespace AvitoNet;

/// <summary>
/// Everything AvitoNet can do for ONE Avito account. The API surfaces share the account's token, so using several
/// costs one token. Create via <see cref="IAvitoClientFactory"/>; cheap to create, safe to keep per account.
/// </summary>
public interface IAvitoClient
{
    /// <summary>Messenger: chats, messages, images, voice, blacklist, webhooks.</summary>
    IAvitoMessengerClient Messenger { get; }

    /// <summary>Ratings &amp; Reviews: seller rating, reviews, answers.</summary>
    IAvitoReviewsClient Reviews { get; }
}

/// <summary>Creates <see cref="IAvitoClient"/>s bound to an account.</summary>
public interface IAvitoClientFactory
{
    /// <summary>An account authorized by its own keys (<c>client_credentials</c>) -- tokens are cached per <c>client_id</c>.</summary>
    IAvitoClient Create(AvitoCredentials credentials);

    /// <summary>An account authorized some other way, e.g. a partner app's user tokens (<see cref="AvitoUserTokenSource"/>).</summary>
    IAvitoClient Create(IAvitoTokenSource tokens);
}

internal sealed class AvitoClient(IAvitoMessengerClient messenger, IAvitoReviewsClient reviews) : IAvitoClient
{
    public IAvitoMessengerClient Messenger { get; } = messenger;
    public IAvitoReviewsClient Reviews { get; } = reviews;
}

internal sealed class AvitoClientFactory(IHttpClientFactory httpClients, IAvitoTokenProvider tokenProvider, IOptions<AvitoClientOptions> options)
    : IAvitoClientFactory
{
    public IAvitoClient Create(AvitoCredentials credentials) => Create(new ClientCredentialsTokenSource(tokenProvider, credentials));

    public IAvitoClient Create(IAvitoTokenSource tokens)
    {
        var http = httpClients.CreateClient(AvitoHttp.ClientName);
        return new AvitoClient(new AvitoMessengerClient(http, tokens, options.Value), new AvitoReviewsClient(http, tokens, options.Value));
    }
}
