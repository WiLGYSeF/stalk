using Shouldly;
using System.Text.Json;
using Wilgysef.Stalk.Core.Utilities;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.UtilitiesTests;

public class JsonUtilsTest : BaseTest
{
    [Fact]
    public void JsonElement_Value_Array()
    {
        var value = new object?[] { "abc", 123, null };
        var element = GetJsonElement(value);
        var result = JsonUtils.GetJsonElementValue(element, out var type);

        result.ShouldBe(value);
        type.ShouldBe(typeof(List<object?>));
    }

    [Fact]
    public void JsonElement_Value_False()
    {
        var element = GetJsonElement(false);
        var result = JsonUtils.GetJsonElementValue(element, out var type);

        ((bool)result!).ShouldBeFalse();
        type.ShouldBe(typeof(bool));
    }

    [Theory]
    [InlineData(-123, typeof(int))]
    [InlineData(123, typeof(int))]
    [InlineData(uint.MaxValue, typeof(long))]
    [InlineData((long)int.MaxValue + 1, typeof(long))]
    [InlineData((long)int.MinValue - 1, typeof(long))]
    [InlineData(ulong.MaxValue, typeof(ulong))]
    [InlineData((double)ulong.MaxValue + 1, typeof(double))]
    [InlineData(-123.45, typeof(double))]
    [InlineData(123.45, typeof(double))]
    public void JsonElement_Value_Number(object value, Type expectedType)
    {
        var element = GetJsonElement(value);
        var result = JsonUtils.GetJsonElementValue(element, out var type);

        result.ShouldBe(value);
        type.ShouldBe(expectedType);
    }

    [Fact]
    public void JsonElement_Value_Object()
    {
        var value = new
        {
            Test = "abc",
            Nested = new
            {
                Key = "value",
            },
        };

        var element = GetJsonElement(value);
        var result = (Dictionary<string, object?>)JsonUtils.GetJsonElementValue(element, out var type)!;

        result["test"].ShouldBe(value.Test);
        ((Dictionary<string, object?>)result["nested"]!)["key"].ShouldBe(value.Nested.Key);
        type.ShouldBe(typeof(Dictionary<string, object?>));
    }

    [Fact]
    public void JsonElement_Value_String()
    {
        var value = "test"
;        var element = GetJsonElement(value);
        var result = JsonUtils.GetJsonElementValue(element, out var type);

        result.ShouldBe(value);
        type.ShouldBe(typeof(string));
    }

    [Fact]
    public void JsonElement_Value_True()
    {
        var element = GetJsonElement(true);
        var result = JsonUtils.GetJsonElementValue(element, out var type);

        ((bool)result!).ShouldBeTrue();
        type.ShouldBe(typeof(bool));
    }

    private JsonElement GetJsonElement(object? obj)
    {
        var json = JsonSerializer.Deserialize<IDictionary<string, object?>>(
            JsonSerializer.Serialize(
                new
                {
                    Value = obj,
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }));
        return (JsonElement)json!["value"]!;
    }
}
