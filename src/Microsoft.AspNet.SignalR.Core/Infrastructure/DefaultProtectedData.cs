using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public class DefaultProtectedData : IProtectedData
    {
        private readonly DataProtectionScope _scope;

        public DefaultProtectedData()
            : this(DataProtectionScope.CurrentUser)
        {
        }

        public DefaultProtectedData(DataProtectionScope scope)
        {
            _scope = scope;
        }

        public string Protect(string data, string purpose)
        {
            byte[] purposeBytes = Encoding.UTF8.GetBytes(purpose);

            byte[] unprotectedBytes = Encoding.UTF8.GetBytes(data);

            byte[] protectedBytes = ProtectedData.Protect(unprotectedBytes, purposeBytes, _scope);

            return Convert.ToBase64String(protectedBytes);
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            byte[] purposeBytes = Encoding.UTF8.GetBytes(purpose);

            byte[] protectedBytes = Convert.FromBase64String(protectedValue);

            byte[] unprotectedBytes = ProtectedData.Unprotect(protectedBytes, purposeBytes, _scope);

            return Encoding.UTF8.GetString(unprotectedBytes);
        }
    }
}
