using Shouldly;
using Wilgysef.Stalk.Core.CacheObjects;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.CacheObjectTests;

public class CacheObjectTest : BaseTest
{
    private DateTime ExpiredTime => DateTime.Now.AddSeconds(-5);

    [Fact]
    public void Set_Value()
    {
        var cache = new CacheObject<string, object?>();
        cache["a"] = 1;
        cache["a"].ShouldBe(1);

        cache["a"] = 2;
        cache["a"].ShouldBe(2);

        cache.Set("b", 2, ExpiredTime);
        cache.ContainsKey("b").ShouldBeFalse();
    }

    [Fact]
    public void Add_Value()
    {
        var cache = new CacheObject<string, object?>();
        cache.Add("a", 1);
        cache.Add("b", 2, ExpiredTime);
        cache.Add("b", 3, ExpiredTime);

        Should.Throw<ArgumentException>(() => cache.Add("a", 2));

        cache["a"].ShouldBe(1);
        cache.TryGetValue("a", out var value).ShouldBeTrue();
        value.ShouldBe(1);
        cache.ContainsKey("a").ShouldBeTrue();

        Should.Throw<ArgumentException>(() => cache["b"]);
        cache.TryGetValue("b", out _).ShouldBeFalse();
        cache.ContainsKey("b").ShouldBeFalse();
    }

    [Fact]
    public void TryAdd_Value()
    {
        var cache = new CacheObject<string, object?>();
        cache.TryAdd("a", 1).ShouldBeTrue();
        cache.TryAdd("a", 2).ShouldBeFalse();
        cache["a"].ShouldBe(1);

        cache.TryAdd("b", 2, ExpiredTime).ShouldBeTrue();
        cache.TryAdd("b", 3).ShouldBeTrue();
        cache["b"].ShouldBe(3);
    }

    [Fact]
    public void Remove_Value()
    {
        var cache = new CacheObject<string, object?>();
        cache["a"] = 1;
        cache.Set("b", 2, ExpiredTime);

        cache.Remove("a").ShouldBeTrue();
        cache.Remove("b").ShouldBeFalse();
        cache.Remove("c").ShouldBeFalse();

        cache.ContainsKey("a").ShouldBeFalse();
        cache.ContainsKey("b").ShouldBeFalse();
    }

    [Fact]
    public void Remove_Get_Value()
    {
        var cache = new CacheObject<string, object?>();
        cache["a"] = 1;
        cache.Set("b", 2, ExpiredTime);

        cache.Remove("a", out var a).ShouldBeTrue();
        cache.Remove("b", out var b).ShouldBeFalse();
        cache.Remove("c", out var c).ShouldBeFalse();

        a.ShouldBe(1);
        b.ShouldBe(default);
        c.ShouldBe(default);

        cache.ContainsKey("a").ShouldBeFalse();
        cache.ContainsKey("b").ShouldBeFalse();
    }

    [Fact]
    public void Clear_Values()
    {
        var cache = new CacheObject<string, object?>();
        cache["a"] = 1;
        cache.Set("b", 2, ExpiredTime);

        cache.Clear();

        cache.ContainsKey("a").ShouldBeFalse();
        cache.ContainsKey("b").ShouldBeFalse();
    }

    [Fact]
    public void Remove_Expired_Values()
    {
        var cache = new CacheObject<string, object?>();
        cache["a"] = 1;
        cache.Set("b", 2, ExpiredTime);

        cache.RemoveExpired().ShouldBe(1);
        cache.ContainsKey("a").ShouldBeTrue();
    }
}
