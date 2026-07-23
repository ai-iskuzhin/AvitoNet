# AvitoNet

.NET clients for [Avito's API for business](https://developers.avito.ru/) — System.Text.Json–native, DI-friendly,
mockable, and built for multi-account use. Each package is a focused client for one Avito API surface; they share a
single OAuth token cache so using several together costs one token per account.

| Package | Avito API | What it does |
|---|---|---|
| [**AvitoNet.Core**](src/AvitoNet.Core) | — | Shared OAuth2 `client_credentials` token provider (per-account cache), transport base, and options. Comes in transitively; you rarely reference it directly. |
| [**AvitoNet.Messenger**](src/AvitoNet.Messenger) | [Messenger](https://developers.avito.ru/api-catalog/messenger/documentation) | Read/send chat messages, upload images, parse inbound webhooks. |
| [**AvitoNet.Reviews**](src/AvitoNet.Reviews) | [Ratings & Reviews](https://developers.avito.ru/api-catalog/ratings/documentation) | Read seller rating, list reviews (paginated), post/delete answers. |

## Install

```
dotnet add package AvitoNet.Messenger   # or AvitoNet.Reviews — Core comes with either
```

All packages target `net8.0` and are MIT licensed.

## One account, one token

An Avito OAuth token is issued per `client_id` and carries every scope granted to that app
(`messenger:read`, `ratings:write`, …). Registering more than one client shares the cache:

```csharp
services.AddAvitoMessenger();   // both call AddAvitoCore() under the hood…
services.AddAvitoReviews();     // …so one ClientCredentialsTokenProvider backs both

// Same credentials → the Messenger and Reviews clients reuse a single cached token.
var creds = new AvitoCredentials(clientId, clientSecret);
var messenger = messengerFactory.Create(creds);
var reviews   = reviewsFactory.Create(creds);
```

See each package's README for usage. Credentials are created by the account owner under
**«Для профессионалов» → «API»** and require the relevant paid subscription/scopes.

## Build & test

```
dotnet build AvitoNet.slnx
dotnet test  AvitoNet.slnx
```

## License

MIT.
