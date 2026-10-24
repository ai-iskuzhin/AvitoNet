using System.Net;
using System.Text;
using System.Text.Json;
using AvitoNet.Auth;
using AvitoNet.Messenger;
using AvitoNet.Messenger.Models;
using AvitoNet.Messenger.Webhooks;

namespace AvitoNet.Tests.Messenger;

// 0.2.0 behaviour, pinned against Avito's live OpenAPI spec (www.avito.ru/web/1/openapi/info/messenger, 2026-10-24).
public class SpecComplianceTests
{
    private sealed class CountingTokenSource : IAvitoTokenSource
    {
        public int Issued { get; private set; }
        public int Invalidated { get; private set; }
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult($"T{++Issued}");
        public void Invalidate() => Invalidated++;
    }

    private static AvitoMessengerClient Client(FakeHttpMessageHandler handler, IAvitoTokenSource? tokens = null) =>
        new(new HttpClient(handler), tokens ?? new CountingTokenSource(), new AvitoClientOptions());

    [Fact]
    public async Task UploadImage_uses_the_spec_field_name()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"12345.abc":{"640x480":"https://img/x"}}"""));
        var id = await Client(handler).UploadImageAsync(1, new MemoryStream([1, 2, 3]), "a.jpg", "image/jpeg");

        Assert.Equal("12345.abc", id);
        Assert.Contains("name=\"uploadfile[]\"", handler.Bodies[0]);   // NOT uploadfile[0]
        Assert.Equal("/messenger/v1/accounts/1/uploadImages", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task A_rejected_token_is_invalidated_and_the_request_retried_once_with_the_same_body()
    {
        var calls = 0;
        var handler = new FakeHttpMessageHandler(_ => ++calls == 1
            ? FakeHttpMessageHandler.Json("""{"error":{"code":403,"message":"expired"}}""", HttpStatusCode.Forbidden)
            : FakeHttpMessageHandler.Json("""{"id":"m1","type":"text","created":1}"""));
        var tokens = new CountingTokenSource();

        await Client(handler, tokens).SendTextAsync(1, "c", "привет");

        Assert.Equal((1, 2), (tokens.Invalidated, calls));
        Assert.Equal("Bearer T2", handler.Requests[1].Headers.Authorization!.ToString());
        Assert.Equal(handler.Bodies[0], handler.Bodies[1]); // the body was rebuilt, not lost
    }

    [Fact]
    public async Task A_second_rejection_surfaces_instead_of_looping()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"error":{"code":403,"message":"forbidden"}}""", HttpStatusCode.Forbidden));
        var ex = await Assert.ThrowsAsync<AvitoApiException>(() => Client(handler).MarkChatReadAsync(1, "c"));
        Assert.Equal((403, "forbidden", 2), (ex.StatusCode, ex.Message, handler.Requests.Count));
    }

    [Fact]
    public async Task The_wrong_avito_tariff_is_a_typed_402()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"error":{"code":402,"message":"Payment required"}}""", HttpStatusCode.PaymentRequired));
        var ex = await Assert.ThrowsAsync<AvitoSubscriptionRequiredException>(() => Client(handler).SendTextAsync(1, "c", "hi"));
        Assert.Equal(402, ex.StatusCode);
    }

    [Fact]
    public async Task Rate_limits_carry_retry_after()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var r = FakeHttpMessageHandler.Json("""{"error":{"code":429,"message":"slow down"}}""", HttpStatusCode.TooManyRequests);
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return r;
        });
        var ex = await Assert.ThrowsAsync<AvitoRateLimitException>(() => Client(handler).GetChatsAsync(1));
        Assert.Equal(TimeSpan.FromSeconds(7), ex.RetryAfter);
    }

    [Fact]
    public async Task Text_over_1000_chars_fails_fast_without_calling_avito()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("must not be called"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Client(handler).SendTextAsync(1, "c", new string('a', 1001)));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Blacklist_posts_the_spec_body()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("{}"));
        await Client(handler).BlacklistAsync(1, [new AvitoBlacklistEntry(42, ItemId: 777, ReasonId: 2), new AvitoBlacklistEntry(43)]);

        Assert.Equal("/messenger/v2/accounts/1/blacklist", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal("""{"users":[{"user_id":42,"context":{"item_id":777,"reason_id":2}},{"user_id":43,"context":{}}]}""", handler.Bodies[0]);
    }

    [Fact]
    public void Split_respects_the_limit_and_prefers_natural_boundaries()
    {
        var para = new string('а', 600);
        var parts = AvitoText.Split($"{para}\n\n{para}. {para}");
        Assert.All(parts, p => Assert.InRange(p.Length, 1, 1000));
        Assert.Equal(para, parts[0]);                       // cut at the paragraph break, trimmed

        var emoji = string.Concat(Enumerable.Repeat("😀", 600)); // 1200 UTF-16 units, no spaces
        Assert.All(AvitoText.Split(emoji), p => Assert.False(char.IsHighSurrogate(p[^1]))); // never splits a pair
        Assert.Equal(["hi"], AvitoText.Split("  hi  "));
        Assert.Empty(AvitoText.Split("   "));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"id":"x","payload":{"type":"message"}}""")]
    public void Avitos_reachability_probe_is_recognised(string body) =>
        Assert.True(AvitoWebhook.TryParse(Encoding.UTF8.GetBytes(body))!.IsProbe);

    [Fact]
    public void Garbage_bodies_dont_throw()
    {
        Assert.Null(AvitoWebhook.TryParse("not json"u8));
        Assert.Null(AvitoWebhook.TryParse(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void A_spec_example_webhook_parses_with_listing_context()
    {
        var body = """
        {"id":"b3f8","version":"v1.1","timestamp":1729765000,"payload":{"type":"message","value":{
          "id":"msg-1","chat_id":"u2i-abc","user_id":100,"author_id":200,"created":1729764999,"type":"text",
          "chat_type":"u2i","item_id":555,"read":null,"content":{"text":"Здравствуйте, ещё продаёте?"}}}}
        """u8;
        var m = AvitoWebhook.TryParse(body)!.Payload!.Value!;
        Assert.Equal(("u2i-abc", 200L, AvitoMessageType.Text, "Здравствуйте, ещё продаёте?"), (m.ChatId, m.AuthorId, m.Type, m.Content!.Text));
        Assert.False(m.IsOutgoing);
    }
}
