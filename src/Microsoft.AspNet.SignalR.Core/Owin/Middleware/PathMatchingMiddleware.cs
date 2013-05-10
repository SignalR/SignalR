using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public abstract class PathMatchingMiddleware : OwinMiddleware
    {
        private readonly string _path;

        protected PathMatchingMiddleware(OwinMiddleware next, string path)
            : base(next)
        {
            _path = path;
        }

        public override Task Invoke(OwinRequest request, OwinResponse response)
        {
            if (request.Path == null || !PrefixMatcher.IsMatch(_path, request.Path))
            {
                return Next.Invoke(request, response);
            }

            return ProcessRequest(request, response);
        }

        protected abstract Task ProcessRequest(OwinRequest request, OwinResponse response);
    }
}
