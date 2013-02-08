using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class EmptyProtectedData : IProtectedData
    {
        public string Protect(string data, string purpose)
        {
            return data;
        }

        public string Unprotect(string protectedValue, string purpose)
        {
            return protectedValue;
        }
    }
}
