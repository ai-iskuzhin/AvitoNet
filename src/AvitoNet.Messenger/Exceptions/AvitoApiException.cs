namespace AvitoNet.Messenger;

/// <summary>Thrown when the Avito API returns a non-success response.</summary>
public class AvitoApiException : Exception
{
    /// <summary>HTTP status code of the failed response.</summary>
    public int StatusCode { get; }

    /// <summary>Avito's <c>error.code</c> from the response body, if present.</summary>
    public int? ErrorCode { get; }

    /// <summary>The raw response body, if any.</summary>
    public string? ResponseBody { get; }

    public AvitoApiException(int statusCode, int? errorCode, string message, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
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
