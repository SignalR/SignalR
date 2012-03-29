namespace SignalR.Hubs
{
    /// <summary>
    /// Describes a parameter resolver for resolving parameter-matching values based on provided information.
    /// </summary>
    public interface IParameterResolver
    {
        /// <summary>
        /// Resolves a parameter value based on the provided object.
        /// </summary>
        /// <param name="descriptor">Parameter descriptor.</param>
        /// <param name="value">Value to resolve the parameter value from.</param>
        /// <returns>The parameter value.</returns>
        object ResolveParameter(ParameterDescriptor descriptor, object value);

        /// <summary>
        /// Resolves method parameter values based on provided objects.
        /// </summary>
        /// <param name="method">Method descriptor.</param>
        /// <param name="values">List of values to resolve parameter values from.</param>
        /// <returns>Array of parameter values.</returns>
        object[] ResolveMethodParameters(MethodDescriptor method, params object[] values);
    }
}