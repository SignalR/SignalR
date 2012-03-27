namespace SignalR.Hubs.Lookup.Descriptors
{
    using System;

    /// <summary>
    /// Holds information about a single hub.
    /// </summary>
    public class HubDescriptor
    {
        /// <summary>
        /// Hub name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Hub type.
        /// </summary>
        public Type Type { get; set; }
    }
}