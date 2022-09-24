using System.Text.Json;

namespace Wilgysef.Stalk.Core.Utilities;

public static class JsonUtils
{
    public static T? TryDeserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return TryDeserialize<T>(json, options, out _);
    }

    public static T? TryDeserialize<T>(string json, JsonSerializerOptions? options, out JsonException? jsonException)
    {
        try
        {
            jsonException = default;
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException exception)
        {
            jsonException = exception;
            return default;
        }
    }

    public static object? GetJsonElementValue(JsonElement element, out Type? type)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                type = typeof(List<object?>);
                return element.EnumerateArray()
                    .Select(i => GetJsonElementValue(i, out _))
                    .ToList();
            case JsonValueKind.False:
                type = typeof(bool);
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                type = null;
                return null;
            case JsonValueKind.Number:
                return GetJsonElementNumber(element, out type);
            case JsonValueKind.Object:
                type = typeof(Dictionary<string, object?>);
                return element.EnumerateObject()
                    .Select(p => (p.Name, GetJsonElementValue(p.Value, out _)))
                    .ToDictionary(p => p.Name, p => p.Item2);
            case JsonValueKind.String:
                type = typeof(string);
                return element.GetString();
            case JsonValueKind.True:
                type = typeof(bool);
                return true;
            default:
                type = null;
                throw new ArgumentOutOfRangeException(nameof(element), "Unknown JsonValueKind");
        }
    }

    public static object GetJsonElementNumber(JsonElement element, out Type type)
    {
        if (element.TryGetInt32(out var intValue))
        {
            type = typeof(int);
            return intValue;
        }
        if (element.TryGetInt64(out var longValue))
        {
            type = typeof(long);
            return longValue;
        }
        if (element.TryGetUInt64(out var ulongValue))
        {
            type = typeof(ulong);
            return ulongValue;
        }

        var doubleValue = element.GetDouble();
        type = typeof(double);
        return doubleValue;
    }
}
