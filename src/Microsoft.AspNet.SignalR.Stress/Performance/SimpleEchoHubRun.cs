// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("SimpleEchoHub", typeof(IRun))]
    public class SimpleEchoHubRun : HostedRun
    {
        private HubConnection[] _connections;
        private IHubProxy[] _proxies;
        private bool _sampling;
        private string _categoryString;
        private TaskCompletionSource<bool>[] _callbacks;
        private ConcurrentBag<long> _latencySamples = new ConcurrentBag<long>();

        [ImportingConstructor]
        public SimpleEchoHubRun(RunData runData)
            : base(runData)
        {
            Debug.Assert(Connections == Senders);

            _categoryString = String.Format("{0};{1}", ScenarioName, "Latency;Milliseconds");
            _connections = new HubConnection[Connections];
            _proxies = new HubProxy[Connections];
            _callbacks = new TaskCompletionSource<bool>[Connections];
        }

        public override void Initialize()
        {
            for (int i = 0; i < Connections; i++)
            {
                _connections[i] = new HubConnection(RunData.Url);
            }

            base.Initialize();
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            // set up the client and start it
            HubConnection connection = _connections[connectionIndex];
            IHubProxy proxy = connection.CreateHubProxy("SimpleEchoHub");

            proxy.On<long>("echo", startTicks =>
            {
                if (_sampling)
                {
                    var elapsedMilliseconds = Math.Round((double)(DateTime.UtcNow.Ticks - startTicks) / TimeSpan.TicksPerMillisecond);
#if PERFRUN
                    Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)elapsedMilliseconds, _categoryString);
#endif
                    _latencySamples.Add((long)elapsedMilliseconds);
                }
                _callbacks[connectionIndex].SetResult(true);
            });

            connection.Start(Host.TransportFactory()).Wait();

            _proxies[connectionIndex] = proxy;

            return Microsoft.AspNet.SignalR.Infrastructure.DisposableAction.Empty;
        }

        public override void Record()
        {
            _sampling = false;
            base.Record();
            base.RecordAggregates(_categoryString, _latencySamples.ToArray());
        }

        public override void Sample()
        {
            _sampling = true;

            base.Sample();
        }

        protected override async Task Send(int senderIndex, string source)
        {
            _callbacks[senderIndex] = new TaskCompletionSource<bool>();
            await _proxies[senderIndex].Invoke("Echo", DateTime.UtcNow.Ticks);
            await _callbacks[senderIndex].Task;
        }
    }
}
