using System;
using System.Collections.Concurrent;
using System.Web;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public static class TransportManager
    {
        private static readonly ConcurrentDictionary<string, Func<HttpContextBase, ITransport>> _transports = new ConcurrentDictionary<string, Func<HttpContextBase, ITransport>>(StringComparer.OrdinalIgnoreCase);

        public static void Register(string transportName, Func<HttpContextBase, ITransport> transportFactory)
        {
            _transports.TryAdd(transportName, transportFactory);
        }

        public static void Remove(string transportName)
        {
            Func<HttpContextBase, ITransport> removed;
            _transports.TryRemove(transportName, out removed);
        }

        internal static ITransport GetTransport(HttpContextBase contextBase)
        {
            string transportName = contextBase.Request.QueryString["transport"];

            if (String.IsNullOrEmpty(transportName))
            {
                return null;
            }

            Func<HttpContextBase, ITransport> factory;
            if (_transports.TryGetValue(transportName, out factory))
            {
                return factory(contextBase);
            }

            return null;
        }

        public static void InitializeDefaultTransports()
        {
            Register("foreverFrame", context => new ForeverFrameTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
            Register("serverSentEvents", context => new ServerSentEventsTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
            Register("longPolling", context => new LongPollingTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
            Register("forever", context => new ForeverTransport(context, DependencyResolver.Resolve<IJsonSerializer>()));
        }
    }
}
