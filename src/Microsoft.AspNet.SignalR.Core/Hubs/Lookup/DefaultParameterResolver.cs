// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class DefaultParameterResolver : IParameterResolver
    {
        private static MethodInfo methodInfo = typeof(DefaultParameterResolver).GetMethod("Convert", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Resolves a parameter value based on the provided object.
        /// </summary>
        /// <param name="descriptor">Parameter descriptor.</param>
        /// <param name="value">Value to resolve the parameter value from.</param>
        /// <returns>The parameter value.</returns>
        public virtual object ResolveParameter(ParameterDescriptor descriptor, object value)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }

            var method = methodInfo.MakeGenericMethod(descriptor.ParameterType);
            var serializer = GlobalHost.DependencyResolver.Resolve<JsonSerializer>();            
            return method.Invoke(null, new object[] { value, serializer });
        }

        // identical or very similar to HubProxyExtensions.Convert<T>
        private static T Convert<T>(object obj, JsonSerializer serializer)
        {
            if (obj == null)
            {
                return default(T);
            }

            if (typeof(T).IsInstanceOfType(obj))
            {
                return (T)obj;
            }

            var jToken = obj as JToken;
            if (jToken != null)
            {
                return jToken.ToObject<T>(serializer);
            }

            return JToken.FromObject(obj).ToObject<T>(serializer);
        }

        /// <summary>
        /// Resolves method parameter values based on provided objects.
        /// </summary>
        /// <param name="method">Method descriptor.</param>
        /// <param name="values">List of values to resolve parameter values from.</param>
        /// <returns>Array of parameter values.</returns>
        public virtual IList<object> ResolveMethodParameters(MethodDescriptor method, IList<object> values)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            return method.Parameters.Zip(values, ResolveParameter).ToArray();
        }
    }
}
