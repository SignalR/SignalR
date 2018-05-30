// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Transports;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Azure.SignalR.Protocol;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SelfHost.Samples
{
    public class Program
    {
        static void Main(string[] args)
        {
            var serviceConnection = new ServiceConnection();
            var configuration = new HubConfiguration();
            configuration.Resolver.Register(typeof(ServiceConnection), () => serviceConnection);
            configuration.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
            var bus = new ServiceMessageBus(configuration.Resolver, serviceConnection);
            configuration.Resolver.Register(typeof(IMessageBus), () => bus);
            var azureTransportManager = new AzureTransportManager();
            configuration.Resolver.Register(typeof(ITransportManager), () => azureTransportManager);

            serviceConnection.StartAsync(configuration).GetAwaiter().GetResult();

            //using (WebApp.Start<Startup>("http://localhost:8080"))
            //{
            //    Console.WriteLine("Server running at http://localhost:8080/");
            //    Console.ReadLine();
            //}
        }

        public class ServiceConnection
        {
            private readonly ServiceProtocol _serviceProtocol = new ServiceProtocol();
            private readonly ConcurrentDictionary<string, AzureTransport> _connections = new ConcurrentDictionary<string, AzureTransport>();
            private JsonSerializer _serializer;
            private HttpConnection _connection;
            private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

            public async Task StartAsync(HubConfiguration configuration)
            {
                _connection = new HttpConnection(new Uri("http://192.168.170.108:5000/v2/server"));

                await _connection.StartAsync();

                // Hub configuration
                // config.Resolver.InitializeHost();
                _serializer = configuration.Resolver.Resolve<JsonSerializer>();


                while (true)
                {
                    var result = await _connection.Transport.Input.ReadAsync();
                    var buffer = result.Buffer;

                    while (_serviceProtocol.TryParseMessage(ref buffer, out var message))
                    {
                        switch (message)
                        {
                            case OpenConnectionMessage m:

                                var dispatcher = new HubDispatcher(configuration);
                                dispatcher.Initialize(configuration.Resolver);

                                var context = new OwinContext();
                                var response = context.Response;
                                var request = context.Request;
                                response.Body = Stream.Null;

                                request.Path = new PathString("/");
                                request.QueryString = new QueryString($"connectionToken={m.ConnectionId}&connectionData=[%7B%22Name%22:%22chat%22%7D]");

                                var hostContext = new HostContext(context.Environment);
                                context.Environment["azure.serviceConnection"] = this;

                                if (dispatcher.Authorize(hostContext.Request))
                                {
                                    _ = dispatcher.ProcessRequest(hostContext);

                                    // TODO: Check for errors written to the response

                                    // Assume OnConnected was raised, send the initialize response
                                    await SendAsync(m.ConnectionId, new { S = 1, M = new object[0] });

                                    _connections[m.ConnectionId] = (AzureTransport)context.Environment["signalr.transport"];
                                }
                                else
                                {
                                    // What do we do here?
                                }

                                break;
                            case ConnectionDataMessage m:
                                {
                                    if (_connections.TryGetValue(m.ConnectionId, out var transport))
                                    {
                                        MemoryMarshal.TryGetArray(m.Payload, out var segment);

                                        transport.OnReceived(Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count));
                                    }
                                }
                                break;
                            case CloseConnectionMessage m:
                                {
                                    if (_connections.TryRemove(m.ConnectionId, out var transport))
                                    {
                                        transport.OnDisconnected();
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    _connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }

            public async Task SendAsync(string connectionId, object value)
            {
                // TODO: Use MemoryPoolTextWriter
                var ms = new MemoryStream();
                using (var sw = new StreamWriter(ms))
                {
                    _serializer.Serialize(new JsonTextWriter(sw), value);
                    sw.Flush();
                    ms.TryGetBuffer(out var buffer);
                    await SendAsync(new ConnectionDataMessage(connectionId, buffer.AsMemory()));
                }
            }

            public async Task SendAsync(ServiceMessage message)
            {
                await _semaphoreSlim.WaitAsync();
                try
                {
                    _serviceProtocol.WriteMessage(message, _connection.Transport.Output);
                    await _connection.Transport.Output.FlushAsync();
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        private class AzureTransport : ITransport
        {
            private readonly TaskCompletionSource<object> _lifetimeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly ServiceConnection _serviceConnection;

            public AzureTransport(HostContext context)
            {
                _serviceConnection = (ServiceConnection)context.Environment["azure.serviceConnection"];
                context.Environment["signalr.transport"] = this;
            }

            public Func<string, Task> Received { get; set; }
            public Func<Task> Connected { get; set; }
            public Func<Task> Reconnected { get; set; }
            public Func<bool, Task> Disconnected { get; set; }
            public string ConnectionId { get; set; }

            public Task<string> GetGroupsToken()
            {
                return Task.FromResult<string>(null);
            }

            public async Task ProcessRequest(ITransportConnection connection)
            {
                var connected = Connected;
                if (connected != null)
                {
                    await connected();
                }

                await _lifetimeTcs.Task;

                var disconnected = Disconnected;
                if (disconnected != null)
                {
                    await disconnected(true);
                }
            }

            public Task Send(object value)
            {
                return _serviceConnection.SendAsync(ConnectionId, value);
            }

            public void OnReceived(string value)
            {
                var received = Received;
                if (received != null)
                {
                    _ = received(value);
                }
            }

            public void OnDisconnected() => _lifetimeTcs.TrySetResult(null);
        }

        public class EmptyProtectedData : IProtectedData
        {
            public string Protect(string data, string purpose)
            {
                return data;
            }

            public string Unprotect(string protectedValue, string purpose)
            {
                return protectedValue;
            }
        }

        public class ServiceMessageBus : MessageBus
        {
            private const string HubPrefix = "h-";
            private const string HubGroupPrefix = "hg-";
            private const string HubConnectionIdPrefix = "hc-";
            private const string HubUserPrefix = "hu-";

            private const string PersistentConnectionPrefix = "pc-";
            private const string PersistentConnectionGroupPrefix = "pcg-";

            private const string ConnectionIdPrefix = "c-";

            private readonly ServiceConnection _serviceConnection;

            public ServiceMessageBus(IDependencyResolver resolver, ServiceConnection serviceConnection) : base(resolver)
            {
                _serviceConnection = serviceConnection;
            }

            public override async Task Publish(Message message)
            {
                Dictionary<string, ReadOnlyMemory<byte>> GetPayload(ReadOnlyMemory<byte> data) =>
                    new Dictionary<string, ReadOnlyMemory<byte>>
                    {
                        { "json", data }
                    };

                var response = new PersistentResponse(m => false, tw => tw.Write("Cursor"))
                {
                    Messages = new List<ArraySegment<Message>> {
                        new ArraySegment<Message>(new[] { message })
                    },
                    TotalCount = 1
                };

                // TODO: Use MemoryPoolTextWriter
                ArraySegment<byte> segment;
                var ms = new MemoryStream();
                using (var sw = new StreamWriter(ms))
                {
                    ((IJsonWritable)response).WriteJson(sw);
                    sw.Flush();
                    ms.TryGetBuffer(out segment);
                }

                // Which hub?
                if (message.Key.StartsWith(HubPrefix))
                {
                    await _serviceConnection.SendAsync(new BroadcastDataMessage(excludedList: null, payloads: GetPayload(segment)));
                }
                // Which group?
                else if (message.Key.StartsWith(HubGroupPrefix))
                {
                    await _serviceConnection.SendAsync(new GroupBroadcastDataMessage(message.Key.Substring(HubGroupPrefix.Length), excludedList: null, payloads: GetPayload(segment)));
                }
                else if (message.Key.StartsWith(ConnectionIdPrefix))
                {
                    await _serviceConnection.SendAsync(new ConnectionDataMessage(message.Key.Substring(ConnectionIdPrefix.Length), segment));
                }

                if (message.IsCommand)
                {
                    // TODO: Handle commands

                    await base.Publish(message);
                }
            }
        }

        public class AzureTransportManager : ITransportManager
        {
            public ITransport GetTransport(HostContext hostContext)
            {
                return new AzureTransport(hostContext);
            }

            public bool SupportsTransport(string transportName)
            {
                // This is only called for websockets, and should never be called in this flow
                return false;
            }
        }
    }
}
