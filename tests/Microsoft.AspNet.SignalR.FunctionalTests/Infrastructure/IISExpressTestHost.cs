using System;
using System.IO;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class IISExpressTestHost : ITestHost
    {
        private readonly SiteManager _siteManager;
        private readonly string _path;
        private readonly string _webConfigPath;

        private const string WebConfigTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <appSettings>
    <add key=""keepAlive"" value=""{0}"" />
    <add key=""connectionTimeout"" value=""{1}"" />
    <add key=""heartbeatInterval"" value=""{2}"" />
    <add key=""enableRejoiningGroups"" value=""{3}"" />
  </appSettings>
  <system.web>
    <compilation debug=""true"" targetFramework=""4.5"" />
    <httpRuntime targetFramework=""4.5"" />
  </system.web>
  <system.webServer>
    <modules runAllManagedModulesForAllRequests=""true"" />
  </system.webServer>
</configuration>";

        public IISExpressTestHost() : this(Path.Combine(Directory.GetCurrentDirectory(), ".."))
        { 
        }

        public IISExpressTestHost(string path)
        {
            // The path to the site is the test path.
            // We treat the test output path just like a site. This makes it super
            // cheap to create and tear down sites. We don't need to copy any files.
            // The downside is that we can't run tests in parallel anymore.
            _path = path;

            // Set the web.config path for this app
            _webConfigPath = Path.Combine(_path, "web.config");

            // Create the site manager
            _siteManager = new SiteManager(_path);
        }

        public string Url { get; private set; }

        public IClientTransport Transport { get; set; }

        public Func<IClientTransport> TransportFactory { get; set; }

        public void Initialize(int? keepAlive,
                               int? connectionTimeout,
                               int? disconnectTimeout,
                               int? hearbeatInterval,
                               bool enableAutoRejoiningGroups)
        {
            Url = _siteManager.GetSiteUrl();

            // Use a configuration file to specify values
            string content = String.Format(_webConfigTemplate.Value,
                                           keepAlive,
                                           connectionTimeout,
                                           disconnectTimeout,
                                           hearbeatInterval,
                                           enableAutoRejoiningGroups);

            File.WriteAllText(_webConfigPath, content);
        }

        public void Dispose()
        {
            Shutdown();
        }

        public void Shutdown()
        {
            _siteManager.StopSite();
        }
    }
}
