using System.Threading.Tasks;

namespace SignalR
{
    internal interface ISubscription
    {
        string Identity { get; }

        bool SetQueued();
        bool UnsetQueued();

        Task WorkAsync();
    }
}
