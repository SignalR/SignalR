using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub
{
    public class TypedDemoHub : Hub<IClient>
    {
        private static int _invokeCount = 0;

        public void Echo(string message)
        {
            Clients.Caller.Echo(message, Interlocked.Increment(ref _invokeCount)).Wait();
        }
    }

    public interface IClient
    {
        Task Echo(string message, int invokeCount);
        void MethodB(int arg1, int arg2);
    }
}