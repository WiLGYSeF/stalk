using System.Text.Json;

namespace Wilgysef.Stalk.Core.Utilities;

public static class JsonUtils
{
    /// <summary>
    /// Deserializes JSON.
    /// </summary>
    /// <typeparam name="T">Deserialized target type.</typeparam>
    /// <param name="json">JSON.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>Deserialized object.</returns>
    /// <exception cref="JsonException">The JSON is invalid or could not be deserialized.</exception>
    public static T? TryDeserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return TryDeserialize<T>(json, options, out _);
    }

    /// <summary>
    /// Deserializes JSON object.
    /// </summary>
    /// <param name="json">JSON.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>Deserialized JSON object.</returns>
    /// <exception cref="JsonException">The JSON is invalid or could not be deserialized.</exception>
    public static IDictionary<string, object?>? TryDeserializeObject(string json, JsonSerializerOptions? options = null)
    {
        var dict = TryDeserialize<IDictionary<string, object?>>(json, options, out _);
        if (dict != null)
        {
            foreach (var (key, value) in dict)
            {
                if (value is JsonElement element)
                {
                    dict[key] = GetJsonElementValue(element, out _);
                }
            }
        }
        return dict;
    }

    /// <summary>
    /// Deserializes JSON.
    /// </summary>
    /// <typeparam name="T">Deserialized target type.</typeparam>
    /// <param name="json">JSON.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <param name="jsonException">JSON exception if deserializing failed.</param>
    /// <returns>Deserialized object, or <see langword="default"/> if a <see cref="JsonException"/> occurred.</returns>
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

    /// <summary>
    /// Gets the value of the <see cref="JsonElement"/>, typed accordingly.
    /// </summary>
    /// <param name="element">JSON element.</param>
    /// <param name="type">Value type.</param>
    /// <returns>Value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The element's <see cref="JsonElement.ValueKind"/> is unknown.</exception>
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

    /// <summary>
    /// Gets the numeric value of the <see cref="JsonElement"/>, typed accordingly.
    /// </summary>
    /// <param name="element">JSON element.</param>
    /// <param name="type">Value type.</param>
    /// <returns>Value.</returns>
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
