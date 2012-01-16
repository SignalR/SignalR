using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IMessageStore
    {
        Task<string> GetLastId();
        Task Save(string key, object value);
        Task<IOrderedEnumerable<Message>> GetAllSince(IEnumerable<string> keys, string id);
    }
}
