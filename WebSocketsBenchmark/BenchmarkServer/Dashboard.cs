using Microsoft.AspNet.SignalR;

namespace BenchmarkServer
{
    public class Dashboard : Hub
    {
        internal static void Init() { }

        public dynamic GetStatus() { return new { }; }

        public void SetConnectionBehavior(ConnectionBehavior behavior) { }

        public void SetBroadcastBehavior(bool batchingEnabled) { }

        public void SetBroadcastRate(int count, int seconds) { }

        public void SetBroadcastSize(int size) { }

        public void ForceGC() { }

        public void BroadcastOnce() { }

        public void StartBroadcast() { }

        public void StopBroadcast() { }

        private static void SetBroadcastPayload() { }
    }
}
