using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
{
    public interface ISubscription
    {
        string Identity { get; }

        bool SetQueued();
        bool UnsetQueued();

        Task WorkAsync();
    }
}
