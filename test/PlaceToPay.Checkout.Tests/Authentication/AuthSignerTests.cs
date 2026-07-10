using System.Security.Cryptography;
using System.Text;
using PlaceToPay.Checkout.Authentication;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Authentication;

/// <summary>
/// Regression tests for <see cref="AuthSigner"/>. Most importantly the
/// <c>cross-SDK test vector</c> — given identical inputs, Python, Java and .NET
/// SDKs must produce byte-exact output.
/// </summary>
public class AuthSignerTests
{
    private const string Login = "SDK_TEST_LOGIN";
    private const string SecretKey = "SDK_TEST_SECRET";
    private const string Seed = "2026-04-22T15:30:45+00:00";

    private static readonly byte[] RawNonce = Convert.FromHexString("00112233445566778899aabbccddeeff");
    private const string ExpectedWireNonce = "ABEiM0RVZneImaq7zN3u/w==";

    [Fact(DisplayName = "deterministic vector reproduces across languages (wire nonce)")]
    public void TestVector_WireNonce()
    {
        var timestamp = DateTimeOffset.Parse(Seed, System.Globalization.CultureInfo.InvariantCulture);
        var auth = AuthSigner.Sign(Login, SecretKey, timestamp, RawNonce);
        Assert.Equal(ExpectedWireNonce, auth.Nonce);
    }

    [Fact(DisplayName = "deterministic vector: tranKey is base64(sha256(rawNonce || utf8(seed) || utf8(secretKey)))")]
    public void TestVector_TranKey()
    {
        var timestamp = DateTimeOffset.Parse(Seed, System.Globalization.CultureInfo.InvariantCulture);
        var auth = AuthSigner.Sign(Login, SecretKey, timestamp, RawNonce);

        // Compute the expected value from first principles. If these bytes match the
        // Java and Python SDKs for the same inputs, all three agree on the wire.
        var seedBytes = Encoding.UTF8.GetBytes(auth.Seed);
        var secretBytes = Encoding.UTF8.GetBytes(SecretKey);
        var buffer = new byte[RawNonce.Length + seedBytes.Length + secretBytes.Length];
        Buffer.BlockCopy(RawNonce, 0, buffer, 0, RawNonce.Length);
        Buffer.BlockCopy(seedBytes, 0, buffer, RawNonce.Length, seedBytes.Length);
        Buffer.BlockCopy(secretBytes, 0, buffer, RawNonce.Length + seedBytes.Length, secretBytes.Length);

        var expected = Convert.ToBase64String(SHA256.HashData(buffer));
        Assert.Equal(expected, auth.TranKey);
    }

    [Fact(DisplayName = "login and seed flow through verbatim")]
    public void LoginAndSeed_FlowThrough()
    {
        var timestamp = DateTimeOffset.Parse(Seed, System.Globalization.CultureInfo.InvariantCulture);
        var auth = AuthSigner.Sign(Login, SecretKey, timestamp, RawNonce);
        Assert.Equal(Login, auth.Login);
        Assert.Equal(Seed, auth.Seed);
    }

    [Fact(DisplayName = "GenerateNonce produces 16 random bytes")]
    public void GenerateNonce_Length()
    {
        var a = AuthSigner.GenerateNonce();
        var b = AuthSigner.GenerateNonce();
        Assert.Equal(16, a.Length);
        Assert.Equal(16, b.Length);
        Assert.NotEqual(a, b);
    }

    [Fact(DisplayName = "FormatSeed uses '+00:00' (not 'Z') for UTC")]
    public void FormatSeed_UsesNumericOffset()
    {
        var utc = new DateTimeOffset(2026, 4, 22, 15, 30, 45, TimeSpan.Zero);
        var seed = AuthSigner.FormatSeed(utc);
        Assert.Equal("2026-04-22T15:30:45+00:00", seed);
    }

    [Fact(DisplayName = "Sign rejects empty credentials")]
    public void Sign_RejectsEmptyCredentials()
    {
        var nonce = AuthSigner.GenerateNonce();
        var ts = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => AuthSigner.Sign("", SecretKey, ts, nonce));
        Assert.Throws<ArgumentException>(() => AuthSigner.Sign(Login, "", ts, nonce));
    }

    [Fact(DisplayName = "Sign rejects wrong-size nonce")]
    public void Sign_RejectsBadNonce()
    {
        var ts = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => AuthSigner.Sign(Login, SecretKey, ts, new byte[15]));
        Assert.Throws<ArgumentException>(() => AuthSigner.Sign(Login, SecretKey, ts, new byte[17]));
    }
}
