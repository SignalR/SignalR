using System;
using System.Security.Claims;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AuthorizeClaimsAttribute : AuthorizeAttribute
    {
        protected override bool UserAuthorized(System.Security.Principal.IPrincipal user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var principal = (ClaimsPrincipal)user;

            if (principal != null)
            {
                Claim authenticated = principal.FindFirst(ClaimTypes.Authentication);
                return authenticated.Value == "true" ? true : false;
            }
            else
            {
                return false;
            }
        }
    }
}