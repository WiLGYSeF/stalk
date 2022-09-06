using Shouldly;
using Wilgysef.Stalk.Core.MetadataObjects;

namespace Wilgysef.Stalk.Core.Tests.MetadataObjectTests;

public class MetadataObjectTest
{
    [Fact]
    public void Add_Values()
    {
        var metadata = new MetadataObject('.');

        metadata.AddValue("abc", 1);
        metadata.AddValue("def", 2);
        metadata.AddValue("aaa.asdf", 3);
        metadata.AddValue("aaa.abc", 4);

        metadata["abc"].ShouldBe(1);
        metadata["def"].ShouldBe(2);
        metadata["aaa.asdf"].ShouldBe(3);
        metadata["aaa.abc"].ShouldBe(4);
    }

    [Fact]
    public void Add_Values_Fail()
    {
        var metadata = new MetadataObject('.');

        metadata.AddValue("abc", 1);
        metadata.AddValue("asdf.aaa.abc", 1);

        Should.Throw<ArgumentException>(() =>
        {
            metadata.AddValue("abc.asdf", 2);
        });

        Should.Throw<ArgumentException>(() =>
        {
            metadata.AddValue("abc.asdf.aaaa", 2);
        });

        Should.Throw<ArgumentException>(() =>
        {
            metadata.AddValue("asdf", 2);
        });

        Should.Throw<ArgumentException>(() =>
        {
            metadata.AddValue("asdf.aaa", 2);
        });
    }

    [Fact]
    public void TryAdd_Values()
    {
        var metadata = new MetadataObject('.');

        metadata.TryAddValue("abc", 1).ShouldBeTrue();
        metadata.TryAddValue("abc", 2).ShouldBeFalse();
        metadata.TryAddValue("abc.asdf", 3).ShouldBeFalse();
        metadata.TryAddValue("aaa.asdf", 4).ShouldBeTrue();

        metadata["abc"].ShouldBe(1);
        metadata["aaa.asdf"].ShouldBe(4);
    }

    [Fact]
    public void Set_Values()
    {
        var metadata = new MetadataObject('.');

        metadata["abc"] = 1;
        metadata["abc"] = 2;

        Should.Throw<ArgumentException>(() =>
        {
            metadata["abc.asdf"] = 3;
        });

        metadata["aaa.asdf"] = 4;

        metadata["abc"].ShouldBe(2);
        metadata["aaa.asdf"].ShouldBe(4);
    }

    [Fact]
    public void Get_Values()
    {
        var metadata = new MetadataObject('.');

        metadata.AddValue("abc", 1);
        metadata.AddValue("aaa.asdf", 2);

        metadata.GetValue("abc").ShouldBe(1);
        metadata.GetValue("aaa.asdf").ShouldBe(2);

        Should.Throw<ArgumentException>(() =>
        {
            metadata.GetValue("asdf");
        });
        Should.Throw<ArgumentException>(() =>
        {
            metadata.GetValue("eee.abc");
        });
        Should.Throw<ArgumentException>(() =>
        {
            metadata.GetValue("aaa.abc");
        });
    }

    [Fact]
    public void TryGet_Values()
    {
        var metadata = new MetadataObject('.');

        metadata.AddValue("abc", 1);
        metadata.AddValue("aaa.asdf", 2);

        metadata.TryGetValue("abc", out var value).ShouldBeTrue();
        value.ShouldBe(1);

        metadata.TryGetValue("aaa.asdf", out value).ShouldBeTrue();
        value.ShouldBe(2);

        metadata.TryGetValue("aaa", out value).ShouldBeTrue();

        metadata.TryGetValue("asdf", out value).ShouldBeFalse();
        metadata.TryGetValue("eee.abc", out value).ShouldBeFalse();
        metadata.TryGetValue("aaa.abc", out value).ShouldBeFalse();
    }

