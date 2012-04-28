using System;
using System.Collections.Concurrent;
using SignalR.Hosting;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class TransportManager : ITransportManager
    {
        private readonly ConcurrentDictionary<string, Func<HostContext, ITransport>> _transports = new ConcurrentDictionary<string, Func<HostContext, ITransport>>(StringComparer.OrdinalIgnoreCase);
        
        public TransportManager(IDependencyResolver resolver)
        {
            Register("foreverFrame", context => new ForeverFrameTransport(context, resolver));
            Register("serverSentEvents", context => new ServerSentEventsTransport(context, resolver));
            Register("longPolling", context => new LongPollingTransport(context, resolver));
            Register("forever", context => new ForeverTransport(context, resolver));
        }

        public void Register(string transportName, Func<HostContext, ITransport> transportFactory)
        {
            _transports.TryAdd(transportName, transportFactory);
        }

        public void Remove(string transportName)
        {
            Func<HostContext, ITransport> removed;
            _transports.TryRemove(transportName, out removed);
        }

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
