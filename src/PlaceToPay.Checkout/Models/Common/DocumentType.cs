using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using PlaceToPay.Checkout.Internal;

namespace PlaceToPay.Checkout.Models.Common;

/// <summary>
/// Identity document type. See <see cref="DocumentValidator"/> for
/// per-country regex validation. See <see cref="Person.DocumentType"/>.
/// </summary>
[JsonConverter(typeof(EnumMemberJsonConverter<DocumentType>))]
public enum DocumentType
{
    // ─── Colombia ────────────────────────────────────────────────────
    /// <summary>Colombian citizen ID (Cédula de Ciudadanía).</summary>
    [EnumMember(Value = "CC")] CC,
    /// <summary>Foreign ID (Cédula de Extranjería).</summary>
    [EnumMember(Value = "CE")] CE,
    /// <summary>Minor ID (Tarjeta de Identidad).</summary>
    [EnumMember(Value = "TI")] TI,
    /// <summary>Birth certificate (Registro Civil).</summary>
    [EnumMember(Value = "RC")] RC,
    /// <summary>Colombian business tax ID.</summary>
    [EnumMember(Value = "NIT")] NIT,
    /// <summary>Rol Único Tributario (business).</summary>
    [EnumMember(Value = "RUT")] RUT,

    // ─── Generic ─────────────────────────────────────────────────────
    /// <summary>Passport.</summary>
    [EnumMember(Value = "PPN")] PPN,
    /// <summary>Tax ID (generic).</summary>
    [EnumMember(Value = "TAX")] TAX,
    /// <summary>License.</summary>
    [EnumMember(Value = "LIC")] LIC,
    /// <summary>Generic identifier (CD).</summary>
    [EnumMember(Value = "CD")] CD,

    // ─── Country-specific ────────────────────────────────────────────
    /// <summary>US Social Security Number.</summary>
    [EnumMember(Value = "SSN")] SSN,
    /// <summary>Panama identity document.</summary>
    [EnumMember(Value = "CIP")] CIP,
    /// <summary>Brazil CPF.</summary>
    [EnumMember(Value = "CPF")] CPF,
    /// <summary>Ecuador citizen ID (Cédula de Identidad).</summary>
    [EnumMember(Value = "CI")] CI,
    /// <summary>Ecuador business tax ID (Registro Único Contribuyente).</summary>
    [EnumMember(Value = "RUC")] RUC,
    /// <summary>Peru DNI.</summary>
    [EnumMember(Value = "DNI")] DNI,
    /// <summary>Costa Rica personal physical ID.</summary>
    [EnumMember(Value = "CRCPF")] CRCPF,
    /// <summary>Costa Rica personal juridical ID (business).</summary>
    [EnumMember(Value = "CPJ")] CPJ,
    /// <summary>Costa Rica Migration &amp; Extranjería document.</summary>
    [EnumMember(Value = "DIMEX")] DIMEX,
    /// <summary>Costa Rica diplomatic identification.</summary>
    [EnumMember(Value = "DIDI")] DIDI,
    /// <summary>Chile Rol Único Tributario (business).</summary>
    [EnumMember(Value = "CLRUT")] CLRUT,

    // ─── Legacy aliases kept for source compatibility ────────────────
    /// <summary>Temporary card. Kept from the legacy enum.</summary>
    [EnumMember(Value = "TE")] TE,
    /// <summary>Passport (short form). Kept from the legacy enum.</summary>
    [EnumMember(Value = "PP")] PP,
}
