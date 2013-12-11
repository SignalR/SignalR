// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Stress
{
    public abstract class StressRunBase : IRun
    {
        private readonly CountdownEvent _countDown;
        private readonly List<IDisposable> _receivers = new List<IDisposable>();
        private string _contractName;
        private bool _disposed;

        public StressRunBase(RunData runData)
        {
            RunData = runData;
            Resolver = new DefaultDependencyResolver();
            CancellationTokenSource = new CancellationTokenSource();
            _countDown = new CountdownEvent(runData.Senders);
        }

        public IDependencyResolver Resolver { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        protected RunData RunData { get; set; }

        public void Run()
        {
            // Step 1: Initialize the Run
            Console.WriteLine("{0}: Starting the stress test {1}.", DateTime.Now, GetContractName());
            Initialize();
            Console.WriteLine("{0}: Initialized completed", DateTime.Now);

            // Step 2: Start the Run and have it run for Duration time
            Console.WriteLine("{0}: Running the stress test: {1} seconds", DateTime.Now, RunData.Duration);
            RunTest();
            Thread.Sleep(RunData.Duration*1000);

            Console.WriteLine("{0}: Test finished.", DateTime.Now);
        }

        /// <summary>
        /// Step 1: Initialize the run. For example, initialize the performance counters. 
        /// </summary>
        public virtual void Initialize()
        {
            // set up the client
            for (int i = 0; i < RunData.Connections; i++)
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
            for (var i = 0; i < RunData.Senders; i++)
            {
                ThreadPool.QueueUserWorkItem(state => Sender(state), Tuple.Create(i, Guid.NewGuid().ToString()));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract IDisposable CreateReceiver(int sendIndex);

        protected abstract Task Send(int senderIndex, string source);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CancellationTokenSource.Cancel();

                    // Wait for all senders to stop
                    _countDown.Wait(TimeSpan.FromMilliseconds(1000 * RunData.Senders));

                    _receivers.ForEach(s => s.Dispose());

                    _receivers.Clear();
                }

                _disposed = true;
            }
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
