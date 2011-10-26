using System.Threading.Tasks;
namespace SignalR
{
    public interface IGroupManager
    {
        Task AddToGroup(string clientId, string groupName);
        Task RemoveFromGroup(string clientId, string groupName);
    }
}
