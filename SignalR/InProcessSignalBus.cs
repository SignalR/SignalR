using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// An in-memory signal bus that signals directly on an incoming signal
    /// </summary>
    public class InProcessSignalBus : ISignalBus
    {
        private readonly ConcurrentDictionary<string, CustomStack<EventHandler<SignaledEventArgs>>> _handlers =
            new ConcurrentDictionary<string, CustomStack<EventHandler<SignaledEventArgs>>>();

        private void OnSignaled(string eventKey)
        {
            CustomStack<EventHandler<SignaledEventArgs>> handlers;
            if (_handlers.TryGetValue(eventKey, out handlers))
            {
                var delegates = handlers.GetAllAndClear();
                if (delegates != null)
                {
                    foreach (var callback in delegates)
                    {
                        if (callback != null)
                        {
                            callback.Invoke(this, new SignaledEventArgs(eventKey));
                        }
                    }
                }
            }
        }

        public Task Signal(string eventKey)
        {
            return Task.Factory.StartNew(() => OnSignaled(eventKey));
        }

        public void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            var delegates = _handlers.GetOrAdd(eventKey, _ => new CustomStack<EventHandler<SignaledEventArgs>>());
            delegates.Add(handler);
        }

        public void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler)
        {
            // Don't need to do anything as our handlers are cleared automatically by CustomStack
        }
    }
}