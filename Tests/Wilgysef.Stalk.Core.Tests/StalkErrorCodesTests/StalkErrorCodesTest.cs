using Shouldly;
using Wilgysef.Stalk.Core.Shared;

namespace Wilgysef.Stalk.Core.Tests.StalkErrorCodesTests;

public class StalkErrorCodesTest
{
    [Fact]
    public void No_Duplicate_Error_Codes()
    {
        var fields = typeof(StalkErrorCodes).GetFields();
        var valueMap = new Dictionary<object, List<string>>();

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(string))
            {
                continue;
            }

            if (!valueMap.TryGetValue(field.GetValue(null)!, out var names))
            {
                names = new List<string>();
                valueMap.Add(field.GetValue(null)!, names);
            }

            names.Add(field.Name);
        }

        valueMap.ShouldNotBeEmpty();

        foreach (var keys in valueMap.Values)
        {
            keys.Count.ShouldBe(1, $"Duplicate error code values: {string.Join(", ", keys)}");
        }
    }
}
