# AvitoNet.Messenger

A small, dependency-light **.NET client for the [Avito Messenger API](https://developers.avito.ru/api-catalog/messenger/documentation)** — System.Text.Json–native, DI-friendly, and mockable. Read and send chat messages, upload images, and handle inbound webhooks (with signature verification). Built for multi-account use: one set of credentials per Avito account.

> Messenger API access requires a paid Avito subscription (Расширенный / Максимальный, depending on the vertical) and OAuth keys created by the account owner under **«Для профессионалов» → «API»**.

## Install

```
dotnet add package AvitoNet.Messenger
```

Targets `net8.0`.

## Authentication

The SDK uses OAuth2 **`client_credentials`**: each Avito account owner creates a `client_id` / `client_secret`, and those act on *their* account. Tokens are fetched from `https://api.avito.ru/token`, cached per `client_id`, and refreshed automatically before expiry.

## Quick start

Register once:

```csharp
services.AddAvitoMessenger();   // optional: AddAvitoMessenger(o => o.Timeout = TimeSpan.FromSeconds(15));
```

Then create a client per account and use it:

```csharp
public sealed class AvitoChatSync(IAvitoMessengerClientFactory factory)
{
    public async Task ReplyAsync(string clientId, string clientSecret)
    {
        IAvitoMessengerClient avito = factory.Create(new AvitoCredentials(clientId, clientSecret));

        // The account's own id is required by every messenger call.
        var me = await avito.GetSelfAsync();

        var chats = await avito.GetChatsAsync(me.Id, new AvitoChatQuery { UnreadOnly = true });
        foreach (var chat in chats.Chats)
        {
            var messages = await avito.GetMessagesAsync(me.Id, chat.Id, limit: 20);
            await avito.SendTextAsync(me.Id, chat.Id, "Спасибо за сообщение! Скоро ответим.");
            await avito.MarkChatReadAsync(me.Id, chat.Id);
        }
    }
}
```

Sending an image:

```csharp
await using var file = File.OpenRead("photo.jpg");
var imageId = await avito.UploadImageAsync(me.Id, file, "photo.jpg", "image/jpeg");
await avito.SendImageAsync(me.Id, chatId, imageId);
```

## Webhooks

Register your public URL (do this once per account, e.g. at provisioning time):

```csharp
await avito.SubscribeWebhookAsync("https://api.example.com/webhooks/avito/<inbox>");
```

Avito then `POST`s new-message events to that URL. Parse and (optionally) verify them:

```csharp
byte[] body = await ReadRequestBodyAsync();
string? signature = request.Headers["x-avito-messenger-signature"];

if (!AvitoWebhook.VerifySignature(body, signature, webhookSecret))
    return Results.Unauthorized();

AvitoWebhookUpdate? update = AvitoWebhook.Parse(body);
var msg = update?.Payload?.Value;
if (msg is { Type: AvitoMessageType.Text } && !msg.IsOutgoing)
{
    // inbound text from the customer: msg.ChatId, msg.AuthorId, msg.Content!.Text
}
```

`IsOutgoing` is true when the event echoes a message your own account sent (author == receiver) — handy for ignoring your own sends.

> **Signature note:** Avito signs deliveries with the `x-avito-messenger-signature` header; the exact key/encoding isn't fully published, so `VerifySignature` assumes hex HMAC-SHA256 over the raw body. Confirm the scheme against the current docs before relying on it as your only check, and always serve the webhook on an unguessable per-account URL.

## What's covered

`GetSelfAsync` · `GetChatsAsync` / `GetChatAsync` · `GetMessagesAsync` · `SendTextAsync` · `UploadImageAsync` / `SendImageAsync` · `MarkChatReadAsync` · `DeleteMessageAsync` · `GetVoiceFilesAsync` · `SubscribeWebhookAsync` / `UnsubscribeWebhookAsync` / `GetSubscriptionsAsync` · webhook parse + signature.

Errors surface as `AvitoApiException` (`StatusCode`, `ErrorCode`, `ResponseBody`); token failures as `AvitoAuthException`.

## License

MIT.
