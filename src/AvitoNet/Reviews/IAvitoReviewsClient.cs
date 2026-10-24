namespace AvitoNet.Reviews;

/// <summary>
/// A client for the Avito Ratings &amp; Reviews API (<c>/ratings/v1</c>) bound to one account's credentials.
/// Create instances via <see cref="IAvitoClientFactory"/> (<see cref="IAvitoClient.Reviews"/>). All calls acquire/reuse a cached OAuth token
/// automatically and throw <see cref="AvitoApiException"/> on API errors.
/// </summary>
public interface IAvitoReviewsClient
{
    /// <summary>Get the seller's aggregate rating (scope <c>ratings:read</c>).</summary>
    Task<AvitoRatingInfo> GetRatingInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a page of published reviews (scope <c>ratings:read</c>). <paramref name="limit"/> must be 1–50.
    /// </summary>
    Task<AvitoReviewsPage> GetReviewsAsync(int offset, int limit, CancellationToken cancellationToken = default);

    /// <summary>Post an answer to a review (scope <c>ratings:write</c>).</summary>
    Task<AvitoReviewAnswerCreated> AnswerReviewAsync(long reviewId, string message, CancellationToken cancellationToken = default);

    /// <summary>Delete a previously posted answer (scope <c>ratings:write</c>). Returns the API's success flag.</summary>
    Task<bool> DeleteAnswerAsync(long answerId, CancellationToken cancellationToken = default);
}
