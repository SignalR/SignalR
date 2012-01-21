using System;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Hosting.AspNet
{
    public abstract class HttpTaskAsyncHandler : IHttpAsyncHandler
    {
        public virtual bool IsReusable
        {
            get { return false; }
        }

        public virtual void ProcessRequest(HttpContext context)
        {
            throw new NotSupportedException();
        }

        public abstract Task ProcessRequestAsync(HttpContextBase context);

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            Task task = ProcessRequestAsync(new HttpContextWrapper(context));
            var retVal = new TaskWrapperAsyncResult(task, extraData);

            if (task == null)
            {
                // No task, so just let ASP.NET deal with it
                return null;
            }

            if (cb != null)
            {
                // The callback needs the same argument that the Begin method returns, which is our special wrapper, not the original Task.
                task.ContinueWith(_ => cb(retVal));
            }

            return retVal;
        }

        void IHttpAsyncHandler.EndProcessRequest(IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            // The End* method doesn't actually perform any actual work, but we do need to maintain two invariants:
            // 1. Make sure the underlying Task actually *is* complete.
            // 2. If the Task encountered an exception, observe it here.
            // (The Wait method handles both of those.)
            var castResult = (TaskWrapperAsyncResult)result;
            castResult.Task.Wait();
        }
    }
}