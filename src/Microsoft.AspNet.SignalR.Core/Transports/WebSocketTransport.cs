﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.AspNet.SignalR.Tracing;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    using WebSocketFunc = Func<IDictionary<string, object>, Task>;

    public class WebSocketTransport : ForeverTransport
    {
        private readonly HostContext _context;
        private IWebSocket _socket;
        private bool _isAlive = true;

        private readonly int? _maxIncomingMessageSize;

        private readonly Action<string> _message;
        private readonly Action _closed;
        private readonly Action<Exception> _error;
        private readonly IPerformanceCounterManager _counters;

        private static readonly byte[] _keepAlive = Encoding.UTF8.GetBytes("{}");

        public WebSocketTransport(HostContext context,
                                  IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<JsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>(),
                   resolver.Resolve<IMemoryPool>(),
                   resolver.Resolve<IConfigurationManager>().MaxIncomingWebSocketMessageSize)
        {
        }

        public WebSocketTransport(HostContext context,
                                  JsonSerializer serializer,
                                  ITransportHeartbeat heartbeat,
                                  IPerformanceCounterManager performanceCounterManager,
                                  ITraceManager traceManager,
                                  IMemoryPool pool,
                                  int? maxIncomingMessageSize)
            : base(context, serializer, heartbeat, performanceCounterManager, traceManager, pool)
        {
            _context = context;
            _maxIncomingMessageSize = maxIncomingMessageSize;

            _message = OnMessage;
            _closed = OnClosed;
            _error = OnSocketError;

            _counters = performanceCounterManager;
        }

        public override bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public override CancellationToken CancellationToken
        {
            get
            {
                return CancellationToken.None;
            }
        }

        public override Task KeepAlive()
        {
            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state =>
            {
                var webSocket = (IWebSocket)state;
                return webSocket.Send(new ArraySegment<byte>(_keepAlive));
            },
            _socket);
        }

        public override Task ProcessRequest(ITransportConnection connection)
        {
            if (IsAbortRequest)
            {
                return connection.Abort(ConnectionId);
            }
            else
            {
                return AcceptWebSocketRequest(socket =>
                {
                    return ProcessRequestCore(connection);
                });
            }
        }

        public override Task Send(object value)
        {
            var context = new WebSocketTransportContext(this, value);

            // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
            return EnqueueOperation(state => PerformSend(state), context);
        }

        public override Task Send(PersistentResponse response)
        {
            OnSendingResponse(response);

            return Send((object)response);
        }

        public override void IncrementConnectionsCount()
        {
            _counters.ConnectionsCurrentWebSockets.Increment();
        }

        public override void DecrementConnectionsCount()
        {
            _counters.ConnectionsCurrentWebSockets.Decrement();
        }

        private Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            var accept = _context.Environment.Get<Action<IDictionary<string, object>, WebSocketFunc>>(OwinConstants.WebSocketAccept);

            if (accept == null)
            {
                // Bad Request
                _context.Response.StatusCode = 400;
                return _context.Response.End(Resources.Error_NotWebSocketRequest);
            }

            Action<IWebSocket> prepareWebSocket = socket => {
                _socket = socket;
                socket.OnClose = _closed;
                socket.OnMessage = _message;
                socket.OnError = _error;
            };

            var handler = new OwinWebSocketHandler(callback, prepareWebSocket, _maxIncomingMessageSize);
            accept(null, handler.ProcessRequest);
            return TaskAsyncHelper.Empty;
        }

        private static async Task PerformSend(object state)
        {
            var context = (WebSocketTransportContext)state;
            var socket = context.Transport._socket;

            using (var writer = new BinaryMemoryPoolTextWriter(context.Transport.Pool))
            {
                try
                {
                    context.Transport.JsonSerializer.Serialize(context.State, writer);
                    writer.Flush();

                    await socket.Send(writer.Buffer).PreserveCulture();

                    context.Transport.TraceOutgoingMessage(writer.Buffer);
                }
                catch (Exception ex)
                {
                    // OnError will close the socket in the event of a JSON serialization or flush error.
                    // The client should then immediately reconnect instead of simply missing keep-alives.
                    context.Transport.OnError(ex);
                    throw;
                }
            }
        }

        private void OnMessage(string message)
        {
            if (Received != null)
            {
                Received(message).Catch(Trace);
            }
        }

        private void OnClosed()
        {
            Trace.TraceInformation("CloseSocket({0})", ConnectionId);

            // Require a request to /abort to stop tracking the connection. #2195
            _isAlive = false;
        }

        private void OnSocketError(Exception error)
        {
            Trace.TraceError("OnError({0}, {1})", ConnectionId, error);
        }

        private class WebSocketTransportContext
        {
            public readonly WebSocketTransport Transport;
            public readonly object State;

            public WebSocketTransportContext(WebSocketTransport transport, object state)
            {
                Transport = transport;
                State = state;
            }
        }
    }
}