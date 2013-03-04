using System;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public static class DependencyResolverExtensions
    {
        internal const string KoSignalPrefix = "ko-";

        private static object _initMethodDescriptorProviderLock = new object();
        private static bool _methodDescriptorProvierInitialized = false;

        public static void ActivateKnockoutHub<T>(this IDependencyResolver resolver) where T : KnockoutHub
        {
            var hubType = typeof(T);
            var hubName = GetHubAttributeName(hubType) ?? hubType.Name;

            var bus = resolver.Resolve<IMessageBus>();
            var serializer = resolver.Resolve<IJsonSerializer>();
            var connectionManager = resolver.Resolve<IConnectionManager>();

            lock (_initMethodDescriptorProviderLock)
            {
                if (!_methodDescriptorProvierInitialized)
                {
                    var knockoutMethodDescriptorProvider = new Lazy<IMethodDescriptorProvider>(() => 
                                                               new KnockoutMethodDescriptorProvider(bus, serializer));
                    resolver.Register(typeof(IMethodDescriptorProvider), () => knockoutMethodDescriptorProvider.Value);
                    _methodDescriptorProvierInitialized = true;
                }
            }

            var diffSubscriber = new DiffSubscriber(bus, serializer, KoSignalPrefix + hubName);
            var hubContext = connectionManager.GetHubContext(hubName); 

            diffSubscriber.Start((source, diff) =>
            {
                hubContext.Clients.AllExcept(source).onKnockoutUpdate(diff);
                return TaskAsyncHelper.Empty;
            });
        }

        // Taken from HubTypeExtensions. Perhaps this should be made public? 
        private static string GetHubAttributeName(this Type type)
        {
            // We can still return null if there is no attribute name
            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type, attr => attr.HubName);
        }
    }
}
