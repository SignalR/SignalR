using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using SignalR.Infrastructure;
using SignalR.Web;

namespace SignalR.ScaleOut
{
    public class PeerToPeerHttpSignalBus : ISignalBus
    {
        private static readonly object _peerDiscoveryLocker = new object();
        private readonly ConcurrentDictionary<string, SafeSet<EventHandler<SignaledEventArgs>>> _handlers = new ConcurrentDictionary<string, SafeSet<EventHandler<SignaledEventArgs>>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _peers = new List<string>();
        private bool _peersDiscovered = false;

        public PeerToPeerHttpSignalBus()
        {
            Id = Guid.NewGuid();
        }

        private IPeerUrlSource PeerUrlSource
        {
            get
            {
                var source = DependencyResolver.Resolve<IPeerUrlSource>();
                if (source == null)
                {
                    throw new InvalidOperationException("No implementation of IPeerUrlSource is registered.");
                }
                return source;
            }
        }

        protected internal Guid Id { get; private set; }

        public virtual Task Signal(string eventKey)
        {
            // Signal ourselves directly as we don't send the signal via P2P to ourself
            OnSignaled(eventKey);

            EnsurePeersDiscovered();

            return Task.Factory.StartNew(() => SendSignalToPeers(eventKey));
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            _handlers.AddOrUpdate(eventKey, new SafeSet<EventHandler<SignaledEventArgs>>(new[] { handler }), (key, list) =>
            {
                list.Add(handler);
                return list;
            });
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            SafeSet<EventHandler<SignaledEventArgs>> handlers;
            if (_handlers.TryGetValue(eventKey, out handlers))
            {
                handlers.Remove(handler);
            }
        }

        protected internal virtual void SignalReceived(string eventKey)
        {
            OnSignaled(eventKey);
        }

        /// <summary>
        /// Override this method to prepare the request before it is sent to peers, e.g. to add authentication credentials
        /// </summary>
        /// <param name="request">The request being sent to peers</param>
        protected virtual void PrepareRequest(HttpWebRequest request) { }

        private void OnSignaled(string eventKey)
        {
            SafeSet<EventHandler<SignaledEventArgs>> handlers;
            if (_handlers.TryGetValue(eventKey, out handlers))
            {
                Parallel.ForEach(handlers.GetSnapshot(), handler => handler(this, new SignaledEventArgs(eventKey)));
            }
        }

        private void EnsurePeersDiscovered()
        {
            PeerToPeerHelper.EnsurePeersDiscovered(ref _peersDiscovered, PeerUrlSource, _peers, SignalReceiverHandler.HandlerName, Id, _peerDiscoveryLocker, PrepareRequest);
        }

        private void SendSignalToPeers(string eventKey)
        {
            // Loop through peers and send the signal
            var queryString = "?" + PeerToPeerHelper.RequestKeys.EventKey + "=" + HttpUtility.UrlEncode(eventKey);
            Parallel.ForEach(_peers, (peer) =>
            {
                PeerToPeerHelper.CreatePrepareAndSendRequestAsync(peer + SignalReceiverHandler.HandlerName + queryString, PrepareRequest)
                    .ContinueWith(t =>
                    {
                        if (t.Exception == null)
                        {
                            t.Result.Close();
                        }
                    }).Wait();
            });
        }
    }
}