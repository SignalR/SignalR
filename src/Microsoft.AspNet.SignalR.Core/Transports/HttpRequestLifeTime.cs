using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Transports
{
    internal class HttpRequestLifeTime
    {
        private readonly TaskCompletionSource<object> _lifeTimetcs = new TaskCompletionSource<object>();
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
                return _lifeTimetcs.Task;
            }
        }

        public void Complete()
        {
            Complete(error: null);
        }

        public void Complete(Exception error)
        {
            _trace.TraceEvent(TraceEventType.Verbose, 0, "DrainWrites(" + _connectionId + ")");

            var context = new DrainContext(_lifeTimetcs, error);

            // Drain the task queue for pending write operations so we don't end the request and then try to write
            // to a corrupted request object.
            _writeQueue.Drain().Catch().Finally(state =>
            {
                // Ensure delegate continues to use the C# Compiler static delegate caching optimization.
                ((DrainContext)state).Complete();
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

        private class DrainContext
        {
            private readonly TaskCompletionSource<object> _lifeTimetcs;
            private readonly Exception _error;

            public DrainContext(TaskCompletionSource<object> lifeTimetcs, Exception error)
            {
                _lifeTimetcs = lifeTimetcs;
                _error = error;
            }

            public void Complete()
            {
                if (_error != null)
                {
                    _lifeTimetcs.TrySetUnwrappedException(_error);
                }
                else
                {
                    _lifeTimetcs.TrySetResult(null);
                }
            }
        }
    }
}
