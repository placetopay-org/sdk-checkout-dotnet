using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaceToPay.Checkout.Internal;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used by both the transport and public
/// helper APIs. Thread-safe and cached.
/// </summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Build(writeIndented: false);

    public static readonly JsonSerializerOptions IndentedOptions = Build(writeIndented: true);

    private static JsonSerializerOptions Build(bool writeIndented)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = writeIndented,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
