using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Internal;

/// <summary>
/// Serializes enums using <see cref="EnumMemberAttribute.Value"/> when present.
/// Falls back to the enum member name when no attribute is declared.
/// </summary>
public sealed class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> ReadMap = BuildReadMap();
    private static readonly Dictionary<TEnum, string> WriteMap = BuildWriteMap();

    /// <inheritdoc/>
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token when parsing {typeof(TEnum).Name}.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException($"Empty value is not valid for {typeof(TEnum).Name}.");
        }

        if (ReadMap.TryGetValue(raw, out var mapped))
        {
            return mapped;
        }

        if (Enum.TryParse(raw, ignoreCase: true, out TEnum byName))
        {
            return byName;
        }

        throw new JsonException($"Value '{raw}' is not valid for {typeof(TEnum).Name}.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (WriteMap.TryGetValue(value, out var wireValue))
        {
            writer.WriteStringValue(wireValue);
            return;
        }

        writer.WriteStringValue(value.ToString());
    }

    private static Dictionary<string, TEnum> BuildReadMap()
    {
        var map = new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = value.ToString();
            var field = typeof(TEnum).GetField(name, BindingFlags.Public | BindingFlags.Static);
            var enumMember = field?.GetCustomAttribute<EnumMemberAttribute>();
            var wireValue = string.IsNullOrWhiteSpace(enumMember?.Value) ? name : enumMember!.Value!;

            map[wireValue] = value;
            map[name] = value;
        }

        return map;
    }

    private static Dictionary<TEnum, string> BuildWriteMap()
    {
        var map = new Dictionary<TEnum, string>();

        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = value.ToString();
            var field = typeof(TEnum).GetField(name, BindingFlags.Public | BindingFlags.Static);
            var enumMember = field?.GetCustomAttribute<EnumMemberAttribute>();
            map[value] = string.IsNullOrWhiteSpace(enumMember?.Value) ? name : enumMember!.Value!;
        }

        return map;
    }
}