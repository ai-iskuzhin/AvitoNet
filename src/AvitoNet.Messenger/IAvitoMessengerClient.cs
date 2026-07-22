using AvitoNet.Messenger.Models;

namespace AvitoNet.Messenger;

/// <summary>
/// A client for the Avito Messenger API bound to one account's credentials. Create instances via
/// <see cref="IAvitoMessengerClientFactory"/>. All calls acquire/reuse a cached OAuth token automatically and
/// throw <see cref="AvitoApiException"/> on API errors. The <c>userId</c> argument is the account's own id
/// (from <see cref="GetSelfAsync"/>).
/// </summary>
public interface IAvitoMessengerClient
{
    /// <summary>Get the authenticated account (its <c>Id</c> is the <c>userId</c> for the other calls).</summary>
    Task<AvitoAccount> GetSelfAsync(CancellationToken cancellationToken = default);

    /// <summary>List the account's chats.</summary>
    Task<AvitoChatList> GetChatsAsync(long userId, AvitoChatQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>Get a single chat by id.</summary>
    Task<AvitoChat> GetChatAsync(long userId, string chatId, CancellationToken cancellationToken = default);

    /// <summary>Get messages in a chat, newest first.</summary>
    Task<IReadOnlyList<AvitoMessage>> GetMessagesAsync(long userId, string chatId, int? limit = null, int? offset = null, CancellationToken cancellationToken = default);

    /// <summary>Send a text message (max 1000 characters). Returns the created message.</summary>
    Task<AvitoMessage> SendTextAsync(long userId, string chatId, string text, CancellationToken cancellationToken = default);

    /// <summary>Send a previously uploaded image (see <see cref="UploadImageAsync"/>).</summary>
    Task<AvitoMessage> SendImageAsync(long userId, string chatId, string imageId, CancellationToken cancellationToken = default);

    /// <summary>Upload an image and get its <c>image_id</c> for <see cref="SendImageAsync"/>.</summary>
    Task<string> UploadImageAsync(long userId, Stream image, string fileName, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>Mark a chat as read.</summary>
    Task MarkChatReadAsync(long userId, string chatId, CancellationToken cancellationToken = default);

    /// <summary>Delete (soft; type becomes <c>deleted</c>) a message sent within the last hour.</summary>
    Task DeleteMessageAsync(long userId, string chatId, string messageId, CancellationToken cancellationToken = default);

    /// <summary>Resolve voice-message ids to playable URLs.</summary>
    Task<AvitoVoiceFiles> GetVoiceFilesAsync(long userId, IEnumerable<string> voiceIds, CancellationToken cancellationToken = default);

    /// <summary>Register a webhook URL to receive new-message notifications (v3).</summary>
    Task SubscribeWebhookAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Remove a previously registered webhook URL.</summary>
    Task UnsubscribeWebhookAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>List the account's active webhook subscriptions.</summary>
    Task<AvitoSubscriptionList> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
}
