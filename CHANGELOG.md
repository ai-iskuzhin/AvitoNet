# Changelog

All notable changes to `AvitoNet` will be documented in this file.

The project uses Semantic Versioning. Versions below `1.0.0` are preview releases and may include public API
changes while the SDK contracts are validated against real Avito accounts. So far the client is verified against
Avito's published OpenAPI specification, not a live account.

## Unreleased

No changes yet.

## 0.1.0-preview.1

First public release: one package covering Avito's Messenger and Ratings & Reviews APIs. This replaces the
unpublished `AvitoNet.Core` / `AvitoNet.Messenger` / `AvitoNet.Reviews` split.

### Added

- `services.AddAvito()` and `IAvitoClientFactory.Create(...)` → `IAvitoClient { Messenger, Reviews }`, with one
  token per account shared by every API surface.
- Auth:
  - `client_credentials` with a per-`client_id` token cache;
  - `authorization_code` / `refresh_token` for partner apps (`IAvitoOAuthClient`, `AvitoUserTokenSource`; refresh
    tokens rotate);
  - the `IAvitoTokenSource` extension point.
- Messenger:
  - chats (v2), messages (v3), send text and image, `uploadImages`, read, delete, voice files, blacklist;
  - webhook subscribe / unsubscribe / list;
  - `AvitoWebhook.TryParse` with `IsProbe` (Avito's `{}` reachability check), and an optional signature helper;
  - `AvitoText.Split` for the 1000-character limit.
- Reviews: rating info, reviews (paginated), answer and delete answer.
- Errors:
  - `AvitoApiException`;
  - `AvitoAuthException`;
  - `AvitoSubscriptionRequiredException` (402: the account's Avito tariff lacks the API);
  - `AvitoRateLimitException` (429, with `RetryAfter`).
- When Avito rejects a token (401/403), it is invalidated and the request retried once.
