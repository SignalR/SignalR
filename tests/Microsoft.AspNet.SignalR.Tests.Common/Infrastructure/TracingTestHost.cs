using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public abstract class TracingTestHost : ITestHost
    {
        private readonly TextWriterTraceListener _listener;
        private ITraceManager _traceManager;

        private static string[] _traceSources = new[] {
            "SignalR.Transports.WebSocketTransport",
            "SignalR.Transports.ServerSentEventsTransport",
            "SignalR.Transports.ForeverFrameTransport",
            "SignalR.Transports.LongPollingTransport",
            "SignalR.Transports.TransportHeartBeat"
        };

        protected TracingTestHost(string logPath)
        {
            _listener = new TextWriterTraceListener(logPath + ".transports.log");
            Disposables = new List<IDisposable>();
            ExtraData = new Dictionary<string, string>();
        }

        public abstract string Url { get; }

        public IClientTransport Transport
        {
            get;
            set;
        }

        public TextWriter ClientTraceOutput
        {
            get;
            set;
        }

        public IList<IDisposable> Disposables
        {
            get;
            private set;
        }

        public IDictionary<string, string> ExtraData
        {
            get;
            private set;
        }

        public Func<IClientTransport> TransportFactory { get; set; }

        public IDependencyResolver Resolver
        {
            get;
            set;
        }

        public virtual void Initialize(int? keepAlive = -1,
                                       int? connectionTimeout = 110,
                                       int? disconnectTimeout = 30,
                                       bool enableAutoRejoiningGroups = false,
                                       MessageBusType type = MessageBusType.Default,
                                       int streams = 1)
        {
            Resolver = Resolver ?? new DefaultDependencyResolver();

            _traceManager = Resolver.Resolve<ITraceManager>();
            _traceManager.Switch.Level = SourceLevels.Verbose;

            foreach (var sourceName in _traceSources)
            {
                TraceSource source = _traceManager[sourceName];
                source.Listeners.Add(_listener);
            }

            var configuration = Resolver.Resolve<IConfigurationManager>();

            if (connectionTimeout != null)
            {
                configuration.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout.Value);
            }

            if (disconnectTimeout != null)
            {
                configuration.DisconnectTimeout = TimeSpan.FromSeconds(disconnectTimeout.Value);
            }

            if (!keepAlive.HasValue)
            {
                configuration.KeepAlive = null;
            }
            // Set only if the keep-alive was changed from the default value.
            else if (keepAlive.Value != -1)
            {
                configuration.KeepAlive = TimeSpan.FromSeconds(keepAlive.Value);
            }
            
            switch (type)
            {
                case MessageBusType.Default:
                    break;
                case MessageBusType.Fake:
                    var bus = new FakeScaleoutBus(Resolver, streams);
                    Resolver.Register(typeof(IMessageBus), () => bus);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public virtual Task Get(string uri, bool disableWrites)
        {
            throw new NotImplementedException();
        }

        public virtual Task Post(string uri, IDictionary<string, string> data)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            _listener.Flush();

            foreach (var sourceName in _traceSources)
            {
                _traceManager[sourceName].Listeners.Remove(_listener);
            }

            _listener.Dispose();

            foreach (var d in Disposables)
            {
                d.Dispose();
            }
        }
    }
}
