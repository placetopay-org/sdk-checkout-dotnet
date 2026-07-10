// All-in-one ASP.NET Core playground for the PlaceToPay .NET SDK.
//
// Single-page UI with forms for every endpoint the SDK exposes:
//   1. Connect (credentials entered by the user, held in memory only)
//   2. Create session
//   3. Create subscription session
//   4. Query session
//   5. Cancel session
//   6. Collect (direct charge)
//   7. Reverse
//   8. Invalidate token
//
// Credentials never touch disk and are cleared on disconnect / restart.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlaceToPay.Checkout;
using PlaceToPay.Checkout.Errors;
using PlaceToPay.Checkout.Models.Collect;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Instrument;
using PlaceToPay.Checkout.Models.Session;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddSimpleConsole();

var app = builder.Build();

// ─── In-memory state: one holder per process, lifetime == server uptime ─────
var holder = new Holder();

// ─── Single-page UI ─────────────────────────────────────────────────────────
app.MapGet("/", (HttpContext ctx) =>
{
    ctx.Response.Headers.CacheControl = "no-store, must-revalidate";
    return Results.Content(IndexHtml, "text/html; charset=utf-8");
});

// ─── Connection management ──────────────────────────────────────────────────
app.MapGet("/api/status", () => Results.Json(holder.Status()));

app.MapPost("/api/connect", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    try
    {
        holder.Connect(
            login: body.GetProperty("login").GetString() ?? "",
            secretKey: body.GetProperty("secret_key").GetString() ?? "",
            environment: GetOrDefault(body, "environment", "SANDBOX"),
          country: GetOrDefault(body, "country", ""),
          customBaseUrl: GetOrDefault(body, "custom_base_url", ""));
        return Results.Json(holder.Status());
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 400);
    }
});

app.MapPost("/api/disconnect", () => { holder.Disconnect(); return Results.Json(new { connected = false }); });

// ─── SDK operations ─────────────────────────────────────────────────────────
app.MapPost("/api/create", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);

    var reference = GetOrDefault(body, "reference", $"demo-{Guid.NewGuid():N}"[..12]);
    var request = new CreateSessionRequest
    {
        IpAddress = "127.0.0.1",
        UserAgent = "checkout-dotnet-playground/1.0",
        ReturnUrl = GetOrDefault(body, "return_url", "https://example.com/return"),
        Payment = new Payment
        {
            Reference = reference,
            Description = GetOrDefault(body, "description", "Demo session"),
            Amount = new Amount
            {
                Currency = GetOrDefault(body, "currency", "COP"),
                Total = decimal.Parse(GetOrDefault(body, "amount", "50000"), System.Globalization.CultureInfo.InvariantCulture),
            },
        },
    };
    try
    {
        return Results.Json(await client.CreateSessionAsync(request));
    }
    catch (PlaceToPayException ex)
    {
        return SdkError(ex);
    }
});

  app.MapPost("/api/create-subscription", async (HttpRequest req) =>
  {
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);

    var reference = GetOrDefault(body, "reference", $"sub-{Guid.NewGuid():N}"[..12]);
    var request = new CreateSessionRequest
    {
      IpAddress = "127.0.0.1",
      UserAgent = "checkout-dotnet-playground/1.0",
      ReturnUrl = GetOrDefault(body, "return_url", "https://example.com/return"),
      Subscription = new SubscriptionRequest
      {
        Reference = reference,
        Description = GetOrDefault(body, "description", "Demo subscription session"),
      },
    };
    try
    {
      return Results.Json(await client.CreateSessionAsync(request));
    }
    catch (PlaceToPayException ex)
    {
      return SdkError(ex);
    }
  });

app.MapPost("/api/query", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);
    var err = RequireLong(body, "request_id", out var id);
    if (err is not null) return err;
    try
    {
        return Results.Json(await client.QuerySessionAsync(id));
    }
    catch (PlaceToPayException ex) { return SdkError(ex); }
});

