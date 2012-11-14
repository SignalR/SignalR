using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    internal class PollingWait
    {
        private readonly Action _action;
        private readonly Func<bool> _isComplete;

        public PollingWait(Action action, Func<bool> isComplete)
        {
            _action = action;
            _isComplete = isComplete;
        }

        public bool IsComplete
        {
            get
            {
                try
                {
                    return _isComplete();
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Invoke()
        {
            _action();

            while (!IsComplete)
            {
                Thread.Sleep(500);
            }
        }
    }
}
