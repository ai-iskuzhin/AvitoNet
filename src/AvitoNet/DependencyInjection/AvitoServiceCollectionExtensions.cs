using AvitoNet;
using AvitoNet.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace -- conventional home for AddXxx extensions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for AvitoNet.</summary>
public static class AvitoServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="IAvitoClientFactory"/> (resolve it and call <c>Create(...)</c> per account),
    /// <see cref="IAvitoOAuthClient"/> for partner-app sign-in, the shared <c>client_credentials</c> token cache, and the
    /// named <see cref="HttpClient"/> <see cref="AvitoHttp.ClientName"/>. Idempotent.
    /// </summary>
    public static IServiceCollection AddAvito(this IServiceCollection services, Action<AvitoClientOptions>? configure = null)
    {
        var options = services.AddOptions<AvitoClientOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.AddHttpClient(AvitoHttp.ClientName, static (sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<AvitoClientOptions>>().Value;
            http.BaseAddress = o.BaseAddress;
            http.Timeout = o.Timeout;
        });

        services.TryAddSingleton<IAvitoTokenProvider>(static sp => new ClientCredentialsTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(AvitoHttp.ClientName),
            sp.GetRequiredService<IOptions<AvitoClientOptions>>()));
        services.TryAddSingleton<IAvitoOAuthClient, AvitoOAuthClient>();
        services.TryAddSingleton<IAvitoClientFactory, AvitoClientFactory>();
        return services;
    }
}
