using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wilgysef.Stalk.Core.UserAgentGenerators;

namespace Wilgysef.Stalk.Core.Tests.UserAgentGeneratorTests;

public class RandomUserAgentGeneratorTest
{
    [Fact]
    public void Generate()
    {
        var generator = new RandomUserAgentGenerator();
        var userAgent = generator.Generate();
        userAgent.Length.ShouldBeGreaterThan(0);
    }
}
