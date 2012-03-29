using System;
using System.Linq;
using Newtonsoft.Json;
using SignalR.Hubs.Lookup.Descriptors;

namespace SignalR.Hubs.Lookup
{
    public class DefaultParameterResolver: IParameterResolver
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
                return descriptor.Type.IsValueType
                           ? Activator.CreateInstance(descriptor.Type) 
                           : null;
            }

            if (value.GetType() == descriptor.Type)
            {
                return value;
            }

            if (descriptor.Type == typeof(Guid))
            {
                return new Guid(value.ToString());
            }

            return JsonConvert.DeserializeObject(value.ToString(), descriptor.Type);
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
    }
}