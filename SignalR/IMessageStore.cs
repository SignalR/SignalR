using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IMessageStore
    {
        Task<long?> GetLastId();
        Task Save(string key, object value);
        Task<IOrderedEnumerable<Message>> GetAllSince(IEnumerable<string> keys, long id);
        Task<IEnumerable<Message>> GetAllSince(string key, long id);
    }
}
