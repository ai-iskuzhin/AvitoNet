using AvitoNet.Auth;
using Microsoft.Extensions.Options;

namespace AvitoNet.Reviews;

/// <summary>Creates <see cref="IAvitoReviewsClient"/> instances bound to specific account credentials.</summary>
public interface IAvitoReviewsClientFactory
{
    /// <summary>Create a client for the given account credentials.</summary>
    IAvitoReviewsClient Create(AvitoCredentials credentials);
}

/// <summary>Default factory. Each created client uses the shared pooled <see cref="HttpClient"/> and token cache.</summary>
public sealed class AvitoReviewsClientFactory : IAvitoReviewsClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAvitoTokenProvider _tokenProvider;
    private readonly AvitoClientOptions _options;

    public AvitoReviewsClientFactory(IHttpClientFactory httpClientFactory, IAvitoTokenProvider tokenProvider, IOptions<AvitoClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    public IAvitoReviewsClient Create(AvitoCredentials credentials) =>
        new AvitoReviewsClient(_httpClientFactory.CreateClient(AvitoHttp.ClientName), credentials, _tokenProvider, _options);
}
