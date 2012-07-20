using System;
using System.Collections.Generic;
using SignalR.Hubs.Attributes;

namespace SignalR.Hubs
{
    /// <summary>
    /// Holds information about a single hub method.
    /// </summary>
    public class MethodDescriptor
    {
        /// <summary>
        /// Name of this method.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The return type of this method.
        /// </summary>
        public virtual Type ReturnType { get; set; }

        /// <summary>
        /// Hub descriptor object, target to his method.
        /// </summary>
        public virtual HubDescriptor Hub { get; set; }

        /// <summary>
        /// Available method parameters.
        /// </summary>
        public virtual IList<ParameterDescriptor> Parameters { get; set; }

        /// <summary>
        /// MethodInvocationFilterAttributes valid for this method
        /// </summary>
        public virtual IList<MethodInvocationFilterAttribute> InvocationFilters { get; set; } 


        /// <summary>
        /// Method invocation delegate.
        /// Takes a target hub and an array of invocation arguments as it's arguments.
        /// </summary>
        public virtual Func<IHub, object[], object> Invoker { get; set; }
    }
}

