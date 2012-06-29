using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.Transports
{
    public class ForeverTransport : TransportDisconnectBase, ITransport
    {
        private IJsonSerializer _jsonSerializer;
        private string _lastMessageId;
        
        public ForeverTransport(HostContext context, IDependencyResolver resolver)
            : this(context,
                   resolver.Resolve<IJsonSerializer>(),
                   resolver.Resolve<ITransportHeartBeat>())
        {

        }

        public ForeverTransport(HostContext context, IJsonSerializer jsonSerializer, ITransportHeartBeat heartBeat)
            : base(context, jsonSerializer, heartBeat)
        {
            _jsonSerializer = jsonSerializer;
        }

        protected string LastMessageId
        {
            get
            {
                if (_lastMessageId == null)
                {
                    _lastMessageId = Context.Request.QueryString["messageId"];
                }

                return _lastMessageId;
            }
            private set
            {
                _lastMessageId = value;
            }
        }

        protected IJsonSerializer JsonSerializer
        {
            get { return _jsonSerializer; }
        }

        protected virtual void OnSending(string payload)
        {
            HeartBeat.MarkConnection(this);

            if (Sending != null)
            {
                Sending(payload);
            }
        }

        protected static void OnReceiving(string data)
        {
            if (Receiving != null)
            {
                Receiving(data);
            }
        }

        // Static events intended for use when measuring performance
        public static event Action<string> Sending;
        public static event Action<string> Receiving;

        public Func<string, Task> Received { get; set; }

        public Func<Task> TransportConnected { get; set; }

        public Func<Task> Connected { get; set; }

        public Func<Task> Reconnected { get; set; }

        public Func<Exception, Task> Error { get; set; }

        protected Task ProcessRequestCore(ITransportConnection connection)
        {
            Connection = connection;

            if (Context.Request.Url.LocalPath.EndsWith("/send"))
            {
                return ProcessSendRequest();
            }
            else if (IsAbortRequest)
            {
                return Connection.Abort();
            }
            else
            {
                if (IsConnectRequest)
                {
                    if (Connected != null)
                    {
                        // Return a task that completes when the connected event task & the receive loop task are both finished
                        bool newConnection = HeartBeat.AddConnection(this);
                        return TaskAsyncHelper.Interleave(ProcessReceiveRequestWithoutTracking, () =>
                        {
                            if (newConnection)
                            {
                                return Connected();
                            }
                            return TaskAsyncHelper.Empty;
                        }
                        , connection, Completed);
                    }

                    return ProcessReceiveRequest(connection);
                }

                if (Reconnected != null)
                {
                    // Return a task that completes when the reconnected event task & the receive loop task are both finished
                    return TaskAsyncHelper.Interleave(ProcessReceiveRequest, Reconnected, connection, Completed);
                }

                return ProcessReceiveRequest(connection);
            }
        }

        public virtual Task ProcessRequest(ITransportConnection connection)
        {
            return ProcessRequestCore(connection);
        }

        public virtual Task Send(PersistentResponse response)
        {
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

        protected virtual Task InitializeResponse(ITransportConnection connection)
        {
            return TaskAsyncHelper.Empty;
        }

        private Task ProcessSendRequest()
        {
            string data = Context.Request.Form["data"];

            OnReceiving(data);

            if (Received != null)
            {
                return Received(data);
            }

            return TaskAsyncHelper.Empty;
        }

        private Task ProcessReceiveRequest(ITransportConnection connection, Action postReceive = null)
        {
            HeartBeat.AddConnection(this);
            return ProcessReceiveRequestWithoutTracking(connection, postReceive);
        }

        private Task ProcessReceiveRequestWithoutTracking(ITransportConnection connection, Action postReceive = null)
        {
            Action afterReceive = () =>
            {
                if (TransportConnected != null)
                {
                    TransportConnected().Catch();
                }

                if (postReceive != null)
                {
                    postReceive();
                }
            };

            return InitializeResponse(connection)
                    .Then((c, pr) => ProcessMessages(c, pr), connection, afterReceive);
        }

        private Task ProcessMessages(ITransportConnection connection, Action postReceive = null)
        {
            var tcs = new TaskCompletionSource<object>();
            // ProcessMessagesImpl(tcs, connection, postReceive);
            ProcessMessages(connection, postReceive, (exception) =>
            {
                if (exception != null)
                {
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
                CompleteRequest();
            });

            return tcs.Task;
        }

        private void ProcessMessages(ITransportConnection connection, Action postReceive, Action<Exception> endRequest)
        {
            IDisposable subscription = null;

            Action<Exception> end = (exception) =>
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                }

                // End the request after any pending sends
                endRequest(exception);
            };

            // End the request if the connection end token is triggered
            ConnectionEndToken.Register(() => end(null));

            subscription = connection.Receive(LastMessageId, (ex, response) =>
            {
                if (ex != null)
                {
                    end(ex);
                    return TaskAsyncHelper.Empty;
                }

                response.TimedOut = IsTimedOut;

                if (response.Disconnect || response.TimedOut || response.Aborted)
                {
                    if (response.Aborted)
                    {
                        // If this was a clean disconnect raise the event.
                        OnDisconnect();
                    }

                    end(null);
                    return TaskAsyncHelper.Empty;
                }
                else
                {
                    return Send(response);
                }
            });

            if (postReceive != null)
            {
                postReceive();
            }
        }

        private void ProcessMessagesImpl(TaskCompletionSource<object> taskCompletetionSource, ITransportConnection connection, Action postReceive = null)
        {
            if (!IsTimedOut && !IsDisconnected && !ConnectionEndToken.IsCancellationRequested)
            {
                // ResponseTask will either subscribe and wait for a signal then return new messages,
                // or return immediately with messages that were pending
                var receiveAsyncTask = LastMessageId == null
                    ? connection.ReceiveAsync(ConnectionEndToken)
                    : connection.ReceiveAsync(LastMessageId, ConnectionEndToken);

                if (postReceive != null)
                {
                    postReceive();
                }

                receiveAsyncTask.Then(response =>
                {
                    LastMessageId = response.MessageId;

                    response.TimedOut = IsTimedOut;

                    // If the response has the Disconnect flag, just send the response and exit the loop,
                    // the server thinks connection is gone. Otherwse, send the response then re-enter the loop
                    Task sendTask = Send(response);
                    if (response.Disconnect || response.TimedOut || response.Aborted)
                    {
                        if (response.Aborted)
                        {
                            // If this was a clean disconnect raise the event.
                            OnDisconnect();
                        }

                        // Signal the tcs when the task is done
                        return sendTask.Then(tcs => tcs.SetResult(null), taskCompletetionSource);
                    }

                    // Continue the receive loop
                    return sendTask.Then((conn) => ProcessMessagesImpl(taskCompletetionSource, conn), connection);
                })
                .ContinueWith(t =>
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
            CompleteRequest();
            return;
        }
    }
}