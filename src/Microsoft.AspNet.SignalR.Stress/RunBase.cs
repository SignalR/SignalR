using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    public class RunBase : IRun
    {
        private readonly CountdownEvent _countDown;
        private readonly List<IDisposable> _receivers = new List<IDisposable>();

        public RunBase(RunData runData)
        {
            Connections = runData.Connections;
            Senders = runData.Senders;
            Payload = runData.Payload;
            Resolver = new DefaultDependencyResolver();
            CancellationTokenSource = new CancellationTokenSource();

            _countDown = new CountdownEvent(runData.Senders);
        }

        public int Connections { get; private set; }
        public int Senders { get; private set; }
        public string Payload { get; private set; }
        public IDependencyResolver Resolver { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public virtual void Initialize()
        {
            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(Resolver, CancellationTokenSource.Token);
        }

        public virtual void Run()
        {
            Initialize();

            for (int i = 0; i < Connections; i++)
            {
                IDisposable receiver = CreateReceiver(i);
                if (receiver != null)
                {
                    _receivers.Add(receiver);
                }
            }

            for (var i = 0; i < Senders; i++)
            {
                ThreadPool.QueueUserWorkItem(state => Sender(state), i);
            }
        }

        protected virtual IDisposable CreateReceiver(int connectionIndex)
        {
            return null;
        }

        protected virtual Task Send(int senderIndex)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual void Dispose()
        {
            CancellationTokenSource.Cancel();

            // Wait for all senders to stop
            _countDown.Wait(TimeSpan.FromMilliseconds(1000 * Senders));

            _receivers.ForEach(s => s.Dispose());

            _receivers.Clear();
        }

        private void Sender(object state)
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                Send((int)state);
            }

            _countDown.Signal();
        }
    }
}
