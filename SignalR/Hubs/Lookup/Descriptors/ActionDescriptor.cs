namespace SignalR.Hubs.Lookup.Descriptors
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Holds information about a single hub action.
    /// </summary>
    public class ActionDescriptor
    {
        /// <summary>
        /// Name of this action.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The return type of this action.
        /// </summary>
        public virtual Type ReturnType { get; set; }

        /// <summary>
        /// Hub descriptor object to which this action is related.
        /// </summary>
        public virtual HubDescriptor Hub { get; set; }

        /// <summary>
        /// Available action parameters.
        /// </summary>
        public virtual IEnumerable<ParameterDescriptor> Parameters { get; set; }

        /// <summary>
        /// Method invocation delegate.
        /// Takes a target hub and an array of invocation arguments as it's arguments.
        /// </summary>
        public virtual Func<IHub, object[], object> Invoker { get; set; }
    }
}
