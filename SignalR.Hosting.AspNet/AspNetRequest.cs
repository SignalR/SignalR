using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Security.Principal;
using System.Web;

namespace SignalR.Hosting.AspNet
{
    public class AspNetRequest : IRequest
    {
        private readonly HttpRequestBase _request;
        private NameValueCollection _form;
        private NameValueCollection _queryString;

        private delegate void GetUnvalidatedCollections(HttpContext context, out Func<NameValueCollection> formGetter, out Func<NameValueCollection> queryStringGetter);
        private static Lazy<GetUnvalidatedCollections> _extractCollectionsMethod = new Lazy<GetUnvalidatedCollections>(ResolveCollectionsMethod);

        public AspNetRequest(HttpRequestBase request, IPrincipal user)
        {
            _request = request;
            Cookies = new HttpCookieCollectionWrapper(request.Cookies);
            User = user;
            ResolveFormAndQueryString();
        }

        public Uri Url
        {
            get
            {
                return _request.Url;
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
                return _request.Headers;
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
                _form = _request.Form;
                _queryString = _request.QueryString;
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
    }
}
