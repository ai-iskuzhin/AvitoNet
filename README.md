# AvitoNet

**Русский** · [English](https://github.com/ai-iskuzhin/AvitoNet/blob/main/README.en.md)

.NET-клиент для [API Авито для бизнеса](https://www.avito.ru/developers). Поддерживает **Мессенджер** (чаты,
сообщения, изображения, голосовые, чёрный список, вебхуки) и **Рейтинги и отзывы**. Сериализация на
System.Text.Json, регистрация через DI, клиенты легко подменить в тестах, работа с многими аккаунтами
одновременно.

```
dotnet add package AvitoNet
```

Целевая платформа — `net8.0`. Лицензия MIT. Один пакет и одна версия: все разделы API используют общий токен
аккаунта.

## Что требует Авито

- **Подписка с доступом к API мессенджера.** Для «Товаров» и «Работы» нужна «Максимальная», для «Услуг» —
  «Расширенная» или выше. Без неё большинство запросов возвращает **402**: SDK выбрасывает
  `AvitoSubscriptionRequiredException`.
- **Основной аккаунт компании.** С ключами или токенами аккаунта сотрудника большинство операций с чатами не
  работает.
- **Первым написать нельзя.** В API Авито нет метода «создать чат»: можно только отвечать в существующем.
- **Текстовое сообщение — не длиннее 1000 символов.** `AvitoText.Split` разбивает длинный текст по абзацам,
  строкам, предложениям и словам.

## Регистрация

```csharp
services.AddAvito();   // при необходимости: AddAvito(o => o.Timeout = TimeSpan.FromSeconds(15));
```

Регистрируются `IAvitoClientFactory`, `IAvitoOAuthClient` и именованный `HttpClient` `AvitoHttp.ClientName`.
Обработчики (прокси, логирование) добавляются через `services.AddHttpClient(AvitoHttp.ClientName)`.

## Авторизация аккаунта

**Ключи аккаунта (`client_credentials`).** Владелец аккаунта создаёт Client ID и Client Secret в разделе
«Для профессионалов» → «API». Токены живут 24 часа; SDK получает их сам и кэширует по `client_id`:

```csharp
IAvitoClient avito = factory.Create(new AvitoCredentials(clientId, clientSecret));
```

**Партнёрское приложение (`authorization_code`).** Это кнопка «Войти через Авито». Такие приложения Авито пока
регистрирует только для доверенных партнёров.

```csharp
// 1. Отправьте пользователя на страницу Авито. Когда он вернётся на зарегистрированный redirect URI, проверьте `state`.
Uri url = oauth.BuildAuthorizeUrl(app.ClientId, ["messenger:read", "messenger:write", "user:read"], state);

// 2. Обменяйте код на токены и сохраните их.
AvitoTokenSet tokens = await oauth.ExchangeCodeAsync(app, code);

// 3. Используйте токены. Каждое обновление выдаёт НОВЫЙ refresh-токен; сохраняйте его в колбэке.
var source = new AvitoUserTokenSource(oauth, app, tokens, (fresh, ct) => store.SaveAsync(accountId, fresh, ct));
IAvitoClient avito = factory.Create(source);
```

Чтобы хранить токены по-своему, реализуйте `IAvitoTokenSource`. Если Авито отклоняет токен (401/403), SDK
сбрасывает его и один раз повторяет запрос с новым.

## Мессенджер

```csharp
var me = await avito.Messenger.GetSelfAsync();   // me.Id — это user_id, который нужен всем методам мессенджера

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

await using var photo = File.OpenRead("photo.jpg");       // JPEG/PNG/HEIC/GIF/BMP, до 24 МБ
var imageId = await avito.Messenger.UploadImageAsync(me.Id, photo, "photo.jpg", "image/jpeg");
await avito.Messenger.SendImageAsync(me.Id, chatId, imageId);
```

По умолчанию `GetChatsAsync` возвращает только чаты по объявлениям (`u2i`). Чтобы получить и чаты по профилю
(`u2u`), передайте `ChatTypes`.

Ещё доступны:
- `GetChatAsync`;
- `DeleteMessageAsync` — своё сообщение, в течение часа после отправки;
- `GetVoiceFilesAsync` — ссылки на голосовые, действуют 1 час;
- `BlacklistAsync`.

### Вебхуки

```csharp
await avito.Messenger.SubscribeWebhookAsync("https://api.example.com/webhooks/avito/<непредсказуемый-id-аккаунта>");
```

**Отвечайте `200 OK` в течение 2 секунд**, иначе Авито отключит подписку. Доступность адреса Авито проверяет
запросом с телом `{}`. Сначала подтвердите получение, потом обрабатывайте:

```csharp
var update = AvitoWebhook.TryParse(body);              // никогда не бросает исключение; null, если это не JSON
if (update is null || update.IsProbe) return Results.Ok();

var msg = update.Payload!.Value!;
if (!msg.IsOutgoing)                                   // пропускаем эхо собственных сообщений
{
    queue.Enqueue(msg);                                // msg.ChatId, msg.AuthorId, msg.Type, msg.Content, msg.ItemId…
}
return Results.Ok();
```

Авито не документирует ни подпись доставки, ни политику повторных отправок.
- **Защита эндпоинта:** непредсказуемый URL для каждого аккаунта.
- **Подстраховка:** периодический опрос `GetChatsAsync(UnreadOnly)`.
- **Подпись:** если в запросах приходит `x-avito-messenger-signature`, её можно проверить через
  `AvitoWebhook.VerifySignature` (hex HMAC-SHA256). Схема не подтверждена, поэтому полагаться на неё не стоит.

## Рейтинги и отзывы

```csharp
var rating = await avito.Reviews.GetRatingInfoAsync();
var page = await avito.Reviews.GetReviewsAsync(offset: 0, limit: 50);
foreach (var review in page.Reviews.Where(r => r.CanAnswer && r.Answer is null))
{
    await avito.Reviews.AnswerReviewAsync(review.Id, "Спасибо за ваш отзыв!");
}
```

## Ошибки

| Исключение | Когда |
|---|---|
| `AvitoApiException` | Любой неуспешный ответ. Содержит `StatusCode`, `ErrorCode` / `ErrorCodeText` и `ResponseBody`. |
| `AvitoAuthException` | Сервер токенов отказал: неверные ключи, недействительный код или refresh-токен. |
| `AvitoSubscriptionRequiredException` | 402: тариф аккаунта не включает этот API. Ключи здесь ни при чём. |
| `AvitoRateLimitException` | 429; в `RetryAfter` — пауза, если Авито её указал. |

## Сборка и тесты

```
dotnet build AvitoNet.slnx
dotnet test  AvitoNet.slnx
```

## Лицензия

MIT.
