using AvitoNet.Messenger.Auth;
using Microsoft.Extensions.Options;

namespace AvitoNet.Messenger;

/// <summary>Creates <see cref="IAvitoMessengerClient"/> instances bound to specific account credentials.</summary>
public interface IAvitoMessengerClientFactory
{
    /// <summary>Create a client for the given account credentials.</summary>
    IAvitoMessengerClient Create(AvitoCredentials credentials);
}

/// <summary>Default factory. Each created client uses a pooled <see cref="HttpClient"/> and the shared token cache.</summary>
public sealed class AvitoMessengerClientFactory : IAvitoMessengerClientFactory
{
    /// <summary>Name of the <see cref="HttpClient"/> registered by <c>AddAvitoMessenger</c>.</summary>
    public const string HttpClientName = "AvitoNet.Messenger";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAvitoTokenProvider _tokenProvider;
    private readonly AvitoMessengerOptions _options;

    public AvitoMessengerClientFactory(IHttpClientFactory httpClientFactory, IAvitoTokenProvider tokenProvider, IOptions<AvitoMessengerOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    public IAvitoMessengerClient Create(AvitoCredentials credentials) =>
        new AvitoMessengerClient(_httpClientFactory.CreateClient(HttpClientName), credentials, _tokenProvider, _options);
}
