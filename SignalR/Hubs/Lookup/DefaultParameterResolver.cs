using System.Linq;

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
        public virtual object ResolveParameter(ParameterDescriptor descriptor, IJsonValue value)
        {
            if (value.GetType() == descriptor.Type)
            {
                return value;
            }

            return value.ConvertTo(descriptor.Type);
        }

        /// <summary>
        /// Resolves method parameter values based on provided objects.
        /// </summary>
        /// <param name="method">Method descriptor.</param>
        /// <param name="values">List of values to resolve parameter values from.</param>
        /// <returns>Array of parameter values.</returns>
        public virtual object[] ResolveMethodParameters(MethodDescriptor method, params IJsonValue[] values)
        {
            return method.Parameters.Zip(values, ResolveParameter).ToArray();
        }
    }
}