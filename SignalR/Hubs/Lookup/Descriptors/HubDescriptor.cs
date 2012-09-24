using System;

namespace SignalR.Hubs
{
    /// <summary>
    /// Holds information about a single hub.
    /// </summary>
    public class HubDescriptor : Descriptor
    {
        /// <summary>
        /// Hub type.
        /// </summary>
        public virtual Type Type { get; set; }

        public string CreateQualifiedName(string unqualifiedName)
        {
            return Name + "." + unqualifiedName;
        }
    }
}
