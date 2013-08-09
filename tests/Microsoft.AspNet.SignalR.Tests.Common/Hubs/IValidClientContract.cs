using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public interface IValidClientContract<T>
    {
        void Echo(T message);
        Task Ping();
    }
}
