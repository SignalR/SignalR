// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
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
        private IPerformanceCounter[] _counters;
        private Dictionary<IPerformanceCounter, List<CounterSample>> _samples;

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
        }

        public int Duration { get; private set; }
        public int Warmup { get; private set; }
        public int Connections { get; private set; }
        public int Senders { get; private set; }
        public string Payload { get; private set; }
        public IDependencyResolver Resolver { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public virtual void InitializePerformanceCounters()
        {
            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(Resolver, CancellationTokenSource.Token);
        }

        public virtual void Initialize()
        {
            InitializePerformanceCounters();

            _counters = GetPerformanceCounters(Resolver.Resolve<IPerformanceCounterManager>());
            _samples = new Dictionary<IPerformanceCounter, List<CounterSample>>(_counters.Length);
            for (int i = 0; i < _counters.Length; i++)
            {
                _samples[_counters[i]] = new List<CounterSample>();
            }
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

        public virtual void Sample()
        {
            for (int i = 0; i < _counters.Length; i++)
            {
                var counter = _counters[i];
                _samples[counter].Add(counter.NextSample());
            }
        }

        public virtual void Record()
        {
            long[] bytesPerSec = null;
            long[] recvsPerSec = null;
            long[] sendsPerSec = null;

            foreach (var item in _samples)
            {
                var counterName = item.Key.CounterName;
                var key = String.Format("Stress-{0};{1}", GetType().Name, counterName);
                var samplesList = item.Value;

                long[] values = new long[samplesList.Count - 1];
                for (int i = 0; i < samplesList.Count - 1; i++)
                {
                    values[i] = (long)Math.Round(CounterSample.Calculate(samplesList[i], samplesList[i + 1]));
#if PERFRUN
                    Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)values[i], key);
#endif
                }
                RecordAggregates(key, values);

                if (counterName.Contains("Bytes/sec"))
                {
                    bytesPerSec = values;
                }
                else if (counterName.Contains("Received/Sec"))
                {
                    recvsPerSec = values;
                }
                else if (counterName.Contains("Published/Sec"))
                {
                    sendsPerSec = values;
                }
            }

            if ((bytesPerSec != null) && (recvsPerSec != null) && (sendsPerSec != null))
            {
                long[] bytesPerMsg = new long[bytesPerSec.Length];
                for (int i = 0; i < bytesPerSec.Length; i++)
                {
                    bytesPerMsg[i] = (long)Math.Round((double)bytesPerSec[i] / (recvsPerSec[i] + sendsPerSec[i]));
                }
                RecordAggregates("Allocated Bytes/Message", bytesPerMsg);
            }
        }

        private void RecordAggregates(string key, long[] values)
        {
            Array.Sort(values);
            double median = values[values.Length / 2];
            if (values.Length % 2 == 0)
            {
                median = median + values[(values.Length / 2) - 1] / 2;
            }

            var average = values.Average();
            var sumOfSquaresDiffs = values.Select(v => (v - average) * (v - average)).Sum();
            var stdDevP = Math.Sqrt(sumOfSquaresDiffs / values.Length) / average * 100;
            Console.WriteLine("{0} (MEDIAN):  {1}", key, Math.Round(median));
            Console.WriteLine("{0} (AVERAGE): {1}", key, Math.Round(average));
            Console.WriteLine("{0} (STDDEV%): {1}%", key, Math.Round(stdDevP));
        }

        protected virtual IPerformanceCounter[] GetPerformanceCounters(IPerformanceCounterManager counterManager)
        {
            return new IPerformanceCounter[]
            {
                counterManager.ConnectionsConnected,
                counterManager.MessageBusMessagesReceivedPerSec,
                counterManager.MessageBusMessagesReceivedTotal,
                counterManager.MessageBusMessagesPublishedPerSec,
                counterManager.MessageBusMessagesPublishedTotal,
                LoadCounter(counterManager, "Processor", "% Processor Time", "_Total"),
                LoadCounter(counterManager, ".NET CLR Memory", "% Time in GC", "_Global_"),
                LoadCounter(counterManager, ".NET CLR Memory", "Allocated Bytes/sec", "_Global_")
            };
        }

        private IPerformanceCounter LoadCounter(IPerformanceCounterManager counterManager, string category, string name, string instance)
        {
            return ((Microsoft.AspNet.SignalR.Infrastructure.PerformanceCounterManager)counterManager).LoadCounter(category, name, instance, readOnly: true);
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
