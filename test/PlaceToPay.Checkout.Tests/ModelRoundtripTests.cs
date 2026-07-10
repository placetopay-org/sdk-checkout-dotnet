using System.Text.Json;
using PlaceToPay.Checkout.Errors;
using PlaceToPay.Checkout.Models.Common;
using PlaceToPay.Checkout.Models.Instrument;
using PlaceToPay.Checkout.Models.Reverse;
using PlaceToPay.Checkout.Models.Session;
using PlaceToPay.Checkout.Models.Transaction;
using Xunit;

namespace PlaceToPay.Checkout.Tests;

/// <summary>
/// Smoke coverage: every public POCO survives a JSON roundtrip (serialize →
/// deserialize → equal). This exercises constructors + property accessors
/// that aren't otherwise reached in behavioral tests.
/// </summary>
public class ModelRoundtripTests
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    [Fact] public void Address() => Roundtrip(new Address { Street = "St 1", City = "Bogotá", State = "DC", PostalCode = "110111", Country = "CO", Phone = "+57 1" });
    [Fact] public void NameValuePair() => Roundtrip(new NameValuePair { Keyword = "k", Value = "v", DisplayOn = "both" });
    [Fact] public void TaxDetail() => Roundtrip(new TaxDetail { Kind = "valueAddedTax", Amount = 190m, Base = 1000m });
    [Fact] public void AmountDetail() => Roundtrip(new AmountDetail { Kind = "discount", Amount = 10m });
    [Fact] public void AmountConversion() => Roundtrip(new AmountConversion { From = new AmountBase { Currency = "COP", Total = 100m }, To = new AmountBase { Currency = "USD", Total = 0.03m }, Factor = 0.0003m });
    [Fact] public void Item() => Roundtrip(new Item { Sku = "SKU-1", Name = "Thing", Category = "cat", Qty = "1", Price = 100m, Tax = 19m });
    [Fact] public void Card() => Roundtrip(new Card { Name = "T Buyer", Number = "4111111111111111", Cvv = "123", ExpirationMonth = "12", ExpirationYear = "30", Installments = 1 });
    [Fact] public void Bank() => Roundtrip(new Bank { Code = "1007", Name = "Bancolombia" });
    [Fact] public void Credit() => Roundtrip(new Credit { Code = "C1", Type = "installment", GroupCode = "G", Installment = 3 });
    [Fact] public void SubscriptionInformation() => Roundtrip(new SubscriptionInformation { Status = new Status { Code = StatusCode.Approved } });
    [Fact] public void Recurring() => Roundtrip(new Recurring { Interval = 1, MaxPeriods = 12, NotificationUrl = "https://example.com/hook" });
    [Fact] public void PaymentModifier() => Roundtrip(new PaymentModifier { Type = "FEDERAL_GOVERNMENT_ENTITY", Code = "X" });
    [Fact] public void DispersionItem() => Roundtrip(new DispersionItem { Agreement = "A", AgreementType = "T", Amount = new AmountBase { Currency = "COP", Total = 100m } });
    [Fact] public void SubscriptionRequest() => Roundtrip(new SubscriptionRequest { Reference = "R", Description = "D" });
    [Fact] public void AutopayRequest() => Roundtrip(new AutopayRequest { Interval = 1, MaxPeriods = 12, NotificationUrl = "https://example.com/hook" });
    [Fact] public void Transaction() => Roundtrip(new Transaction { Reference = "R", InternalReference = 1, Authorization = "A", PaymentMethod = "visa", Status = new Status { Code = StatusCode.Approved } });
    [Fact] public void ReverseResponse() => Roundtrip(new ReverseResponse { Status = new Status { Code = StatusCode.Approved } });

    [Fact]
    public void FieldError_Record_Roundtrips()
    {
        var fe = new ValidationException.FieldError("payment.reference", "required");
        var json = JsonSerializer.Serialize(fe, Opt);
        var back = JsonSerializer.Deserialize<ValidationException.FieldError>(json, Opt);
        Assert.Equal(fe, back);
    }

    private static void Roundtrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, Opt);
        var back = JsonSerializer.Deserialize<T>(json, Opt);
        Assert.NotNull(back);
        // Records have value equality; classes won't but we still want to prove
        // serialization round-trips cleanly.
        var json2 = JsonSerializer.Serialize(back, Opt);
        Assert.Equal(json, json2);
    }
}
