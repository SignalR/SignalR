using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Test
{
    public class LongRunningHub : Hub
    {
        private static ManualResetEvent myEvent = new ManualResetEvent(false);

        public void Set()
        {
            myEvent.Set();
        }

        public void Reset()
        {
            myEvent.Reset();
        }

        public Task LongRunningMethod(int i)
        {
            Clients.Caller.serverIsWaiting(i);

            return Task.Run(() =>
            {
                myEvent.WaitOne();
            });
        }
    }
}