﻿using System;
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
        public async Task ReconnectFiresAfterHostShutdown(TransportType transportType, MessageBusType messageBusType)
        {
            var serverRestarts = 0;
            var serverReconnects = 0;

            var host = new ServerRestarter(app =>
            {
                var config = new ConnectionConfiguration
                {
                    Resolver = new DefaultDependencyResolver()
                };

                UseMessageBus(messageBusType, config.Resolver);

                app.MapSignalR<MyReconnect>("/endpoint", config);

                serverRestarts++;
                serverReconnects = 0;
                config.Resolver.Register(typeof(MyReconnect), () => new MyReconnect(() => serverReconnects++));
            });

            using (host)
            {
                using (var connection = CreateConnection("http://foo/endpoint"))
                {
                    var transport = CreateTransport(transportType, host);
                    var pollEvent = new AsyncManualResetEvent();
                    var reconnectedEvent = new AsyncManualResetEvent();

                    host.OnPoll = () =>
                    {
                        pollEvent.Set();
                    };

                    connection.Reconnected += () =>
                    {
                        reconnectedEvent.Set();
                    };

                    await connection.Start(transport);

                    // Wait for the /poll before restarting the server
                    Assert.True(await pollEvent.WaitAsync(TimeSpan.FromSeconds(15)), "Timed out waiting for poll request");

                    host.Restart();

                    Assert.True(await reconnectedEvent.WaitAsync(TimeSpan.FromSeconds(15)), "Timed out waiting for client side reconnect");

                    Assert.Equal(2, serverRestarts);
                    Assert.Equal(1, serverReconnects);
                }
            }
        }

        private class ServerRestarter : Client.Http.IHttpClient, IDisposable
        {
            private readonly Action<IAppBuilder> _startup;
            private MemoryHost _server;
            private readonly object _lockobj = new object();

            public Action OnPoll = () => { };

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
                    return ((Client.Http.IHttpClient)_server).Get(url, prepareRequest, isLongRunning);
                }
            }

            public Task<Client.Http.IResponse> Post(string url, Action<Client.Http.IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
            {
                lock (_lockobj)
                {
                    Task<Client.Http.IResponse> task = ((Client.Http.IHttpClient)_server).Post(url, prepareRequest, postData, isLongRunning);

                    if (url.Contains("poll"))
                    {
                        OnPoll();
                    }

                    return task;
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
