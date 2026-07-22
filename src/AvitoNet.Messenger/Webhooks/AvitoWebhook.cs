using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AvitoNet.Messenger.Webhooks;

/// <summary>Helpers for handling inbound Avito webhook requests: parse the body and verify the signature.</summary>
public static class AvitoWebhook
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    /// Avito signs webhook deliveries with this header; the exact key/encoding is not fully published, so
    /// confirm the scheme against the current Avito docs before relying on it as your sole check. Defense in
    /// depth: also serve the webhook on an unguessable, per-inbox URL. <see cref="ComputeSignature"/> exposes
    /// the raw computation if a different encoding is required.
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
