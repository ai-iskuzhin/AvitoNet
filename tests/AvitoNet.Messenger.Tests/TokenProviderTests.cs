using System.Net;
using AvitoNet.Messenger.Auth;
using Microsoft.Extensions.Options;

namespace AvitoNet.Messenger.Tests;

public class TokenProviderTests
{
    private static ClientCredentialsTokenProvider Provider(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(new AvitoMessengerOptions()));

    [Fact]
    public async Task Fetches_then_caches_the_token_per_client_id()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json("""{"access_token":"AAA","token_type":"Bearer","expires_in":86400}"""));
        var provider = Provider(handler);
        var creds = new AvitoCredentials("client-1", "secret");

        var first = await provider.GetAccessTokenAsync(creds);
        var second = await provider.GetAccessTokenAsync(creds);

        Assert.Equal("AAA", first);
        Assert.Equal("AAA", second);
        Assert.Single(handler.Requests);                                   // second call served from cache
        Assert.EndsWith("/token", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("grant_type=client_credentials", handler.Bodies[0]);
        Assert.Contains("client_id=client-1", handler.Bodies[0]);
    }

    [Fact]
    public async Task Concurrent_callers_for_the_same_account_share_one_fetch()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json("""{"access_token":"AAA","expires_in":86400}"""));
        var provider = Provider(handler);
        var creds = new AvitoCredentials("client-1", "secret");

        var tokens = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => provider.GetAccessTokenAsync(creds)));

        Assert.All(tokens, t => Assert.Equal("AAA", t));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Different_accounts_get_different_tokens()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            var body = req.Content!.ReadAsStringAsync().Result;
            var token = body.Contains("client_id=a") ? "TA" : "TB";
            return FakeHttpMessageHandler.Json($$"""{"access_token":"{{token}}","expires_in":86400}""");
        });
        var provider = Provider(handler);

        Assert.Equal("TA", await provider.GetAccessTokenAsync(new AvitoCredentials("a", "s")));
        Assert.Equal("TB", await provider.GetAccessTokenAsync(new AvitoCredentials("b", "s")));
    }

    [Fact]
    public async Task Throws_auth_exception_on_error_status()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            FakeHttpMessageHandler.Json("""{"error":"invalid_client"}""", HttpStatusCode.Unauthorized));
        var provider = Provider(handler);

        var ex = await Assert.ThrowsAsync<AvitoAuthException>(() =>
            provider.GetAccessTokenAsync(new AvitoCredentials("x", "y")));
        Assert.Equal(401, ex.StatusCode);
    }
}
