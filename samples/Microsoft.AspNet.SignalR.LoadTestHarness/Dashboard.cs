using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class Dashboard : Hub
    {
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;

        private static readonly Lazy<HighFrequencyTimer> _timerInstance = new Lazy<HighFrequencyTimer>(() =>
        {
            var clients = GlobalHost.ConnectionManager.GetHubContext<Dashboard>().Clients;
            var connection = GlobalHost.ConnectionManager.GetConnectionContext<TestConnection>().Connection;
            return new HighFrequencyTimer(1,
                _ => connection.Broadcast(_broadcastPayload),
                () => clients.All.started(),
                () => clients.All.stopped(),
                fps => clients.All.serverFps(fps)
            );
        });

        private HighFrequencyTimer _timer { get { return _timerInstance.Value; } }

        internal static void Init()
        {
            SetBroadcastPayload();
        }

        public bool IsBroadcasting()
        {
            return _timer.IsRunning();
        }

        public void SetConnectionBehavior(ConnectionBehavior behavior)
        {
            TestConnection.Behavior = behavior;
            Clients.All.connectionBehaviorChanged(behavior);
        }

        public void StartBroadcast()
        {
            _timer.Start();
        }

        public void StopBroadcast()
        {
            _timer.Stop();
        }

        public void SetBroadcastRate(long rate)
        {
            _timer.FPS = rate;
            Clients.All.broadcastRateChanged(rate);
        }

        public void SetBroadcastSize(int size)
        {
            _broadcastSize = size;
            SetBroadcastPayload();
            Clients.All.broadcastSizeChanged(size);
        }

        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}
