using System;

namespace SignalR.Hubs
{
    /// <summary>
    /// Represents a parameter value.
    /// </summary>
    public interface IParameterValue
    {
        /// <summary>
        /// Converts the parameter value to the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to convert the parameter to.</param>
        /// <returns>The converted parameter value.</returns>
        object ConvertTo(Type type);

        /// <summary>
        /// Determines if the parameter can be converted to the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns>True if the parameter can be converted to the specified <see cref="Type"/>, false otherwise.</returns>
        bool CanConvertTo(Type type);
    }
}
