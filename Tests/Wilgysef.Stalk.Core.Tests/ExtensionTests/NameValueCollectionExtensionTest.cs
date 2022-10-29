using Shouldly;
using System.Collections.Specialized;
using Wilgysef.Stalk.Core.Shared.Extensions;

namespace Wilgysef.Stalk.Core.Tests.ExtensionTests;

public class NameValueCollectionExtensionTest
{
    [Fact]
    public void TryGetValue()
    {
        var collection = new NameValueCollection
        {
            { "one", "1" }
        };

        collection.TryGetValue("one", out var value).ShouldBeTrue();
        value.ShouldBe("1");

        collection.TryGetValue("two", out _).ShouldBeFalse();
    }
}
