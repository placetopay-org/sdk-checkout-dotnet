using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PlaceToPay.Checkout.Authentication;
using PlaceToPay.Checkout.Errors;
using PlaceToPay.Checkout.Internal;
using PlaceToPay.Checkout.Models.Common;

namespace PlaceToPay.Checkout.Http;

/// <summary>
/// The HTTP engine that backs <see cref="CheckoutClient"/>. Handles: auth injection,
/// retry with exponential backoff + jitter, <c>Retry-After</c> honouring, typed
/// exception mapping, and debug logging with redaction.
/// </summary>
/// <remarks>
/// Thread-safe. One <see cref="HttpTransport"/> per <see cref="CheckoutClient"/>.
/// Accepts an externally-owned <see cref="HttpClient"/> via
/// <see cref="CheckoutClientOptions.HttpClient"/> (e.g. from <c>IHttpClientFactory</c>);
/// otherwise constructs (and owns) an <see cref="HttpClient"/> with the configured
/// connect + read timeouts.
/// </remarks>
internal sealed class HttpTransport : IDisposable
{
    internal const string SdkVersion = "1.0.0";

    private readonly CheckoutClientOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly Uri _baseUrl;
    private readonly IClock _clock;
    private readonly RedactingJsonLogger _bodyLogger;
    private readonly ILogger _logger;
    private readonly string _userAgent;

    public HttpTransport(CheckoutClientOptions options, ILogger logger)
        : this(options, logger, SystemClock.Instance) { }

