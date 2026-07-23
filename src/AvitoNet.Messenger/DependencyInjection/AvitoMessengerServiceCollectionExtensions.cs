using AvitoNet;
using AvitoNet.Messenger;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace -- conventional home for AddXxx extensions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for the Avito Messenger client.</summary>
public static class AvitoMessengerServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="IAvitoMessengerClientFactory"/> plus the shared AvitoNet token provider/HTTP client
    /// (via <c>AddAvitoCore</c>). Resolve the factory and call <c>Create(credentials)</c> per account.
    /// </summary>
    public static IServiceCollection AddAvitoMessenger(this IServiceCollection services, Action<AvitoClientOptions>? configure = null)
    {
        services.AddAvitoCore(configure);
        services.TryAddSingleton<IAvitoMessengerClientFactory, AvitoMessengerClientFactory>();
        return services;
    }
}
