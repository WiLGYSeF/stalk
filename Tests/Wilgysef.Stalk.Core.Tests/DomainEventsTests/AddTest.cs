using Shouldly;
using Wilgysef.Stalk.Core.DomainEvents;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.DomainEventsTests;

public class AddTest : BaseTest
{
    [Fact]
    public void Add_Events()
    {
        var events = new DomainEventCollection();

        var testCount = 3;
        for (var i = 0; i < testCount; i++)
        {
            events.Add(new TestEvent());
        }

        var anotherTestCount = 3;
        for (var i = 0; i < anotherTestCount; i++)
        {
            events.Add(new AnotherTestEvent());
        }

        events.Count.ShouldBe(testCount + anotherTestCount);
    }

    private class TestEvent : IDomainEvent
    {

    }

    private class AnotherTestEvent : IDomainEvent
    {

    }
}
