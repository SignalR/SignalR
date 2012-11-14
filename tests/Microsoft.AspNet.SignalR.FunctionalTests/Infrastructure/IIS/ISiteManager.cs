using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public interface ISiteManager
    {
        string CreateSite(string applicationName);
        void DeleteSite(string applicationName);
    }
}
