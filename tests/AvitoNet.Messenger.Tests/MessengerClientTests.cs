using System.Net;
using System.Text.Json;
using AvitoNet.Messenger.Auth;
using AvitoNet.Messenger.Models;

namespace AvitoNet.Messenger.Tests;

public class MessengerClientTests
{
    private sealed class FixedTokenProvider : IAvitoTokenProvider
    {
        public Task<string> GetAccessTokenAsync(AvitoCredentials credentials, CancellationToken cancellationToken = default) =>
            Task.FromResult("TESTTOKEN");
    }

    private static AvitoMessengerClient Client(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), new AvitoCredentials("id", "secret"), new FixedTokenProvider(), new AvitoMessengerOptions());

    [Fact]
    public async Task SendTextAsync_posts_the_right_path_body_and_bearer()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """{"id":"m1","type":"text","content":{"text":"hi"},"created":1563202313,"direction":"out"}"""));
        var client = Client(handler);

        var sent = await client.SendTextAsync(123, "chat-1", "hi");

        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/messenger/v1/accounts/123/chats/chat-1/messages", req.RequestUri!.AbsolutePath);
        Assert.Equal("TESTTOKEN", req.Headers.Authorization!.Parameter);
        Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);

        using var body = JsonDocument.Parse(handler.Bodies[0]!);
        Assert.Equal("hi", body.RootElement.GetProperty("message").GetProperty("text").GetString());
        Assert.Equal("text", body.RootElement.GetProperty("type").GetString());

        Assert.Equal("m1", sent.Id);
        Assert.Equal(AvitoMessageType.Text, sent.Type);
        Assert.Equal("hi", sent.Content!.Text);
    }

    [Fact]
    public async Task GetMessagesAsync_parses_the_array_and_content()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """[{"id":"m1","author_id":94235311,"type":"text","content":{"text":"hello"},"created":1571654040,"direction":"in","is_read":false}]"""));
        var client = Client(handler);

        var messages = await client.GetMessagesAsync(123, "chat-1", limit: 50);

        Assert.Equal("/messenger/v3/accounts/123/chats/chat-1/messages/", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("limit=50", handler.Requests[0].RequestUri!.Query);
        var m = Assert.Single(messages);
        Assert.Equal(94235311, m.AuthorId);
        Assert.Equal("hello", m.Content!.Text);
        Assert.Equal(AvitoMessageDirection.In, m.Direction);
    }

    [Fact]
    public async Task Api_error_becomes_AvitoApiException_with_code_and_message()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """{"error":{"code":403,"message":"Forbidden"}}""", HttpStatusCode.Forbidden));
        var client = Client(handler);

        var ex = await Assert.ThrowsAsync<AvitoApiException>(() => client.SendTextAsync(1, "c", "x"));
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal(403, ex.ErrorCode);
        Assert.Equal("Forbidden", ex.Message);
    }

    [Fact]
    public async Task SubscribeWebhookAsync_posts_v3_with_the_url()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"ok":true}"""));
        var client = Client(handler);

        await client.SubscribeWebhookAsync("https://example.test/webhooks/avito/abc");

        Assert.Equal("/messenger/v3/webhook", handler.Requests[0].RequestUri!.AbsolutePath);
        using var body = JsonDocument.Parse(handler.Bodies[0]!);
        Assert.Equal("https://example.test/webhooks/avito/abc", body.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public async Task UploadImageAsync_returns_the_image_id_key()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """{"917f2e-abc":{"1280x960":"https://img/x.jpg"}}"""));
        var client = Client(handler);

        using var stream = new MemoryStream([1, 2, 3]);
        var imageId = await client.UploadImageAsync(9, stream, "photo.jpg", "image/jpeg");

        Assert.Equal("917f2e-abc", imageId);
        Assert.Equal("/messenger/v1/accounts/9/uploadImages", handler.Requests[0].RequestUri!.AbsolutePath);
    }
}
