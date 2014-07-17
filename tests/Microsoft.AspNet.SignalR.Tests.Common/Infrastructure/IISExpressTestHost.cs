using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure.IIS;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class IISExpressTestHost : ITestHost
    {
        private readonly SiteManager _siteManager;
        private readonly string _path;
        private readonly string _webConfigPath;
        private readonly string _logFileName;

        private static readonly Lazy<string> _webConfigTemplate = new Lazy<string>(() => GetConfig());

        public IISExpressTestHost()
            : this("all")
        {
        }

        public IISExpressTestHost(string logFileName)
            : this(Path.Combine(Directory.GetCurrentDirectory(), ".."), logFileName)
        {
        }

        public IISExpressTestHost(string path, string logFileName)
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

            // Trace file name
            _logFileName = logFileName;

            Disposables = new List<IDisposable>();

            ExtraData = new Dictionary<string, string>();
        }

        public string Url { get; private set; }

        public IClientTransport Transport { get; set; }

        public Func<IClientTransport> TransportFactory { get; set; }

        public TextWriter ClientTraceOutput { get; set; }

        public IDictionary<string, string> ExtraData { get; private set; }

        public IDependencyResolver Resolver { get; set; }
        
        public IList<IDisposable> Disposables
        {
            get;
            private set;
        }

        public bool AttachToPreSendRequestHeaders { get; set; }

        public void Initialize(int? keepAlive,
                               int? connectionTimeout,
                               int? disconnectTimeout,
                               int? transportConnectTimeout,
                               int? maxIncomingWebSocketMessageSize,
                               bool enableAutoRejoiningGroups,
                               MessageBusType messageBusType)
        {
            if (messageBusType != MessageBusType.Default)
            {
                throw new NotImplementedException();
            }

            // Use a configuration file to specify values
            string content = String.Format(_webConfigTemplate.Value,
                                           keepAlive,
                                           connectionTimeout,
                                           disconnectTimeout,
                                           transportConnectTimeout,
                                           maxIncomingWebSocketMessageSize,
                                           enableAutoRejoiningGroups,
                                           _logFileName,
                                           AttachToPreSendRequestHeaders);

            File.WriteAllText(_webConfigPath, content);

            Url = _siteManager.GetSiteUrl(ExtraData);
        }

        public void Dispose()
        {
            Trace.TraceInformation("IISExpressTestHost.Dispose()");

            Shutdown();

            foreach (var d in Disposables)
            {
                d.Dispose();
            }
        }

        public Task Get(string uri)
        {
            throw new NotImplementedException();
        }

        public Task Post(string uri, IDictionary<string, string> data)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            Trace.TraceInformation("IISExpressTestHost.Shutdown()");

            _siteManager.StopSite();
        }

        private static string GetConfig()
        {
            using (Stream resourceStream = typeof(IISExpressTestHost).Assembly.GetManifestResourceStream("Microsoft.AspNet.SignalR.Tests.Common.Infrastructure.IIS.site.web.config"))
            {
                var reader = new StreamReader(resourceStream);
                return reader.ReadToEnd();
            }
        }
    }
}
