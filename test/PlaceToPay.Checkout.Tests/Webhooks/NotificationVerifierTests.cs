using System.Security.Cryptography;
using System.Text;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Webhooks;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Webhooks;

public class NotificationVerifierTests
{
    private const string Secret = "test-secret";

    private static NotificationPayload Payload(StatusCode code, DateTimeOffset date, string sig) =>
        new()
        {
            RequestId = 12345,
            Reference = "ORDER-1",
            Signature = sig,
            Status = new Status
            {
                Code = code,
                Reason = "00",
                Message = "ok",
                Date = date,
            },
        };

    private static string Hash(string algorithm, string s)
    {
        using var hasher = algorithm switch
        {
            "SHA-256" => (HashAlgorithm)SHA256.Create(),
            "SHA-1" => SHA1.Create(),
            _ => throw new ArgumentException(algorithm),
        };
        return Convert.ToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
    }

    private static string BaseString(long requestId, StatusCode code, DateTimeOffset date, string secret) =>
        $"{requestId}{code.ToString().ToUpperInvariant()}{date:yyyy-MM-ddTHH:mm:sszzz}{secret}";

    [Fact]
    public void VerifySha256_ReturnsTrueForValidSignature()
    {
        var date = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var baseStr = BaseString(12345, StatusCode.Approved, date, Secret);
        var payload = Payload(StatusCode.Approved, date, Hash("SHA-256", baseStr));
        Assert.True(NotificationVerifier.VerifySha256(payload, Secret));
    }

    [Fact]
    public void VerifySha256_ReturnsFalseForTamperedStatus()
    {
        var date = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var sig = Hash("SHA-256", BaseString(12345, StatusCode.Approved, date, Secret));
        // Payload now claims REJECTED but carries the APPROVED signature.
        var payload = Payload(StatusCode.Rejected, date, sig);
        Assert.False(NotificationVerifier.VerifySha256(payload, Secret));
    }

    [Fact]
    public void VerifySha256_ReturnsFalseForWrongSecret()
    {
        var date = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var sig = Hash("SHA-256", BaseString(12345, StatusCode.Approved, date, Secret));
        var payload = Payload(StatusCode.Approved, date, sig);
        Assert.False(NotificationVerifier.VerifySha256(payload, "different-secret"));
    }

    [Fact]
    public void VerifySha256_RejectsSha1Signature()
    {
        var date = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var sig = Hash("SHA-1", BaseString(12345, StatusCode.Approved, date, Secret));
        var payload = Payload(StatusCode.Approved, date, sig);
        Assert.False(NotificationVerifier.VerifySha256(payload, Secret));
    }

    [Fact]
    public void VerifySha256_RejectsEmptySignature()
    {
        var payload = Payload(StatusCode.Approved, DateTimeOffset.UtcNow, "");
        Assert.False(NotificationVerifier.VerifySha256(payload, Secret));
    }

    [Fact]
    public void VerifySha256_ThrowsForNullPayload()
    {
        Assert.Throws<ArgumentNullException>(
            () => NotificationVerifier.VerifySha256(null!, Secret));
    }

    [Fact]
    public void VerifySha256_ThrowsForEmptySecret()
    {
        var payload = Payload(StatusCode.Approved, DateTimeOffset.UtcNow, "sig");
        Assert.Throws<ArgumentException>(
            () => NotificationVerifier.VerifySha256(payload, ""));
    }

    [Fact]
#pragma warning disable CS0618
    public void VerifySha1Legacy_ReturnsTrueForValidSignature()
    {
        var date = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var sig = Hash("SHA-1", BaseString(12345, StatusCode.Approved, date, Secret));
        var payload = Payload(StatusCode.Approved, date, sig);
        Assert.True(NotificationVerifier.VerifySha1Legacy(payload, Secret));
    }

    [Fact]
    public void VerifySha1Legacy_RejectsSha256Signature()
    {
        var date = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var sig = Hash("SHA-256", BaseString(12345, StatusCode.Approved, date, Secret));
        var payload = Payload(StatusCode.Approved, date, sig);
        Assert.False(NotificationVerifier.VerifySha1Legacy(payload, Secret));
    }
#pragma warning restore CS0618
}
