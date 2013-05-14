using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public abstract class SignalRMiddleware : OwinMiddleware
    {
        private readonly string _path;
        private readonly ConnectionConfiguration _configuration;

        protected SignalRMiddleware(OwinMiddleware next, string path, ConnectionConfiguration configuration)
            : base(next)
        {
            _path = path;
            _configuration = configuration;
        }

        public override Task Invoke(OwinRequest request, OwinResponse response)
        {
            if (request.Path == null || !PrefixMatcher.IsMatch(_path, request.Path))
            {
                return Next.Invoke(request, response);
            }

            if (_configuration.EnableCrossDomain)
            {
                CorsUtility.AddHeaders(request, response);
            }
            else if (CorsUtility.IsCrossDomainRequest(request))
            {
                response.StatusCode = 403;
                response.Environment[OwinConstants.ResponseReasonPhrase] = Resources.Forbidden_CrossDomainIsDisabled;

                return TaskAsyncHelper.Empty;
            }

            return ProcessRequest(request, response);
        }

        protected abstract Task ProcessRequest(OwinRequest request, OwinResponse response);
    }
}
