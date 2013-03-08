using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    public class RunBase : IRun
    {
        private readonly CountdownEvent _countDown;
        private readonly List<IDisposable> _receivers = new List<IDisposable>();
        private readonly IPerformanceCounter[] _counters;
        private Dictionary<IPerformanceCounter, CounterSample[]> _samples;
        private int _sampleCount = 0;

        public RunBase(RunData runData)
        {
            Duration = runData.Duration;
            Warmup = runData.Warmup;
            Connections = runData.Connections;
            Senders = runData.Senders;
            Payload = runData.Payload;
            Resolver = new DefaultDependencyResolver();
            CancellationTokenSource = new CancellationTokenSource();

            _countDown = new CountdownEvent(runData.Senders);

            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(Resolver, CancellationTokenSource.Token);
            _counters = GetPerformanceCounters(Resolver.Resolve<IPerformanceCounterManager>());
            if (_counters != null)
            {
                _samples = new Dictionary<IPerformanceCounter, CounterSample[]>(_counters.Length);
                for (int i = 0; i < _counters.Length; i++)
                {
                    _samples[_counters[i]] = new CounterSample[2];
                }
            }
        }

        public int Duration { get; private set; }
        public int Warmup { get; private set; }
        public int Connections { get; private set; }
        public int Senders { get; private set; }
        public string Payload { get; private set; }
        public IDependencyResolver Resolver { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public virtual void Run()
        {
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

        public virtual void Sample()
        {
            if (_sampleCount >= 2)
            {
                throw new InvalidOperationException();
            }

            if ((_counters != null) && (_samples != null))
            {
                for (int i = 0; i < _counters.Length; i++)
                {
                    var counter = _counters[i];
                    _samples[counter][_sampleCount] = counter.NextSample();
                }
                _sampleCount++;
            }
        }

        public virtual void Record()
        {
            if (_counters != null)
            {
                foreach (var item in _samples)
                {
                    var key = String.Format("Stress-{0};{1}", GetType().Name, item.Key.CounterName);
                    var value = (ulong)Math.Round(CounterSample.Calculate(item.Value[0], item.Value[1]));
#if PERFRUN
                    Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark(value, key);
#endif
                    Console.WriteLine("{0}={1}", key, value);
                }
            }
        }

        protected virtual IPerformanceCounter[] GetPerformanceCounters(IPerformanceCounterManager counterManager)
        {
            return new IPerformanceCounter[]
            {
                counterManager.MessageBusMessagesReceivedPerSec,
                counterManager.MessageBusMessagesReceivedTotal
            };
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
