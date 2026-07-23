# AvitoNet.Reviews

A small, dependency-light **.NET client for the [Avito Ratings & Reviews API](https://developers.avito.ru/api-catalog/ratings/documentation)** (`/ratings/v1`) — System.Text.Json–native, DI-friendly, and mockable. Read your seller rating, list published reviews, and post or delete answers. Built for multi-account use: one set of credentials per Avito account.

Part of the **AvitoNet** SDK family — it builds on [`AvitoNet.Core`](https://www.nuget.org/packages/AvitoNet.Core) for OAuth, so it shares a single cached token with [`AvitoNet.Messenger`](https://www.nuget.org/packages/AvitoNet.Messenger) when both are used for the same account.

> Requires OAuth keys with the `ratings:read` / `ratings:write` scopes, created by the account owner under **«Для профессионалов» → «API»**.

## Install

```
dotnet add package AvitoNet.Reviews
```

Targets `net8.0`. (`AvitoNet.Core` comes with it.)

## Authentication

OAuth2 **`client_credentials`**, identical to the rest of AvitoNet: each account owner creates a `client_id` /
`client_secret`; tokens are fetched from `https://api.avito.ru/token`, cached per `client_id`, and refreshed
automatically. If you also use `AvitoNet.Messenger`, the same token serves both.

## Quick start

Register once:

```csharp
services.AddAvitoReviews();
```

Then create a client per account and use it:

```csharp
public sealed class ReviewResponder(IAvitoReviewsClientFactory factory)
{
    public async Task AnswerNewReviewsAsync(string clientId, string clientSecret)
    {
        IAvitoReviewsClient avito = factory.Create(new AvitoCredentials(clientId, clientSecret));

        var rating = await avito.GetRatingInfoAsync();
        if (rating.Rating is { } r)
        {
            Console.WriteLine($"Score {r.Score} over {r.ReviewsCount} reviews");
        }

        // Paginated: offset + limit (1–50), with Total for paging.
        var page = await avito.GetReviewsAsync(offset: 0, limit: 50);
        foreach (var review in page.Reviews)
        {
            if (review.CanAnswer && review.Answer is null)
            {
                await avito.AnswerReviewAsync(review.Id, "Спасибо за ваш отзыв!");
            }
        }
    }
}
```

Deleting an answer:

```csharp
bool removed = await avito.DeleteAnswerAsync(answerId);
```

## What's covered

`GetRatingInfoAsync` (`GET /ratings/v1/info`) · `GetReviewsAsync` (`GET /ratings/v1/reviews`) ·
`AnswerReviewAsync` (`POST /ratings/v1/answers`) · `DeleteAnswerAsync` (`DELETE /ratings/v1/answers/{id}`).

Review stages (`AvitoReviewStage`) and answer statuses (`AvitoReviewAnswerStatus`) are exposed as string constants.

Errors surface as `AvitoApiException`. The ratings API returns string error codes (e.g. `"validation"`), available
on `ErrorCodeText`; `ErrorCode` holds the numeric code when the API returns one.

## License

MIT.
