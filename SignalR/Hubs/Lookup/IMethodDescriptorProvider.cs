using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SignalR.Hubs
{
    /// <summary>
    /// Describes a hub method provider that builds a collection of available methods on a given hub.
    /// </summary>
    public interface IMethodDescriptorProvider
    {
        /// <summary>
        /// Retrieve all methods on a given hub.
        /// </summary>
        /// <param name="hub">Hub descriptor object.</param>
        /// <returns>Available methods.</returns>
        IEnumerable<MethodDescriptor> GetMethods(HubDescriptor hub);

        /// <summary>
        /// Tries to retrieve a method.
        /// </summary>
        /// <param name="hub">Hub descriptor object</param>
        /// <param name="method">Name of the method.</param>
        /// <param name="descriptor">Descriptor of the method, if found. Null otherwise.</param>
        /// <param name="parameters">Method parameters to match.</param>
        /// <returns>True, if a method has been found.</returns>
        bool TryGetMethod(HubDescriptor hub, string method, out MethodDescriptor descriptor, params JToken[] parameters);
    }
}