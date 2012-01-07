using System;
using System.Collections.Concurrent;
using SignalR.Abstractions;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class TransportManager : ITransportManager
    {
        private readonly ConcurrentDictionary<string, Func<HostContext, ITransport>> _transports = new ConcurrentDictionary<string, Func<HostContext, ITransport>>(StringComparer.OrdinalIgnoreCase);
        private static readonly TransportManager _default = new TransportManager();

        private TransportManager()
        {
            Register("foreverFrame", context => new ForeverFrameTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
            Register("serverSentEvents", context => new ServerSentEventsTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
            Register("longPolling", context => new LongPollingTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
            Register("forever", context => new ForeverTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
        }

        public static TransportManager Default
        {
            get
            {
                return _default;
            }
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
    }
}
