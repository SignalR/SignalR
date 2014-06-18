using Microsoft.AspNet.SignalR;
using System;
using System.Linq;
using System.Threading;

namespace BenchmarkServer
{
    public class Dashboard : Hub
    {
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;
        private static int _broadcastCount = 1;
        private static int _broadcastSeconds = 1;
        private static bool _batchingEnabled;
        private static int _actualFps = 0;
        private static bool _isRunning = false;
        private static long _rate = 1000;

        private static readonly Lazy<Timer> _timerInstance = new Lazy<Timer>(() =>
        {
            return new Timer(
                async _ =>
                {
                    if (_batchingEnabled)
                    {
                        var count = _broadcastCount;
                        var payload = _broadcastPayload;
                        for (var i = 0; i < count; i++)
                        {
                            await WebSocketHandler.Broadcast(payload);
                        }
                    }
                    else
                    {
                        await WebSocketHandler.Broadcast(_broadcastPayload);
                    }
                },
                null,
                Timeout.Infinite,
                _rate
                );
        });

        internal static void Init()
        {
            SetBroadcastPayload();
        }

        public dynamic GetStatus()
        {
            return new
            {
                ConnectionBehavior = WebSocketHandler.Behavior,
                BroadcastBatching = _batchingEnabled,
                BroadcastCount = _broadcastCount,
                BroadcastSeconds = _broadcastSeconds,
                BroadcastSize = _broadcastSize,
                Broadcasting = _isRunning,
                ServerFps = _actualFps
            };
        }

        public void SetConnectionBehavior(ConnectionBehavior behavior)
        {
            WebSocketHandler.Behavior = behavior;
            Clients.Others.connectionBehaviorChanged(((int)behavior).ToString());
        }

        public void SetBroadcastBehavior(bool batchingEnabled)
        {
            _batchingEnabled = batchingEnabled;
            Clients.Others.broadcastBehaviorChanged(batchingEnabled);
        }

        public void SetBroadcastRate(int count, int seconds)
        {
            // Need to turn the count/seconds into FPS
            _broadcastCount = count;
            _broadcastSeconds = seconds;
            _rate = _batchingEnabled ? 1000 * seconds : 1000 / count;

            if (_isRunning)
            {
                _timerInstance.Value.Change(0, _rate);
            }

            Clients.Others.broadcastbroadcastRateChanged(count, seconds);
        }

        public void SetBroadcastSize(int size)
        {
            _broadcastSize = size;
            SetBroadcastPayload();
            Clients.Others.broadcastSizeChanged(size.ToString());
        }

        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void BroadcastOnce()
        {
            WebSocketHandler.Broadcast(_broadcastPayload).Wait();
        }

        public void StartBroadcast() {
            _timerInstance.Value.Change(0, _rate);
            _isRunning = true;
            Clients.All.started();
        }

        public void StopBroadcast() {
            _timerInstance.Value.Change(0, Timeout.Infinite);
            _isRunning = false;
            Clients.All.stopped();
            Clients.All.serverFps(0);
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}
