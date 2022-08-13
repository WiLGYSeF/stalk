using Shouldly;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DomainEventsTests;

public class RemoveTypeTest : BaseTest
{
    [Fact]
    public void Remove_Events()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent());
        events.Add(new TestEvent());
        events.Add(new AnotherTestEvent());

        events.RemoveType<TestEvent>().ShouldBe(2);
        events.Count.ShouldBe(1);
    }

    [Fact]
    public void Remove_Events_Not_Found()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent());
        events.RemoveType<AnotherTestEvent>().ShouldBe(0);
        events.Count.ShouldBe(1);
    }

    private class TestEvent : IDomainEvent
    {

    }

    private class AnotherTestEvent : IDomainEvent
    {

    }
}
