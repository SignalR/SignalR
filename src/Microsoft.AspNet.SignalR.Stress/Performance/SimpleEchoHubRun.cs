// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private bool _startedSampling;
        private string _categoryString;
        
#if !PERFRUN
        List<long> _latencySamples = new List<long>();
#endif

        [ImportingConstructor]
        public SimpleEchoHubRun(RunData runData)
            : base(runData)
        {
            _categoryString = String.Format("{0};{1}", ScenarioName, "Latency in Milliseconds");
            _connections = new HubConnection[Connections];
            _proxies = new HubProxy[Connections];
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

            proxy.On<string>("echo", message =>
            {
                if (_startedSampling)
                {
                    // now we can measure the latency
                    DateTime receiveTime = DateTime.UtcNow;
                    DateTime sentTime = Convert.ToDateTime(message);
                    TimeSpan latency = receiveTime - sentTime;
#if PERFRUN
                    Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)latency.TotalMilliseconds, _categoryString);
#else
                    _latencySamples.Add((long)latency.TotalMilliseconds);
#endif
                }
            });

            connection.Start(Host.Transport).Wait();

            _proxies[connectionIndex] = proxy;

            return Microsoft.AspNet.SignalR.Infrastructure.DisposableAction.Empty;
        }

        public override void Record()
        {
            base.Record();

#if !PERFRUN
            base.RecordAggregates(_categoryString, _latencySamples.ToArray());
#endif
        }

        public override void Sample()
        {
            _startedSampling = true;

            base.Sample();
        }

        // client sends message. Note senderIndex could be smaller than the #connections
        protected override Task Send(int senderIndex, string source)
        {
            return _proxies[senderIndex].Invoke("Echo", DateTime.UtcNow);
        }
    }
}
