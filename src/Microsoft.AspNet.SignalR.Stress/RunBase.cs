// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Stress.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress
{
    public abstract class RunBase : IRun
    {
        private readonly CountdownEvent _countDown;
        private readonly List<IDisposable> _receivers = new List<IDisposable>();
        private IPerformanceCounter[] _counters;
        private Dictionary<IPerformanceCounter, List<CounterSample>> _samples;
        private string _contractName;
        private bool _disposed;

        public RunBase(RunData runData)
        {
            RunData = runData;
            Resolver = new DefaultDependencyResolver();
            CancellationTokenSource = new CancellationTokenSource();
            _countDown = new CountdownEvent(runData.Senders);
        }

        public RunData RunData { get; private set; }
        public int Duration { get { return RunData.Duration; } }
        public int Warmup { get { return RunData.Warmup; } }
        public int Connections { get { return RunData.Connections; } }
        public int Senders { get { return RunData.Senders; } }
        public string Payload { get { return RunData.Payload; } }
        public IDependencyResolver Resolver { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        protected virtual string ScenarioName
        {
            get
            {
                return GetContractName();
            }
        }

        /// <summary>
        /// This is the starting point. 
        /// </summary>
        public void Run()
        {
            // Step 1: Initialize the Run
            Console.WriteLine("{0}: Starting the test.", DateTime.Now);
            Initialize();
            Console.WriteLine("{0}: Initialized completed", DateTime.Now);

            // Step 2: Start the Run and have it run for (Warmup + Duration) time
            RunTest();
            Console.WriteLine("{0}: Warming up: {1} ", DateTime.Now, Warmup);
            Thread.Sleep(Warmup * 1000);
            
            // Step 3: Start the sampling after some warm up period
            Console.WriteLine("{0}: Sampling started: {1}", DateTime.Now, Duration);
            TimeSpan endTime = TimeSpan.FromSeconds(Duration);
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                Sample();
                Thread.Sleep(RunData.SampleRate);
            }
            while (timer.Elapsed < endTime);
            Console.WriteLine("{0}: Test finished.", DateTime.Now);

            // Step 4: Time to collect some samples
            Record();
            Console.WriteLine("{0}: Recording finished.", DateTime.Now);
        }

        /// <summary>
        /// Step 1: Initialize the run. For example, initialize the performance counters. 
        /// </summary>
        public virtual void Initialize()
        {
            // set up the perf counter for recording purpose
            InitializePerformanceCounters();

            _counters = GetPerformanceCounters(Resolver.Resolve<IPerformanceCounterManager>());
            _samples = new Dictionary<IPerformanceCounter, List<CounterSample>>(_counters.Length);
            for (int i = 0; i < _counters.Length; i++)
            {
                if (_counters[i] != null)
                {
                    _samples[_counters[i]] = new List<CounterSample>();
                }
            }

            // set up the client
            for (int i = 0; i < Connections; i++)
            {
                IDisposable receiver = CreateReceiver(i);
                if (receiver != null)
                {
                    _receivers.Add(receiver);
                }
            }
        }

        /// <summary>
        /// Step 2: Run the test by scheduling multiple background threads to send the messages to the server.
        /// </summary>
        public virtual void RunTest()
        {
            for (var i = 0; i < Senders; i++)
            {
                ThreadPool.QueueUserWorkItem(state => Sender(state), Tuple.Create(i, Guid.NewGuid().ToString()));
            }
        }

        /// <summary>
        /// Step 3: Collect samples data at certain sampling rate ( configurable ) while the test is still running but after
        /// certain warm up time.
        /// </summary>
        public virtual void Sample()
        {
            for (int i = 0; i < _counters.Length; i++)
            {
                var counter = _counters[i];
                if (counter != null)
                {
                    _samples[counter].Add(counter.NextSample());
                }
            }
        }

        /// <summary>
        /// Step 4: Now let us record the sampling data we just collected for this run, and aggregate the results if necessary.
        /// </summary>
        public virtual void Record()
        {
            long[] bytesPerSec = null;
            long[] recvsPerSec = null;
            long[] sendsPerSec = null;

            foreach (var item in _samples)
            {
                var counterName = item.Key.CounterName;
                var key = String.Format("{0};{1}", ScenarioName, counterName);
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
                var bytesPerMsg = new long[bytesPerSec.Length];
                for (int i = 0; i < bytesPerSec.Length; i++)
                {
                    bytesPerMsg[i] = (long)Math.Round((double)bytesPerSec[i] / (recvsPerSec[i] + sendsPerSec[i]));
                }
                RecordAggregates("Allocated Bytes/Message", bytesPerMsg);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CancellationTokenSource.Cancel();

                    // Wait for all senders to stop
                    _countDown.Wait(TimeSpan.FromMilliseconds(1000 * Senders));

                    _receivers.ForEach(s => s.Dispose());

                    _receivers.Clear();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Sets up the receivers for the messages. For example, it can set up the client connection and HubProxies 
        /// so that in the hub cases, this can receives messages from the server. In the low level case, it can set up the subscribers 
        /// for the messagebus. 
        /// </summary>
        /// <param name="connectionIndex"></param>
        /// <returns></returns>
        protected abstract IDisposable CreateReceiver(int connectionIndex);

        /// <summary>
        /// Sends the messages from client to the server or inject message into connection or the message bus. 
        /// </summary>
        /// <param name="senderIndex"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        protected abstract Task Send(int senderIndex, string source);
        
        /// <summary>
        /// Called by the Initialize method to initialize a default set of perf counters
        /// </summary>
        protected virtual void InitializePerformanceCounters()
        {
            // Initialize performance counters for this run
            Utility.InitializePerformanceCounters(Resolver, CancellationTokenSource.Token);
        }

        /// <summary>
        /// Aggregate the sample data by sorting the input array and dispaly the Median/Average/Stddev
        /// for a particular measurement
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        protected void RecordAggregates(string key, long[] values)
        {
            Array.Sort(values);
            double median = values[values.Length / 2];
            if (values.Length % 2 == 0)
            {
                median = (median + values[(values.Length / 2) - 1]) / 2;
            }

            var sum = values.Select(i => new BigInteger(i)).Aggregate((aggregate, bi) => aggregate + bi);
            BigInteger remainder;
            var average = (double)BigInteger.DivRem(sum, values.Length, out remainder) + ((double)remainder / values.Length);
            
            double stdDevP = 0;
            if (average > 0)
            {
                var sumOfSquares = values.Sum(i => Math.Pow(i - average, 2.0));
                stdDevP = Math.Sqrt(sumOfSquares / values.Length) / average * 100;
            }
            
            Console.WriteLine("{0} (MEDIAN):  {1}", key, Math.Round(median));
            Console.WriteLine("{0} (AVERAGE): {1}", key, Math.Round(average));
            Console.WriteLine("{0} (STDDEV%): {1}%", key, Math.Round(stdDevP));
        }

        protected string GetContractName()
        {
            if (String.IsNullOrEmpty(_contractName))
            {
                var type = GetType();
                var export = (ExportAttribute)type.GetCustomAttributes(typeof(ExportAttribute), true).FirstOrDefault();
                _contractName = (export == null) ? type.Name : export.ContractName;
            }
            return _contractName;
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
                counterManager.LoadCounter("Processor", "% Processor Time", "_Total", isReadOnly:true),
                counterManager.LoadCounter(".NET CLR Memory", "% Time in GC", "_Global_", isReadOnly:true),
                counterManager.LoadCounter(".NET CLR Memory", "Allocated Bytes/sec", "_Global_", isReadOnly:true)
            };
        }

        private async void Sender(object state)
        {
            var senderState = (Tuple<int, string>)state;

            while (!CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await Send(senderState.Item1, senderState.Item2);

                    if (RunData.SendDelay > 0)
                    {
                        await Task.Delay(RunData.SendDelay);
                    }
                }
                catch (Exception)
                {
                    // If a sender fails then continue
                    continue;
                }
            }

            _countDown.Signal();
        }
    }
}
