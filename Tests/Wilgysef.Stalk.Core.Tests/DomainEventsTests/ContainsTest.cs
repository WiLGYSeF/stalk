using Shouldly;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DomainEventsTests;

public class ContainsTest : BaseTest
{
    [Fact]
    public void Contains_Events()
    {
        var events = new DomainEventCollection();

        var @event = new TestEvent();
        events.Add(@event);
        events.Contains(@event).ShouldBeTrue();
    }

    [Fact]
    public void Contains_Events_Not_Found()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent());
        events.Contains(new TestEvent()).ShouldBeFalse();
    }

    private class TestEvent : IDomainEvent
    {

    }
}
