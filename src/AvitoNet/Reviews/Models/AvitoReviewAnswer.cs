using System.Text.Json.Serialization;

namespace AvitoNet.Reviews;

/// <summary>The seller's answer to a review.</summary>
public sealed class AvitoReviewAnswer
{
    public long Id { get; init; }

    public string Text { get; init; } = "";

    /// <summary>Moderation status — one of <see cref="AvitoReviewAnswerStatus"/>.</summary>
    public string Status { get; init; } = "";

    /// <summary>Unix timestamp (seconds) when the answer was created.</summary>
    public long CreatedAt { get; init; }

    /// <summary>Reasons the answer was rejected (present when <see cref="Status"/> is <c>rejected</c>).</summary>
    [JsonPropertyName("reject_reasons")]
    public IReadOnlyList<AvitoRejectReason> RejectReasons { get; init; } = [];
}

/// <summary>A reason an answer was rejected by moderation.</summary>
public sealed class AvitoRejectReason
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
}
