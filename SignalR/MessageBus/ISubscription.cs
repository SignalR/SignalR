using System.Threading.Tasks;

namespace SignalR
{
    public interface ISubscription
    {
        string Identity { get; }

        bool SetQueued();
        bool UnsetQueued();

        Task WorkAsync();
    }
}
