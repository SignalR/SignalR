using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Abstractions;

namespace SignalR.Transports
{
    public class ForeverTransport : ITransport, ITrackingDisconnect
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HostContext _context;
        private readonly ITransportHeartBeat _heartBeat;
        private IReceivingConnection _connection;
        private bool _disconnected;

        public ForeverTransport(HostContext context, IJsonSerializer jsonSerializer)
            : this(context, jsonSerializer, TransportHeartBeat.Instance)
        {

        }

        public ForeverTransport(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
        {
            _context = context;
            _jsonSerializer = jsonSerializer;
            _heartBeat = heartBeat;
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        protected HostContext Context
        {
            get { return _context; }
        }

        protected string LastMessageId
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
            get { return _context.Response.IsClientConnected; }
        }

        public virtual TimeSpan DisconnectThreshold
        {
            get { return TimeSpan.FromSeconds(5); }
        }

        public IEnumerable<string> Groups
        {
            get
            {
                string groupValue = _context.Request.QueryString["groups"];

                if (String.IsNullOrEmpty(groupValue))
                {
                    return Enumerable.Empty<string>();
                }

                return _jsonSerializer.Parse<string[]>(groupValue);
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

        public Func<string, Task> Received { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Disconnected { get; set; }

        public Func<Exception, Task> Error { get; set; }

        public Task ProcessRequest(IReceivingConnection connection)
        {
            _connection = connection;

            if (_context.Request.Url.LocalPath.EndsWith("/send"))
            {
                return ProcessSendRequest();
            }
            else
            {
                if (IsConnectRequest && Connected != null)
                {
                    return Connected().Then(() => ProcessReceiveRequest(connection)).FastUnwrap();
                }

                return ProcessReceiveRequest(connection);
            }
        }

        public virtual Task Send(PersistentResponse response)
        {
            _heartBeat.MarkConnection(this);
            var data = JsonSerializer.Stringify(response);
            OnSending(data);
            return _context.Response.WriteAsync(data);
        }

        public virtual Task Send(object value)
        {
            var data = JsonSerializer.Stringify(value);
            OnSending(data);
            return _context.Response.EndAsync(data);
        }

        public Task Disconnect()
        {
            if (!_disconnected && Disconnected != null)
            {
                return Disconnected().Then(() => SendDisconnectCommand()).FastUnwrap();
            }

            return SendDisconnectCommand();
        }

        private Task SendDisconnectCommand()
        {
            _disconnected = true;

            var command = new SignalCommand
            {
                Type = CommandType.Disconnect,
                ExpiresAfter = TimeSpan.FromMinutes(30)
            };

            return _connection.SendCommand(command);
        }

        protected virtual bool IsConnectRequest
        {
            get { return true; }
        }

        protected virtual Task InitializeResponse(IReceivingConnection connection)
        {
            // Don't timeout
            connection.ReceiveTimeout = TimeSpan.FromDays(1);

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessSendRequest()
        {
            string data = _context.Request.Form["data"];

            if (Receiving != null)
            {
                Receiving(data);
            }

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessReceiveRequest(IReceivingConnection connection)
        {
            _heartBeat.AddConnection(this);

            return InitializeResponse(connection)
                    .Then((c, id) => ProcessMessages(c, id), connection, LastMessageId)
                    .FastUnwrap();
        }

        private Task ProcessMessages(IReceivingConnection connection, string lastMessageId)
        {
            var tcs = new TaskCompletionSource<object>();
            ProcessMessagesImpl(tcs, connection, lastMessageId);
            return tcs.Task;
        }

        private void ProcessMessagesImpl(TaskCompletionSource<object> taskCompletetionSource, IReceivingConnection connection, string lastMessageId)
        {
            if (!_disconnected && _context.Response.IsClientConnected)
            {
                // ResponseTask will either subscribe and wait for a signal then return new messages,
                // or return immediately with messages that were pending
                var receiveAsyncTask = lastMessageId == null
                    ? connection.ReceiveAsync()
                    : connection.ReceiveAsync(lastMessageId);

                receiveAsyncTask.Then(response =>
                {
                    LastMessageId = response.MessageId;
                    // If the response has the Disconnect flag, just send the response and exit the loop,
                    // the server thinks connection is gone. Otherwse, send the response then re-enter the loop
                    Task sendTask = Send(response);
                    if (response.Disconnect || response.TimedOut)
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
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnRanToCompletion);

                // Stop execution here
                return;
            }

            taskCompletetionSource.SetResult(null);
            return;
        }
    }
}