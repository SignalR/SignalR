using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Hosting;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class ForeverTransport : TransportDisconnectBase, ITransport
    {
        private IJsonSerializer _jsonSerializer;
        
        public ForeverTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartBeat>())
        {

        }

        public ForeverTransport(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
            : base(context, heartBeat)
        {
            _jsonSerializer = jsonSerializer;
        }

        protected string LastMessageId
        {
            get;
            set;
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        public IEnumerable<string> Groups
        {
            get
            {
                string groupValue = Context.Request.QueryString["groups"];

                if (String.IsNullOrEmpty(groupValue))
                {
                    return Enumerable.Empty<string>();
                }

                return _jsonSerializer.Parse<string[]>(groupValue);
            }
        }

        protected virtual void OnSending(string payload)
        {
            HeartBeat.MarkConnection(this);
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

        public Func<Task> Reconnected { get; set; }

        public override Func<Task> Disconnected { get; set; }

        public Func<Exception, Task> Error { get; set; }

        public Task ProcessRequest(IReceivingConnection connection)
        {
            Connection = connection;

            if (Context.Request.Url.LocalPath.EndsWith("/send"))
            {
                return ProcessSendRequest();
            }
            else
            {
                if (IsConnectRequest)
                {
                    if (Connected != null)
                    {
                        return ProcessReceiveRequest(connection, () => Connected());
                    }

                    return ProcessReceiveRequest(connection);
                }

                if (Reconnected != null)
                {
                    return ProcessReceiveRequest(connection, () => Reconnected());
                }

                return ProcessReceiveRequest(connection);
            }
        }

        public virtual Task Send(PersistentResponse response)
        {
            HeartBeat.MarkConnection(this);
            var data = _jsonSerializer.Stringify(response);
            OnSending(data);
            return Context.Response.WriteAsync(data);
        }

        public virtual Task Send(object value)
        {
            var data = _jsonSerializer.Stringify(value);
            OnSending(data);
            return Context.Response.EndAsync(data);
        }

        protected virtual bool IsConnectRequest
        {
            get { return true; }
        }

        protected virtual Task InitializeResponse(IReceivingConnection connection)
        {
            return TaskAsyncHelper.Empty;
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"];

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

        private Task ProcessReceiveRequest(IReceivingConnection connection, Func<Task> postReceive = null)
        {
            HeartBeat.AddConnection(this);
            HeartBeat.MarkConnection(this);

            var state = new
            {
                Connection = connection,
                LastMessageId = LastMessageId,
                PostReceive = postReceive
            };

            return InitializeResponse(connection)
                    .Then((s) => ProcessMessages(s.Connection, s.LastMessageId, s.PostReceive), state)
                    .FastUnwrap();
        }

        private Task ProcessMessages(IReceivingConnection connection, string lastMessageId, Func<Task> postReceive = null)
        {
            var tcs = new TaskCompletionSource<object>();
            ProcessMessagesImpl(tcs, connection, lastMessageId, postReceive);
            return tcs.Task;
        }

        private void ProcessMessagesImpl(TaskCompletionSource<object> taskCompletetionSource, IReceivingConnection connection, string lastMessageId, Func<Task> postReceive = null)
        {
            if (!IsTimedOut && !IsDisconnected && Context.Response.IsClientConnected)
            {
                // ResponseTask will either subscribe and wait for a signal then return new messages,
                // or return immediately with messages that were pending
                var receiveAsyncTask = lastMessageId == null
                    ? connection.ReceiveAsync()
                    : connection.ReceiveAsync(lastMessageId);

                if (postReceive != null)
                {
                    postReceive().Catch();
                }

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