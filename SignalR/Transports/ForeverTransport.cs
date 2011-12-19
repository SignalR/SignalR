using System;
using System.Threading.Tasks;
using System.Web;
using SignalR.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace SignalR.Transports
{
    public class ForeverTransport : ITransport, ITrackingDisconnect
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HttpContextBase _context;
        private readonly ITransportHeartBeat _heartBeat;
        private IReceivingConnection _connection;
        private bool _disconnected;

        public ForeverTransport(HttpContextBase context, IJsonSerializer jsonSerializer)
            : this(context, jsonSerializer, TransportHeartBeat.Instance)
        {

        }

        public ForeverTransport(HttpContextBase context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
            _heartBeat = heartBeat;
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        protected HttpContextBase Context
        {
            get { return _context; }
        }

        protected long? LastMessageId
        {
            get;
            set;
        }

        public string ConnectionId
        {
            get
            {
                return _context.Request.QueryString["connectionId"];
            }
        }

        public bool IsAlive
        {
            get { return Context.Response.IsClientConnected; }
        }

        public virtual TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromSeconds(5); }
        }

        public IEnumerable<string> Groups
        {
            get
            {
                string groupValue = _context.Request.QueryStringOrForm("groups");

                if (String.IsNullOrEmpty(groupValue))
                {
                    return Enumerable.Empty<string>();
                }

                return groupValue.Split(',');
            }
        }

        protected virtual void OnSending(string payload)
        {
            _heartBeat.MarkConnection(this);
            if (Sending != null)
            {
                Sending(payload);
            }
        }

        // Static events intended for use when measuring performance
        public static event Action<string> Sending;
        public static event Action<string> Receiving;

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public event Action<Exception> Error;

        public Func<Task> ProcessRequest(IReceivingConnection connection)
        {
            _connection = connection;

            if (Context.Request.Path.EndsWith("/send"))
            {
                ProcessSendRequest();
            }
            else
            {
                if (IsConnectRequest && Connected != null)
                {
                    Connected();
                }

                _heartBeat.AddConnection(this);

                return ProcessReceiveRequest(connection);
            }

            return null;
        }

        public virtual Task Send(PersistentResponse response)
        {
            _heartBeat.MarkConnection(this);
            return Send((object)response);
        }

        public virtual Task Send(object value)
        {
            var data = JsonSerializer.Stringify(value);
            OnSending(data);
            return Context.Response.WriteAsync(data);
        }

        public void Disconnect()
        {
            if (!_disconnected && Disconnected != null)
            {
                Disconnected();
            }
            _disconnected = true;
            _connection.SendCommand(
                new SignalCommand
                {
                    Type = CommandType.Disconnect,
                    ExpiresAfter = TimeSpan.FromMinutes(30)
                });
        }

        protected virtual bool IsConnectRequest
        {
            get { return true; }
        }

        protected virtual Task InitializeResponse(IReceivingConnection connection)
        {
            // Don't timeout
            connection.ReceiveTimeout = TimeSpan.FromDays(1);

            // This forces the IIS compression module to leave this response alone.
            // If we don't do this, it will buffer the response to suit its own compression
            // logic, resulting in partial messages being sent to the client.
            Context.Request.Headers.Remove("Accept-Encoding");

            Context.Response.Buffer = false;
            Context.Response.BufferOutput = false;
            Context.Response.CacheControl = "no-cache";
            Context.Response.AddHeader("Connection", "keep-alive");

            return TaskAsyncHelper.Empty;
        }

        private void ProcessSendRequest()
        {
            string data = _context.Request.Form["data"];
            if (Receiving != null)
            {
                Receiving(data);
            }
            if (Received != null)
            {
                Received(data);
            }
        }

        private Func<Task> ProcessReceiveRequest(IReceivingConnection connection)
        {
            return () => InitializeResponse(connection)
                    .Then((c, id) => ProcessMessages(c, id), connection, LastMessageId)
                    .FastUnwrap();
        }

        private Task ProcessMessages(IReceivingConnection connection, long? lastMessageId)
        {
            var tcs = new TaskCompletionSource<object>();
            ProcessMessagesImpl(tcs, connection, lastMessageId);
            return tcs.Task;
        }

        private void ProcessMessagesImpl(TaskCompletionSource<object> taskCompletetionSource, IReceivingConnection connection, long? lastMessageId)
        {
            if (!_disconnected && Context.Response.IsClientConnected)
            {
                // ResponseTask will either subscribe and wait for a signal then return new messages,
                // or return immediately with messages that were pending
                var receiveAsyncTask = lastMessageId == null
                    ? connection.ReceiveAsync()
                    : connection.ReceiveAsync(lastMessageId.Value);

                receiveAsyncTask.Then(response =>
                {
                    LastMessageId = response.MessageId;
                    // If the response has the Disconnect flag, just send the response and exit the loop,
                    // the server thinks connection is gone. Otherwse, send the response then re-enter the loop
                    Task sendTask = Send(response);
                    if (response.Disconnect)
                    {
                        // Signal the tcs when the task is done
                        return sendTask.Then(tcs => tcs.SetResult(null), taskCompletetionSource);
                    }

                    // Continue the receive loop
                    return sendTask.Then((conn, id) => ProcessMessagesImpl(taskCompletetionSource, conn, id), connection, LastMessageId);
                })
                .FastUnwrap().ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        taskCompletetionSource.SetCanceled();
                    }
                    else if (t.IsFaulted)
                    {
                        taskCompletetionSource.SetException(t.Exception);
                    }
                },
                TaskContinuationOptions.ExecuteSynchronously & TaskContinuationOptions.NotOnRanToCompletion);

                // Stop execution here
                return;
            }

            // Client is no longer connected
            Disconnect();

            // We're done
            taskCompletetionSource.SetResult(null);
            return;
        }
    }
}