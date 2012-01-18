using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SignalR.Infrastructure;
using System.Collections.Generic;

namespace SignalR
{
    /// <summary>
    /// An in-memory signal bus that signals directly on an incoming signal
    /// </summary>
    public class InProcessSignalBus : ISignalBus
    {
        private readonly ConcurrentDictionary<string, LockedList<EventHandler<SignaledEventArgs>>> _handlers =
            new ConcurrentDictionary<string, LockedList<EventHandler<SignaledEventArgs>>>();

        private void OnSignaled(string eventKey)
        {
            LockedList<EventHandler<SignaledEventArgs>> handlers;
            if (_handlers.TryGetValue(eventKey, out handlers))
            {
                var delegates = handlers.Copy();
                
                foreach (var callback in delegates)
                {
                    if (callback != null)
                    {
                        callback.Invoke(this, new SignaledEventArgs(eventKey));
                    }
                }
            }
        }

        public Task Signal(string eventKey)
        {
            return Task.Factory.StartNew(() => OnSignaled(eventKey));
        }

        public void AddHandler(IEnumerable<string> eventKeys, EventHandler<SignaledEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            foreach (var key in eventKeys)
            {
                var handlers = _handlers.GetOrAdd(key, _ => new LockedList<EventHandler<SignaledEventArgs>>());
                handlers.Add(handler);
            }
        }

        public void RemoveHandler(IEnumerable<string> eventKeys, EventHandler<SignaledEventArgs> handler)
        {
            foreach (var key in eventKeys)
            {
                LockedList<EventHandler<SignaledEventArgs>> handlers;
                if (_handlers.TryGetValue(key, out handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }
    }
}