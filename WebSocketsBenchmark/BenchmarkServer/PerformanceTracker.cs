using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;

namespace BenchmarkServer
{
    public class PerformanceTracker
    {
        private CancellationTokenSource _samplingTcs;
        private Stopwatch _stopwatch;
        private TimeSpan _lastSampleTime;
        private long _lastClientsConnected;
        private long _lastMessagesSent;
        private long _lastBroadcastsCompleted;

        private static long _clientsConnected = 0;
        private static long _messagesSent = 0;
        private static long _broadcastsStarted = 0;
        private static long _broadcastsCompleted = 0;
        private static long _broadcastTime = 0;

        public event EventHandler<PerformanceSample> Sampling;

        public void ClientConnected() { Interlocked.Increment(ref _clientsConnected); }
        public void ClientDisconnected() { Interlocked.Decrement(ref _clientsConnected); }

        public void MessageSent() { Interlocked.Increment(ref _messagesSent); }

        public void BroadcastStarted() { Interlocked.Increment(ref _broadcastsStarted); }

        public void BroadcastCompleted() { Interlocked.Increment(ref _broadcastsCompleted); }

        public void UpdateBroadcastTime(long broadcastTime) { Interlocked.Exchange(ref _broadcastTime, broadcastTime); }

        public void StartSampling()
        {
            _samplingTcs = new CancellationTokenSource();

            new Thread(new ThreadStart(Sample)).Start();
        }

        public void StopSampling()
        {
            _samplingTcs.Cancel();
        }

        protected virtual void OnTakingSample(PerformanceSample sample)
        {
            var handler = Sampling;

            if (handler != null)
            {
                handler(this, sample);
            }
        }

        private void Sample()
        {
            _stopwatch = Stopwatch.StartNew();
            _lastSampleTime = new TimeSpan(0);
            _lastClientsConnected = Interlocked.Read(ref _clientsConnected);
            _lastMessagesSent = Interlocked.Read(ref _messagesSent);
            _lastBroadcastsCompleted = Interlocked.Read(ref _broadcastsCompleted);

            while (!_samplingTcs.IsCancellationRequested)
            {

                var time = _stopwatch.Elapsed;
                var changeInTime = time - _lastSampleTime;
                _lastSampleTime = time;

                var clientsConnected = Interlocked.Read(ref _clientsConnected);
                var changeInConnections = clientsConnected - _lastClientsConnected;
                _lastClientsConnected = clientsConnected;

                var messagesSent = Interlocked.Read(ref _messagesSent);
                var changeInMessages = messagesSent - _lastMessagesSent;
                _lastMessagesSent = messagesSent;

                var broadcastsCompleted = Interlocked.Read(ref _broadcastsCompleted);
                var changeInBroadcastCompleted = broadcastsCompleted - _lastBroadcastsCompleted;
                _lastBroadcastsCompleted = broadcastsCompleted;

                var broadcastTime = Interlocked.Read(ref _broadcastTime);

                OnTakingSample(new PerformanceSample
                {
                    SampleTime = (long)time.TotalMilliseconds,
                    ClientsConnected = clientsConnected,
                    ClientConnectionsPerSecond = (long)(1000 * changeInConnections / changeInTime.TotalMilliseconds),
                    MessagesSent = messagesSent,
                    MessagesPerSecond = (long)(1000 * changeInMessages / changeInTime.TotalMilliseconds),
                    BroadcastRate = (long)(1000 * changeInBroadcastCompleted / changeInTime.TotalMilliseconds),
                    LastBroadcastDuration = _broadcastTime
                });

                Thread.Sleep(1000);
            }
        }
    }
}