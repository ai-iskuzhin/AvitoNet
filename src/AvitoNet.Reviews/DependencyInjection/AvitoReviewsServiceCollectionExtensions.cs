using AvitoNet;
using AvitoNet.Reviews;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace -- conventional home for AddXxx extensions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration for the Avito Reviews client.</summary>
public static class AvitoReviewsServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="IAvitoReviewsClientFactory"/> plus the shared AvitoNet token provider/HTTP client
    /// (via <c>AddAvitoCore</c>). Resolve the factory and call <c>Create(credentials)</c> per account.
    /// </summary>
    public static IServiceCollection AddAvitoReviews(this IServiceCollection services, Action<AvitoClientOptions>? configure = null)
    {
        services.AddAvitoCore(configure);
        services.TryAddSingleton<IAvitoReviewsClientFactory, AvitoReviewsClientFactory>();
        return services;
    }
}
