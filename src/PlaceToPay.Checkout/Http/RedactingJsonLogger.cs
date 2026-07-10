using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PlaceToPay.Checkout.Http;

/// <summary>
/// DEBUG-level logger for request / response bodies. Walks the parsed JSON tree,
/// replaces sensitive values with fixed tokens, and re-serializes.
/// </summary>
/// <remarks>
/// Redaction rules, from ARCHITECTURE.md §9:
/// <list type="bullet">
///   <item><c>auth.tranKey</c>, <c>auth.nonce</c> → <c>***</c></item>
///   <item><c>instrument.card.number</c>, <c>instrument.token.token</c> → keep last 4 digits only</item>
///   <item><c>instrument.card.cvv</c>, <c>instrument.pin</c>, <c>instrument.password</c>, <c>instrument.credit.code</c> → <c>***</c></item>
///   <item>Any field named (case-insensitive) <c>secretKey</c>, <c>cvv</c>, <c>pin</c>, <c>password</c>, <c>token</c> → <c>***</c></item>
/// </list>
/// </remarks>
internal sealed class RedactingJsonLogger
{
    private const string Mask = "***";

    private static readonly HashSet<string> HardRedactNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "tranKey",
        "nonce",
        "secretKey",
        "cvv",
        "pin",
        "password",
        "code",
    };

    private static readonly HashSet<string> MaskButShowLast4 = new(StringComparer.OrdinalIgnoreCase)
    {
        "number",
        "token",
    };

    private readonly ILogger _logger;

    public RedactingJsonLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogRequest(HttpMethod method, Uri uri, string? jsonBody)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var redacted = Redact(jsonBody);
        _logger.LogDebug("PlaceToPay HTTP {Method} {Uri} -> body: {Body}", method, uri, redacted ?? "<empty>");
    }

    public void LogResponse(HttpMethod method, Uri uri, int statusCode, string? jsonBody)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var redacted = Redact(jsonBody);
        _logger.LogDebug("PlaceToPay HTTP {Method} {Uri} <- {StatusCode}: {Body}", method, uri, statusCode, redacted ?? "<empty>");
    }

    /// <summary>
    /// Public-for-tests entry point. Returns the redacted JSON string, or the input
    /// unchanged when it is not parseable as JSON.
    /// </summary>
    public static string? Redact(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
            {
                RedactElement(doc.RootElement, writer);
            }
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        catch (JsonException)
        {
            // Not JSON — return a safe placeholder rather than the raw body.
            return "<non-json body; redacted>";
        }
    }

    private static void RedactElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    RedactProperty(property.Name, property.Value, writer);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var child in element.EnumerateArray())
                {
                    RedactElement(child, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void RedactProperty(string name, JsonElement value, Utf8JsonWriter writer)
    {
        if (HardRedactNames.Contains(name))
        {
            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(Mask);
            }
            return;
        }

        if (MaskButShowLast4.Contains(name) && value.ValueKind == JsonValueKind.String)
        {
            var raw = value.GetString() ?? string.Empty;
            writer.WriteStringValue(MaskLeavingLast4(raw));
            return;
        }

        RedactElement(value, writer);
    }

    private static string MaskLeavingLast4(string raw)
    {
        if (raw.Length <= 4)
        {
            return Mask;
        }
        return new string('*', raw.Length - 4) + raw[^4..];
    }
}
