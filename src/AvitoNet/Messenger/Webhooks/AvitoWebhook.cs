using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AvitoNet.Messenger.Webhooks;

/// <summary>Helpers for handling inbound Avito webhook requests: parse the body and verify the signature.</summary>
public static class AvitoWebhook
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Parse a body without throwing: null for anything that isn't JSON. Avito checks a webhook URL by POSTing <c>{}</c>
    /// (and expects 200 within 2 s) -- that parses to an update whose <see cref="AvitoWebhookUpdate.IsProbe"/> is true.
    /// Always answer 200 quickly, then process off the request path.
    /// </summary>
    public static AvitoWebhookUpdate? TryParse(ReadOnlySpan<byte> body)
    {
        try
        {
            return body.IsEmpty ? null : JsonSerializer.Deserialize<AvitoWebhookUpdate>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Parse a raw webhook request body into a typed update.</summary>
    public static AvitoWebhookUpdate? Parse(string body) =>
        JsonSerializer.Deserialize<AvitoWebhookUpdate>(body, JsonOptions);

    /// <summary>Parse a raw webhook request body into a typed update.</summary>
    public static AvitoWebhookUpdate? Parse(ReadOnlySpan<byte> body) =>
        JsonSerializer.Deserialize<AvitoWebhookUpdate>(body, JsonOptions);

    /// <summary>
    /// Verify the <c>x-avito-messenger-signature</c> header against the raw request body, using HMAC-SHA256
    /// keyed by <paramref name="secret"/> (hex-encoded), with a constant-time comparison.
    /// </summary>
    /// <remarks>
    /// Avito's public webhook docs describe only the "reachable + 200 within 2s" check and do not document a
    /// delivery signature, so treat this as optional: verify only when the header is actually present, and rely
    /// primarily on an unguessable per-account webhook URL. The exact key/encoding isn't published — confirm the
    /// scheme before depending on it. <see cref="ComputeSignature"/> exposes the raw computation if a different
    /// encoding is required.
    /// </remarks>
    public static bool VerifySignature(ReadOnlySpan<byte> body, string? signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(signatureHeader))
        {
            return false;
        }

        var expected = ComputeSignature(body, secret);
        var provided = Encoding.UTF8.GetBytes(signatureHeader.Trim());
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, provided);
    }

    /// <summary>Compute the lowercase hex HMAC-SHA256 of <paramref name="body"/> keyed by <paramref name="secret"/>.</summary>
    public static string ComputeSignature(ReadOnlySpan<byte> body, string secret)
    {
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body, hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
