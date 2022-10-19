using Moq;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Wilgysef.MoqExtensions
{
    public static class MockExtensions
    {
        private static readonly MethodInfo ItIsAnyMethod = typeof(It).GetMethod(nameof(It.IsAny))!;

        public static Mock<TMock> Decorate<T, TMock>(T implementation)
            where T : TMock
            where TMock : class
        {
            var mock = new Mock<TMock>();
            var mockType = typeof(TMock);
            var implementationMethods = typeof(T).GetMethods();

            foreach (var method in mockType.GetMethods())
            {
                var implementationMethod = GetMatchingMethod(method, implementationMethods);
                var setup = mock.Setup((dynamic)SetupExpression(mockType, method));

                var returnOrCallbackMethod = setup.GetType().GetMethod(
                    method.ReturnType != typeof(void) ? "Returns" : "Callback",
                    new[] { typeof(Delegate) });

                var lambda = CreateExpressionLambda(implementation, implementationMethod).Compile();
                returnOrCallbackMethod!.Invoke(setup, new[] { lambda });
            }

            return mock;
        }

        #region SetupAnyArgs

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

        #endregion

        #region GetInvocations

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

        #endregion

        private static Expression<Action<T>> SetupExpression<T>(MethodInfo method)
        {
            var param = Expression.Parameter(typeof(T));
            return Expression.Lambda<Action<T>>(
                Expression.Call(param, method, GetItIsAnyMethodCallExpressions(method)),
                param);
        }

        private static Expression<Func<T, TResult>> SetupExpression<T, TResult>(MethodInfo method)
        {
            var param = Expression.Parameter(typeof(T));
            return Expression.Lambda<Func<T, TResult>>(
                Expression.Call(param, method, GetItIsAnyMethodCallExpressions(method)),
                param);
        }

        private static LambdaExpression SetupExpression(Type type, MethodInfo method)
        {
            var param = Expression.Parameter(type);
            return Expression.Lambda(
                Expression.Call(param, method, GetItIsAnyMethodCallExpressions(method)),
                param);
        }

        private static LambdaExpression SetupExpression(Type type, MethodInfo method, out Type lambdaType)
        {
            lambdaType = method.ReturnType != typeof(void)
                ? typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(type, method.ReturnType))
                : typeof(Expression<>).MakeGenericType(typeof(Action<>).MakeGenericType(type));
            return SetupExpression(type, method);
        }

        private static LambdaExpression CreateExpressionLambda<T>(T implementation, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var parameterExpressions = new ParameterExpression[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                parameterExpressions[i] = Expression.Parameter(parameters[i].ParameterType);
            }

            return Expression.Lambda(
                Expression.Call(Expression.Constant(implementation), method, parameterExpressions),
                parameterExpressions);
        }

        private static MethodCallExpression[] GetItIsAnyMethodCallExpressions(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var arguments = new MethodCallExpression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                arguments[i] = Expression.Call(ItIsAnyMethod.MakeGenericMethod(parameters[i].ParameterType));
            }

            return arguments;
        }

        private static MethodInfo GetMatchingMethod(MethodInfo method, MethodInfo[] methodList)
        {
            var genericArgumentsCount = method.GetGenericArguments().Length;
            var parameters = method.GetParameters();
            return methodList.Single(m => m.Name == method.Name
                && m.GetGenericArguments().Length == genericArgumentsCount
                && m.GetParameters().SequenceEqual(parameters, new ParameterInfoComparer()));
        }

        private class ParameterInfoComparer : IEqualityComparer<ParameterInfo>
        {
            public bool Equals(ParameterInfo? x, ParameterInfo? y)
            {
                return x?.ParameterType == y?.ParameterType;
            }

            public int GetHashCode([DisallowNull] ParameterInfo obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
