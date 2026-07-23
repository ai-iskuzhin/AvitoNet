using AvitoNet.Auth;
using Microsoft.Extensions.Options;

namespace AvitoNet.Messenger;

/// <summary>Creates <see cref="IAvitoMessengerClient"/> instances bound to specific account credentials.</summary>
public interface IAvitoMessengerClientFactory
{
    /// <summary>Create a client for the given account credentials.</summary>
    IAvitoMessengerClient Create(AvitoCredentials credentials);
}

/// <summary>Default factory. Each created client uses the shared pooled <see cref="HttpClient"/> and token cache.</summary>
public sealed class AvitoMessengerClientFactory : IAvitoMessengerClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAvitoTokenProvider _tokenProvider;
    private readonly AvitoClientOptions _options;

    public AvitoMessengerClientFactory(IHttpClientFactory httpClientFactory, IAvitoTokenProvider tokenProvider, IOptions<AvitoClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    public IAvitoMessengerClient Create(AvitoCredentials credentials) =>
        new AvitoMessengerClient(_httpClientFactory.CreateClient(AvitoHttp.ClientName), credentials, _tokenProvider, _options);
}
