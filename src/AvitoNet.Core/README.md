# AvitoNet.Core

Shared foundation for the **AvitoNet** SDK family. You normally don't reference this directly — install
[`AvitoNet.Messenger`](https://www.nuget.org/packages/AvitoNet.Messenger) or
[`AvitoNet.Reviews`](https://www.nuget.org/packages/AvitoNet.Reviews) and this comes with it.

It provides the pieces every Avito API client needs:

- **`AvitoCredentials`** — a `client_id` / `client_secret` pair for one Avito account.
- **`IAvitoTokenProvider` / `ClientCredentialsTokenProvider`** — OAuth2 `client_credentials` tokens fetched from
  `https://api.avito.ru/token`, **cached per `client_id`** and refreshed before expiry. Concurrent callers for the
  same account share a single fetch.
- **`AvitoApiClientBase`** — the JSON/HTTP transport: attaches the bearer token, sends requests, and turns
  non-success responses into `AvitoApiException` (`StatusCode`, `ErrorCode` / `ErrorCodeText`, `ResponseBody`).
- **`AvitoClientOptions`** — base address, token path, timeout, refresh skew.
- **`AddAvitoCore()`** — registers the shared token provider and the named `HttpClient` (`AvitoHttp.ClientName`).

## Why it's shared

An Avito OAuth token is issued per `client_id` and carries **every scope** granted to that app
(`messenger:read`, `ratings:write`, …). So one cached token can serve the Messenger API *and* the Reviews API for
the same account. `AddAvitoCore` is idempotent — calling `AddAvitoMessenger()` and `AddAvitoReviews()` together
wires up **one** token cache shared by both.

Targets `net8.0`. MIT licensed.
