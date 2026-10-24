# AvitoNet

[Русский](README.md) · **English**

A .NET client for [Avito's API for business](https://www.avito.ru/developers): **Messenger** (chats, messages,
images, voice, blacklist, webhooks) and **Ratings & Reviews**. System.Text.Json-native, DI-friendly, mockable,
built for many accounts at once.

```
dotnet add package AvitoNet
```

Targets `net8.0`. MIT licensed. One package, one version: every API surface shares the account's token.

## Before you start: what Avito requires

- **A subscription that includes the Messenger API.** That's «Максимальная» for Товары and Работа, or «Расширенная»
  and above for Услуги. Without it most calls return **402**, surfaced as `AvitoSubscriptionRequiredException`.
- **The company's main account.** Keys or tokens of an employee sub-account fail on most chat operations.
- **No starting conversations.** Avito has no "create chat" call; you can only answer in an existing chat.
- **Text replies are at most 1000 characters.** `AvitoText.Split` cuts longer text on natural boundaries.

## Register

```csharp
services.AddAvito();   // optional: AddAvito(o => o.Timeout = TimeSpan.FromSeconds(15));
```

That registers `IAvitoClientFactory`, `IAvitoOAuthClient` and the named `HttpClient` `AvitoHttp.ClientName`. Add
handlers to that client, such as a proxy or logging, with `services.AddHttpClient(AvitoHttp.ClientName)`.

## Authorize an account

**Account keys (`client_credentials`).** The account owner creates a Client ID and Client Secret under
«Для профессионалов» → «API». Tokens (24 h) are fetched and cached per `client_id`:

```csharp
IAvitoClient avito = factory.Create(new AvitoCredentials(clientId, clientSecret));
```

**Partner app (`authorization_code`).** "Войти через Авито". Avito registers apps for this flow for trusted
partners only.

```csharp
// 1. Send the user to Avito (verify `state` when they come back to your registered redirect URI).
Uri url = oauth.BuildAuthorizeUrl(app.ClientId, ["messenger:read", "messenger:write", "user:read"], state);

// 2. Exchange the code, store the tokens.
AvitoTokenSet tokens = await oauth.ExchangeCodeAsync(app, code);

// 3. Use them. Every refresh returns a NEW refresh token -- persist it in the callback.
var source = new AvitoUserTokenSource(oauth, app, tokens, (fresh, ct) => store.SaveAsync(accountId, fresh, ct));
IAvitoClient avito = factory.Create(source);
```

Bring your own token storage by implementing `IAvitoTokenSource`. If Avito rejects a token (401/403), it's
invalidated and the request retried once with a fresh one.

## Messenger

```csharp
var me = await avito.Messenger.GetSelfAsync();   // me.Id is the user_id every messenger call needs

var chats = await avito.Messenger.GetChatsAsync(me.Id, new AvitoChatQuery { UnreadOnly = true, ChatTypes = ["u2i", "u2u"] });
foreach (var chat in chats.Chats)
{
    var history = await avito.Messenger.GetMessagesAsync(me.Id, chat.Id, limit: 20);
    foreach (var part in AvitoText.Split("Спасибо за сообщение! …"))
    {
        await avito.Messenger.SendTextAsync(me.Id, chat.Id, part);
    }

    await avito.Messenger.MarkChatReadAsync(me.Id, chat.Id);
}

await using var photo = File.OpenRead("photo.jpg");       // JPEG/PNG/HEIC/GIF/BMP, ≤ 24 MB
var imageId = await avito.Messenger.UploadImageAsync(me.Id, photo, "photo.jpg", "image/jpeg");
await avito.Messenger.SendImageAsync(me.Id, chatId, imageId);
```

`GetChatsAsync` returns only listing chats (`u2i`) by default; pass `ChatTypes` for profile chats (`u2u`) too.
Also available: `GetChatAsync`, `DeleteMessageAsync` (your own message, within 1 hour), `GetVoiceFilesAsync`
(playable links, valid for 1 hour), `BlacklistAsync`.

### Webhooks

```csharp
await avito.Messenger.SubscribeWebhookAsync("https://api.example.com/webhooks/avito/<unguessable-per-account-id>");
```

**Answer `200 OK` within 2 seconds**, or Avito drops the subscription. Avito checks the URL by posting `{}`.
ACK first and process afterwards:

```csharp
var update = AvitoWebhook.TryParse(body);              // never throws; null for non-JSON
if (update is null || update.IsProbe) return Results.Ok();

var msg = update.Payload!.Value!;
if (!msg.IsOutgoing)                                   // skip echoes of your own sends
{
    queue.Enqueue(msg);                                // msg.ChatId, msg.AuthorId, msg.Type, msg.Content, msg.ItemId…
}
return Results.Ok();
```

Avito doesn't document a delivery signature or a retry policy. Protect the endpoint with an unguessable per-account
URL, and poll `GetChatsAsync(UnreadOnly)` as a safety net. If your deliveries carry `x-avito-messenger-signature`,
`AvitoWebhook.VerifySignature` checks it as hex HMAC-SHA256. That scheme is unconfirmed, so don't depend on it.

## Ratings & Reviews

```csharp
var rating = await avito.Reviews.GetRatingInfoAsync();
var page = await avito.Reviews.GetReviewsAsync(offset: 0, limit: 50);
foreach (var review in page.Reviews.Where(r => r.CanAnswer && r.Answer is null))
{
    await avito.Reviews.AnswerReviewAsync(review.Id, "Спасибо за ваш отзыв!");
}
```

## Errors

| Exception | When |
|---|---|
| `AvitoApiException` | Any non-success response. Carries `StatusCode`, `ErrorCode` / `ErrorCodeText` and `ResponseBody`. |
| `AvitoAuthException` | The token endpoint refused (bad keys, invalid code or refresh token). |
| `AvitoSubscriptionRequiredException` | 402: the account's tariff doesn't include this API. Not a credentials problem. |
| `AvitoRateLimitException` | 429, with `RetryAfter` when Avito sends it. |

## Build & test

```
dotnet build AvitoNet.slnx
dotnet test  AvitoNet.slnx
```

## License

MIT.
