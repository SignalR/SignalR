// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
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
        private static List<ManualResetEvent> _callbacks;

#if !PERFRUN
        List<long> _latencySamples = new List<long>();
#endif

        [ImportingConstructor]
        public SimpleEchoHubRun(RunData runData)
            : base(runData)
        {
            Debug.Assert(Connections == Senders);

            _categoryString = String.Format("{0};{1}", ScenarioName, "Latency in Microseconds");
            _connections = new HubConnection[Connections];
            _proxies = new HubProxy[Connections];
        }

        public override void Initialize()
        {
            _callbacks = new List<ManualResetEvent>(Connections);
            for (int i = 0; i < Connections; i++)
            {
                _connections[i] = new HubConnection(RunData.Url);
                _callbacks.Add(new ManualResetEvent(true));
            }

            base.Initialize();
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            // set up the client and start it
            HubConnection connection = _connections[connectionIndex];
            IHubProxy proxy = connection.CreateHubProxy("SimpleEchoHub");

            proxy.On<string>("echo", startTicks =>
            {
                if (_sampling)
                {
                    var elapsedMicroseconds = (double)(DateTime.Now.Ticks - long.Parse(startTicks)) / TimeSpan.TicksPerMillisecond * 1000;
#if PERFRUN
                    Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)elapsedMicroseconds, _categoryString);
#else
                    _latencySamples.Add((long)elapsedMicroseconds);
#endif
                }
                _callbacks[connectionIndex].Set();
            });

            connection.Start(Host.TransportFactory()).Wait();

            _proxies[connectionIndex] = proxy;

            return Microsoft.AspNet.SignalR.Infrastructure.DisposableAction.Empty;
        }

        public override void Record()
        {
            _sampling = false;
            base.Record();

#if !PERFRUN
            base.RecordAggregates(_categoryString, _latencySamples.ToArray());
#endif
        }

        public override void Sample()
        {
            _sampling = true;

            base.Sample();
        }

        // client sends message. Note senderIndex could be smaller than the #connections
        protected override Task Send(int senderIndex, string source)
        {
            _callbacks[senderIndex].Reset();
            return _proxies[senderIndex].Invoke("Echo", DateTime.Now.Ticks).ContinueWith(_ => _callbacks[senderIndex].WaitOne());
        }
    }
}
