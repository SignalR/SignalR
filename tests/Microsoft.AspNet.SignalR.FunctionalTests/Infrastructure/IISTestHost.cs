using System;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class IISTestHost : ITestHost
    {
        private ISiteManager _siteManager;
        private string _applicationName;

        public IISTestHost()
        {
            // Create a random application name
            _applicationName = Guid.NewGuid().ToString().Substring(0, 8);

            // The path to the site is the test path
            string path = Path.Combine(Directory.GetCurrentDirectory(), "..");

            // Create the site manager
            _siteManager = new SiteManager(new DefaultPathResolver(path));
        }

        public string Url { get; private set; }

        public IClientTransport Transport { get; set; }

        public void Initialize()
        {
            Url = _siteManager.CreateSite(_applicationName);
        }

        public void Dispose()
        {
            _siteManager.DeleteSite(_applicationName);
        }
    }
}