    internal HttpTransport(CheckoutClientOptions options, ILogger logger, IClock clock)
    {
        _options = options;
        _logger = logger;
        _clock = clock;
        _baseUrl = options.ResolveBaseUrl();
        _bodyLogger = new RedactingJsonLogger(logger);
        _userAgent = BuildUserAgent(options.UserAgentSuffix);

        if (options.HttpClient is not null)
        {
            _http = options.HttpClient;
            _ownsHttpClient = false;
        }
        else
        {
            // Own HttpClient -> keep connect + read timeout separate. The client-level
            // Timeout is read-timeout-ish; we use a SocketsHttpHandler for connect.
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = options.ConnectTimeout,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            };
            _http = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = options.Timeout,
                BaseAddress = _baseUrl,
            };
            _ownsHttpClient = true;
        }
    }

    public Uri BaseUrl => _baseUrl;

    public Task<T> PostAsync<T>(string path, object body, string? idempotencyKey, CancellationToken ct) =>
        SendAsync<T>(path, body, idempotencyKey, ct);

    public Task<T> PostAuthOnlyAsync<T>(string path, CancellationToken ct) =>
        SendAsync<T>(path, body: null, idempotencyKey: null, ct);

    // ─── Core send loop ──────────────────────────────────────────────────

    private async Task<T> SendAsync<T>(string path, object? body, string? idempotencyKey, CancellationToken ct)
    {
        var effectivePath = path.StartsWith('/') ? path : "/" + path;
        var url = new Uri(_baseUrl, effectivePath);
        var maxAttempts = _options.MaxRetryAttempts;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var auth = AuthSigner.Sign(_options.Login, _options.SecretKey, _clock.UtcNow, _options.AuthAdditional);
            var payload = BuildBody(body, auth);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new ByteArrayContent(payload),
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
            }
            if (_options.AdditionalHeaders is not null)
            {
                foreach (var (k, v) in _options.AdditionalHeaders)
                {
                    request.Headers.TryAddWithoutValidation(k, v);
                }
            }

            _bodyLogger.LogRequest(HttpMethod.Post, url, Encoding.UTF8.GetString(payload));

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (RetryHandler.IsRetriableException(ex))
            {
                if (attempt == maxAttempts)
                {
                    throw new NetworkException($"Network error after {attempt} attempt(s): {ex.Message}", innerException: ex);
                }
                _logger.LogDebug(ex, "transport error on attempt {Attempt}/{Max}; retrying", attempt, maxAttempts);
                var delay = RetryHandler.ComputeBackoff(attempt, _options.InitialRetryDelay, _options.MaxRetryDelay);
                await Task.Delay(delay, ct).ConfigureAwait(false);
                continue;
            }

            using (response)
            {
                var statusCode = response.StatusCode;
                var bodyString = response.Content is null
                    ? string.Empty
                    : await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                _bodyLogger.LogResponse(HttpMethod.Post, url, (int)statusCode, bodyString);

                if ((int)statusCode >= 200 && (int)statusCode < 300)
                {
                    return Deserialize<T>(bodyString);
                }

                if (RetryHandler.IsRetriableStatus(statusCode) && attempt < maxAttempts)
                {
                    var retryAfterHeader = RetryAfterHeaderValue(response);
                    var retryAfter = RetryHandler.ParseRetryAfter(retryAfterHeader, _clock);
                    var delay = retryAfter ?? RetryHandler.ComputeBackoff(attempt, _options.InitialRetryDelay, _options.MaxRetryDelay);
                    _logger.LogDebug("retry {Attempt}/{Max} after HTTP {Status}", attempt, maxAttempts, (int)statusCode);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                    continue;
                }

                throw BuildError(statusCode, bodyString, response);
            }
        }

        // Unreachable — kept to satisfy the compiler.
        throw new NetworkException("Unexpected fallthrough in retry loop");
    }

    // ─── Body / auth injection ──────────────────────────────────────────

    private static byte[] BuildBody(object? body, Auth auth)
    {
        JsonObject root;
        if (body is null)
        {
            root = new JsonObject();
        }
        else
        {
            var node = JsonSerializer.SerializeToNode(body, body.GetType(), JsonDefaults.Options);
            root = node as JsonObject ?? throw new InvalidOperationException(
                "Request body must serialize to a JSON object.");
        }

        // Always overwrite — callers may have left a null Auth placeholder on their record.
        root.Remove("auth");
        root["auth"] = JsonSerializer.SerializeToNode(auth, JsonDefaults.Options);

        return JsonSerializer.SerializeToUtf8Bytes(root, JsonDefaults.Options);
    }

    private static T Deserialize<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new PlaceToPayException("Empty response body.");
        }
        try
        {
            var result = JsonSerializer.Deserialize<T>(body, JsonDefaults.Options);
            if (result is null)
            {
                throw new PlaceToPayException("Response deserialized to null.");
            }
            return result;
        }
        catch (JsonException ex)
        {
            throw new PlaceToPayException($"Failed to parse response body: {ex.Message}", innerException: ex, rawBody: body);
        }
    }

    // ─── Error translation ──────────────────────────────────────────────

    private PlaceToPayException BuildError(HttpStatusCode statusCode, string body, HttpResponseMessage response)
    {
        Status? status = null;
        string message = $"HTTP {(int)statusCode}";
        string? reason = null;
        string? requestId = response.Headers.TryGetValues("X-Request-Id", out var idValues) ? idValues.FirstOrDefault() : null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("status", out var statusEl))
            {
                status = statusEl.Deserialize<Status>(JsonDefaults.Options);
                if (status is not null)
                {
                    reason = status.Reason;
                    if (!string.IsNullOrWhiteSpace(status.Message))
                    {
                        message = status.Message!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // keep the generic message
        }

        var code = (int)statusCode;
        return code switch
        {
            400 => new ValidationException(message, status, reason, requestId, body),
            401 => new AuthenticationException(message, status, reason, requestId, body),
            404 => new NotFoundException(message, status, reason, requestId, body),
            429 => new RateLimitException(
                message,
                RetryHandler.ParseRetryAfter(RetryAfterHeaderValue(response), _clock),
                status, reason, requestId, body),
            >= 500 and <= 599 => new ServerException(message, code, status, reason, requestId, body),
            _ => new PlaceToPayException(message + ": " + body, status, reason, requestId, body),
        };
    }

    // ─── User-Agent ─────────────────────────────────────────────────────

    private static string BuildUserAgent(string? suffix)
    {
        var runtime = RuntimeInformation.FrameworkDescription;
        var os = RuntimeInformation.OSDescription;
        var ua = $"placetopay-sdk-dotnet/{SdkVersion} ({runtime}; {os})";
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            ua += $" {suffix}";
        }
        return ua;
    }

    // ─── Retry-After helper ─────────────────────────────────────────────

    private static string? RetryAfterHeaderValue(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } d)
        {
            return ((int)d.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            return values.FirstOrDefault();
        }
        return null;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }
}
