using System;
using System.Linq;
using System.Security.Principal;
using System.Web.Security;
using System.Web.UI;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    public partial class _Default : Page
    {
        protected void Login(object sender, EventArgs e)
        {
            var userId = Guid.NewGuid().ToString();
            FormsAuthentication.SetAuthCookie(userId, createPersistentCookie: false);
            var identity = new GenericIdentity(userName.Text);
            var principal = new GenericPrincipal(identity, SplitString(roles.Text));
            Context.User = principal;
            Cache[userId] = principal;
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