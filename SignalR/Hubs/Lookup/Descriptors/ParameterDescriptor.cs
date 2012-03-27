namespace SignalR.Hubs.Lookup.Descriptors
{
    using System;

    /// <summary>
    /// Holds information about a single hub action parameter.
    /// </summary>
    public class ParameterDescriptor
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parameter type.
        /// </summary>
        public Type Type { get; set; }
    }
}
