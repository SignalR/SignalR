using System;
using System.Threading.Tasks;

namespace SignalR
{
    public interface ISignalBus
    {
        void AddHandler(string eventKey, EventHandler<SignaledEventArgs> handler);
        void RemoveHandler(string eventKey, EventHandler<SignaledEventArgs> handler);
        Task Signal(string eventKey);
    }
}