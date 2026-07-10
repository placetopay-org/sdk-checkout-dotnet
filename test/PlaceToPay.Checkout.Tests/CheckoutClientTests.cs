using System.Net;
using System.Text;
using System.Text.Json;
using PlaceToPay.Checkout.Models.Collect;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Instrument;
using PlaceToPay.Checkout.Models.Session;
using Xunit;

namespace PlaceToPay.Checkout.Tests;

/// <summary>
/// Integration-style tests exercising every public method of
/// <see cref="CheckoutClient"/> via a stubbed <see cref="HttpMessageHandler"/>.
/// Covers the facade layer that <c>HttpTransportTests</c> bypasses.
/// </summary>
public class CheckoutClientTests
{
    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders;
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> RequestBodies { get; } = new();

        public QueueHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders)
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
            return _responders.Dequeue()(request);
        }
    }

    private const string Base = "https://checkout-test.placetopay.com";

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static CheckoutClient NewClient(HttpClient http, Country? country = null) =>
        new(new CheckoutClientOptions
        {
            Login = "test-login",
            SecretKey = "test-secret",
            Environment = PlaceToPay.Checkout.Environment.Sandbox,
            Country = country,
            HttpClient = http,
            MaxRetryAttempts = 1,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
        });

    private const string OkStatus = """
    {"status":"ok","reason":"00","message":"ok","date":"2026-04-22T15:30:45+00:00"}
    """;

    // ─── CreateSessionAsync ────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_PostsToApiSessionAndReturnsResponse()
    {
        var body = $$"""
        {"requestId": 42, "processUrl": "https://example.com/redirect", "status": {{OkStatus}}}
        """;
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http, Country.Colombia);

        var response = await client.CreateSessionAsync(new CreateSessionRequest
        {
            IpAddress = "127.0.0.1",
            UserAgent = "test/1.0",
            ReturnUrl = "https://example.com/return",
            Payment = new Payment
            {
                Reference = "ORDER-1",
                Description = "Demo",
                Amount = new Amount { Currency = "COP", Total = 50_000m },
            },
        });
        Assert.Equal(42, response.RequestId);
        Assert.Equal("https://example.com/redirect", response.ProcessUrl);

        Assert.Equal($"{Base}/api/session", handler.Requests[0].RequestUri!.ToString());
        var sent = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.True(sent.RootElement.TryGetProperty("auth", out _));
        // Colombia default locale flowed through.
        Assert.Equal("es_CO", sent.RootElement.GetProperty("locale").GetString());
    }

    [Fact]
    public async Task CreateSession_DoesNotOverrideCallerLocale()
    {
        var body = $$"""{"requestId":1,"processUrl":"u","status":{{OkStatus}}}""";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http, Country.Colombia);

        await client.CreateSessionAsync(new CreateSessionRequest
        {
            IpAddress = "127.0.0.1",
            UserAgent = "test/1.0",
            ReturnUrl = "https://example.com/return",
            Locale = "en_US",
            Payment = new Payment
            {
                Reference = "R",
                Description = "D",
                Amount = new Amount { Currency = "USD", Total = 100m },
            },
        });
        var sent = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("en_US", sent.RootElement.GetProperty("locale").GetString());
    }

    [Fact]
    public async Task CreateSession_IdempotencyKeyFlowsToHeader()
    {
        var body = $$"""{"requestId":1,"processUrl":"u","status":{{OkStatus}}}""";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        await client.CreateSessionAsync(new CreateSessionRequest
        {
            IpAddress = "127.0.0.1",
            UserAgent = "t",
            ReturnUrl = "https://example.com/return",
            Payment = new Payment
            {
                Reference = "R",
                Description = "D",
                Amount = new Amount { Currency = "COP", Total = 100m },
            },
        }, idempotencyKey: "IDEM-42");

        Assert.True(handler.Requests[0].Headers.TryGetValues("X-Idempotency-Key", out var values));
        Assert.Equal("IDEM-42", values!.First());
    }

    // ─── QuerySessionAsync ─────────────────────────────────────────────

    [Fact]
    public async Task QuerySession_PostsToApiSessionId()
    {
        var body = $$"""{"requestId":99,"status":{{OkStatus}}}""";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        var info = await client.QuerySessionAsync(99);
        Assert.Equal(99, info.RequestId);
        Assert.Equal($"{Base}/api/session/99", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task QuerySession_AcceptsMixedProcessorFieldValueTypes()
    {
        var body = "{" +
            "\"requestId\":99," +
            "\"status\":" + OkStatus + "," +
            "\"payment\":[{" +
                "\"status\":{\"status\":\"approved\",\"date\":\"2026-05-22T12:00:00+00:00\"}," +
                "\"processorFields\":[" +
                    "{\"keyword\":\"is3ds\",\"value\":true}," +
                    "{\"keyword\":\"installments\",\"value\":1}," +
                    "{\"keyword\":\"score\",\"value\":55002.35}," +
                    "{\"keyword\":\"installmentInfo\",\"value\":{\"code\":\"1\",\"installments\":1}}" +
                "]" +
            "}]" +
        "}";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        var info = await client.QuerySessionAsync(99);
        var fields = info.Payment?.Single().ProcessorFields;
        Assert.NotNull(fields);
        Assert.Equal(4, fields!.Count);

        Assert.IsType<JsonElement>(fields[0].Value);
        Assert.True(((JsonElement)fields[0].Value!).GetBoolean());
        Assert.Equal(JsonValueKind.Number, ((JsonElement)fields[1].Value!).ValueKind);
        Assert.Equal(1, ((JsonElement)fields[1].Value!).GetInt32());
        Assert.Equal(JsonValueKind.Number, ((JsonElement)fields[2].Value!).ValueKind);
        Assert.Equal(JsonValueKind.Object, ((JsonElement)fields[3].Value!).ValueKind);
    }

    // ─── CancelSessionAsync ────────────────────────────────────────────

    [Fact]
    public async Task CancelSession_PostsToCancelPath()
    {
        var body = "{\"status\":" + OkStatus +
            ",\"session\":{\"requestId\":99,\"status\":{\"status\":\"rejected\",\"date\":\"2026-04-22T15:30:45+00:00\"}}}";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        var resp = await client.CancelSessionAsync(99);
        Assert.True(resp.Status.IsSuccessful);
        Assert.Equal(StatusCode.Rejected, resp.Session?.Status.Code);
        Assert.Equal($"{Base}/api/session/99/cancel", handler.Requests[0].RequestUri!.ToString());
    }

    // ─── CollectAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task Collect_PassesProviderAndInstrumentOnWire()
    {
        var body = $$"""
        {
          "requestId": 99,
          "status": {{OkStatus}},
          "payment": [{
            "status": {"status":"approved","date":"2026-04-22T15:30:45+00:00"},
            "authorization": "AUTH-1",
            "internalReference": 42
          }]
        }
        """;
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        var info = await client.CollectAsync(new CollectRequest
        {
            Payer = new Person
            {
                Document = "1040",
                DocumentType = DocumentType.CC,
                Name = "T",
                Surname = "B",
                Email = "t@e.co",
            },
            Payment = new Payment
            {
                Reference = "C-1",
                Description = "D",
                Amount = new Amount { Currency = "COP", Total = 10_000m },
            },
            Instrument = new Instrument { Token = new Token { TokenValue = "tk-abc" } },
            Provider = "evertec",
        });
        Assert.Equal(99, info.RequestId);
        Assert.Single(info.Payment!);
        Assert.Equal("AUTH-1", info.LastApprovedTransaction!.Authorization);

        var sent = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("evertec", sent.RootElement.GetProperty("provider").GetString());
        Assert.Equal("tk-abc",
            sent.RootElement.GetProperty("instrument").GetProperty("token").GetProperty("token").GetString());
    }

    // ─── ReverseAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task Reverse_PostsInternalReferenceInBody()
    {
        const string body = """{"status":{"status":"approved","reason":"00","message":"ok","date":"2026-04-22T15:30:45+00:00"}}""";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        var resp = await client.ReverseAsync("42");
        Assert.Equal(StatusCode.Approved, resp.Status?.Code);

        var sent = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("42", sent.RootElement.GetProperty("internalReference").GetString());
        Assert.Equal($"{Base}/api/reverse", handler.Requests[0].RequestUri!.ToString());
    }

    // ─── InvalidateTokenAsync ──────────────────────────────────────────

    [Fact]
    public async Task InvalidateToken_ReturnsStatusFromEnvelope()
    {
        var body = "{\"status\":" + OkStatus + "}";
        var handler = new QueueHandler(_ => Json(HttpStatusCode.OK, body));
        using var http = new HttpClient(handler);
        using var client = NewClient(http);

        var status = await client.InvalidateTokenAsync(new InvalidateTokenRequest
        {
            Instrument = new Instrument { Token = new Token { TokenValue = "tk-abc" } },
        });
        Assert.Equal(StatusCode.Ok, status.Code);
        Assert.Equal($"{Base}/api/instrument/invalidate", handler.Requests[0].RequestUri!.ToString());
    }

    // ─── Constructor validation ────────────────────────────────────────

    [Fact]
    public void Constructor_ValidatesOptions()
    {
        Assert.Throws<InvalidOperationException>(() => new CheckoutClient(new CheckoutClientOptions
        {
            Login = "",   // invalid
            SecretKey = "S",
            Environment = PlaceToPay.Checkout.Environment.Sandbox,
        }));
    }

    [Fact]
    public void Constructor_ThrowsOnHttpUrl()
    {
        Assert.Throws<InvalidOperationException>(() => new CheckoutClient(new CheckoutClientOptions
        {
            Login = "L",
            SecretKey = "S",
            BaseUrl = new Uri("http://example.com"),
        }));
    }

    [Fact]
    public void Options_ExposedOnClient()
    {
        using var http = new HttpClient(new QueueHandler());
        using var client = NewClient(http);
        Assert.Equal("test-login", client.Options.Login);
    }

    // ─── Country presets ───────────────────────────────────────────────

    [Fact]
    public void Country_EcuadorOverridesBaseUrlAndDefaults()
    {
        using var http = new HttpClient(new QueueHandler());
        var prodOptions = new CheckoutClientOptions
        {
            Login = "L",
            SecretKey = "S",
            Environment = PlaceToPay.Checkout.Environment.Production,
            Country = Country.Ecuador,
            HttpClient = http,
        };
        using var client = new CheckoutClient(prodOptions);
        Assert.Equal("es_EC", client.Options.DefaultLocale);
        Assert.Equal("USD", client.Options.DefaultCurrency);
        Assert.Equal("https://checkout.placetopay.ec/", prodOptions.ResolveBaseUrl().ToString());

        var sandboxOptions = new CheckoutClientOptions
        {
            Login = "L",
            SecretKey = "S",
            Environment = PlaceToPay.Checkout.Environment.Sandbox,
            Country = Country.Ecuador,
            HttpClient = http,
        };
        Assert.Equal("https://checkout-test.placetopay.ec/", sandboxOptions.ResolveBaseUrl().ToString());
    }
}
