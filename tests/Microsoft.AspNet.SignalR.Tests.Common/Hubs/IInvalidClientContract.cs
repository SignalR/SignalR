using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public interface IInvalidClientContract
    {
        // Methods must return void or Task, so this is invalid
        bool Echo(string message);
        Task Ping();
    }
}