app.MapPost("/api/cancel", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);
    var err = RequireLong(body, "request_id", out var id);
    if (err is not null) return err;
    try
    {
        return Results.Json(await client.CancelSessionAsync(id));
    }
    catch (PlaceToPayException ex) { return SdkError(ex); }
});

app.MapPost("/api/collect", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);

    try
    {
        var request = new CollectRequest
        {
            Payer = new Person
            {
                Document = GetOrDefault(body, "document", "1040035000"),
                DocumentType = Enum.TryParse<DocumentType>(GetOrDefault(body, "document_type", "CC"), out var dt) ? dt : DocumentType.CC,
                Name = GetOrDefault(body, "name", "Test"),
                Surname = GetOrDefault(body, "surname", "Buyer"),
                Email = GetOrDefault(body, "email", "test@example.com"),
            },
            Payment = new Payment
            {
                Reference = $"collect-{Guid.NewGuid():N}"[..12],
                Description = GetOrDefault(body, "description", "Collect via token"),
                Amount = new Amount
                {
                    Currency = GetOrDefault(body, "currency", "COP"),
                    Total = decimal.Parse(GetOrDefault(body, "amount", "10000"), System.Globalization.CultureInfo.InvariantCulture),
                },
            },
            Instrument = new Instrument
            {
                Token = new Token { TokenValue = GetOrDefault(body, "token", "") },
            },
        };
        if (string.IsNullOrWhiteSpace(request.Instrument?.Token?.TokenValue))
            return Results.Problem("'token' is required.", statusCode: 400);
        return Results.Json(await client.CollectAsync(request));
    }
    catch (PlaceToPayException ex) { return SdkError(ex); }
});

app.MapPost("/api/reverse", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);
    var err = RequireString(body, "internal_reference", out var internalRef);
    if (err is not null) return err;
    try
    {
        return Results.Json(await client.ReverseAsync(internalRef));
    }
    catch (PlaceToPayException ex) { return SdkError(ex); }
});

app.MapPost("/api/invalidate", async (HttpRequest req) =>
{
    var body = await ReadJson(req);
    var client = holder.Require();
    if (client is null) return Results.Problem("Not connected.", statusCode: 400);
    try
    {
        var tokenValue = GetOrDefault(body, "token", "");
        if (string.IsNullOrWhiteSpace(tokenValue))
            return Results.Problem("'token' is required.", statusCode: 400);
        var request = new InvalidateTokenRequest
        {
            Instrument = new Instrument { Token = new Token { TokenValue = tokenValue } },
        };
        return Results.Json(await client.InvalidateTokenAsync(request));
    }
    catch (PlaceToPayException ex) { return SdkError(ex); }
});

app.Run();

// ─── Helpers ────────────────────────────────────────────────────────────────

static async Task<JsonElement> ReadJson(HttpRequest req)
{
    using var doc = await JsonDocument.ParseAsync(req.Body);
    return doc.RootElement.Clone();
}

static string GetOrDefault(JsonElement obj, string name, string fallback)
{
    if (!obj.TryGetProperty(name, out var v)) return fallback;
    if (v.ValueKind == JsonValueKind.String)
    {
        var s = v.GetString();
        return string.IsNullOrWhiteSpace(s) ? fallback : s;
    }
    return v.ValueKind == JsonValueKind.Null || v.ValueKind == JsonValueKind.Undefined
        ? fallback : v.ToString();
}

// Returns a RequiredField error result when a JSON field is missing or blank.
static IResult? RequireString(JsonElement obj, string name, out string value)
{
    if (!obj.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Number && string.IsNullOrWhiteSpace(v.GetString()))
    {
        value = "";
        return Results.Problem($"'{name}' is required.", statusCode: 400);
    }
    value = v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString();
    return null;
}

// Returns a RequiredField error result when a numeric JSON field is missing.
static IResult? RequireLong(JsonElement obj, string name, out long value)
{
    value = 0;
    if (!obj.TryGetProperty(name, out var v))
        return Results.Problem($"'{name}' is required.", statusCode: 400);
    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out value))
        return null;
    if (!long.TryParse(v.ToString(), System.Globalization.CultureInfo.InvariantCulture, out value))
        return Results.Problem($"'{name}' must be a numeric request id.", statusCode: 400);
    return null;
}

