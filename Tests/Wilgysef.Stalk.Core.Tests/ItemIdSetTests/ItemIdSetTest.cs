using Shouldly;
using Wilgysef.Stalk.Core.ItemIdSetServices;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.ItemIdSetTests;

public class ItemIdSetTest : BaseTest
{
    [Fact]
    public void Add_Items()
    {
        var itemIds = new ItemIdSet();

        itemIds.Add("abc").ShouldBeTrue();
        itemIds.Add("def").ShouldBeTrue();
        itemIds.Add("def").ShouldBeFalse();

        itemIds.Count.ShouldBe(2);
        itemIds.Contains("abc").ShouldBeTrue();
        itemIds.Contains("def").ShouldBeTrue();
        itemIds.Contains("ghi").ShouldBeFalse();
    }

    [Fact]
    public void Add_Items_Constructor()
    {
        var itemIds = new ItemIdSet(new[]
        {
            "abc",
            "def",
            "def"
        });

        itemIds.Count.ShouldBe(2);
        itemIds.Contains("abc").ShouldBeTrue();
        itemIds.Contains("def").ShouldBeTrue();
    }

    [Fact]
    public void Clear_Items()
    {
        var itemIds = new ItemIdSet();

        itemIds.Add("abc");
        itemIds.Add("def");

        itemIds.Count.ShouldBe(2);
        itemIds.Clear();
        itemIds.Count.ShouldBe(0);
    }

    [Fact]
    public void Remove_Items()
    {
        var itemIds = new ItemIdSet();

        itemIds.Add("abc");
        itemIds.Remove("abc").ShouldBeTrue();
        itemIds.Remove("def").ShouldBeFalse();
        itemIds.Count.ShouldBe(0);
    }

    [Fact]
    public void Change_Tracking()
    {
        var itemIds = new ItemIdSet(new[]
        {
            "existing"
        });

        itemIds.Add("abc").ShouldBeTrue();
        itemIds.Add("def").ShouldBeTrue();

        itemIds.PendingItems.Count.ShouldBe(2);
        itemIds.PendingItems.ShouldContain("abc");
        itemIds.PendingItems.ShouldContain("def");

        itemIds.ResetChangeTracking().ShouldBe(2);
        itemIds.PendingItems.Count.ShouldBe(0);
        itemIds.Count.ShouldBe(3);
    }
}
