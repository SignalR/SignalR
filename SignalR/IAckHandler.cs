using System;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IAckHandler
    {
        Task CreateAck(string id);
        bool TriggerAck(string id);
    }
}
