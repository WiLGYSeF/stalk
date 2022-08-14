using Shouldly;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DomainEventsTests;

public class RemoveTest : BaseTest
{
    [Fact]
    public void Remove_Events()
    {
        var events = new DomainEventCollection();

        var @event = new TestEvent();
        events.Add(@event);

        events.Remove(@event).ShouldBeTrue();
        events.Count.ShouldBe(0);
    }

    [Fact]
    public void Remove_Events_Not_Found()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent());
        events.Remove(new TestEvent()).ShouldBeFalse();
        events.Count.ShouldBe(1);
    }

    private class TestEvent : IDomainEvent
    {

    }
}
