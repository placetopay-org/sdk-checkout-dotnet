using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PlaceToPay.Checkout.Errors;
using PlaceToPay.Checkout.Http;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Session;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Http;

/// <summary>
/// Integration-style tests for <see cref="HttpTransport"/>. Uses a stub
/// <see cref="HttpMessageHandler"/> so we exercise the real retry loop,
/// auth injection, Retry-After parsing and typed-error mapping without
/// making a network request.
/// </summary>
public class HttpTransportTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders;
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> RequestBodies { get; } = new();

        public StubHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders)
        {
            _responders = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responders);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }
            if (_responders.Count == 0)
            {
                throw new InvalidOperationException("Test did not queue enough responses.");
            }
            return _responders.Dequeue()(request);
        }
    }

    private static CheckoutClientOptions OptionsWith(HttpClient http) => new()
    {
        Login = "test-login",
        SecretKey = "test-secret",
        Environment = PlaceToPay.Checkout.Environment.Sandbox,
        Country = Country.Colombia,
        MaxRetryAttempts = 3,
        InitialRetryDelay = TimeSpan.FromMilliseconds(1),
        MaxRetryDelay = TimeSpan.FromMilliseconds(5),
        HttpClient = http,
    };

    private static HttpResponseMessage Json(HttpStatusCode code, string body, IDictionary<string, string>? headers = null)
    {
        var msg = new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (headers is not null)
        {
            foreach (var (k, v) in headers)
            {
                msg.Headers.TryAddWithoutValidation(k, v);
            }
        }
        return msg;
    }

    private const string OkBody = """
    {
      "requestId": 42,
      "status": {"status":"ok","reason":"00","message":"ok","date":"2026-04-22T15:30:45+00:00"}
    }
    """;

    // ─── Retry policy ──────────────────────────────────────────────────

    [Fact]
    public async Task Post_RetriesOn500_ThenSucceeds()
    {
        var stub = new StubHandler(
            _ => Json(HttpStatusCode.InternalServerError, "{}"),
            _ => Json(HttpStatusCode.OK, OkBody));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        var info = await t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None);
        Assert.Equal(42, info.RequestId);
        Assert.Equal(2, stub.Requests.Count);
    }

    [Fact]
    public async Task Post_FailsAfterMaxAttemptsOn500()
    {
        var stub = new StubHandler(
            _ => Json(HttpStatusCode.InternalServerError, "{}"),
            _ => Json(HttpStatusCode.InternalServerError, "{}"),
            _ => Json(HttpStatusCode.InternalServerError, "{}"));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        var ex = await Assert.ThrowsAsync<ServerException>(
            () => t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None));
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal(3, stub.Requests.Count);
    }

    [Fact]
    public async Task Post_DoesNotRetryOn4xx()
    {
        const string body = """{"status":{"status":"failed","reason":"?X","message":"Bad"}}""";
        var stub = new StubHandler(_ => Json(HttpStatusCode.BadRequest, body));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        await Assert.ThrowsAsync<ValidationException>(
            () => t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None));
        Assert.Single(stub.Requests);
    }

    [Fact]
    public async Task Post_MapsStatusCodesToTypedExceptions()
    {
        (HttpStatusCode code, Type ex)[] cases =
        [
            (HttpStatusCode.BadRequest, typeof(ValidationException)),
            (HttpStatusCode.Unauthorized, typeof(AuthenticationException)),
            (HttpStatusCode.NotFound, typeof(NotFoundException)),
            ((HttpStatusCode)418, typeof(PlaceToPayException)),
        ];
        foreach (var (code, expectedType) in cases)
        {
            var stub = new StubHandler(_ => Json(code, """{"status":{"status":"error"}}"""));
            using var http = new HttpClient(stub);
            using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
            var caught = await Record.ExceptionAsync(
                () => t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None));
            Assert.NotNull(caught);
            Assert.IsType(expectedType, caught);
        }
    }

    [Fact]
    public async Task Post_RateLimit_ExposesRetryAfter()
    {
        var stub = new StubHandler(
            _ => Json(HttpStatusCode.TooManyRequests, "{}",
                headers: new Dictionary<string, string> { ["Retry-After"] = "7" }),
            _ => Json(HttpStatusCode.TooManyRequests, "{}",
                headers: new Dictionary<string, string> { ["Retry-After"] = "7" }),
            _ => Json(HttpStatusCode.TooManyRequests, "{}",
                headers: new Dictionary<string, string> { ["Retry-After"] = "7" }));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        var ex = await Assert.ThrowsAsync<RateLimitException>(
            () => t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None));
        Assert.NotNull(ex.RetryAfter);
    }

    // ─── Auth injection ────────────────────────────────────────────────

    [Fact]
    public async Task Post_InjectsAuthBlockOnEveryRequest()
    {
        var stub = new StubHandler(
            _ => Json(HttpStatusCode.OK, OkBody),
            _ => Json(HttpStatusCode.OK, OkBody));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        await t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None);
        await t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None);

        var body1 = JsonDocument.Parse(stub.RequestBodies[0]);
        var body2 = JsonDocument.Parse(stub.RequestBodies[1]);
        Assert.True(body1.RootElement.TryGetProperty("auth", out var auth1));
        Assert.True(body2.RootElement.TryGetProperty("auth", out var auth2));
        Assert.Equal("test-login", auth1.GetProperty("login").GetString());
        // Fresh nonces per request.
        Assert.NotEqual(auth1.GetProperty("nonce").GetString(),
                        auth2.GetProperty("nonce").GetString());
    }

    [Fact]
    public async Task Post_AdditionalFlowsToWire()
    {
        var stub = new StubHandler(_ => Json(HttpStatusCode.OK, OkBody));
        var options = new CheckoutClientOptions
        {
            Login = "L",
            SecretKey = "S",
            Environment = PlaceToPay.Checkout.Environment.Sandbox,
            HttpClient = new HttpClient(stub),
            AuthAdditional = new Dictionary<string, string> { ["tenant"] = "acme" },
            MaxRetryAttempts = 1,
        };
        using var t = new HttpTransport(options, NullLogger.Instance);
        await t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None);

        var body = JsonDocument.Parse(stub.RequestBodies[0]);
        var additional = body.RootElement.GetProperty("auth").GetProperty("additional");
        Assert.Equal("acme", additional.GetProperty("tenant").GetString());
    }

    [Fact]
    public async Task Post_SetsUserAgentHeader()
    {
        var stub = new StubHandler(_ => Json(HttpStatusCode.OK, OkBody));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        await t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None);
        var ua = stub.Requests[0].Headers.UserAgent.ToString();
        Assert.Contains("placetopay-sdk-dotnet/", ua, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_IdempotencyKeyFlowsToHeader()
    {
        var stub = new StubHandler(_ => Json(HttpStatusCode.OK, OkBody));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        await t.PostAsync<SessionInformation>("/api/collect", new { foo = "bar" }, "IDEM-1", CancellationToken.None);
        Assert.True(stub.Requests[0].Headers.TryGetValues("X-Idempotency-Key", out var values));
        Assert.Equal("IDEM-1", values!.First());
    }

    [Fact]
    public async Task Post_NetworkFailureWrappedAsNetworkException()
    {
        var stub = new StubHandler(
            _ => throw new HttpRequestException("dns failure"),
            _ => throw new HttpRequestException("dns failure"),
            _ => throw new HttpRequestException("dns failure"));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        await Assert.ThrowsAsync<NetworkException>(
            () => t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None));
    }

    // ─── Auth-only vs body requests ────────────────────────────────────

    [Fact]
    public async Task PostAuthOnly_SendsOnlyAuthBlock()
    {
        var stub = new StubHandler(_ => Json(HttpStatusCode.OK, OkBody));
        using var http = new HttpClient(stub);
        using var t = new HttpTransport(OptionsWith(http), NullLogger.Instance);
        await t.PostAuthOnlyAsync<SessionInformation>("/api/session/42", CancellationToken.None);
        var body = JsonDocument.Parse(stub.RequestBodies[0]);
        // Only `auth` property should be on the wire — no other top-level keys.
        var keys = body.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Single(keys);
        Assert.Equal("auth", keys[0]);
    }
}
