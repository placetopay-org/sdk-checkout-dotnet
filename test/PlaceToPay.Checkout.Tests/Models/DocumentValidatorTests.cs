using PlaceToPay.Checkout.Models.Common;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Models;

public class DocumentValidatorTests
{
    [Theory]
    [InlineData(DocumentType.CC)]
    [InlineData(DocumentType.NIT)]
    [InlineData(DocumentType.RUC)]
    [InlineData(DocumentType.CLRUT)]
    public void KnownTypes_AreRecognized(DocumentType type)
    {
        Assert.True(DocumentValidator.IsValidType(type));
    }

    [Theory]
    [InlineData(DocumentType.NIT, true)]
    [InlineData(DocumentType.RUT, true)]
    [InlineData(DocumentType.RUC, true)]
    [InlineData(DocumentType.CLRUT, true)]
    [InlineData(DocumentType.CC, false)]
    [InlineData(DocumentType.CPF, false)]
    [InlineData(DocumentType.DNI, false)]
    public void IsBusinessDocument_MatchesPhpSpec(DocumentType type, bool expected)
    {
        Assert.Equal(expected, DocumentValidator.IsBusinessDocument(type));
    }

    // ─── Pattern tests (one valid + one invalid per type) ────────────────

    [Theory]
    [InlineData(DocumentType.CC, "1040035000", true)]
    [InlineData(DocumentType.CC, "987654321", true)]
    [InlineData(DocumentType.CC, "0123456789", false)]
    [InlineData(DocumentType.CC, "abc123", false)]
    [InlineData(DocumentType.CE, "12345", true)]
    [InlineData(DocumentType.CE, "abcde12345", true)]
    [InlineData(DocumentType.CE, "012345", false)]
    [InlineData(DocumentType.TI, "12345", true)]
    [InlineData(DocumentType.TI, "1234", false)]
    [InlineData(DocumentType.NIT, "900123456", true)]
    [InlineData(DocumentType.NIT, "12345", false)]
    [InlineData(DocumentType.RUT, "900123456", true)]
    [InlineData(DocumentType.RUT, "abc", false)]
    [InlineData(DocumentType.PPN, "AB123", true)]
    [InlineData(DocumentType.PPN, "passport_001", true)]
    [InlineData(DocumentType.PPN, "abc", false)]
    [InlineData(DocumentType.PPN, "!@#$", false)]
    [InlineData(DocumentType.SSN, "123456789", true)]
    [InlineData(DocumentType.SSN, "12345", false)]
    [InlineData(DocumentType.CIP, "8-123-456", true)]
    [InlineData(DocumentType.CIP, "PE-1234-567890", true)]
    [InlineData(DocumentType.CIP, "invalid", false)]
    [InlineData(DocumentType.CPF, "12345678901", true)]
    [InlineData(DocumentType.CPF, "123.456.789-01", false)]
    [InlineData(DocumentType.CI, "1234567890", true)]
    [InlineData(DocumentType.CI, "12345", false)]
    [InlineData(DocumentType.CI, "12345678901", false)]
    [InlineData(DocumentType.RUC, "1234567890123", true)]
    [InlineData(DocumentType.RUC, "123456789", false)]
    [InlineData(DocumentType.DNI, "12345678", true)]
    [InlineData(DocumentType.DNI, "1234567", false)]
    [InlineData(DocumentType.CRCPF, "123456789", true)]
    [InlineData(DocumentType.CRCPF, "012345678", false)]
    [InlineData(DocumentType.CPJ, "1234567890", true)]
    [InlineData(DocumentType.CPJ, "123456789", false)]
    [InlineData(DocumentType.DIMEX, "12345678901", true)]
    [InlineData(DocumentType.DIDI, "123456789012", true)]
    [InlineData(DocumentType.DIMEX, "1234567890", false)]
    [InlineData(DocumentType.CLRUT, "12.345.678-9", true)]
    [InlineData(DocumentType.CLRUT, "12.345.678-K", true)]
    [InlineData(DocumentType.CLRUT, "12345678", false)]
    public void PatternValidation(DocumentType type, string document, bool expected)
    {
        Assert.Equal(expected, DocumentValidator.IsValid(type, document));
    }

    [Fact]
    public void StringOverload_UnknownTypeFailsSafely()
    {
        Assert.False(DocumentValidator.IsValid("XYZ", "anything"));
    }

    [Fact]
    public void StringOverload_RoundTrips()
    {
        Assert.True(DocumentValidator.IsValid("CC", "1040035000"));
        Assert.False(DocumentValidator.IsValid("CC", "abc"));
    }

    [Fact]
    public void StringOverload_CaseSensitive()
    {
        // PHP is case-sensitive; mirror that behavior.
        Assert.False(DocumentValidator.IsValid("cc", "1040035000"));
    }

    [Fact]
    public void SupportedTypes_HasNineteenEntries()
    {
        Assert.Equal(19, DocumentValidator.SupportedTypes.Count);
    }

    [Fact]
    public void LegacyEnumAliases_AreNotInSupportedSet()
    {
        // TE and PP are kept on the enum for source compatibility but the
        // PHP DocumentHelper does not list them.
        Assert.False(DocumentValidator.SupportedTypes.Contains(DocumentType.TE));
        Assert.False(DocumentValidator.SupportedTypes.Contains(DocumentType.PP));
    }
}
