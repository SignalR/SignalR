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

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData) {
            return TaskAsyncHelper.BeginTask(() => ProcessRequestAsync(context), cb, extraData);
        }

        void IHttpAsyncHandler.EndProcessRequest(IAsyncResult result) {
            TaskAsyncHelper.EndTask(result);
        }
    }
}