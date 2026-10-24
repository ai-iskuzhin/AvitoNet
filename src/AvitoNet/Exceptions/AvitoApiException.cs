namespace AvitoNet;

/// <summary>Thrown when the Avito API returns a non-success response.</summary>
public class AvitoApiException : Exception
{
    /// <summary>HTTP status code of the failed response.</summary>
    public int StatusCode { get; }

    /// <summary>Avito's numeric <c>error.code</c> from the response body, when it is an integer.</summary>
    public int? ErrorCode { get; }

    /// <summary>Avito's <c>error.code</c> as text — some APIs return a string code such as <c>"validation"</c>.</summary>
    public string? ErrorCodeText { get; }

    /// <summary>The raw response body, if any.</summary>
    public string? ResponseBody { get; }

    public AvitoApiException(int statusCode, int? errorCode, string message, string? responseBody, string? errorCodeText = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorCodeText = errorCodeText;
        ResponseBody = responseBody;
    }
}

/// <summary>Thrown when acquiring an OAuth token fails (bad credentials, token endpoint error).</summary>
public sealed class AvitoAuthException : AvitoApiException
{
    public AvitoAuthException(int statusCode, string message, string? responseBody)
        : base(statusCode, null, message, responseBody)
    {
    }
}

/// <summary>
/// 402 Payment Required: the account's Avito subscription doesn't include this API. For the Messenger API that's
/// «Максимальная» in Товары / Работа, «Расширенная» or higher in Услуги. Not a credentials problem.
/// </summary>
public sealed class AvitoSubscriptionRequiredException : AvitoApiException
{
    public AvitoSubscriptionRequiredException(int? errorCode, string message, string? responseBody, string? errorCodeText = null)
        : base(402, errorCode, message, responseBody, errorCodeText)
    {
    }
}

/// <summary>429 Too Many Requests. <see cref="RetryAfter"/> is Avito's hint, when it sends one.</summary>
public sealed class AvitoRateLimitException : AvitoApiException
{
    public TimeSpan? RetryAfter { get; }

    public AvitoRateLimitException(TimeSpan? retryAfter, int? errorCode, string message, string? responseBody, string? errorCodeText = null)
        : base(429, errorCode, message, responseBody, errorCodeText)
    {
        RetryAfter = retryAfter;
    }
}
