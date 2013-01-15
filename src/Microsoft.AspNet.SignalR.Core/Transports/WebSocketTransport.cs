// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Transports
{
    public class WebSocketTransport : ForeverTransport
    {
        private readonly HostContext _context;
        private IWebSocket _socket;
        private bool _isAlive = true;

        public WebSocketTransport(HostContext context,
                                  IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartbeat>(),
                   resolver.Resolve<IPerformanceCounterManager>(),
                   resolver.Resolve<ITraceManager>())
        {
        }

        public WebSocketTransport(HostContext context,
                                  IJsonSerializer serializer,
                                  ITransportHeartbeat heartbeat,
                                  IPerformanceCounterManager performanceCounterWriter,
                                  ITraceManager traceManager)
            : base(context, serializer, heartbeat, performanceCounterWriter, traceManager)
        {
            _context = context;
        }

        public override bool IsAlive
        {
            get
            {
                return _isAlive;
            }
        }

        public override Task KeepAlive()
        {
            return Send(new object());
        }

        public override Task ProcessRequest(ITransportConnection connection)
        {
            var webSocketRequest = _context.Request as IWebSocketRequest;

            // Throw if the server implementation doesn't support websockets
            if (webSocketRequest == null)
            {
                throw new InvalidOperationException(Resources.Error_WebSocketsNotSupported);
            }

            return webSocketRequest.AcceptWebSocketRequest(socket =>
            {
                _socket = socket;

                socket.OnClose = clean =>
                {
                    // If we performed a clean disconnect then we go through the normal disconnect routine.  However,
                    // If we performed an unclean disconnect we want to mark the connection as "not alive" and let the
                    // HeartBeat clean it up.  This is to maintain consistency across the transports.
                    if (clean)
                    {
                        OnDisconnect();
                    }

                    _isAlive = false;
                };

                socket.OnMessage = message =>
                {
                    if (Received != null)
                    {
                        Received(message).Catch();
                    }
                };

                return ProcessRequestCore(connection);
            });
        }

        public override Task Send(object value)
        {
            var data = JsonSerializer.Stringify(value);

            OnSending(data);

            return _socket.Send(data).Catch(IncrementErrorCounters);
        }

        public override Task Send(PersistentResponse response)
        {
            return Send((object)response);
        }
    }
}
