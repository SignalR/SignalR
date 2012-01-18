using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SignalR
{
    public interface ISignalBus
    {
        void AddHandler(IEnumerable<string> eventKeys, EventHandler<SignaledEventArgs> handler);
        void RemoveHandler(IEnumerable<string> eventKeys, EventHandler<SignaledEventArgs> handler);
        Task Signal(string eventKey);
    }
}