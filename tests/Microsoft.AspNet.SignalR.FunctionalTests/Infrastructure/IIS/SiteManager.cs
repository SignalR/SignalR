using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Web.Administration;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public class SiteManager
    {
        private static Random portNumberGenRnd = new Random((int)DateTime.Now.Ticks);

        private readonly string _path;
        private readonly string _appHostConfigPath;
        private readonly string _iisHomePath;
        private readonly ServerManager _serverManager;
        private static Process _iisExpressProcess;

        private static readonly string IISExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                                     "IIS Express",
                                                                     "iisexpress.exe");

        private const string TestSiteName = "signalr-test-site";

        public SiteManager(string path)
        {
            _path = Path.GetFullPath(path);
            _appHostConfigPath = Path.GetFullPath(Path.Combine(_path, "..", "..", "config", "applicationHost.config"));
            _iisHomePath = Path.GetFullPath(Path.Combine(_appHostConfigPath, "..", ".."));
            _serverManager = new ServerManager(_appHostConfigPath);
        }

        public string GetSiteUrl()
        {
            Site site = _serverManager.Sites[TestSiteName];

            if (site == null)
            {
                int sitePort = GetRandomPort();
                site = _serverManager.Sites.Add(TestSiteName, "http", "*:" + sitePort + ":localhost", _path);

                _serverManager.CommitChanges();
            }

            EnsureIISExpressProcess();

            return String.Format("http://localhost:{0}", site.Bindings[0].EndPoint.Port);
        }

        public void RecycleApplication()
        {
            // This blows up with access denied. We need a way to force clearing the state without killing the process
            // ApplicationPool appPool = _serverManager.ApplicationPools[_serverManager.ApplicationDefaults.ApplicationPoolName];
            // appPool.Stop();
        }

        private int GetRandomPort()
        {
            int randomPort = portNumberGenRnd.Next(1025, 65535);
            while (!IsPortAvailable(randomPort))
            {
                randomPort = portNumberGenRnd.Next(1025, 65535);
            }

            return randomPort;
        }

        private bool IsPortAvailable(int port)
        {
            foreach (var iisSite in _serverManager.Sites)
            {
                foreach (var binding in iisSite.Bindings)
                {
                    if (binding.EndPoint != null && binding.EndPoint.Port == port)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void EnsureIISExpressProcess()
        {
            Process oldProcess = Interlocked.CompareExchange(ref _iisExpressProcess, CreateIISExpressProcess(), null);
            if (oldProcess == null)
            {
                _iisExpressProcess.Start();
                return;
            }
        }

        private Process CreateIISExpressProcess()
        {
            if (!File.Exists(IISExpressPath))
            {
                throw new InvalidOperationException("Unable to locate IIS Express on the machine");
            }

            var iisExpressProcess = new Process();
            iisExpressProcess.StartInfo = new ProcessStartInfo(IISExpressPath, "/config:" + _appHostConfigPath);
            iisExpressProcess.StartInfo.EnvironmentVariables["IIS_USER_HOME"] = _iisHomePath;
            //iisExpressProcess.StartInfo.CreateNoWindow = false;
            iisExpressProcess.StartInfo.UseShellExecute = false;
            iisExpressProcess.EnableRaisingEvents = true;
            iisExpressProcess.Exited += OnIIsExpressQuit;

            return iisExpressProcess;
        }

        void OnIIsExpressQuit(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref _iisExpressProcess, null);
        }

    }
}