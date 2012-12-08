﻿using System;
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

        private static readonly Lazy<string> _webConfigTemplate = new Lazy<string>(() => GetConfig());

        public IISExpressTestHost()
        {
            // The path to the site is the test path.
            // We treat the test output path just like a site. This makes it super
            // cheap to create and tear down sites. We don't need to copy any files.
            // The downside is that we can't run tests in parallel anymore.
            _path = Path.Combine(Directory.GetCurrentDirectory(), "..");

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

        private static string GetConfig()
        {
            using (Stream resourceStream = typeof(IISExpressTestHost).Assembly.GetManifestResourceStream("Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS.site.web.config"))
            {
                var reader = new StreamReader(resourceStream);
                return reader.ReadToEnd();
            }
        }
    }
}