    [Fact]
    public void Contains_Values()
    {
        var metadata = new MetadataObject('.');

        metadata.AddValue("abc", 1);
        metadata.AddValue("aaa.asdf", 2);

        metadata.ContainsValue("abc").ShouldBeTrue();
        metadata.ContainsValue("aaa").ShouldBeTrue();
        metadata.ContainsValue("aaa.asdf").ShouldBeTrue();

        metadata.ContainsValue("asdf").ShouldBeFalse();
        metadata.ContainsValue("aaa.abc").ShouldBeFalse();
    }

    [Fact]
    public void Remove_Values()
    {
        var metadata = new MetadataObject('.');

        metadata.AddValue("abc", 1);
        metadata.AddValue("aaa.asdf", 2);

        metadata.RemoveValue("abc").ShouldBeTrue();
        metadata.ContainsValue("abc").ShouldBeFalse();

        metadata.RemoveValue("aaa.asdf").ShouldBeTrue();
        metadata.ContainsValue("aaa.asdf").ShouldBeFalse();
        metadata.ContainsValue("aaa").ShouldBeTrue();

        metadata.RemoveValue("aaa").ShouldBeTrue();
        metadata.ContainsValue("aaa").ShouldBeFalse();

        metadata.RemoveValue("asdf").ShouldBeFalse();
        metadata.RemoveValue("aaa.abc").ShouldBeFalse();
    }

    [Fact]
    public void Copy()
    {
        var metadata = new MetadataObject('.');
        metadata["abc"] = 1;
        metadata["aaa.asdf.test"] = 2;
        metadata["aaa.aaa"] = 4;
        metadata["aaa.123"] = 99;
        metadata["aaa.nest.value"] = 5;
        metadata["test.key"] = 3;

        var copy = metadata.Copy();
        copy["abc"].ShouldBe(1);
        copy["aaa.asdf.test"].ShouldBe(2);
        copy["aaa.aaa"].ShouldBe(4);
        copy["aaa.123"].ShouldBe(99);
        copy["aaa.nest.value"].ShouldBe(5);
        copy["test.key"].ShouldBe(3);
    }

    [Fact]
    public void Get_Dictionary()
    {
        var metadata = new MetadataObject('.');
        metadata["abc"] = 1;
        metadata["aaa.asdf.test"] = 2;
        metadata["aaa.aaa"] = 4;
        metadata["aaa.123"] = 99;
        metadata["aaa.nest.value"] = 5;
        metadata["test.key"] = 3;

        var dictionary = metadata.GetDictionary();

        GetNestedValue(dictionary, "abc").ShouldBe(1);
        GetNestedValue(dictionary, "aaa", "asdf", "test").ShouldBe(2);
        GetNestedValue(dictionary, "aaa", "aaa").ShouldBe(4);
        GetNestedValue(dictionary, "aaa", "123").ShouldBe(99);
        GetNestedValue(dictionary, "aaa", "nest", "value").ShouldBe(5);
        GetNestedValue(dictionary, "test", "key").ShouldBe(3);
    }

    [Fact]
    public void From_Dictionary()
    {
        var dict = new Dictionary<object, object>
        {
            { "abc", 1 },
            {
                "aaa",
                new Dictionary<object, object>
                {
                    { "asdf.test", 2 },
                    { "aaa", 4 },
                    { 123, 99 },
                    {
                        "nest",
                        new Dictionary<object, object>
                        {
                            { "value", 5 },
                        }
                    }
                }
            },
            { "test.key", 3 },
        };
        var metadata = new MetadataObject('.');
        metadata.From(dict);

        metadata.GetValue("abc").ShouldBe(1);
        metadata.GetValue("aaa.asdf.test").ShouldBe(2);
        metadata.GetValue("aaa.aaa").ShouldBe(4);
        metadata.GetValue("aaa.123").ShouldBe(99);
        metadata.GetValue("aaa.nest.value").ShouldBe(5);
        metadata.GetValue("test.key").ShouldBe(3);
    }

    private object? GetNestedValue(IDictionary<string, object> dictionary, params string[] keys)
    {
        object? dict = dictionary;
        foreach (var key in keys)
        {
            dict = (dict as System.Collections.IDictionary)![key];
        }
        return dict;
    }
}
