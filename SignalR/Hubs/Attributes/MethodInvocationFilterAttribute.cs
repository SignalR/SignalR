using System;

namespace SignalR.Hubs.Attributes
{
    /// <summary>
    /// Provides methods for executing before and after a Hub action is invoked
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public abstract class MethodInvocationFilterAttribute : Attribute
    {

        /// <summary>
        /// Executed directly before the method is invoked
        /// </summary>
        /// <param name="hub"></param>
        public virtual void OnMethodInvoking(IHub hub)
        {

        }

        /// <summary>
        /// Executed directly after the method is invoked
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="result"></param>
        public virtual void OnMethodInvoked(IHub hub, ref object result)
        {

        }

        /// <summary>
        /// The order of witch this attribute should be executed
        /// </summary>
        public int Order { get; set; }
    }
}