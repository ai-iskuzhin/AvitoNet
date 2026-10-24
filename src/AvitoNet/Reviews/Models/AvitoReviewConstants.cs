namespace AvitoNet.Reviews;

/// <summary>Values of <see cref="AvitoReview.Stage"/> (the deal stage the review refers to).</summary>
public static class AvitoReviewStage
{
    /// <summary>The deal went through.</summary>
    public const string Done = "done";

    /// <summary>The deal fell through.</summary>
    public const string FellThrough = "fell_through";

    /// <summary>The parties did not agree.</summary>
    public const string NotAgree = "not_agree";

    /// <summary>The parties did not communicate.</summary>
    public const string NotCommunicate = "not_communicate";
}

/// <summary>Values of <see cref="AvitoReviewAnswer.Status"/>.</summary>
public static class AvitoReviewAnswerStatus
{
    /// <summary>Awaiting moderation.</summary>
    public const string Moderation = "moderation";

    /// <summary>Published.</summary>
    public const string Published = "published";

    /// <summary>Rejected by moderation (see <see cref="AvitoReviewAnswer.RejectReasons"/>).</summary>
    public const string Rejected = "rejected";
}
