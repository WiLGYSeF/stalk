using Shouldly;
using Wilgysef.Stalk.Core.ObjectInstances;

namespace Wilgysef.Stalk.Core.Tests.ObjectInstanceCollectionTests;

public class ObjectInstanceCollectionTest
{
    [Fact]
    public void GetHandle_New()
    {
        var instances = new ObjectInstanceCollection<string, TestObject>();

        using var handle1 = instances.GetHandle("abc", () => new TestObject(1));
        using var handle2 = instances.GetHandle("def", () => new TestObject(2));

        handle1.Value.ShouldNotBe(handle2.Value);
        handle1.Value.Number.ShouldBe(1);
        handle2.Value.Number.ShouldBe(2);
    }

    [Fact]
    public void GetHandle_Existing()
    {
        var instances = new ObjectInstanceCollection<string, TestObject>();

        using var handle1 = instances.GetHandle("abc", () => new TestObject(1));
        using var handle2 = instances.GetHandle("abc", () => new TestObject(1));
        using var handle3 = instances.GetHandle("abc", null);

        handle1.Value.ShouldBe(handle2.Value);
        handle1.Value.ShouldBe(handle3.Value);
        handle1.Value.Number.ShouldBe(1);
    }

    [Fact]
    public void GetHandle_New_No_Factory()
    {
        var instances = new ObjectInstanceCollection<string, TestObject>();

        Should.Throw<ArgumentNullException>(() => instances.GetHandle("abc", null));
    }

    [Fact]
    public void Releases_Instance()
    {
        var instances = new ObjectInstanceCollection<string, TestObject>();
        var released = false;
        instances.InstanceReleased += Instances_InstanceReleased;

        using (var handle = instances.GetHandle("abc", () => new TestObject(1)))
        {
            instances.Keys.Count.ShouldBe(1);
            instances.Keys.Single().ShouldBe("abc");
        }

        instances.Keys.ShouldBeEmpty();
        released.ShouldBeTrue();

        void Instances_InstanceReleased(object? sender, TestObject e)
        {
            sender.ShouldBe(instances);
            e.Number.ShouldBe(1);
            released = true;
        }
    }

    [Fact]
    public void Releases_Instances()
    {
        var instances = new ObjectInstanceCollection<string, TestObject>();
        var releasedCount = 0;
        instances.InstanceReleased += Instances_InstanceReleased;

        using (var handle1 = instances.GetHandle("abc", () => new TestObject(1)))
        {
            instances.Keys.Count.ShouldBe(1);

            using (var handle2 = instances.GetHandle("abc", null))
            {
                instances.Keys.Count.ShouldBe(1);

                using (var handle3 = instances.GetHandle("def", () => new TestObject(2)))
                {
                    instances.Keys.Count.ShouldBe(2);
                }

                instances.Keys.Count.ShouldBe(1);
                releasedCount.ShouldBe(1);
            }

            instances.Keys.Count.ShouldBe(1);
            releasedCount.ShouldBe(1);
        }

        instances.Keys.ShouldBeEmpty();
        releasedCount.ShouldBe(2);

        void Instances_InstanceReleased(object? sender, TestObject e)
        {
            releasedCount++;
        }
    }

    [Fact]
    public void Releases_Instance_Disposable()
    {
        var instances = new ObjectInstanceCollection<string, TestDisposableObject>();
        TestDisposableObject testObject;
        var released = false;
        instances.InstanceReleased += Instances_InstanceReleased;

        using (var handle = instances.GetHandle("abc", () => new TestDisposableObject()))
        {
            testObject = handle.Value;
        }

        testObject.IsDisposed.ShouldBeTrue();
        released.ShouldBeTrue();

        void Instances_InstanceReleased(object? sender, TestDisposableObject e)
        {
            released = true;
        }
    }

    private class TestObject
    {
        public int Number { get; }

        public TestObject(int number)
        {
            Number = number;
        }
    }

    private class TestDisposableObject : IDisposable
    {
        public bool IsDisposed => _disposed;
        private bool _disposed;

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
