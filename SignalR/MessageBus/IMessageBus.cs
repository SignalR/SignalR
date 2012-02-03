using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.MessageBus
{
    public interface IMessageBus
    {
        Task<MessageResult> GetMessages(IEnumerable<string> eventKeys, string id);
        Task Send(string eventKey, object value);
    }
}
