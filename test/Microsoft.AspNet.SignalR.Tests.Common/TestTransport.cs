using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Transports;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class TestTransport : ITransport
    {
        public int ProcessRequestCalls { get; private set; } = 0;

        public Func<string, Task> Received { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        public Func<bool, Task> Disconnected { get; set; }

        public string ConnectionId { get; set; }

        public string GroupsToken { get; set; }

        public Task<string> GetGroupsToken()
        {
            return Task.FromResult(GroupsToken);
        }

        public Task ProcessRequest(ITransportConnection connection)
        {
            ProcessRequestCalls += 1;
            // Just no-op
            return Task.CompletedTask;
        }

        public Task Send(object value)
        {
            throw new InvalidOperationException("Reached TestTransport.Send");
        }
    }
}
