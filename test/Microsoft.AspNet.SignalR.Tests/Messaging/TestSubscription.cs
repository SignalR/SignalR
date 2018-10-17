using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Tests.Messaging
{
    public class TestSubscription : ISubscription, IDisposable
    {
        private readonly Func<Task> _onWork;

        public bool Disposed { get; private set; }

        public string Identity { get; }

        public TestSubscription(string identity, Func<Task> onWork)
        {
            Identity = identity;
            _onWork = onWork;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public bool SetQueued()
        {
            return true;
        }

        public bool UnsetQueued()
        {
            return true;
        }

        public Task Work()
        {
            return _onWork();
        }
    }
}
