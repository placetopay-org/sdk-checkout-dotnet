using PlaceToPay.Checkout.Http;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Http;

public class RedactingJsonLoggerTests
{
    [Fact]
    public void Redact_MasksTranKeyAndNonce()
    {
        const string input = """{"auth":{"login":"visible","tranKey":"secret-digest","nonce":"secret-nonce","seed":"2026-04-22T15:30:45+00:00"}}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("\"login\":\"visible\"", result, StringComparison.Ordinal);
        Assert.Contains("\"tranKey\":\"***\"", result, StringComparison.Ordinal);
        Assert.Contains("\"nonce\":\"***\"", result, StringComparison.Ordinal);
        // Seed survives — note Utf8JsonWriter escapes '+' as + by default; the
        // date string itself passes through unchanged and remains debuggable.
        Assert.Contains("2026-04-22T15:30:45", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_MasksCardPanKeepingLast4()
    {
        const string input = """{"instrument":{"card":{"number":"4111111111111111","cvv":"123"}}}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("1111\"", result, StringComparison.Ordinal);         // last 4 preserved
        Assert.DoesNotContain("4111111111111111", result, StringComparison.Ordinal);
        Assert.Contains("\"cvv\":\"***\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_MasksTokenKeepingLast4()
    {
        const string input = """{"instrument":{"token":{"token":"tk-abcdefghij"}}}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("ghij", result, StringComparison.Ordinal);
        Assert.Contains("*", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_MasksPinPasswordAndSecretKey()
    {
        const string input = """{"instrument":{"pin":"1234","password":"secret"},"secretKey":"never-log"}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("\"pin\":\"***\"", result, StringComparison.Ordinal);
        Assert.Contains("\"password\":\"***\"", result, StringComparison.Ordinal);
        Assert.Contains("\"secretKey\":\"***\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_WalksArrays()
    {
        const string input = """{"payments":[{"instrument":{"card":{"cvv":"999"}}},{"instrument":{"card":{"cvv":"111"}}}]}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.DoesNotContain("\"999\"", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\"111\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_LeavesNonSensitiveFieldsUntouched()
    {
        const string input = """{"reference":"ORDER-1","amount":{"currency":"COP","total":50000}}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("\"reference\":\"ORDER-1\"", result, StringComparison.Ordinal);
        Assert.Contains("\"currency\":\"COP\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_ReturnsPlaceholderForNonJson()
    {
        var result = RedactingJsonLogger.Redact("not valid json {");
        Assert.NotNull(result);
        Assert.Contains("non-json", result!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Redact_PassesThroughNullOrEmpty()
    {
        Assert.Null(RedactingJsonLogger.Redact(null));
        Assert.Equal("", RedactingJsonLogger.Redact(""));
        Assert.Equal("   ", RedactingJsonLogger.Redact("   "));
    }

    [Fact]
    public void Redact_MasksShortNumberFully()
    {
        const string input = """{"card":{"number":"1234"}}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("***", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_MasksNumericCvv()
    {
        // cvv as a string — numeric-looking still masked.
        const string input = """{"card":{"cvv":"123"}}""";
        var result = RedactingJsonLogger.Redact(input);
        Assert.Contains("\"cvv\":\"***\"", result, StringComparison.Ordinal);
    }
}
