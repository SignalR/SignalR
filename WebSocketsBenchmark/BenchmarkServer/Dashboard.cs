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
        private static bool _isRecording = false;
        private static long _rate = 1000;
        private static bool _reuseBuffer = true;

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
                            await WebSocketHandler.Broadcast(payload, _reuseBuffer);
                        }
                    }
                    else
                    {
                        await WebSocketHandler.Broadcast(_broadcastPayload, _reuseBuffer);
                    }
                },
                null,
                Timeout.Infinite,
                _rate
                );
        });

        private static readonly PerformanceRecorder<PerformanceSample> _performanceRecorder = new PerformanceRecorder<PerformanceSample>();

        internal static void Init()
        {
            SetBroadcastPayload();
            WebSocketHandler.PerformanceTracker.Sampling += RecordSample;
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
                Recording = _isRecording,
                ServerFps = _actualFps,
                BufferReuse = _reuseBuffer
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

        public void SetBufferReuse(bool reuseBuffer)
        {
            _reuseBuffer = reuseBuffer;
            Clients.Others.bufferReuseChanged(reuseBuffer);
        }

        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void BroadcastOnce()
        {
            WebSocketHandler.Broadcast(_broadcastPayload, _reuseBuffer).Wait();
        }

        public void StartBroadcast()
        {
            _timerInstance.Value.Change(0, _rate);
            _isRunning = true;
            Clients.All.started();
        }

        public void StopBroadcast()
        {
            _timerInstance.Value.Change(0, Timeout.Infinite);
            _isRunning = false;
            Clients.All.stopped();
            Clients.All.serverFps(0);
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }

        public void StartRecording()
        {
            if (!_isRecording)
            {
                _isRecording = true;
                _performanceRecorder.Reset();
                WebSocketHandler.PerformanceTracker.StartSampling();
                Clients.All.startedRecording();
            }
        }

        public void StopRecording()
        {
            if (_isRecording)
            {
                WebSocketHandler.PerformanceTracker.StopSampling();
                _performanceRecorder.Record();
                Clients.All.stoppedRecording();
            }
        }

        private static void RecordSample(object sender, PerformanceSample performanceSample)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<Dashboard>();
            context.Clients.All.update(performanceSample);
            _performanceRecorder.AddSample(performanceSample);
        }
    }
}
