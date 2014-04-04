using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationServer.Caching;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{

    public class AzureCacheMessageBus : ScaleoutMessageBus
    {
        private readonly DataCache _dataCache;
        private readonly TimeSpan _timeToLive;
        private readonly string _cacheKey;
        private readonly string _cacheAdminKey;
        private const string AdminKey = "admin";
        private const string InternalKey = "signalr";
        private readonly object _callbackLock = new object();

        public AzureCacheMessageBus(IDependencyResolver resolver, AzureCacheScaleoutConfiguration configuration) : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            Open(0);
            _dataCache = configuration.DataCacheFactory();

            _timeToLive = configuration.TimeToLive;
            _cacheKey = configuration.CacheKey;
            _cacheAdminKey = string.Format("{0}_{1}", _cacheKey, AdminKey);

            _dataCache.AddRegionLevelCallback(_cacheKey, DataCacheOperations.AddItem | DataCacheOperations.ReplaceItem, OnNotificated);
            _dataCache.CreateRegion(_cacheKey);
        }

        private void OnNotificated(string cachename, string regionname, string key, DataCacheItemVersion version, DataCacheOperations cacheoperation, DataCacheNotificationDescriptor nd)
        {
            var msg = AzureCacheMessage.FromBytes((byte[])_dataCache.Get(key, regionname));

             lock (_callbackLock)
            {
                OnReceived(0, msg.Id,msg.ScaleoutMessage);
            }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return Task.Factory.FromAsync(BeginSendToCache(streamIndex,messages,null,null),EndSendToCache);
        }

        private IAsyncResult BeginSendToCache(int streamIndex, IList<Message> messages, AsyncCallback asyncCallback,object asyncState)
        {
            return new CompletedAsyncResult<SendInfo>(new SendInfo
            {
                StreamIndex = streamIndex,
                Messages = messages
            });
        }

        private void EndSendToCache(IAsyncResult asyncResult)
        {
            var sendInfo = asyncResult as CompletedAsyncResult<SendInfo>;
            if (sendInfo != null) { 
                SendToCache(sendInfo.Data.Messages);
            }
        }

        private void SendToCache(IList<Message> messages)
        {
            var newKey = _dataCache.Increment(_cacheAdminKey, 1, 0);

            byte[] data = AzureCacheMessage.ToBytes(newKey, messages);

            _dataCache.Put(string.Format("{0}_{1}", InternalKey, newKey), data, _timeToLive, _cacheKey);
        }
    }
}

