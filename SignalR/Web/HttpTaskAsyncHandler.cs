using System;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Web {
    public abstract class HttpTaskAsyncHandler : IHttpAsyncHandler {
        public virtual bool IsReusable {
            get { return false; }
        }

        public virtual void ProcessRequest(HttpContext context) {
            throw new NotSupportedException();
        }

        public abstract Task ProcessRequestAsync(HttpContext context);

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
        	var task = ProcessRequestAsync(context);
        	if (cb != null) {
        		task.ContinueWith(_ => cb(task));
        	}
        	return task;
        }

        void IHttpAsyncHandler.EndProcessRequest(IAsyncResult result) {
        	// The End* method doesn't actually perform any actual work, but we do need to maintain two invariants:
        	// 1. Make sure the underlying Task actually *is* complete.
        	// 2. If the Task encountered an exception, observe it here.
        	// (The Wait method handles both of those.)
        	using (var task = (Task)result)
        		task.Wait();
        }
    }
}