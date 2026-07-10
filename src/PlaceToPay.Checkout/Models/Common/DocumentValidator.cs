using System.Text.RegularExpressions;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Client-side document-number validation for LatAm identity / tax IDs.
/// Ported from the PHP SDK's <c>Dnetix\Redirection\Helpers\DocumentHelper</c>
/// with byte-exact regex patterns so decisions are identical across languages.
/// </summary>
/// <remarks>
/// <para>
/// Use this to reject obviously-malformed <c>Person.Document</c> values
/// before hitting the gateway — the server will still validate
/// authoritatively, but catching bad input locally saves a round-trip and
/// improves UX.
/// </para>
/// <para>
/// Unknown types fail validation. Known types with no registered pattern
/// pass through.
/// </para>
/// </remarks>
public static class DocumentValidator
{
    /// <summary>Document types the validator recognizes.</summary>
    public static IReadOnlySet<DocumentType> SupportedTypes { get; } =
        new HashSet<DocumentType>
        {
            DocumentType.CC,    DocumentType.CE,    DocumentType.TI,
            DocumentType.NIT,   DocumentType.RUT,
            DocumentType.PPN,   DocumentType.TAX,   DocumentType.LIC,
            DocumentType.SSN,   DocumentType.CIP,   DocumentType.CPF,
            DocumentType.CI,    DocumentType.RUC,   DocumentType.DNI,
            DocumentType.CRCPF, DocumentType.CPJ,
            DocumentType.DIMEX, DocumentType.DIDI,  DocumentType.CLRUT,
        };

    /// <summary>Types that identify a business rather than a natural person.</summary>
    public static IReadOnlySet<DocumentType> BusinessDocumentTypes { get; } =
        new HashSet<DocumentType>
        {
            DocumentType.NIT,
            DocumentType.RUT,
            DocumentType.RUC,
            DocumentType.CLRUT,
        };

    // ─── Patterns (byte-exact port from PHP SDK DocumentHelper) ───────────
    private static readonly RegexOptions Opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly Dictionary<DocumentType, Regex> Patterns = new()
    {
        [DocumentType.CC] = new Regex(@"^[1-9][0-9]{3,9}$", Opts),
        [DocumentType.CE] = new Regex(@"^([a-zA-Z]{1,5})?[1-9][0-9]{3,7}$", Opts),
        [DocumentType.TI] = new Regex(@"^[1-9][0-9]{4,11}$", Opts),
        [DocumentType.NIT] = new Regex(@"^[1-9]\d{6,9}$", Opts),
        [DocumentType.RUT] = new Regex(@"^[1-9]\d{6,9}$", Opts),
        [DocumentType.PPN] = new Regex(@"^[a-zA-Z0-9_]{4,16}$", Opts),
        [DocumentType.TAX] = new Regex(@"^[a-zA-Z0-9_]{4,16}$", Opts),
        [DocumentType.LIC] = new Regex(@"^[a-zA-Z0-9_]{4,16}$", Opts),
        [DocumentType.SSN] = new Regex(@"^\d{3}\d{2,3}\d{4}$", Opts),
        [DocumentType.CIP] = new Regex(@"^(PE|E|N|[23456789](?:AV|PI)?|1[0123]?(?:AV|PI)?)-?(\d{1,4})-?(\d{1,6})$", Opts),
        [DocumentType.CPF] = new Regex(@"^\d{10,11}$", Opts),
        [DocumentType.CI] = new Regex(@"^\d{10}$", Opts),
        [DocumentType.RUC] = new Regex(@"^\d{13}$", Opts),
        [DocumentType.DNI] = new Regex(@"^\d{8}$", Opts),
        [DocumentType.CRCPF] = new Regex(@"^[1-9][0-9]{8}$", Opts),
        [DocumentType.CPJ] = new Regex(@"^[1-9][0-9]{9}$", Opts),
        [DocumentType.DIMEX] = new Regex(@"^[1-9][0-9]{10,11}$", Opts),
        [DocumentType.DIDI] = new Regex(@"^[1-9][0-9]{10,11}$", Opts),
        [DocumentType.CLRUT] = new Regex(@"^(\d{1,2}(?:\.?\d{1,3}){2}-[\dkK])$", Opts),
    };

    /// <summary>True when the given <paramref name="type"/> is a recognized document-type code.</summary>
    public static bool IsValidType(DocumentType type) => SupportedTypes.Contains(type);

    /// <summary>
    /// Validate <paramref name="document"/> against the pattern registered for
    /// <paramref name="type"/>. Returns <c>false</c> for unknown types; returns
    /// <c>true</c> for known types that have no registered pattern.
    /// </summary>
    public static bool IsValid(DocumentType type, string document)
    {
        if (!IsValidType(type))
        {
            return false;
        }
        if (!Patterns.TryGetValue(type, out var pattern))
        {
            return true;
        }
        return document is not null && pattern.IsMatch(document);
    }

    /// <summary>Convenience string overload — useful when <paramref name="typeCode"/> comes from user input.</summary>
    public static bool IsValid(string typeCode, string document)
    {
        if (string.IsNullOrEmpty(typeCode) || !Enum.TryParse<DocumentType>(typeCode, ignoreCase: false, out var t))
        {
            return false;
        }
        return IsValid(t, document);
    }

    /// <summary>True when <paramref name="type"/> identifies a business (NIT / RUT / RUC / CLRUT).</summary>
    public static bool IsBusinessDocument(DocumentType type) => BusinessDocumentTypes.Contains(type);
}
