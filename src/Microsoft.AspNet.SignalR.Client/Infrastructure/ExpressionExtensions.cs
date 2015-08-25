using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal static class ExpressionExtensions
    {
        public static void GetInvocationDetails<T>(this Expression<Action<T>> invocation, out string methodName,
            out object[] args)
        {
            if (!(invocation.Body is MethodCallExpression))
            {
                throw new InvalidOperationException(Resources.Error_InvocationMustBeAMethodCall);
            }

            GetInvocationDetails((MethodCallExpression) invocation.Body, out methodName, out args);
        }

        public static void GetInvocationDetails<TInput, TResult>(this Expression<Func<TInput, TResult>> invocation,
            out string methodName, out object[] args)
        {
            if (!(invocation.Body is MethodCallExpression))
            {
                throw new InvalidOperationException(Resources.Error_InvocationMustBeAMethodCall);
            }

            GetInvocationDetails((MethodCallExpression) invocation.Body, out methodName, out args);
        }

        public static string GetMethodName<TInterface>(this Expression<Func<TInterface, Action>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T>(this Expression<Func<TInterface, Action<T>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T1, T2>(
            this Expression<Func<TInterface, Action<T1, T2>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T1, T2, T3>(
            this Expression<Func<TInterface, Action<T1, T2, T3>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T1, T2, T3, T4>(
            this Expression<Func<TInterface, Action<T1, T2, T3, T4>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T1, T2, T3, T4, T5>(
            this Expression<Func<TInterface, Action<T1, T2, T3, T4, T5>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T1, T2, T3, T4, T5, T6>(
            this Expression<Func<TInterface, Action<T1, T2, T3, T4, T5, T6>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        public static string GetMethodName<TInterface, T1, T2, T3, T4, T5, T6, T7>(
            this Expression<Func<TInterface, Action<T1, T2, T3, T4, T5, T6, T7>>> invocation)
        {
            return GetMethodNameFromExpression(invocation);
        }

        private static string GetMethodNameFromExpression(LambdaExpression lambdaExpression)
        {
            var unaryExpression = (UnaryExpression) lambdaExpression.Body;
            var methodCallExpression = (MethodCallExpression) unaryExpression.Operand;

            if (methodCallExpression.Object == null)
            {
                throw new InvalidOperationException(Resources.Error_CannotGetMethodInfoFromLambdaExpression);
            }

            var methodInfo = (MethodInfo) ((ConstantExpression) methodCallExpression.Object).Value;

            return methodInfo.Name;
        }

        private static void GetInvocationDetails(MethodCallExpression callExpression, out string methodName,
            out object[] args)
        {
            methodName = callExpression.Method.Name;
            args = callExpression.Arguments.Select(ConvertToConstant).ToArray();
        }

        private static object ConvertToConstant(Expression expression)
        {
            UnaryExpression objectMember = Expression.Convert(expression, typeof (object));
            Expression<Func<object>> getterLambda = Expression.Lambda<Func<object>>(objectMember);
            Func<object> getter = getterLambda.Compile();

            return getter();
        }
    }
}