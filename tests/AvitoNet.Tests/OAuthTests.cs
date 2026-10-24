using System.Net;
using AvitoNet.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AvitoNet.Tests;

public class OAuthTests
{
    private sealed class Clients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static AvitoOAuthClient OAuth(FakeHttpMessageHandler handler) => new(new Clients(handler), Options.Create(new AvitoClientOptions()));

    private static readonly AvitoCredentials App = new("app-id", "app-secret");

    [Fact]
    public void Authorize_url_is_avitos_partner_flow()
    {
        var url = OAuth(new FakeHttpMessageHandler(_ => throw new InvalidOperationException())).BuildAuthorizeUrl("app-id", ["messenger:read", "messenger:write"], "st 1");
        Assert.Equal("https://avito.ru/oauth?response_type=code&pro_users_flow=true&client_id=app-id&scope=messenger%3Aread%2Cmessenger%3Awrite&state=st%201", url.AbsoluteUri);
    }

    [Fact]
    public async Task Code_exchange_and_refresh_post_the_right_grants()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json(
            """{"access_token":"AT","expires_in":86400,"refresh_token":"RT2","scope":"messenger:read","token_type":"Bearer"}"""));
        var oauth = OAuth(handler);

        var set = await oauth.ExchangeCodeAsync(App, "the-code");
        await oauth.RefreshAsync(App, "RT1");

        Assert.Equal(("AT", "RT2", "messenger:read"), (set.AccessToken, set.RefreshToken, set.Scope));
        Assert.InRange(set.ExpiresAt, DateTimeOffset.UtcNow.AddHours(23), DateTimeOffset.UtcNow.AddHours(25));
        Assert.Equal("grant_type=authorization_code&client_id=app-id&client_secret=app-secret&code=the-code", handler.Bodies[0]);
        Assert.Equal("grant_type=refresh_token&client_id=app-id&client_secret=app-secret&refresh_token=RT1", handler.Bodies[1]);
        Assert.Equal("/token", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task A_failed_exchange_is_an_auth_error()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"error":"invalid_grant"}""", HttpStatusCode.BadRequest));
        var ex = await Assert.ThrowsAsync<AvitoAuthException>(() => OAuth(handler).ExchangeCodeAsync(App, "bad"));
        Assert.Contains("invalid_grant", ex.ResponseBody);
    }

    [Fact]
    public async Task User_token_source_refreshes_near_expiry_and_hands_over_the_rotated_tokens()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"access_token":"AT2","expires_in":86400,"refresh_token":"RT2"}"""));
        AvitoTokenSet? saved = null;
        var source = new AvitoUserTokenSource(OAuth(handler), App,
            new AvitoTokenSet("AT1", "RT1", DateTimeOffset.UtcNow.AddMinutes(2)),   // inside the 5-minute skew
            (t, _) => { saved = t; return Task.CompletedTask; });

        Assert.Equal("AT2", await source.GetAccessTokenAsync());
        Assert.Equal("RT2", saved!.RefreshToken);                 // persist this: RT1 no longer works
        Assert.Equal("AT2", await source.GetAccessTokenAsync());  // cached now
        Assert.Single(handler.Requests);

        source.Invalidate();                                      // Avito rejected AT2 early
        await source.GetAccessTokenAsync();
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("refresh_token=RT2", handler.Bodies[1]);
    }

    [Fact]
    public async Task Concurrent_callers_share_one_refresh()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""{"access_token":"AT2","expires_in":86400,"refresh_token":"RT2"}"""));
        var source = new AvitoUserTokenSource(OAuth(handler), App, new AvitoTokenSet("AT1", "RT1", DateTimeOffset.UtcNow), (_, _) => Task.CompletedTask);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => source.GetAccessTokenAsync()));
        Assert.Single(handler.Requests); // a second refresh with the rotated-away RT1 would fail
    }

    [Fact]
    public void AddAvito_wires_one_factory_for_both_surfaces()
    {
        using var sp = new ServiceCollection().AddAvito().BuildServiceProvider();
        var client = sp.GetRequiredService<IAvitoClientFactory>().Create(App);
        Assert.NotNull(client.Messenger);
        Assert.NotNull(client.Reviews);
        Assert.NotNull(sp.GetRequiredService<IAvitoOAuthClient>());
    }
}
