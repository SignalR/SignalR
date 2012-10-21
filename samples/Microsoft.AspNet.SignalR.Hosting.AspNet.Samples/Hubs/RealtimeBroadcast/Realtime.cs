using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.RealtimeBroadcast
{
    public class Realtime : Hub
    {
        private static readonly Lazy<Engine> _engineInstance = new Lazy<Engine>();

        private Engine _engine { get { return _engineInstance.Value; } }

        public bool IsEngineRunning()
        {
            return _engine.IsRunning();
        }

        public void Start()
        {
            _engine.Start();
        }

        public void Stop()
        {
            _engine.Stop();
        }

        public long GetInterval()
        {
            return _engine.GetInterval();
        }

        public void SetInterval(int interval)
        {
            _engine.SetInterval(interval);
        }

        public long GetTickId()
        {
            return _engine.GetTickId();
        }

        private class Engine
        {
            private Timer _timer;
            private long _interval = 1000 / 25;
            private long _tickId = 0;

            // 0 = stopped
            // 1 = starting
            // 2 = running
            // 3 = stopping
            private long _state = 0;

            public bool Start()
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                {
                    // Wasn't stopped
                    return false;
                }

                HubContext.Clients.All.engineStarted();

                var interval =  Interlocked.Read(ref _interval);
                _timer = new Timer(Tick, null, interval, interval);

                Interlocked.Exchange(ref _state, 2);
                return true;
            }

            public bool Stop()
            {
                if (Interlocked.CompareExchange(ref _state, 3, 2) != 2)
                {
                    // Wasn't running
                    return false;
                }

                Interlocked.Exchange(ref _tickId, 0);
                _timer.Dispose();
                _timer = null;

                Interlocked.Exchange(ref _state, 0);
                HubContext.Clients.All.engineStopped();
                return true;
            }

            public void Restart()
            {
                // TODO: Fix race here
                Stop();
                Start();
            }

            public long GetInterval()
            {
                return Interlocked.Read(ref _interval);
            }

            public void SetInterval(int interval)
            {
                Interlocked.Exchange(ref _interval, interval);
                HubContext.Clients.All.intervalChanged(interval);
                Restart();
            }

            public long GetTickId()
            {
                return Interlocked.Read(ref _tickId);
            }

            public bool IsRunning()
            {
                return Interlocked.Read(ref _state) == 2;
            }

            private void Tick(object state)
            {
                HubContext.Clients.All.tick(Interlocked.Increment(ref _tickId));
            }

            private IHubContext HubContext
            {
                get { return GlobalHost.ConnectionManager.GetHubContext<Realtime>(); }
            }
        }
    }
}