using AutoMapper;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Application.Tests;

public class AutoMapperValidationTest : BaseTest
{
    [Fact]
    public void Validate_Configuration()
    {
        var mapperConfiguration = GetRequiredService<MapperConfiguration>();

        mapperConfiguration.AssertConfigurationIsValid();
    }
}
