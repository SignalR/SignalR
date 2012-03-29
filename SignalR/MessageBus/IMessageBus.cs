using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IMessageBus
    {
        Task<MessageResult> GetMessages(IEnumerable<string> eventKeys, string id, CancellationToken timeoutToken);
        Task Send(string connectionId, string eventKey, object value);
    }
}
