using AvitoNet.Messenger;
using AvitoNet.Messenger.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace -- conventional home for AddXxx extensions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for the Avito Messenger client.</summary>
public static class AvitoMessengerServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="IAvitoMessengerClientFactory"/> and <see cref="IAvitoTokenProvider"/> (plus the
    /// backing <see cref="HttpClient"/>). Resolve the factory and call <c>Create(credentials)</c> per account.
    /// </summary>
    public static IServiceCollection AddAvitoMessenger(this IServiceCollection services, Action<AvitoMessengerOptions>? configure = null)
    {
        var options = services.AddOptions<AvitoMessengerOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.AddHttpClient(AvitoMessengerClientFactory.HttpClientName, static (sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<AvitoMessengerOptions>>().Value;
            http.BaseAddress = o.BaseAddress;
            http.Timeout = o.Timeout;
        });

        services.TryAddSingleton<IAvitoTokenProvider>(static sp => new ClientCredentialsTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(AvitoMessengerClientFactory.HttpClientName),
            sp.GetRequiredService<IOptions<AvitoMessengerOptions>>()));

        services.TryAddSingleton<IAvitoMessengerClientFactory, AvitoMessengerClientFactory>();

        return services;
    }
}
