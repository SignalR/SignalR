using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Hubs
{
    public class DefaultParameterResolver : IParameterResolver
    {
        /// <summary>
        /// Resolves a parameter value based on the provided object.
        /// </summary>
        /// <param name="descriptor">Parameter descriptor.</param>
        /// <param name="value">Value to resolve the parameter value from.</param>
        /// <returns>The parameter value.</returns>
        public virtual object ResolveParameter(ParameterDescriptor descriptor, object value)
        {
            if (value == null)
            {
                if (TypeAllowsNull(descriptor.Type))
                {
                    return null;
                }

                throw new InvalidOperationException(String.Format("Unable to resolve value for parameter '{0}'.", descriptor.Name));
            }

            var token = value as JToken;

            if (token != null)
            {
                // A non generic implementation of ToObject<T> on JToken
                using (var jsonReader = new JTokenReader(token))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize(jsonReader, descriptor.Type);
                }
            }

            if (value.GetType() == descriptor.Type)
            {
                return value;
            }

            return Convert.ChangeType(value, descriptor.Type);
        }

        /// <summary>
        /// Resolves method parameter values based on provided objects.
        /// </summary>
        /// <param name="method">Method descriptor.</param>
        /// <param name="values">List of values to resolve parameter values from.</param>
        /// <returns>Array of parameter values.</returns>
        public virtual object[] ResolveMethodParameters(MethodDescriptor method, params object[] values)
        {
            return method.Parameters
                .Select((p, index) => ResolveParameter(p, values[index]))
                .ToArray();
        }

        private bool TypeAllowsNull(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }
    }
}