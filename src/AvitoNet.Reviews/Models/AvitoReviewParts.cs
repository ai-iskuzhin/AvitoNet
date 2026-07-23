namespace AvitoNet.Reviews;

/// <summary>The reviewer.</summary>
public sealed class AvitoReviewSender
{
    public string Name { get; init; } = "";
}

/// <summary>The listing a review is about.</summary>
public sealed class AvitoReviewItem
{
    public long Id { get; init; }
    public string? Title { get; init; }
}

/// <summary>An image attached to a review, in one or more resolutions.</summary>
public sealed class AvitoReviewImage
{
    /// <summary>Ordinal position of the image.</summary>
    public int Number { get; init; }
    public IReadOnlyList<AvitoReviewImageSize> Sizes { get; init; } = [];
}

/// <summary>A single resolution of a review image.</summary>
public sealed class AvitoReviewImageSize
{
    /// <summary>Resolution, e.g. <c>"100x100"</c>.</summary>
    public string Size { get; init; } = "";
    public string Url { get; init; } = "";
}
