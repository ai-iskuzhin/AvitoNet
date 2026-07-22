using System.Text.Json.Serialization;

namespace AvitoNet.Messenger.Models;

/// <summary>The authenticated Avito account (from <c>GET /core/v1/accounts/self</c>). Its <see cref="Id"/> is
/// the <c>user_id</c> every messenger call needs.</summary>
public sealed class AvitoAccount
{
    [JsonPropertyName("id")] public long Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
    [JsonPropertyName("phone")] public string? Phone { get; init; }
    [JsonPropertyName("profile_url")] public string? ProfileUrl { get; init; }
}

/// <summary>Voice-file URLs, keyed by <c>voice_id</c> (from <c>GetVoiceFilesAsync</c>).</summary>
public sealed class AvitoVoiceFiles
{
    [JsonPropertyName("voices_urls")] public IReadOnlyDictionary<string, string> VoicesUrls { get; init; }
        = new Dictionary<string, string>();
}

/// <summary>A registered webhook subscription.</summary>
public sealed class AvitoSubscription
{
    [JsonPropertyName("url")] public string Url { get; init; } = "";
    [JsonPropertyName("version")] public string? Version { get; init; }
}

/// <summary>Wrapper for the subscriptions-list response.</summary>
public sealed class AvitoSubscriptionList
{
    [JsonPropertyName("subscriptions")] public IReadOnlyList<AvitoSubscription> Subscriptions { get; init; } = [];
}
