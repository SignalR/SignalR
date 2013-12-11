using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Basic;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class BasicAuthenticationProvider : IBasicAuthenticationProvider
    {
        public Task<IPrincipal> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(userName) && password == "password")
            {
                var identity = new ClaimsIdentity("Basic");
                identity.AddClaim(new Claim(ClaimTypes.Name, userName));
                var principal = new ClaimsPrincipal(identity);
                return Task.FromResult<IPrincipal>(principal);
            }

            return Task.FromResult<IPrincipal>(null);
        }
    }
}
