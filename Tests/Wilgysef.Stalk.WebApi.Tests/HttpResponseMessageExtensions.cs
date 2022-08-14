﻿using System.Text.Json;

namespace Wilgysef.Stalk.WebApi.Tests;

internal static class HttpResponseMessageExtensions
{
    public static async Task<T> EnsureSuccessAndDeserializeContent<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        }

        var deserialized = Deserialize<T>(content);
        if (deserialized == null)
        {
            throw new InvalidOperationException("Could not deserialize response.");
        }

        return deserialized;
    }

    private static T? Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }
}
