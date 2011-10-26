using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IMessageStore
    {
        Task<long?> GetLastId();
        Task Save(string key, object value);
        Task<IEnumerable<Message>> GetAllSince(string key, long id);
    }
}
