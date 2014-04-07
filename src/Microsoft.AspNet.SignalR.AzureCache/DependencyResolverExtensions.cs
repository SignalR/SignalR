using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {

        public static IDependencyResolver UseAzureCache(this IDependencyResolver resolver, string cacheName, string cacheKey, TimeSpan timeToLive)
        {
            var configuration = new AzureCacheScaleoutConfiguration(cacheName,cacheKey,timeToLive);

            return UseAzureCache(resolver, configuration);
        }

        public static IDependencyResolver UseAzureCache(this IDependencyResolver resolver, AzureCacheScaleoutConfiguration configuration)
        {
            var bus = new Lazy<AzureCacheMessageBus>(() => new AzureCacheMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}

