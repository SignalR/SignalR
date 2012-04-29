using System;

namespace SignalR.Hubs
{
    /// <summary>
    /// Holds information about a single hub method parameter.
    /// </summary>
    public class ParameterDescriptor
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Parameter type.
        /// </summary>
        public virtual Type Type { get; set; }
    }
}

