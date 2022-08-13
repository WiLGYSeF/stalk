using Shouldly;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DomainEventsTests;

public class AddOrReplaceTest : BaseTest
{
    [Fact]
    public void AddOrReplace_Events()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent(1));
        events.Add(new TestEvent(2));
        events.AddOrReplace(new TestEvent(0));

        events.AddOrReplace(new AnotherTestEvent(3));
        events.AddOrReplace(new AnotherTestEvent(2));
        events.AddOrReplace(new AnotherTestEvent(1));

        events.Count.ShouldBe(2);
        (events.Single(e => e is TestEvent) as TestEvent)!.Value.ShouldBe(0);
        (events.Single(e => e is AnotherTestEvent) as AnotherTestEvent)!.Value.ShouldBe(1);
    }

    private class TestEvent : IDomainEvent
    {
        public int Value { get; }

        public TestEvent(int value)
        {
            Value = value;
        }
    }

    private class AnotherTestEvent : IDomainEvent
    {
        public int Value { get; }

        public AnotherTestEvent(int value)
        {
            Value = value;
        }
    }
}
