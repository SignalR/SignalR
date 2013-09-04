using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Transports;

namespace Microsoft.AspNet.SignalR
{
    public interface IDuplexConnection : IConnection, ITransportConnection
    {
        string ConnectionId { get; }
    }
}
