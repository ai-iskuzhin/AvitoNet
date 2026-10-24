using System.Net;
using System.Text.Json;
using AvitoNet;
using AvitoNet.Auth;
using AvitoNet.Reviews;

namespace AvitoNet.Tests.Reviews;

public class ReviewsClientTests
{
    private sealed class FixedTokenSource : IAvitoTokenSource
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult("TESTTOKEN");
        public void Invalidate() { }
    }

    private static AvitoReviewsClient Client(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), new FixedTokenSource(), new AvitoClientOptions());

    [Fact]
    public async Task GetReviewsAsync_sends_offset_limit_bearer_and_parses_the_page()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """
            {"total":35,"reviews":[{"id":92312343,"score":5,"stage":"fell_through","text":"Отличный продавец!",
            "usedInScore":true,"canAnswer":true,"createdAt":1571654040,"sender":{"name":"Владимир"},
            "item":{"id":1234567,"title":"Автомобиль"},
            "images":[{"number":1,"sizes":[{"size":"100x100","url":"https://img/x.jpg"}]}],
            "extraParams":{"vin":"4Y1SL65848Z411439"},
            "answer":{"id":7,"text":"Спасибо!","status":"published","createdAt":1571654100}}]}
            """));
        var client = Client(handler);

        var page = await client.GetReviewsAsync(offset: 10, limit: 20);

        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("/ratings/v1/reviews", req.RequestUri!.AbsolutePath);
        Assert.Contains("offset=10", req.RequestUri!.Query);
        Assert.Contains("limit=20", req.RequestUri!.Query);
        Assert.Equal("TESTTOKEN", req.Headers.Authorization!.Parameter);
        Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);

        Assert.Equal(35, page.Total);
        var review = Assert.Single(page.Reviews);
        Assert.Equal(92312343, review.Id);
        Assert.Equal(5, review.Score);
        Assert.Equal(AvitoReviewStage.FellThrough, review.Stage);
        Assert.True(review.CanAnswer);
        Assert.True(review.UsedInScore);
        Assert.Equal("Владимир", review.Sender!.Name);
        Assert.Equal(1234567, review.Item!.Id);
        Assert.Equal("4Y1SL65848Z411439", review.ExtraParams!.Vin);
        Assert.Equal(AvitoReviewAnswerStatus.Published, review.Answer!.Status);
        var size = Assert.Single(Assert.Single(review.Images).Sizes);
        Assert.Equal("100x100", size.Size);
    }

    [Fact]
    public async Task GetRatingInfoAsync_parses_the_rating()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """{"isEnabled":true,"rating":{"score":4.3,"reviewsWithScoreCount":12,"reviewsCount":21}}"""));
        var client = Client(handler);

        var info = await client.GetRatingInfoAsync();

        Assert.Equal("/ratings/v1/info", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.True(info.IsEnabled);
        Assert.Equal(4.3, info.Rating!.Score, precision: 3);
        Assert.Equal(12, info.Rating!.ReviewsWithScoreCount);
        Assert.Equal(21, info.Rating!.ReviewsCount);
    }

    [Fact]
    public async Task GetRatingInfoAsync_allows_a_null_rating()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"isEnabled":false}"""));

        var info = await Client(handler).GetRatingInfoAsync();

        Assert.False(info.IsEnabled);
        Assert.Null(info.Rating);
    }

    [Fact]
    public async Task AnswerReviewAsync_posts_camelCase_body_and_parses_the_response()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"id":1234567,"createdAt":1571654040}"""));
        var client = Client(handler);

        var created = await client.AnswerReviewAsync(reviewId: 1234567, message: "Спасибо за отзыв");

        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/ratings/v1/answers", req.RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(handler.Bodies[0]!);
        Assert.Equal(1234567, body.RootElement.GetProperty("reviewId").GetInt64());
        Assert.Equal("Спасибо за отзыв", body.RootElement.GetProperty("message").GetString());

        Assert.Equal(1234567, created.Id);
        Assert.Equal(1571654040, created.CreatedAt);
    }

    [Fact]
    public async Task DeleteAnswerAsync_sends_delete_and_returns_success()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"success":true}"""));
        var client = Client(handler);

        var ok = await client.DeleteAnswerAsync(1234567);

        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Delete, req.Method);
        Assert.Equal("/ratings/v1/answers/1234567", req.RequestUri!.AbsolutePath);
        Assert.True(ok);
    }

    [Fact]
    public async Task Validation_error_surfaces_string_code_and_message()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """{"error":{"code":"validation","message":"Field `offset` is not integer"}}""", HttpStatusCode.BadRequest));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<AvitoApiException>(() => client.GetReviewsAsync(-1, 20));
        Assert.Equal(400, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
        Assert.Equal("validation", ex.ErrorCodeText);
        Assert.Equal("Field `offset` is not integer", ex.Message);
    }
}
