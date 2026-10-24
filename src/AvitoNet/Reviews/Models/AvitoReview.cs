namespace AvitoNet.Reviews;

/// <summary>A published review left for the seller.</summary>
public sealed class AvitoReview
{
    public long Id { get; init; }

    /// <summary>Rating score, 1–5.</summary>
    public int Score { get; init; }

    public string Text { get; init; } = "";

    /// <summary>Deal stage — one of <see cref="AvitoReviewStage"/>.</summary>
    public string Stage { get; init; } = "";

    /// <summary>Whether this review contributes to the seller's score.</summary>
    public bool UsedInScore { get; init; }

    /// <summary>Whether the seller may still post an answer.</summary>
    public bool CanAnswer { get; init; }

    /// <summary>Unix timestamp (seconds) when the review was created.</summary>
    public long CreatedAt { get; init; }

    /// <summary>The seller's answer, if one exists.</summary>
    public AvitoReviewAnswer? Answer { get; init; }

    public AvitoReviewSender? Sender { get; init; }

    public AvitoReviewItem? Item { get; init; }

    public IReadOnlyList<AvitoReviewImage> Images { get; init; } = [];

    public AvitoReviewExtraParams? ExtraParams { get; init; }
}

/// <summary>Optional extra parameters attached to a review (vertical-specific).</summary>
public sealed class AvitoReviewExtraParams
{
    /// <summary>VIN of the car in the listing (autos vertical only).</summary>
    public string? Vin { get; init; }
}
