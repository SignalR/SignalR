using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeInvocationManager
    {
        private readonly IDictionary<string, IList<object[]>> _invocations = new Dictionary<string, IList<object[]>>();

        private readonly IDictionary<string, object> _setups = new Dictionary<string, object>(); 

        public void AddInvocation(string methodName, params object[] parameters)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(methodName), "incorrect methodName");

            IList<object[]> methodInvocations;
            if (!_invocations.TryGetValue(methodName, out methodInvocations))
            {
                methodInvocations = _invocations[methodName] = new List<object[]>();
            }

            methodInvocations.Add(parameters);
        }

        public void AddSetup(string methodName, object returnValue)
        {
            _setups[methodName] = returnValue;
        }

        public void AddSetup<T>(string methodName, Func<T> behavior)
        {
            _setups[methodName] = behavior;
        }

        public void Verify(string methodName, List<object[]> expectedParameters)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(methodName), "incorrect methodName");
            Debug.Assert(expectedParameters != null, "expectedParameters is null");

            IList<object[]> methodInvocations;
            if (!_invocations.TryGetValue(methodName, out methodInvocations) || methodInvocations.Count == 0)
            {
                if (expectedParameters.Count == 0)
                {
                    return;
                }

                EnsureInvocationCountsMatch(methodName, expectedParameters.Count, 0);
            }

            for (var idx = 0; idx < Math.Min(expectedParameters.Count, methodInvocations.Count); idx++)
            {
                if (expectedParameters[idx] != null && !CompareParameterValues(expectedParameters[idx], methodInvocations[idx]))
                {
                    throw new InvalidOperationException(
                        string.Format("Invocation {0} of the method {1} failed.\nExpected parameters: '{2}'. Actual parameters:   '{3}'",
                        idx + 1, methodName, string.Join(", ", expectedParameters[idx]), string.Join(", ", methodInvocations[idx])));
                }
            }

            EnsureInvocationCountsMatch(methodName, expectedParameters.Count, methodInvocations.Count);
        }

        private bool CompareParameterValues(object[] params1, object[] params2)
        {
            for (var i = 0; i < Math.Min(params1.Length, params2.Length); i++)
            {
                if (!params1[i].Equals(params2[i]))
                {
                    return false;
                }
            }

            return params1.Length == params2.Length;
        }

        private void EnsureInvocationCountsMatch(string methodName, int expectedInvocationCount, int actualInvocationCount)
        {
            if (expectedInvocationCount != actualInvocationCount)
            {
                throw new InvalidOperationException(
                    string.Format("Expected {0} invocations of the method {1}. Actual invocations: {2}.",
                        expectedInvocationCount, methodName, actualInvocationCount));
            }
        }

        public IList<object[]> GetInvocations(string methodName)
        {
            IList<object[]> invocations;

            return _invocations.TryGetValue(methodName, out invocations)
                ? invocations
                : new List<object[]>();
        }

        public T GetReturnValue<T>(string methodName)
        {
            object value;

            if (!_setups.TryGetValue(methodName, out value))
            {
                return default(T);
            }

            var genericTypeDefinition = value.GetType().GetGenericTypeDefinition();
            if (genericTypeDefinition != null && genericTypeDefinition == typeof (Func<>))
            {
                return ((Func<T>)value)();
            }

            return (T)value;
        }
    }
}