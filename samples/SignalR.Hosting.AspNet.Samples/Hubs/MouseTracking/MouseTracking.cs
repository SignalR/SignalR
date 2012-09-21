using System.Collections.Generic;
using System.Threading;
using SignalR.Hubs;

namespace SignalR.Samples.Hubs.MouseTracking
{
    public class MouseTracking : Hub
    {
        private static long _id;

        public void Join()
        {
            Clients.Caller.id = Interlocked.Increment(ref _id);
        }

        public void Move(int x, int y)
        {
            Clients.Others.moveMouse(Clients.Caller.id, x, y);
        }
    }
}