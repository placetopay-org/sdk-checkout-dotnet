using System.Text.Json;
using PlaceToPay.Checkout.Authentication;
using PlaceToPay.Checkout.Internal;
using PlaceToPay.Checkout.Models.Collect;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Session;
using Xunit;
using Txn = PlaceToPay.Checkout.Models.Transaction.Transaction;

namespace PlaceToPay.Checkout.Tests;

/// <summary>
/// Tests for the PHP-parity improvements: <c>auth.additional</c>,
/// <c>CollectRequest.Provider</c>, <see cref="Discount"/>, <see cref="Status"/>
/// predicates, and <see cref="SessionInformation"/> transaction helpers.
/// </summary>
public class ImprovementsTests
{
    private static readonly byte[] RawNonce = Convert.FromHexString("00112233445566778899aabbccddeeff");
    private static readonly DateTimeOffset VectorSeed = DateTimeOffset.Parse("2026-04-22T15:30:45+00:00", System.Globalization.CultureInfo.InvariantCulture);

    // ─── auth.additional ────────────────────────────────────────────────

    [Fact]
    public void Auth_Additional_IsOmittedWhenNull()
    {
        var auth = AuthSigner.Sign("L", "S", VectorSeed, RawNonce);
        var json = JsonSerializer.Serialize(auth, JsonDefaults.Options);
        Assert.DoesNotContain("\"additional\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Auth_Additional_FlowsToWire()
    {
        var extra = new Dictionary<string, string> { ["tenant"] = "acme", ["channel"] = "web" };
        var auth = AuthSigner.Sign("L", "S", VectorSeed, RawNonce, extra);
        var json = JsonSerializer.Serialize(auth, JsonDefaults.Options);
        Assert.Contains("\"additional\"", json, StringComparison.Ordinal);
        Assert.Contains("\"tenant\"", json, StringComparison.Ordinal);
        Assert.Contains("\"acme\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Auth_AdditionalDoesNotChangeDigest()
    {
        var without = AuthSigner.Sign("L", "S", VectorSeed, RawNonce);
        var with = AuthSigner.Sign("L", "S", VectorSeed, RawNonce,
            new Dictionary<string, string> { ["x"] = "y" });
        Assert.Equal(without.TranKey, with.TranKey);
        Assert.Equal(without.Nonce, with.Nonce);
    }

    [Fact]
    public void Options_AuthAdditional_IsStored()
    {
        var options = new CheckoutClientOptions
        {
            Login = "L",
            SecretKey = "S",
            Environment = PlaceToPay.Checkout.Environment.Sandbox,
            AuthAdditional = new Dictionary<string, string> { ["mid"] = "42" },
        };
        Assert.NotNull(options.AuthAdditional);
        Assert.Equal("42", options.AuthAdditional!["mid"]);
    }

    // ─── CollectRequest.Provider ────────────────────────────────────────

    [Fact]
    public void CollectRequest_Provider_IsOptional()
    {
        var req = new CollectRequest
        {
            Payer = new Person { Document = "1040035000", DocumentType = DocumentType.CC },
            Payment = new Payment
            {
                Reference = "r1",
                Description = "d",
                Amount = new Amount { Currency = "COP", Total = 10_000m },
            },
            Instrument = new PlaceToPay.Checkout.Models.Instrument.Instrument
            {
                Token = new PlaceToPay.Checkout.Models.Instrument.Token { TokenValue = "tok-abc" },
            },
        };
        var json = JsonSerializer.Serialize(req, JsonDefaults.Options);
        Assert.DoesNotContain("\"provider\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectRequest_Provider_SerializesWhenSet()
    {
        var req = new CollectRequest
        {
            Provider = "evertec",
            Payer = new Person { Document = "1040035000", DocumentType = DocumentType.CC },
            Payment = new Payment
            {
                Reference = "r2",
                Description = "d",
                Amount = new Amount { Currency = "COP", Total = 10_000m },
            },
            Instrument = new PlaceToPay.Checkout.Models.Instrument.Instrument
            {
                Token = new PlaceToPay.Checkout.Models.Instrument.Token { TokenValue = "tok-abc" },
            },
        };
        var json = JsonSerializer.Serialize(req, JsonDefaults.Options);
        Assert.Contains("\"provider\":\"evertec\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void CollectRequest_Enums_SerializeUsingEnumMemberValues()
    {
        var req = new CollectRequest
        {
            Payer = new Person { Document = "1040035000", DocumentType = DocumentType.CC },
            Payment = new Payment
            {
                Reference = "r3",
                Description = "d",
                Amount = new Amount { Currency = "COP", Total = 10_000m },
            },
            Instrument = new PlaceToPay.Checkout.Models.Instrument.Instrument
            {
                Card = new PlaceToPay.Checkout.Models.Instrument.Card
                {
                    Number = "4111111111111111",
                    ExpirationMonth = "12",
                    ExpirationYear = "29",
                    Cvv = "123",
                    Kind = PlaceToPay.Checkout.Models.Instrument.CardKind.Credit,
                },
            },
        };

        var json = JsonSerializer.Serialize(req, JsonDefaults.Options);

        Assert.Contains("\"documentType\":\"CC\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"C\"", json, StringComparison.Ordinal);
    }

    // ─── Discount entity ────────────────────────────────────────────────

    [Fact]
    public void Discount_Roundtrips()
    {
        var d = new Discount { Code = "XMAS25", Type = "percentage", Amount = 25m, Base = 100m, Percent = 25m };
        var json = JsonSerializer.Serialize(d, JsonDefaults.Options);
        var restored = JsonSerializer.Deserialize<Discount>(json, JsonDefaults.Options);
        Assert.Equal(d, restored);
    }

    [Fact]
    public void Transaction_ParsesNestedDiscount()
    {
        const string json = """
        {
          "status": {"status":"approved","date":"2026-04-22T15:30:45+00:00"},
          "discount": {"code":"PROMO","amount":5000.0,"base":50000.0}
        }
        """;
        var tx = JsonSerializer.Deserialize<Txn>(json, JsonDefaults.Options);
        Assert.NotNull(tx);
        Assert.NotNull(tx!.Discount);
        Assert.Equal("PROMO", tx.Discount!.Code);
        Assert.Equal(5000m, tx.Discount.Amount);
    }

    // ─── Status predicates ──────────────────────────────────────────────

    [Theory]
    [InlineData(StatusCode.Approved, true, false, false, true)]
    [InlineData(StatusCode.Rejected, false, true, false, false)]
    [InlineData(StatusCode.Error, false, false, true, false)]
    [InlineData(StatusCode.Pending, false, false, false, false)]
    [InlineData(StatusCode.ApprovedPartial, false, false, false, true)]
    public void StatusPredicates_MatchExpected(
        StatusCode code, bool approved, bool rejected, bool error, bool successful)
    {
        var s = new Status { Code = code };
        Assert.Equal(approved, s.IsApproved);
        Assert.Equal(rejected, s.IsRejected);
        Assert.Equal(error, s.IsError);
        Assert.Equal(successful, s.IsSuccessful);
    }

    [Fact]
    public void Status_WithNullCode_IsNoneOfThem()
    {
        var s = new Status();
        Assert.False(s.IsApproved);
        Assert.False(s.IsRejected);
        Assert.False(s.IsError);
        Assert.False(s.IsSuccessful);
    }

    // ─── SessionInformation helpers ─────────────────────────────────────

    private static Txn Txn(StatusCode code, string? authorization = null) => new()
    {
        Status = new Status { Code = code, Date = DateTimeOffset.UtcNow },
        Authorization = authorization,
    };

    [Fact]
    public void NoTransactions_HelpersReturnNoneOrEmpty()
    {
        var info = new SessionInformation { RequestId = 1, Status = new Status { Code = StatusCode.Ok } };
        Assert.Null(info.LastTransaction());
        Assert.Null(info.LastApprovedTransaction);
        Assert.Equal(string.Empty, info.LastAuthorization());
    }

    [Fact]
    public void LastTransaction_DefaultReturnsTail()
    {
        var t1 = Txn(StatusCode.Rejected);
        var t2 = Txn(StatusCode.Approved, "AUTH-123");
        var info = new SessionInformation
        {
            RequestId = 1,
            Status = new Status { Code = StatusCode.Ok },
            Payment = new[] { t1, t2 },
        };
        Assert.Same(t2, info.LastTransaction());
    }

    [Fact]
    public void LastApprovedTransaction_WalksList()
    {
        var approved = Txn(StatusCode.Approved, "A");
        var rejected = Txn(StatusCode.Rejected);
        var info = new SessionInformation
        {
            RequestId = 1,
            Status = new Status { Code = StatusCode.Ok },
            Payment = new[] { approved, rejected },
        };
        Assert.Same(approved, info.LastApprovedTransaction);
        Assert.Equal("A", info.LastAuthorization());
    }

    [Fact]
    public void LastAuthorization_EmptyWhenNoneApproved()
    {
        var info = new SessionInformation
        {
            RequestId = 1,
            Status = new Status { Code = StatusCode.Ok },
            Payment = new[] { Txn(StatusCode.Rejected), Txn(StatusCode.Failed) },
        };
        Assert.Equal(string.Empty, info.LastAuthorization());
        Assert.Null(info.LastApprovedTransaction);
    }

    // ─── Transaction predicates ─────────────────────────────────────────

    [Fact]
    public void Transaction_IsApproved_ReflectsStatus()
    {
        Assert.True(Txn(StatusCode.Approved).IsApproved);
        Assert.False(Txn(StatusCode.Rejected).IsApproved);
        Assert.False(new Txn().IsApproved);  // null status
    }

    [Fact]
    public void Transaction_IsRejected_ReflectsStatus()
    {
        Assert.True(Txn(StatusCode.Rejected).IsRejected);
        Assert.False(Txn(StatusCode.Approved).IsRejected);
    }

    [Fact]
    public void Transaction_IsSuccessful_CoversApprovedAndPartial()
    {
        Assert.True(Txn(StatusCode.Approved).IsSuccessful);
        Assert.True(Txn(StatusCode.ApprovedPartial).IsSuccessful);
        Assert.True(Txn(StatusCode.Ok).IsSuccessful);
        Assert.False(Txn(StatusCode.Rejected).IsSuccessful);
    }
}
