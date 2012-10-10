using System;
using System.Collections.Generic;

namespace SignalR.Hubs
{
    /// <summary>
    /// Holds information about a single hub method.
    /// </summary>
    public class MethodDescriptor : Descriptor
    {
        /// <summary>
        /// The return type of this method.
        /// </summary>
        public virtual Type ReturnType { get; set; }

        /// <summary>
        /// Hub descriptor object, target to this method.
        /// </summary>
        public virtual HubDescriptor Hub { get; set; }

        /// <summary>
        /// Available method parameters.
        /// </summary>
        public virtual IList<ParameterDescriptor> Parameters { get; set; }

        /// <summary>
        /// Method invocation delegate.
        /// Takes a target hub and an array of invocation arguments as it's arguments.
        /// </summary>
        public virtual Func<IHub, object[], object> Invoker { get; set; }

        /// <summary>
        /// Attributes attached to this method.
        /// </summary>
        public virtual IEnumerable<Attribute> Attributes { get; set; }
    }
}

