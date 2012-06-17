using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.Hosting.AspNet
{
    public class AspNetRequest : IRequest
    {
        private readonly HttpContextBase _context;
        private NameValueCollection _form;
        private NameValueCollection _queryString;

        private delegate void GetUnvalidatedCollections(HttpContext context, out Func<NameValueCollection> formGetter, out Func<NameValueCollection> queryStringGetter);
        private static Lazy<GetUnvalidatedCollections> _extractCollectionsMethod = new Lazy<GetUnvalidatedCollections>(ResolveCollectionsMethod);

        public AspNetRequest(HttpContextBase context)
        {
            _context = context;
            Cookies = new HttpCookieCollectionWrapper(context.Request.Cookies);
            User = context.User;
            ResolveFormAndQueryString();
        }

        public Uri Url
        {
            get
            {
                return _context.Request.Url;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return _queryString;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return _context.Request.Headers;
            }
        }

        public NameValueCollection ServerVariables
        {
            get
            {
                return _context.Request.ServerVariables;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                return _form;
            }
        }

        public IRequestCookieCollection Cookies
        {
            get;
            private set;
        }

        public IPrincipal User
        {
            get;
            private set;
        }

        private void ResolveFormAndQueryString()
        {
            // Since the ValidationUtility has a dependency on HttpContext (not HttpContextBase) we
            // need to check if we're out of HttpContext to preserve testability.
            if (!ResolveUnvalidatedCollections())
            {
                _form = _context.Request.Form;
                _queryString = _context.Request.QueryString;
            }
        }

        private bool ResolveUnvalidatedCollections()
        {
            var context = HttpContext.Current;

            if (context == null)
            {
                return false;
            }

            try
            {
                if (_extractCollectionsMethod.Value == null)
                {
                    return false;
                }

                Func<NameValueCollection> formGetter, queryStringGetter;
                _extractCollectionsMethod.Value.Invoke(context, out formGetter, out queryStringGetter);

                _form = formGetter.Invoke();
                _queryString = queryStringGetter.Invoke();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static GetUnvalidatedCollections ResolveCollectionsMethod()
        {
            const string mwiAssemblyName = "Microsoft.Web.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
            const string validationUtilityHelper = "Microsoft.Web.Infrastructure.DynamicValidationHelper.ValidationUtility";

            Assembly mwiAssembly = Assembly.Load(mwiAssemblyName);

            if (mwiAssembly == null)
            {
                return null;
            }

            Type validationUtilityHelperType = mwiAssembly.GetType(validationUtilityHelper);

            if (validationUtilityHelperType == null)
            {
                return null;
            }

            MethodInfo getUnvalidatedCollectionsMethod = validationUtilityHelperType.GetMethod("GetUnvalidatedCollections", BindingFlags.Public | BindingFlags.Static);

            if (getUnvalidatedCollectionsMethod == null)
            {
                return null;
            }

            return (GetUnvalidatedCollections)Delegate.CreateDelegate(typeof(GetUnvalidatedCollections), firstArgument: null, method: getUnvalidatedCollectionsMethod);
        }

        public void AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
#if NET45
            _context.AcceptWebSocketRequest(ws =>
            {
                var handler = new AspNetWebSocketHandler();
                var task = handler.ProcessWebSocketRequestAsync(ws);
                callback(handler).Then(h => h.CleanClose(), handler);
                return task;
            });
#else
            throw new NotSupportedException();
#endif
        }
    }
}
