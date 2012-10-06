using System;
using System.Linq;
using System.Security.Principal;

namespace SignalR.Hubs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizeAttribute : Attribute, IAuthorizeHubConnection, IAuthorizeHubMethodInvocation
    {
        private string _roles;
        private string[] _rolesSplit = new string[0];
        private string _users;
        private string[] _usersSplit = new string[0];

        public AuthorizeAttribute() { }

        public AuthorizeAttribute(string roles, string users)
        {
            Roles = roles;
            Users = users;
        }

        public string Roles
        {
            get { return _roles ?? String.Empty; }
            set
            {
                _roles = value;
                _rolesSplit = SplitString(value);
            }
        }

        public string Users
        {
            get { return _users ?? String.Empty; }
            set
            {
                _users = value;
                _usersSplit = SplitString(value);
            }
        }

        public virtual bool AuthorizeHubConnection(IHub hub)
        {
             return UserAuthorized(hub.Context.User);
        }

        public virtual bool AuthorizeHubMethodInvocation(IHubIncomingInvokerContext hubIncomingInvokerContext)
        {
            return UserAuthorized(hubIncomingInvokerContext.Hub.Context.User);

        }

        private bool UserAuthorized(IPrincipal user)
        {
            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            if (_usersSplit.Length > 0 && !_usersSplit.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_rolesSplit.Length > 0 && !_rolesSplit.Any(user.IsInRole))
            {
                return false;
            }

            return true;
        }

        private static string[] SplitString(string original)
        {
            if (String.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = from piece in original.Split(',')
                        let trimmed = piece.Trim()
                        where !String.IsNullOrEmpty(trimmed)
                        select trimmed;
            return split.ToArray();
        }
    }
}
