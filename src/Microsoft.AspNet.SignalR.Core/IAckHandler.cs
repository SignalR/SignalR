using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
{
    public interface IAckHandler
    {
        Task CreateAck(string id);
        bool TriggerAck(string id);
    }
}
