// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Infrastructure;
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
        //[InlineData(TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task ReconnectFiresAfterHostShutdown(TransportType transportType, MessageBusType messageBusType)
        {
            var serverStarts = 0;
            var serverReconnects = 0;

            var host = new ServerRestarter(app =>
            {
                var config = new ConnectionConfiguration
                {
                    Resolver = new DefaultDependencyResolver()
                };

                UseMessageBus(messageBusType, config.Resolver);

                app.MapSignalR<MyReconnect>("/endpoint", config);

                serverStarts++;
                config.Resolver.Register(typeof(MyReconnect), () => new MyReconnect(() => serverReconnects++));
            });

            using (host)
            {
                using (var connection = CreateConnection("http://foo/endpoint"))
                {
                    var transport = CreateTransport(transportType, host);
                    var pollEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var reconnectedEvent = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    host.OnPoll = () =>
                    {
                        pollEvent.TrySetResult(null);
                    };

                    connection.Reconnected += () =>
                    {
                        reconnectedEvent.TrySetResult(null);
                    };

                    await connection.Start(transport);

                    // Wait for the /poll before restarting the server
                    await pollEvent.Task.OrTimeout(TimeSpan.FromSeconds(15));

                    host.Restart();

                    await reconnectedEvent.Task.OrTimeout(TimeSpan.FromSeconds(15));

                    Assert.Equal(2, serverStarts);
                    Assert.Equal(1, serverReconnects);
                }
            }
        }

        [Fact]
        public async Task GroupsWorkAfterServerRestart()
        {
            var host = new ServerRestarter(app =>
            {
                var config = new HubConfiguration
                {
                    Resolver = new DefaultDependencyResolver()
                };

                app.MapSignalR(config);
            });

            using (host)
            {
                using (var connection = CreateHubConnection("http://foo/"))
                {
                    var reconnectedEvent = new TaskCompletionSource<object>();
                    connection.Reconnected += () => reconnectedEvent.TrySetResult(null);

                    var hubProxy = connection.CreateHubProxy("groupChat");
                    var sendEvent = new TaskCompletionSource<object>();
                    string sendMessage = null;
                    hubProxy.On<string>("send", message =>
                    {
                        sendMessage = message;
                        sendEvent.TrySetResult(null);
                    });

                    var groupName = "group$&+,/:;=?@[]1";
                    var groupMessage = "hello";

                    // MemoryHost doesn't support WebSockets, and it is difficult to ensure that
                    // the reconnected event is reliably fired with the LongPollingTransport.
                    await connection.Start(new ServerSentEventsTransport(host));

                    await hubProxy.Invoke("Join", groupName);

                    host.Restart();

                    await reconnectedEvent.Task.OrTimeout(TimeSpan.FromSeconds(15));

                    await hubProxy.Invoke("Send", groupName, groupMessage);

                    await sendEvent.Task.OrTimeout(TimeSpan.FromSeconds(15));
                    Assert.Equal(groupMessage, sendMessage);
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