// Builds a structured SDK error response including reason and status code.
static IResult SdkError(PlaceToPayException ex)
{
  var detail = string.IsNullOrEmpty(ex.Reason)
        ? ex.Message
        : $"{ex.Message} | reason={ex.Reason}";
  if (ex.Message.Contains("HTTP 401", StringComparison.OrdinalIgnoreCase))
  {
    detail += " | Unauthorized by gateway: verify login, secret_key, and environment/base URL.";
  }
    return Results.Problem(detail, statusCode: 502);
}

// ─── Holder for the live credentials / client ───────────────────────────────
sealed class Holder
{
    private readonly object _gate = new();
    private CheckoutClient? _client;
    private string? _secret;
    private string? _loginMasked;
    private string? _environment;
    private string? _country;
    private string? _baseUrl;

    public string? Secret { get { lock (_gate) { return _secret; } } }

    public object Status()
    {
        lock (_gate)
        {
            if (_client is null) return new { connected = false };
            return new
            {
                connected = true,
                login_masked = _loginMasked,
                environment = _environment,
                country = _country,
                base_url = _baseUrl,
            };
        }
    }

          public void Connect(string login, string secretKey, string environment, string country, string customBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(login)) throw new ArgumentException("login required");
        if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentException("secret_key required");

        var env = Enum.Parse<PlaceToPay.Checkout.Environment>(environment, ignoreCase: true);
        Country? ctry = string.IsNullOrWhiteSpace(country)
            ? null
            : Enum.Parse<Country>(country, ignoreCase: true);

            Uri? baseUrl = null;
            if (!string.IsNullOrWhiteSpace(customBaseUrl))
            {
              if (!Uri.TryCreate(customBaseUrl.Trim(), UriKind.Absolute, out var parsed))
                throw new ArgumentException("custom_base_url must be an absolute URL");
              baseUrl = parsed;
            }

        var client = new CheckoutClient(new CheckoutClientOptions
        {
            Login = login.Trim(),
            SecretKey = secretKey.Trim(),
              Environment = baseUrl is null ? env : null,
              BaseUrl = baseUrl,
            Country = ctry,
        });

        lock (_gate)
        {
            _client?.Dispose();
            _client = client;
            _secret = secretKey.Trim();
            _loginMasked = login.Length > 12 ? login[..6] + "…" + login[^4..] : "…";
            _environment = env.ToString().ToUpperInvariant();
            _country = ctry?.ToString().ToUpperInvariant() ?? "—";
            _baseUrl = client.Options.ResolveBaseUrl().ToString().TrimEnd('/');
        }
    }

    public void Disconnect()
    {
        lock (_gate)
        {
            _client?.Dispose();
            _client = null;
            _secret = null;
            _loginMasked = null;
            _environment = null;
            _country = null;
            _baseUrl = null;
        }
    }

    public ICheckoutClient? Require()
    {
        lock (_gate) { return _client; }
    }
}

