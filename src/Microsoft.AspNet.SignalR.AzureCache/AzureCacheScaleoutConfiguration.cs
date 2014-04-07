using System;
using Microsoft.ApplicationServer.Caching;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    public class AzureCacheScaleoutConfiguration : ScaleoutConfiguration
    {
       
        public AzureCacheScaleoutConfiguration(string cacheName, string cacheKey, TimeSpan timeToLive) : this(MakeDataCacheFactory(cacheName),cacheKey,timeToLive) 
        {
           
        }

        public AzureCacheScaleoutConfiguration(Func<DataCache> dataCacheFactory, string cacheKey, TimeSpan timeToLive)
        {
            DataCacheFactory = dataCacheFactory;
            CacheKey = cacheKey;
            TimeToLive = timeToLive;
        }

        public string CacheKey { get; private set; }

        internal Func<DataCache> DataCacheFactory { get; private set; } 


        /// <summary>
        /// Gets or sets the message’s time to live value. This is the duration after
        /// which the message expires, starting from when the message is sent to the
        /// Azure Cache. Messages older than their TimeToLive value will expire and no
        /// longer be retained in the message store. Subscribers will be unable to receive
        /// expired messages.
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        private static Func<DataCache> MakeDataCacheFactory(string cacheName)
        {
            return ()=> new DataCache(cacheName);
        }
    }
}
