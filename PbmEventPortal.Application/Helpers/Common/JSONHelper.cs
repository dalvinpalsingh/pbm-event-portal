using System.Globalization;
using System.Text.Json;

namespace PbmEventPortal.Application.Helpers.Common;

public class JSONHelper
{
    public static DateOnly? GetDateValueFromJSON(string payloadJson, string propertyName)
    {
        var value = GetStringValueFromJSON(payloadJson, propertyName);

        return DateOnly.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    public static List<Guid> GetGuidsFromJSON(string jsonArray)
    {
        var result = new List<Guid>();
        if (string.IsNullOrWhiteSpace(jsonArray)) return result;

        try
        {
            using var document = JsonDocument.Parse(jsonArray);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return result;

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var value = item.ValueKind == JsonValueKind.String
                    ? item.GetString()
                    : item.ToString();

                if (Guid.TryParse(value, out var id))
                {
                    result.Add(id);
                }
            }
        }
        catch
        {
            return [];
        }

        return result;
    }

    public static string GetStringValueFromJSON(string payloadJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty(propertyName, out var property)) return string.Empty;
            if (property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return string.Empty;

            return property.GetString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
