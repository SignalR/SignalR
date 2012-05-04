using System;
using System.Collections.Concurrent;
using SignalR.Hosting;

namespace SignalR.Transports
{
    /// <summary>
    /// The default <see cref="ITransportManager"/> implementation.
    /// </summary>
    public class TransportManager : ITransportManager
    {
        private readonly ConcurrentDictionary<string, Func<HostContext, ITransport>> _transports = new ConcurrentDictionary<string, Func<HostContext, ITransport>>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Initializes a new instance of <see cref="TransportManager"/> class.
        /// </summary>
        /// <param name="resolver">The default <see cref="IDependencyResolver"/>.</param>
        public TransportManager(IDependencyResolver resolver)
        {
            Register("foreverFrame", context => new ForeverFrameTransport(context, resolver));
            Register("serverSentEvents", context => new ServerSentEventsTransport(context, resolver));
            Register("longPolling", context => new LongPollingTransport(context, resolver));
            Register("forever", context => new ForeverTransport(context, resolver));
        }

        /// <summary>
        /// Adds a new transport to the list of supported transports.
        /// </summary>
        /// <param name="transportName">The specified transport.</param>
        /// <param name="transportFactory">The factory method for the specified transport.</param>
        public void Register(string transportName, Func<HostContext, ITransport> transportFactory)
        {
            _transports.TryAdd(transportName, transportFactory);
        }

        /// <summary>
        /// Removes a transport from the list of supported transports.
        /// </summary>
        /// <param name="transportName">The specified transport.</param>
        public void Remove(string transportName)
        {
            Func<HostContext, ITransport> removed;
            _transports.TryRemove(transportName, out removed);
        }

        /// <summary>
        /// Gets the specified transport for the specified <see cref="HostContext"/>.
        /// </summary>
        /// <param name="hostContext">The <see cref="HostContext"/> for the current request.</param>
        /// <returns>The <see cref="ITransport"/> for the specified <see cref="HostContext"/>.</returns>
        public ITransport GetTransport(HostContext context)
        {
            string transportName = context.Request.QueryString["transport"];

            if (String.IsNullOrEmpty(transportName))
            {
                return null;
            }

            Func<HostContext, ITransport> factory;
            if (_transports.TryGetValue(transportName, out factory))
            {
                return factory(context);
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified transport is supported.
        /// </summary>
        /// <param name="transportName">The name of the transport to test.</param>
        /// <returns>True if the transport is supported, otherwise False.</returns>
        public bool SupportsTransport(string transportName)
        {
            if (String.IsNullOrEmpty(transportName))
            {
                return false;
            }

            return _transports.ContainsKey(transportName);
        }
    }
}
