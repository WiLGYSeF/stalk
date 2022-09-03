using Moq;
using System.Reflection;

namespace Wilgysef.Stalk.TestBase;

public static class MockExtensions
{
    public static IInvocation GetInvocation<T>(this Mock<T> mock, MethodInfo methodInfo) where T : class
    {
        return mock.Invocations.Single(i => i.Method == methodInfo);
    }

    public static IInvocation GetInvocation<T>(this Mock<T> mock, string methodName) where T : class
    {
        return GetInvocation(mock, typeof(T).GetMethod(methodName)!);
    }

    public static List<IInvocation> GetInvocations<T>(this Mock<T> mock, MethodInfo methodInfo) where T : class
    {
        return mock.Invocations.Where(i => i.Method == methodInfo).ToList();
    }

    public static List<IInvocation> GetInvocations<T>(this Mock<T> mock, string methodName) where T : class
    {
        return GetInvocations(mock, typeof(T).GetMethod(methodName)!);
    }
}
