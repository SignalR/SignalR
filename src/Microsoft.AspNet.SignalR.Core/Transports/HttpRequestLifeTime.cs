using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    internal class HttpRequestLifeTime
    {
        private readonly TaskCompletionSource<object> _lifetimeTcs = new TaskCompletionSource<object>();
        private readonly TaskQueue _writeQueue;
        private readonly TraceSource _trace;
        private readonly string _connectionId;

        public HttpRequestLifeTime(TaskQueue writeQueue, TraceSource trace, string connectionId)
        {
            _trace = trace;
            _connectionId = connectionId;
            _writeQueue = writeQueue;
        }

        public Task Task
        {
            get
            {
                return _lifetimeTcs.Task;
            }
        }

        public void Complete()
        {
            Complete(error: null);
        }

        public void Complete(Exception error)
        {
            _trace.TraceEvent(TraceEventType.Verbose, 0, "DrainWrites(" + _connectionId + ")");

            var context = new LifetimeContext(_lifetimeTcs, error);

            // Drain the task queue for pending write operations so we don't end the request and then try to write
            // to a corrupted request object.
            _writeQueue.Drain().Catch().Finally(state =>
            {
                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                ((LifetimeContext)state).Complete();
            },
            context);

            if (error != null)
            {
                _trace.TraceEvent(TraceEventType.Error, 0, "CompleteRequest (" + _connectionId + ") failed: " + error.GetBaseException());
            }
            else
            {
                _trace.TraceInformation("CompleteRequest (" + _connectionId + ")");
            }
        }

        private class LifetimeContext
        {
            private readonly TaskCompletionSource<object> _lifetimeTcs;
            private readonly Exception _error;

            public LifetimeContext(TaskCompletionSource<object> lifeTimetcs, Exception error)
            {
                _lifetimeTcs = lifeTimetcs;
                _error = error;
            }

            public void Complete()
            {
                if (_error != null)
                {
                    _lifetimeTcs.TrySetUnwrappedException(_error);
                }
                else
                {
                    _lifetimeTcs.TrySetResult(null);
                }
            }
        }
    }
}
