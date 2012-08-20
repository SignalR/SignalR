using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Server.Utils;

namespace SignalR.Server
{
    class ServerResponseStream : StreamNotImpl
    {
        readonly ServerResponse _serverResponse;

        bool _startCalled;
        object _startLock = new object();
        Task<Stream> _startTask;

        volatile Stream _stream;

        readonly object _pendingLock = new object();
        volatile Queue<Action<Stream>> _pendingQueue = new Queue<Action<Stream>>();

        public ServerResponseStream(ServerResponse serverResponse)
        {
            _serverResponse = serverResponse;
        }

        struct AsyncVoid { }

        Task<Stream> StartAsync()
        {
            return LazyInitializer.EnsureInitialized(ref _startTask, ref _startCalled, ref _startLock, DoStartAsync);
        }

        Task<Stream> DoStartAsync()
        {
            return _serverResponse.StartAsync()
                .Then(
                    stream =>
                    {
                        lock (_pendingLock)
                        {
                            try
                            {
                                while (_pendingQueue.Count != 0)
                                {
                                    var pending = _pendingQueue.Dequeue();
                                    pending.Invoke(stream);
                                }
                            }
                            catch
                            {
                                // ignoring defered-sync errors
                            }
                            finally
                            {
                                _pendingQueue = null;
                                _stream = stream;
                            }
                        }
                        return stream;
                    },
                    default(CancellationToken),
                    runSynchronously: true);
        }

        void Pending(Action<Stream> pending)
        {
            lock (_pendingLock)
            {
                if (_pendingQueue != null)
                {
                    _pendingQueue.Enqueue(pending);
                    return;
                }
            }
            pending(_stream);
        }


        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            if (!_serverResponse.IsClientConnected)
            {
                return;
            }

            if (_stream != null)
            {
                try
                {
                    _stream.Flush();
                }
                catch
                {
                    // ignoring sync errors
                }
            }
            else
            {
                Pending(stream => stream.Flush());
                StartAsync();
            }
        }

        public override void Close()
        {
            if (_stream != null)
            {
                _stream.Close();
            }
            else
            {
                Pending(stream => stream.Close());
                StartAsync();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_serverResponse.IsClientConnected)
            {
                return;
            }

            if (_stream != null)
            {
                _stream.Write(buffer, offset, count);
            }
            else
            {
                var copy = new byte[count];
                Array.Copy(buffer, offset, copy, 0, count);
                Pending(stream => stream.Write(copy, 0, count));
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_stream != null)
            {
                return _stream.BeginWrite(buffer, offset, count, callback, state);
            }

            var tcs = new TaskCompletionSource<AsyncVoid>(state);
            var work = StartAsync()
                .Then(
                    stream => Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null),
                    runSynchronously: true)
                .CopyResultToCompletionSource(tcs, default(AsyncVoid));
            if (callback != null)
            {
                work.Finally(() => callback(tcs.Task), runSynchronously: true);
            }
            return tcs.Task;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task<AsyncVoid>;
            if (task == null)
            {
                _stream.EndWrite(asyncResult);
            }
            else
            {
                task.Wait();
            }
        }
    }
}