// ─── Single-page UI (shared HTML across the 3 SDK playgrounds) ──────────────
partial class Program
{
    public const string IndexHtml = """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>PlaceToPay — .NET playground</title>
<link rel="icon" type="image/png" href="https://placetopay-static-prod-bucket.s3.us-east-2.amazonaws.com/placetopay-com/favicons/apple-icon-57x57.png">
<style>
  :root{
    --p2p-orange: #F36C21;
    --p2p-orange-dark: #C94518;
    --p2p-orange-soft: #FEF2EC;
    --p2p-orange-ring: #FCD9C2;
    --ink: #1F2937;
    --ink-soft: #4B5563;
    --muted: #6B7280;
    --border: #E5E7EB;
    --bg: #FAF7F3;
    --card: #FFFFFF;
    --ok-bg: #DCFCE7; --ok-ink: #166534;
    --err-bg: #FEE2E2; --err-ink: #991B1B;
    --err: #DC2626;
  }
  *{box-sizing:border-box;}
  html,body{margin:0;}
  body{font:14px/1.5 -apple-system,"Segoe UI",Roboto,sans-serif;color:var(--ink);background:var(--bg);min-height:100vh;}
  .layout{display:grid;grid-template-columns:260px 1fr;min-height:100vh;}
  aside{background:var(--card);border-right:1px solid var(--border);padding:1.5rem 1.1rem;position:sticky;top:0;height:100vh;overflow-y:auto;}
  main{padding:2rem 2.5rem;max-width:900px;}
  .brand{display:flex;align-items:center;gap:.6rem;margin-bottom:1.25rem;}
  .brand-mark{width:38px;height:38px;border-radius:999px;box-shadow:0 2px 6px rgba(243,108,33,.35);display:block;object-fit:cover;}
  .brand-text{line-height:1.1;}
  .brand-text .t1{font-weight:700;font-size:14px;color:var(--ink);}
  .brand-text .t2{font-size:11px;color:var(--muted);}
  .status-card{border:1px solid var(--border);border-radius:8px;padding:.7rem .85rem;background:var(--bg);margin-bottom:1.25rem;}
  .status-card.live{background:var(--p2p-orange-soft);border-color:var(--p2p-orange-ring);}
  .status-row{display:flex;align-items:center;gap:.5rem;font-size:12.5px;}
  .dot{width:8px;height:8px;border-radius:999px;background:#9CA3AF;}
  .dot.on{background:#16A34A;box-shadow:0 0 0 3px #DCFCE7;}
  .dot.off{background:#DC2626;box-shadow:0 0 0 3px #FEE2E2;}
  .status-detail{margin-top:.5rem;font-size:11.5px;color:var(--ink-soft);display:grid;gap:.15rem;}
  .status-detail code{background:white;border:1px solid var(--border);padding:1px 5px;border-radius:3px;font-size:11px;}
  .status-card button{margin-top:.5rem;width:100%;}
  .menu-heading{font-size:10.5px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:var(--muted);margin:1rem 0 .35rem;padding:0 .5rem;}
  nav ul{list-style:none;margin:0;padding:0;}
  nav li a{display:flex;align-items:center;gap:.55rem;padding:.5rem .65rem;border-radius:6px;color:var(--ink-soft);text-decoration:none;font-size:13px;cursor:pointer;transition:all .12s;}
  nav li a:hover{background:var(--p2p-orange-soft);color:var(--p2p-orange-dark);}
  nav li a.active{background:var(--p2p-orange);color:white;font-weight:600;}
  nav li a.active .num{background:rgba(255,255,255,.22);color:white;}
  nav li a.disabled{opacity:.45;cursor:not-allowed;}
  nav li a.disabled:hover{background:transparent;color:var(--ink-soft);}
  .num{display:inline-flex;width:22px;height:22px;border-radius:999px;background:var(--border);color:var(--ink-soft);font-size:11px;font-weight:700;align-items:center;justify-content:center;flex-shrink:0;}
  h1{margin:0 0 .25rem;font-size:1.6rem;letter-spacing:-0.01em;}
  .sub{color:var(--muted);font-size:12.5px;margin-bottom:1.5rem;}
  section{background:var(--card);border:1px solid var(--border);border-radius:10px;padding:1.5rem 1.75rem;margin:0;box-shadow:0 1px 2px rgba(0,0,0,0.03);display:none;}
  section.active{display:block;}
  section.connected{background:linear-gradient(180deg,var(--p2p-orange-soft) 0%,var(--card) 60%);border-color:var(--p2p-orange-ring);}
  section h2{margin:0 0 1rem;font-size:1.15rem;color:var(--ink);display:flex;align-items:center;gap:.6rem;}
  section h2 .badge{background:var(--p2p-orange);color:white;font-size:11px;padding:2px 8px;border-radius:999px;font-weight:700;}
  form{display:grid;gap:.75rem;grid-template-columns:1fr 1fr;align-items:end;}
  form .full{grid-column:1/-1;}
  label{display:grid;gap:.25rem;font-size:12px;color:var(--ink-soft);font-weight:500;}
  input,select,textarea{padding:.55rem .7rem;font:inherit;border:1px solid var(--border);border-radius:6px;background:white;color:var(--ink);transition:border-color .12s,box-shadow .12s;}
  input:focus,select:focus,textarea:focus{outline:0;border-color:var(--p2p-orange);box-shadow:0 0 0 3px var(--p2p-orange-ring);}
  textarea{min-height:6rem;font-family:ui-monospace,Menlo,Consolas,monospace;font-size:12px;resize:vertical;}
  button{cursor:pointer;background:var(--p2p-orange);color:white;border:0;font-weight:600;padding:.6rem 1.2rem;border-radius:6px;font:inherit;font-weight:600;transition:background .12s;}
  button:hover{background:var(--p2p-orange-dark);}
  button.ghost{background:transparent;color:var(--p2p-orange-dark);border:1px solid var(--p2p-orange);}
  button.ghost:hover{background:var(--p2p-orange-soft);}
  button.danger{background:var(--err);}
  button.danger:hover{background:#B91C1C;}
  pre{background:#0F172A;color:#E2E8F0;padding:1rem 1.1rem;border-radius:8px;overflow-x:auto;font-size:11.5px;line-height:1.5;white-space:pre-wrap;word-break:break-all;max-height:420px;margin-top:1rem;}
  .hint{font-size:11.5px;color:var(--muted);margin-top:.35rem;}
  .link-cta{display:inline-block;margin-top:.75rem;padding:.55rem .85rem;background:var(--p2p-orange-soft);border:1px solid var(--p2p-orange-ring);border-radius:6px;color:var(--p2p-orange-dark);text-decoration:none;font-weight:600;font-size:13px;}
  .link-cta:hover{background:var(--p2p-orange-ring);}
  .not-connected-banner{padding:.9rem 1.1rem;background:#FEF3C7;color:#78350F;border:1px solid #FCD34D;border-radius:8px;font-size:13px;margin-bottom:1rem;}
</style></head><body>

<div class="layout">

<aside>
  <div class="brand">
    <img class="brand-mark" src="https://placetopay-static-prod-bucket.s3.us-east-2.amazonaws.com/placetopay-com/favicons/apple-icon-57x57.png" alt="PlaceToPay" width="38" height="38">
    <div class="brand-text">
      <div class="t1">PlaceToPay</div>
      <div class="t2">.NET SDK playground</div>
    </div>
  </div>

  <div id="status-card" class="status-card">
    <div class="status-row"><span id="status-dot" class="dot off"></span><strong id="status-text">Disconnected</strong></div>
    <div id="status-detail" class="status-detail" style="display:none"></div>
    <button id="btn-disconnect" class="ghost" style="display:none" onclick="disconnect()">Disconnect</button>
  </div>

  <div class="menu-heading">Operations</div>
  <nav><ul id="menu">
    <li><a data-target="sec-connect"    onclick="show(event,'sec-connect')"    class="active"><span class="num">1</span>Connection</a></li>
    <li><a data-target="sec-create"     onclick="show(event,'sec-create')"     class="disabled"><span class="num">2</span>Create session</a></li>
    <li><a data-target="sec-create-subscription" onclick="show(event,'sec-create-subscription')" class="disabled"><span class="num">3</span>Create subscription session</a></li>
    <li><a data-target="sec-query"      onclick="show(event,'sec-query')"      class="disabled"><span class="num">4</span>Query session</a></li>
    <li><a data-target="sec-cancel"     onclick="show(event,'sec-cancel')"     class="disabled"><span class="num">5</span>Cancel session</a></li>
    <li><a data-target="sec-collect"    onclick="show(event,'sec-collect')"    class="disabled"><span class="num">6</span>Collect</a></li>
    <li><a data-target="sec-reverse"    onclick="show(event,'sec-reverse')"    class="disabled"><span class="num">7</span>Reverse</a></li>
    <li><a data-target="sec-invalidate" onclick="show(event,'sec-invalidate')" class="disabled"><span class="num">8</span>Invalidate token</a></li>
  </ul></nav>
</aside>

<main>
  <h1>Checkout SDK Playground</h1>
  <p class="sub">Every PlaceToPay Checkout SDK operation in one place. Credentials live only in this process's memory — a server restart wipes them.</p>

  <div id="connect-banner" class="not-connected-banner">Start by entering your PlaceToPay credentials in section <strong>1 · Connection</strong>. The other operations light up once you're connected.</div>

  <section id="sec-connect" class="active">
    <h2>Connection <span class="badge">1</span></h2>
    <form id="form-connect" onsubmit="return connect(event)">
      <label>Login<input name="login" required autocomplete="off"></label>
      <label>Secret key<input name="secret_key" type="password" required autocomplete="off"></label>
      <label>Environment<select name="environment"><option>SANDBOX</option><option>PRODUCTION</option></select></label>
      <label>Country (optional)<select name="country"><option value="">—</option><option>COLOMBIA</option><option>ECUADOR</option></select></label>
      <div class="full hint">Country applies regional defaults (currency/locale) and can affect production routing.</div>
      <label class="full"><input id="custom-url-enabled" type="checkbox"> Use custom WebCheckout URL</label>
      <label id="custom-url-row" class="full" style="display:none">Custom URL<input id="custom-base-url" name="custom_base_url" type="url" placeholder="https://checkout-test.placetopay.com" autocomplete="off"></label>
      <div class="full"><button>Connect</button><div class="hint">Secret goes to this local server only — never logged, never persisted.</div><div id="connect-target" class="hint"></div></div>
    </form>
    <pre id="out-connect" style="display:none"></pre>
  </section>

  <section id="sec-create">
    <h2>Create session <span class="badge">2</span></h2>
    <form onsubmit="call(event, '/api/create', 'out-create'); return false;">
      <label>Amount<input name="amount" type="number" step="0.01" value="50000" required></label>
      <label>Currency<input name="currency" value="COP" required></label>
      <label class="full">Description<input name="description" value="Demo session"></label>
      <label class="full">Return URL<input name="return_url" value="https://example.com/return"></label>
      <div class="full"><button>Create &amp; get processUrl</button></div>
    </form>
    <pre id="out-create" style="display:none"></pre>
  </section>

  <section id="sec-create-subscription">
    <h2>Create subscription session <span class="badge">3</span></h2>
    <form onsubmit="call(event, '/api/create-subscription', 'out-create-subscription'); return false;">
      <label class="full">Reference<input name="reference" value="sub-demo-001" required></label>
      <label class="full">Description<input name="description" value="Demo subscription session" required></label>
      <label class="full">Return URL<input name="return_url" value="https://example.com/return"></label>
      <div class="full"><button>Create subscription &amp; get processUrl</button></div>
    </form>
    <pre id="out-create-subscription" style="display:none"></pre>
  </section>

  <section id="sec-query">
    <h2>Query session <span class="badge">4</span></h2>
    <form onsubmit="call(event, '/api/query', 'out-query'); return false;">
      <label>Request ID<input name="request_id" type="number" required></label>
      <div><button>Query</button></div>
    </form>
    <pre id="out-query" style="display:none"></pre>
  </section>

  <section id="sec-cancel">
    <h2>Cancel session <span class="badge">5</span></h2>
    <form onsubmit="call(event, '/api/cancel', 'out-cancel'); return false;">
      <label>Request ID<input name="request_id" type="number" required></label>
      <div><button class="danger">Cancel</button></div>
    </form>
    <pre id="out-cancel" style="display:none"></pre>
  </section>

  <section id="sec-collect">
    <h2>Collect (direct charge) <span class="badge">6</span></h2>
    <form onsubmit="call(event, '/api/collect', 'out-collect'); return false;">
      <label class="full">Token<input name="token" required placeholder="subscription token"></label>
      <label>Amount<input name="amount" type="number" step="0.01" value="10000"></label>
      <label>Currency<input name="currency" value="COP"></label>
      <label>Payer document<input name="document" value="1040035000"></label>
      <label>Document type<input name="document_type" value="CC"></label>
      <label>Name<input name="name" value="Test"></label>
      <label>Surname<input name="surname" value="Buyer"></label>
      <label>Email<input name="email" type="email" value="test@example.com"></label>
      <div class="full"><button>Collect</button></div>
    </form>
    <pre id="out-collect" style="display:none"></pre>
  </section>

  <section id="sec-reverse">
    <h2>Reverse <span class="badge">7</span></h2>
    <form onsubmit="call(event, '/api/reverse', 'out-reverse'); return false;">
      <label>Internal reference<input name="internal_reference" required></label>
      <div><button class="danger">Reverse</button></div>
    </form>
    <pre id="out-reverse" style="display:none"></pre>
  </section>

  <section id="sec-invalidate">
    <h2>Invalidate token <span class="badge">8</span></h2>
    <form onsubmit="call(event, '/api/invalidate', 'out-invalidate'); return false;">
      <label class="full">Token<input name="token" required></label>
      <div><button class="danger">Invalidate</button></div>
    </form>
    <pre id="out-invalidate" style="display:none"></pre>
  </section>
</main>

</div>

<script>
function endpointForSelection(environment, country){
  const env = (environment || 'SANDBOX').toUpperCase();
  const ctry = (country || '').toUpperCase();
  if (ctry === 'ECUADOR'){
    return env === 'PRODUCTION'
      ? 'https://checkout.placetopay.ec'
      : 'https://checkout-test.placetopay.ec';
  }
  return env === 'PRODUCTION'
    ? 'https://checkout.placetopay.com'
    : 'https://checkout-test.placetopay.com';
}

function syncCustomBaseUrlState(){
  const enabled = document.getElementById('custom-url-enabled');
  const row = document.getElementById('custom-url-row');
  const input = document.getElementById('custom-base-url');
  if(!enabled || !row || !input) return;
  row.style.display = enabled.checked ? 'grid' : 'none';
  input.disabled = !enabled.checked;
  input.required = enabled.checked;
}

let connectedBaseUrl = '';

function refreshTargetHint(){
  const form = document.getElementById('form-connect');
  const hint = document.getElementById('connect-target');
  if(!form || !hint) return;
  const fd = new FormData(form);
  const env = (fd.get('environment') || 'SANDBOX').toString().toUpperCase();
  const country = (fd.get('country') || '').toString().toUpperCase();
  const customEnabled = document.getElementById('custom-url-enabled')?.checked;
  const customUrl = (document.getElementById('custom-base-url')?.value || '').trim();
  const target = customEnabled && customUrl ? customUrl : endpointForSelection(env, country);
  const connected = document.getElementById('status-text')?.textContent === 'Connected';
  hint.innerHTML = 'WebCheckout target: <code>' + target + '</code>' + (connected ? ' <span class="hint">(applies on reconnect)</span>' : '');

  const next = document.getElementById('status-next-target');
  if (connected && next){
    const sameAsActive = !!connectedBaseUrl && connectedBaseUrl === target;
    next.style.display = 'block';
    next.innerHTML = 'Selected target: <code>' + target + '</code>'
      + (sameAsActive ? ' <span class="hint">(active)</span>' : ' <span class="hint">(pending reconnect)</span>');
  }
}

function show(ev, id){
  if(ev) ev.preventDefault();
  const link = document.querySelector('a[data-target="'+id+'"]');
  if(link && link.classList.contains('disabled')) return false;
  for(const s of document.querySelectorAll('section')) s.classList.remove('active');
  document.getElementById(id).classList.add('active');
  for(const a of document.querySelectorAll('nav a')) a.classList.remove('active');
  if(link) link.classList.add('active');
  return false;
}

async function refreshStatus(){
  const r = await fetch('/api/status'); const j = await r.json();
  const dot   = document.getElementById('status-dot');
  const text  = document.getElementById('status-text');
  const detail= document.getElementById('status-detail');
  const btn   = document.getElementById('btn-disconnect');
  const card  = document.getElementById('status-card');
  const banner= document.getElementById('connect-banner');
  const secConnect = document.getElementById('sec-connect');
  const menu = document.getElementById('menu');

  if (j.connected){
    dot.className='dot on'; text.textContent='Connected';
    card.classList.add('live');
    detail.style.display='grid';
    const activeUrl = j.base_url || endpointForSelection((j.environment || 'SANDBOX').toUpperCase(), (j.country || '').toUpperCase());
    connectedBaseUrl = activeUrl;
    detail.innerHTML = '<div>Login: <code>'+j.login_masked+'</code></div>'
      + '<div>Env: <code>'+j.environment+'</code> &nbsp; Country: <code>'+j.country+'</code></div>'
      + '<div>WebCheckout: <code>'+activeUrl+'</code></div>'
      + '<div id="status-next-target" class="hint" style="display:none"></div>';
    // Keep form selections editable while connected; changes are applied on reconnect.
    refreshTargetHint();
    btn.style.display='block';
    banner.style.display='none';
    secConnect.classList.add('connected');
    for(const a of menu.querySelectorAll('a')) a.classList.remove('disabled');
  } else {
    dot.className='dot off'; text.textContent='Disconnected';
    connectedBaseUrl = '';
    card.classList.remove('live');
    detail.style.display='none'; detail.innerHTML='';
    btn.style.display='none';
    banner.style.display='block';
    secConnect.classList.remove('connected');
    for(const a of menu.querySelectorAll('a'))
      if(a.getAttribute('data-target')!=='sec-connect') a.classList.add('disabled');
    if(!document.getElementById('sec-connect').classList.contains('active')) show(null,'sec-connect');
  }
}
document.querySelector('#form-connect select[name="environment"]')?.addEventListener('change', refreshTargetHint);
document.querySelector('#form-connect select[name="country"]')?.addEventListener('change', refreshTargetHint);
document.getElementById('custom-url-enabled')?.addEventListener('change', () => { syncCustomBaseUrlState(); refreshTargetHint(); });
document.getElementById('custom-base-url')?.addEventListener('input', refreshTargetHint);
syncCustomBaseUrlState();
refreshTargetHint();
refreshStatus();

function fmt(out, r, j){
  out.style.display='block';
  out.textContent = 'HTTP ' + r.status + '\n\n' + JSON.stringify(j, null, 2);
}

async function connect(ev){
  ev.preventDefault();
  const f = ev.target; const body = Object.fromEntries(new FormData(f).entries());
  const customEnabled = document.getElementById('custom-url-enabled')?.checked;
  if (!customEnabled) delete body.custom_base_url;
  const out = document.getElementById('out-connect');
  const r = await fetch('/api/connect',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  const j = await r.json(); fmt(out, r, j);
  if (r.ok){
    const secretInput = f.querySelector('input[name="secret_key"]');
    if (secretInput) secretInput.value = '';
    await refreshStatus();
  }
  return false;
}

async function disconnect(){
  await fetch('/api/disconnect',{method:'POST'}); await refreshStatus();
}

async function call(ev, path, outId){
  ev.preventDefault();
  const form = ev.target;
  const out = document.getElementById(outId);
  const body = Object.fromEntries(new FormData(form).entries());
  const r = await fetch(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  let j; try { j = await r.json(); } catch { j = {error:'non-JSON response'}; }
  fmt(out, r, j);
  if ((path==='/api/create' || path==='/api/create-subscription') && j.processUrl){
    const sibling = out.nextElementSibling;
    if (sibling && sibling.classList && sibling.classList.contains('link-cta')) sibling.remove();
    const a = document.createElement('a'); a.href=j.processUrl; a.target='_blank';
    a.className='link-cta';
    a.textContent='→ Open processUrl in a new tab (complete the payment)';
    out.after(a);
  }
  return false;
}
</script>

</body></html>
""";
}
