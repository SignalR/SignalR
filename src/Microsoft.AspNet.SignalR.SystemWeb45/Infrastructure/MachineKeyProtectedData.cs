using System.Text;
using System.Web;
using System.Web.Security;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.SystemWeb.Infrastructure
{
    public class MachineKeyProtectedData : IProtectedData
    {
        public string Protect(string data, string purpose)
        {
            byte[] unprotectedBytes = Encoding.UTF8.GetBytes(data);

            byte[] protectedBytes = MachineKey.Protect(unprotectedBytes, purpose);

            return HttpServerUtility.UrlTokenEncode(protectedBytes);
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            byte[] protectedBytes = HttpServerUtility.UrlTokenDecode(protectedValue);

            byte[] unprotectedBytes = MachineKey.Unprotect(protectedBytes, purpose);

            return Encoding.UTF8.GetString(unprotectedBytes);
        }
    }
}
