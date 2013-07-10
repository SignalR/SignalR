using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Owin.Infrastructure;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin.Middleware
{
    public abstract class SignalRMiddleware : OwinMiddleware
    {
        private readonly ConnectionConfiguration _configuration;

        protected SignalRMiddleware(OwinMiddleware next, ConnectionConfiguration configuration)
            : base(next)
        {
            _configuration = configuration;
        }

        public override Task Invoke(IOwinContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (_configuration.EnableCrossDomain)
            {
                CorsUtility.AddHeaders(context);
            }
            else if (CorsUtility.IsCrossDomainRequest(context.Request))
            {
                context.Response.StatusCode = 403;
                context.Response.Environment[OwinConstants.ResponseReasonPhrase] = Resources.Forbidden_CrossDomainIsDisabled;

                return TaskAsyncHelper.Empty;
            }

            return ProcessRequest(context);
        }

        protected abstract Task ProcessRequest(IOwinContext context);
    }
}
