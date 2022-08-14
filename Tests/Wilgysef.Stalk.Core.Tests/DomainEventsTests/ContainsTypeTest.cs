using Shouldly;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DomainEventsTests;

public class ContainsTypeTest : BaseTest
{
    [Fact]
    public void Contains_Events()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent());
        events.Add(new TestEvent());
        events.Add(new AnotherTestEvent());

        events.ContainsType<AnotherTestEvent>().ShouldBeTrue();
    }

    [Fact]
    public void Contains_Events_Not_Found()
    {
        var events = new DomainEventCollection();

        events.Add(new TestEvent());
        events.ContainsType<AnotherTestEvent>().ShouldBeFalse();
    }

    private class TestEvent : IDomainEvent
    {

    }

    private class AnotherTestEvent : IDomainEvent
    {

    }
}
