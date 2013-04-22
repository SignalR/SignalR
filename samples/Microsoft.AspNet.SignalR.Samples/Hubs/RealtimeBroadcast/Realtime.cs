using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.RealtimeBroadcast
{
    public class Realtime : Hub
    {
        private static readonly Lazy<HighFrequencyTimer> _timerInstance = new Lazy<HighFrequencyTimer>(() =>
            {
                var clients = GlobalHost.ConnectionManager.GetHubContext<Realtime>().Clients;
                return new HighFrequencyTimer(25,
                    id => clients.All.frame(id),
                    () => clients.All.engineStarted(),
                    () => clients.All.engineStopped(),
                    fps => clients.All.serverFps(fps)
                );
            });

        private HighFrequencyTimer _timer { get { return _timerInstance.Value; } }

        public bool IsEngineRunning()
        {
            return _timer.IsRunning();
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public double GetFPS()
        {
            return _timer.FPS;
        }

        public void SetFPS(int fps)
        {
            _timer.FPS = fps;
        }

        public long GetFrameId()
        {
            return _timer.GetFrameId();
        }
    }
}