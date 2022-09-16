using Shouldly;
using Wilgysef.Stalk.Core.UserAgentGenerators;

namespace Wilgysef.Stalk.Core.Tests.UserAgentGeneratorTests;

public class RandomUserAgentGeneratorTest
{
    [Fact]
    public void Generate()
    {
        var generator = new RandomUserAgentGenerator();
        var userAgents = new List<string>
        {
            generator.Generate(),
            generator.Generate(),
            generator.Generate(),
        };

        foreach (var userAgent in userAgents)
        {
            userAgent.Length.ShouldBeGreaterThan(0);
        }

        var userAgentSet = new HashSet<string>(userAgents);
        userAgentSet.Count.ShouldBe(userAgents.Count);
    }
}
