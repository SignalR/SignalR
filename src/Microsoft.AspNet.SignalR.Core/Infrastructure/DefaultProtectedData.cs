using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public class DefaultProtectedData : IProtectedData
    {
        public string Protect(string data, string purpose)
        {
            byte[] purposeBytes = Encoding.UTF8.GetBytes(purpose);

            byte[] unprotectedBytes = Encoding.UTF8.GetBytes(data);

            byte[] protectedBytes = ProtectedData.Protect(unprotectedBytes, purposeBytes, DataProtectionScope.LocalMachine);

            return Convert.ToBase64String(protectedBytes);
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            byte[] purposeBytes = Encoding.UTF8.GetBytes(purpose);

            byte[] protectedBytes = Convert.FromBase64String(protectedValue);

            byte[] unprotectedBytes = ProtectedData.Unprotect(protectedBytes, purposeBytes, DataProtectionScope.LocalMachine);

            return Encoding.UTF8.GetString(unprotectedBytes);
        }
    }
}
