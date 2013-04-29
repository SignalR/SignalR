// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("MemoryHostEndToEndHub", typeof(IRun))]
    public class MemoryHostEndToEndHubRun : MemoryHostRun
    {
        HubConnection _connection;
        IHubProxy _proxy;
        bool _startedSampling;
        private string _categoryString;
        
#if !PERFRUN
        List<long> _latencySamples = new List<long>();
#endif

        [ImportingConstructor]
        public MemoryHostEndToEndHubRun(RunData runData)
            : base(runData)
        {
            _categoryString = string.Format("{0};{1}", ScenarioName, "Latency in Milliseconds");
        }

        public override string Endpoint
        {
            get { return "signalr"; }
        }

        protected override void ConfigureApp(IAppBuilder app)
        {
            // set up the server
            var config = new HubConfiguration
            {
                Resolver = Resolver
            };

            app.MapHubs(config);

            config.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
        }

        public override void Initialize()
        {
            // calling base to set up the server
            base.Initialize(); 

            // set up the client and start it
            _connection = new Client.Hubs.HubConnection("http://localhost/");
            _proxy = _connection.CreateHubProxy("EchoHub");

            _proxy.On<string>("echo", message =>
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

            _connection.Start(CreateTransport(Transport, Host)).Wait();      
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

        // client sends message
        protected override Task Send(int senderIndex, string source)
        {
            return _proxy.Invoke("Echo", DateTime.UtcNow);
        }

        private static IClientTransport CreateTransport(string transport, MemoryHost host)
        {
            IClientTransport result; 

            switch ( transport )
            {  
                case "LongPolling":
                    result = new LongPollingTransport(host);
                    break;
                case "WebSocket":
                    result = new WebSocketTransport(host);
                    break;
                case "ServerSentEvents":
                    result = new ServerSentEventsTransport(host);
                    break;
                default:
                case "Auto":
                    result = new AutoTransport(host);
                    break;               
            }

            return result;
        }
    }
}
