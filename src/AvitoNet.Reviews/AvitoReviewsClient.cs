using System.Text.Json.Serialization;
using AvitoNet.Auth;

namespace AvitoNet.Reviews;

/// <summary>Default <see cref="IAvitoReviewsClient"/>. Construct via <see cref="IAvitoReviewsClientFactory"/>.</summary>
public sealed class AvitoReviewsClient : AvitoApiClientBase, IAvitoReviewsClient
{
    public AvitoReviewsClient(HttpClient http, AvitoCredentials credentials, IAvitoTokenProvider tokens, AvitoClientOptions options)
        : base(http, credentials, tokens, options)
    {
    }

    public Task<AvitoRatingInfo> GetRatingInfoAsync(CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoRatingInfo>(HttpMethod.Get, "/ratings/v1/info", null, cancellationToken);

    public Task<AvitoReviewsPage> GetReviewsAsync(int offset, int limit, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoReviewsPage>(HttpMethod.Get, $"/ratings/v1/reviews?offset={offset}&limit={limit}", null, cancellationToken);

    public Task<AvitoReviewAnswerCreated> AnswerReviewAsync(long reviewId, string message, CancellationToken cancellationToken = default) =>
        RequestJsonAsync<AvitoReviewAnswerCreated>(HttpMethod.Post, "/ratings/v1/answers",
            new CreateAnswerRequest(reviewId, message), cancellationToken);

    public async Task<bool> DeleteAnswerAsync(long answerId, CancellationToken cancellationToken = default)
    {
        var result = await RequestJsonAsync<RemoveAnswerResponse>(
            HttpMethod.Delete, $"/ratings/v1/answers/{answerId}", null, cancellationToken).ConfigureAwait(false);
        return result.Success;
    }
}

// Request/response bodies. The ratings API uses camelCase, which the Web serializer produces by default;
// the explicit names below document the wire contract and pin it regardless of policy.
file sealed record CreateAnswerRequest(
    [property: JsonPropertyName("reviewId")] long ReviewId,
    [property: JsonPropertyName("message")] string Message);

file sealed class RemoveAnswerResponse
{
    public bool Success { get; init; }
}
