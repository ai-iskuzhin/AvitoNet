namespace AvitoNet.Reviews;

/// <summary>Seller rating info returned by <c>GET /ratings/v1/info</c>.</summary>
public sealed class AvitoRatingInfo
{
    /// <summary>Whether the rating is enabled for this account.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>The aggregate rating, or <see langword="null"/> when not yet available.</summary>
    public AvitoRating? Rating { get; init; }
}

/// <summary>The seller's aggregate rating.</summary>
public sealed class AvitoRating
{
    /// <summary>Rating score, e.g. 4.3.</summary>
    public double Score { get; init; }

    /// <summary>Number of active reviews that contribute to the score.</summary>
    public int ReviewsWithScoreCount { get; init; }

    /// <summary>Total number of active reviews.</summary>
    public int ReviewsCount { get; init; }
}

/// <summary>A page of published reviews with the total count for pagination.</summary>
public sealed class AvitoReviewsPage
{
    /// <summary>Total number of reviews across all pages.</summary>
    public int Total { get; init; }

    /// <summary>The reviews on this page.</summary>
    public IReadOnlyList<AvitoReview> Reviews { get; init; } = [];
}

/// <summary>Result of posting an answer to a review.</summary>
public sealed class AvitoReviewAnswerCreated
{
    public long Id { get; init; }

    /// <summary>Unix timestamp (seconds) when the answer was created.</summary>
    public long CreatedAt { get; init; }
}
