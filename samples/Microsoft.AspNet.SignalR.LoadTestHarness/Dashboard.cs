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
        private static int _broadcastCount = 1;
        private static int _broadcastSeconds = 1;
        private static bool _batchingEnabled;
        private static int _actualFps = 0;

        private static readonly Lazy<HighFrequencyTimer> _timerInstance = new Lazy<HighFrequencyTimer>(() =>
        {
            var clients = GlobalHost.ConnectionManager.GetHubContext<Dashboard>().Clients;
            var connection = GlobalHost.ConnectionManager.GetConnectionContext<TestConnection>().Connection;
            return new HighFrequencyTimer(1,
                _ =>
                    {
                        if (_batchingEnabled)
                        {
                            var count = _broadcastCount;
                            var payload = _broadcastPayload;
                            for (var i = 0; i < count; i++)
                            {
                                connection.Broadcast(payload);
                            }
                        }
                        else
                        {
                            connection.Broadcast(_broadcastPayload);
                        }
                    },
                () => clients.All.started(),
                () => clients.All.stopped(),
                fps => { _actualFps = fps; clients.All.serverFps(fps); }
            );
        });

        private HighFrequencyTimer _timer { get { return _timerInstance.Value; } }

        internal static void Init()
        {
            SetBroadcastPayload();
        }

        public dynamic GetStatus()
        {
            return new
            {
                ConnectionBehavior = TestConnection.Behavior,
                BroadcastBatching = _batchingEnabled,
                BroadcastCount = _broadcastCount,
                BroadcastSeconds = _broadcastSeconds,
                BroadcastSize = _broadcastSize,
                Broadcasting = _timer.IsRunning(),
                ServerFps = _actualFps
            };
        }

        public void SetConnectionBehavior(ConnectionBehavior behavior)
        {
            TestConnection.Behavior = behavior;
            Clients.All.connectionBehaviorChanged(behavior);
        }

        public void SetBroadcastBehavior(bool batchingEnabled)
        {
            _batchingEnabled = batchingEnabled;
            Clients.All.broadcastBehaviorChanged(batchingEnabled);
        }

        public void SetBroadcastRate(int count, int seconds)
        {
            // Need to turn the count/seconds into FPS
            _broadcastCount = count;
            _broadcastSeconds = seconds;
            _timer.FPS = _batchingEnabled ? seconds : 1;
            Clients.All.broadcastRateChanged(count, seconds);
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

        public void StartBroadcast()
        {
            _timer.Start();
        }

        public void StopBroadcast()
        {
            _timer.Stop();
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}
