using AvitoNet;
using AvitoNet.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace -- conventional home for AddXxx extensions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for the shared AvitoNet foundation (token provider + HTTP client).</summary>
public static class AvitoCoreServiceCollectionExtensions
{
    /// <summary>
    /// Register the shared <see cref="IAvitoTokenProvider"/> and the named <see cref="System.Net.Http.HttpClient"/>
    /// used by every AvitoNet client. Idempotent: safe to call from multiple <c>AddAvitoXxx</c> methods — a single
    /// token cache is shared across Messenger, Reviews, and any other AvitoNet client for the same account.
    /// </summary>
    public static IServiceCollection AddAvitoCore(this IServiceCollection services, Action<AvitoClientOptions>? configure = null)
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

        return services;
    }
}
