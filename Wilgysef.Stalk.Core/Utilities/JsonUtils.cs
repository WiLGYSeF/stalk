using System.Text.Json;

namespace Wilgysef.Stalk.Core.Utilities;

public static class JsonUtils
{
    public static T? TryDeserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return TryDeserialize<T>(json, options, out _);
    }

    public static T? TryDeserialize<T>(string json, out JsonException? jsonException)
    {
        return TryDeserialize<T>(json, null, out jsonException);
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
}
