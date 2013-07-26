using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Owin;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ReconnectFacts : HostedTest
    {
        [Theory]
        [InlineData(TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public void ReconnectFiresAfterHostShutdown(TransportType transportType, MessageBusType messageBusType)
        {
            MyReconnect conn = null;
            var host = new ServerRestarter(app =>
            {
                var config = new ConnectionConfiguration
                {
                    Resolver = new DefaultDependencyResolver()
                };

                UseMessageBus(messageBusType, config.Resolver);

                app.MapConnection<MyReconnect>("/endpoint", config);

                conn = new MyReconnect();
                config.Resolver.Register(typeof(MyReconnect), () => conn);
            });

            using (host)
            {
                var connection = new Client.Connection("http://foo/endpoint");
                var transport = CreateTransport(transportType, host);
                connection.Start(transport).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(2));
                host.Restart();

                connection.Stop();

                Assert.Equal(1, conn.Reconnects);
            }
        }

        private class ServerRestarter : Client.Http.IHttpClient, IDisposable
        {
            private readonly Action<IAppBuilder> _startup;
            private MemoryHost _server;
            private int _counter = 0;
            private readonly object _lockobj = new object();

            public void Initialize(SignalR.Client.IConnection connection)
            {
                // Not implemented by MemoryHost
            }

            public ServerRestarter(Action<IAppBuilder> startup)
            {
                _startup = startup;
                Restart();
            }

            public Task<Client.Http.IResponse> Get(string url, Action<Client.Http.IRequest> prepareRequest, bool isLongRunning)
            {
                lock (_lockobj)
                {
                    Debug.WriteLine("Server {0}: GET {1}", _counter, url);
                    return ((Client.Http.IHttpClient)_server).Get(url, prepareRequest, isLongRunning);
                }
            }

            public Task<Client.Http.IResponse> Post(string url, Action<Client.Http.IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
            {
                lock (_lockobj)
                {
                    Debug.WriteLine("Server {0}: POST {1}", _counter, url);
                    return ((Client.Http.IHttpClient)_server).Post(url, prepareRequest, postData, isLongRunning);
                }
            }

            public void Restart()
            {
                lock (_lockobj)
                {
                    Dispose();
                    _server = new MemoryHost();
                    // Ensure that all servers have the same instance name so tokens can be successfully unprotected
                    _server.InstanceName = "ServerRestarter";
                    _server.Configure(_startup);
                    _counter++;
                }
            }

            public void Dispose()
            {
                lock (_lockobj)
                {
                    if (_server != null)
                    {
                        _server.Dispose();
                    }
                }
            }
        }
    }
}
