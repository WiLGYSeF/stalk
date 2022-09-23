using Moq;
using Moq.Language.Flow;
using System.Linq.Expressions;
using System.Reflection;

namespace Wilgysef.Stalk.TestBase;

public static class MockExtensions
{
    private static readonly MethodInfo ItIsAnyMethod = typeof(It).GetMethod(nameof(It.IsAny))!;

    public static ISetup<T> SetupAnyArgs<T>(this Mock<T> mock, MethodInfo methodInfo) where T : class
    {
        return mock.Setup(SetupExpression<T>(methodInfo));
    }

    public static ISetup<T> SetupAnyArgs<T>(this Mock<T> mock, string methodName) where T : class
    {
        return SetupAnyArgs(mock, typeof(T).GetMethod(methodName)!);
    }

    public static ISetup<T> SetupAnyArgs<T>(this Mock<T> mock, string methodName, params Type[] types) where T : class
    {
        return SetupAnyArgs(mock, typeof(T).GetMethod(methodName, types)!);
    }

    public static ISetup<T, TResult> SetupAnyArgs<T, TResult>(this Mock<T> mock, MethodInfo methodInfo) where T : class
    {
        return mock.Setup(SetupExpression<T, TResult>(methodInfo));
    }

    public static ISetup<T, TResult> SetupAnyArgs<T, TResult>(this Mock<T> mock, string methodName) where T : class
    {
        return SetupAnyArgs<T, TResult>(mock, typeof(T).GetMethod(methodName)!);
    }

    public static ISetup<T, TResult> SetupAnyArgs<T, TResult>(this Mock<T> mock, string methodName, params Type[] types) where T : class
    {
        return SetupAnyArgs<T, TResult>(mock, typeof(T).GetMethod(methodName, types)!);
    }

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

    private static Expression<Action<T>> SetupExpression<T>(MethodInfo methodInfo)
    {
        var param = Expression.Parameter(typeof(T));
        return Expression.Lambda<Action<T>>(
            Expression.Call(param, methodInfo, GetItIsAnyMethodCallExpressions(methodInfo)),
            param);
    }

    private static Expression<Func<T, TResult>> SetupExpression<T, TResult>(MethodInfo methodInfo)
    {
        var param = Expression.Parameter(typeof(T));
        return Expression.Lambda<Func<T, TResult>>(
            Expression.Call(param, methodInfo, GetItIsAnyMethodCallExpressions(methodInfo)),
            param);
    }

    private static MethodCallExpression[] GetItIsAnyMethodCallExpressions(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var arguments = new MethodCallExpression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = Expression.Call(ItIsAnyMethod.MakeGenericMethod(parameters[i].ParameterType));
        }

        return arguments;
    }
}
