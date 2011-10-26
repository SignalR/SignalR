using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// An in-memory signaler that signals directly on an incoming signal
    /// </summary>
    public class InProcessSignalBus : ISignalBus
    {
        private readonly ConcurrentDictionary<string, SafeSet<EventHandler<SignaledEventArgs>>> _handlers = new ConcurrentDictionary<string, SafeSet<EventHandler<SignaledEventArgs>>>(StringComparer.OrdinalIgnoreCase);

        private void OnSignaled(string eventKey)
        {
            SafeSet<EventHandler<SignaledEventArgs>> handlers;
            if (_handlers.TryGetValue(eventKey, out handlers) && handlers.Any())
            {
                Parallel.ForEach(handlers.GetSnapshot(), handler => handler(this, new SignaledEventArgs(eventKey)));
            }
        }

        public Task Signal(string eventKey)
        {
            return Task.Factory.StartNew(() => OnSignaled(eventKey));
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
    }
}