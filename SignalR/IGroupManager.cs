using System.Threading.Tasks;
namespace SignalR
{
    public interface IGroupManager
    {
        Task AddToGroup(string connectionId, string groupName);
        Task RemoveFromGroup(string connectionId, string groupName);
    }
}
