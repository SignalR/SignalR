using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    internal class BasicAuthModule
    {
        private readonly Func<IDictionary<string, object>, Task> _next;
        private readonly string _user;
        private readonly string _password;

        private const string WwwAuthenticateHeader = "WWW-Authenticate";
        private const string AuthorizationHeader = "Authorization";

        private const string OwinRequestHeadersKey = "owin.RequestHeaders";
        private const string OwinResponseHeadersKey = "owin.ResponseHeaders";
        private const string OwinResponseStatusCode = "owin.ResponseStatusCode";

        private const string ServerUserKey = "server.User";

        public BasicAuthModule(Func<IDictionary<string, object>, Task> next, string user, string password)
        {
            _next = next;
            _user = user;
            _password = password;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var requestHeaders = (IDictionary<string, string[]>)env[OwinRequestHeadersKey];
            string[] authHeaders;
            string authHeader = null;

            if (requestHeaders.TryGetValue(AuthorizationHeader, out authHeaders))
            {
                authHeader = authHeaders[0];
            }

            if (String.IsNullOrEmpty(authHeader) ||
                !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                Challenge(env);
                return TaskAsyncHelper.Empty;
            }

            string user;
            string pass;

            if (!TryGetCredentials(authHeader, out user, out pass))
            {
                env[OwinResponseStatusCode] = 400;
                return TaskAsyncHelper.Empty;
            }

            if (_user.Equals(user) && _password.Equals(pass))
            {
                // Set the IPrincipal
                env[ServerUserKey] = new GenericPrincipal(new GenericIdentity(user, "Basic"), new string[0]);
            }
            else
            {
                Challenge(env);
            }

            return _next(env);
        }

        private static void Challenge(IDictionary<string, object> env)
        {
            env[OwinResponseStatusCode] = 401;
            var responseHeaders = (IDictionary<string, string[]>)env[OwinResponseHeadersKey];
            responseHeaders[WwwAuthenticateHeader] = new[] { "Basic" };
        }

        private static bool TryGetCredentials(string authHeader, out string user, out string password)
        {
            byte[] data = Convert.FromBase64String(authHeader.Substring(6).Trim());
            string userAndPass = Encoding.UTF8.GetString(data);
            int colonIndex = userAndPass.IndexOf(':');

            if (colonIndex < 0)
            {
                user = null;
                password = null;
                return false;
            }

            user = userAndPass.Substring(0, colonIndex);
            password = userAndPass.Substring(colonIndex + 1);
            return true;
        }
    }
}
