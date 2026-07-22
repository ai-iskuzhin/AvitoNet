using System.Text;
using AvitoNet.Messenger.Models;
using AvitoNet.Messenger.Webhooks;

namespace AvitoNet.Messenger.Tests;

public class WebhookTests
{
    private const string Body = """
    {
      "id": "u-1",
      "version": "v3.0.0",
      "timestamp": 1710000000,
      "payload": {
        "type": "message",
        "value": {
          "id": "m-1",
          "user_id": 100,
          "author_id": 200,
          "chat_id": "c-1",
          "chat_type": "u2i",
          "type": "text",
          "content": { "text": "hello" },
          "item_id": 555,
          "created": 1710000000,
          "published_at": "2024-03-09T13:33:09Z"
        }
      }
    }
    """;

    [Fact]
    public void Parse_reads_the_message_envelope()
    {
        var update = AvitoWebhook.Parse(Body);

        Assert.NotNull(update);
        Assert.Equal("message", update!.Payload!.Type);
        var msg = update.Payload.Value!;
        Assert.Equal("m-1", msg.Id);
        Assert.Equal("c-1", msg.ChatId);
        Assert.Equal(AvitoChatType.UserToItem, msg.ChatType);
        Assert.Equal(AvitoMessageType.Text, msg.Type);
        Assert.Equal("hello", msg.Content!.Text);
        Assert.Equal(555, msg.ItemId);
        Assert.Equal(200, msg.AuthorId);
        Assert.False(msg.IsOutgoing);   // author 200 != receiver 100 -> inbound from the customer
    }

    [Fact]
    public void Outgoing_echo_is_detected()
    {
        var body = Body.Replace("\"author_id\": 200", "\"author_id\": 100");
        var msg = AvitoWebhook.Parse(body)!.Payload!.Value!;
        Assert.True(msg.IsOutgoing);    // author == receiver
    }

    [Fact]
    public void Signature_roundtrips_and_rejects_tampering()
    {
        var bytes = Encoding.UTF8.GetBytes(Body);
        const string secret = "webhook-secret";

        var signature = AvitoWebhook.ComputeSignature(bytes, secret);

        Assert.True(AvitoWebhook.VerifySignature(bytes, signature, secret));
        Assert.False(AvitoWebhook.VerifySignature(bytes, signature, "other-secret"));
        Assert.False(AvitoWebhook.VerifySignature(bytes, "deadbeef", secret));
        Assert.False(AvitoWebhook.VerifySignature(bytes, null, secret));
    }
}
