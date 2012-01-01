using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using SignalR.Abstractions;

namespace SignalR.AspNet
{
    public class AspNetResponse : IResponse
    {
        private delegate void RemoveHeaderDel(HttpWorkerRequest workerRequest);

        private const string IIS7WorkerRequestTypeName = "System.Web.Hosting.IIS7WorkerRequest";
        private static readonly RemoveHeaderDel IIS7RemoveHeader = GetRemoveHeaderDelegate();        

        private readonly HttpContextBase _context;

        public AspNetResponse(HttpContextBase context)
        {
            _context = context;
        }

        public bool Buffer
        {
            get
            {
                return _context.Response.Buffer;
            }
            set
            {
                _context.Response.Buffer = value;
                _context.Response.BufferOutput = value;

                if (!value)
                {
                    // This forces the IIS compression module to leave this response alone.
                    // If we don't do this, it will buffer the response to suit its own compression
                    // logic, resulting in partial messages being sent to the client.                    
                    RemoveAcceptEncoding();

                    _context.Response.CacheControl = "no-cache";
                    _context.Response.AddHeader("Connection", "keep-alive");
                }
            }
        }

        public bool IsClientConnected
        {
            get
            {
                return _context.Response.IsClientConnected;
            }
        }

        public string ContentType
        {
            get
            {
                return _context.Response.ContentType;
            }
            set
            {
                _context.Response.ContentType = value;
            }
        }

        public Task WriteAsync(string data)
        {
            _context.Response.Write(data);
            return TaskAsyncHelper.Empty;
        }

        private void RemoveAcceptEncoding()
        {
            var workerRequest = (HttpWorkerRequest)_context.GetService(typeof(HttpWorkerRequest));
            if (IsIIS7WorkerRequest(workerRequest))
            {
                // Optimized code path for IIS7, accessing Headers causes all headers to be read
                IIS7RemoveHeader(workerRequest);
            }
            else
            {
                _context.Request.Headers.Remove("Accept-Encoding");
            }
        }

        private bool IsIIS7WorkerRequest(HttpWorkerRequest workerRequest)
        {
            return workerRequest.GetType().FullName == IIS7WorkerRequestTypeName;
        }
        
        private static RemoveHeaderDel GetRemoveHeaderDelegate()
        {
            var iis7wrType = typeof(HttpContext).Assembly.GetType(IIS7WorkerRequestTypeName);
            var methodInfo = iis7wrType.GetMethod("SetKnownRequestHeader", BindingFlags.NonPublic | BindingFlags.Instance);

            var wrParamExpr = Expression.Parameter(typeof(HttpWorkerRequest));
            var iis7wrParamExpr = Expression.Convert(wrParamExpr, iis7wrType);
            var callExpr = Expression.Call(iis7wrParamExpr, methodInfo, Expression.Constant(HttpWorkerRequest.HeaderAcceptEncoding), Expression.Constant(null, typeof(string)), Expression.Constant(false));
            return Expression.Lambda<RemoveHeaderDel>(callExpr, wrParamExpr).Compile();
        }
    }
}
