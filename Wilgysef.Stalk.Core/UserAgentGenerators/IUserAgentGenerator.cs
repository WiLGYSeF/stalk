namespace Wilgysef.Stalk.Core.UserAgentGenerators;

public interface IUserAgentGenerator
{
    /// <summary>
    /// Generates user agent.
    /// </summary>
    /// <returns>User agent.</returns>
    string Generate();
}
