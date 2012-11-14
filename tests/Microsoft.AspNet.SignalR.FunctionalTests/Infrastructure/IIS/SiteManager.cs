using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using IISServer = Microsoft.Web.Administration;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public class SiteManager : ISiteManager
    {
        private static Random portNumberGenRnd = new Random((int)DateTime.Now.Ticks);

        private readonly IPathResolver _pathResolver;

        public SiteManager(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public string CreateSite(string applicationName)
        {
            var iis = new IISServer.ServerManager();

            try
            {
                // Create the main site
                string siteName = GetSiteName(applicationName);
                string sitePath = _pathResolver.GetApplicationPath(applicationName);

                EnsureDirectory(sitePath);

                int sitePort = CreateSite(iis, applicationName, siteName, sitePath);
                string url = String.Format("http://localhost:{0}/", sitePort);

                // Commit the changes to iis
                iis.CommitChanges();

                // Give IIS some time to create the site and map the path
                // REVIEW: Should we poll the site's state?
                Thread.Sleep(1000);

                return url;
            }
            catch
            {
                DeleteSite(applicationName);
                throw;
            }
        }

        public void DeleteSite(string applicationName)
        {
            var iis = new IISServer.ServerManager();

            // Get the app pool for this application
            string appPoolName = GetAppPool(applicationName);
            IISServer.ApplicationPool appPool = iis.ApplicationPools[appPoolName];

            // Make sure the acls are gone
            RemoveAcls(applicationName, appPoolName);

            if (appPool == null)
            {
                // If there's no app pool then do nothing
                return;
            }

            string siteName = GetSiteName(applicationName);
            DeleteSite(iis, siteName);

            iis.CommitChanges();

            string appPath = _pathResolver.GetApplicationPath(applicationName);

            // Remove the app pool and commit changes
            iis.ApplicationPools.Remove(iis.ApplicationPools[appPoolName]);
            iis.CommitChanges();
        }

        private IISServer.ApplicationPool EnsureAppPool(IISServer.ServerManager iis, string appName)
        {
            string appPoolName = GetAppPool(appName);
            var appPool = iis.ApplicationPools[appPoolName];
            if (appPool == null)
            {
                iis.ApplicationPools.Add(appPoolName);
                iis.CommitChanges();
                appPool = iis.ApplicationPools[appPoolName];
                appPool.ManagedPipelineMode = IISServer.ManagedPipelineMode.Integrated;
                appPool.ManagedRuntimeVersion = "v4.0";
                appPool.AutoStart = true;
                appPool.ProcessModel.LoadUserProfile = false;
                appPool.WaitForState(IISServer.ObjectState.Started);

                SetupAcls(appName, appPoolName);
            }

            return appPool;
        }

        private void RemoveAcls(string appName, string appPoolName)
        {
            // Setup Acls for this user
            var icacls = new Executable(@"C:\Windows\System32\icacls.exe", Directory.GetCurrentDirectory());

            string applicationPath = _pathResolver.GetApplicationPath(appName);

            try
            {
                // Give full control to the app folder (we can make it minimal later)
                icacls.Execute(@"""{0}\"" /remove ""IIS AppPool\{1}""", applicationPath, appPoolName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SetupAcls(string appName, string appPoolName)
        {
            // Setup Acls for this user
            var icacls = new Executable(@"C:\Windows\System32\icacls.exe", Directory.GetCurrentDirectory());

            // Make sure the application path exists
            string applicationPath = _pathResolver.GetApplicationPath(appName);
            Directory.CreateDirectory(applicationPath);

            try
            {
                // Give full control to the app folder (we can make it minimal later)
                icacls.Execute(@"""{0}"" /grant:r ""IIS AppPool\{1}:(OI)(CI)(F)"" /C /Q /T", applicationPath, appPoolName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private int GetRandomPort(IISServer.ServerManager iis)
        {
            int randomPort = portNumberGenRnd.Next(1025, 65535);
            while (!IsAvailable(randomPort, iis))
            {
                randomPort = portNumberGenRnd.Next(1025, 65535);
            }

            return randomPort;
        }

        private bool IsAvailable(int port, IISServer.ServerManager iis)
        {
            var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            foreach (var connectionInfo in tcpConnections)
            {
                if (connectionInfo.LocalEndPoint.Port == port)
                {
                    return false;
                }
            }

            foreach (var iisSite in iis.Sites)
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

        private string GetSiteUrl(IISServer.Site site)
        {
            if (site == null)
            {
                return null;
            }

            IISServer.Binding binding = site.Bindings.Last();
            var builder = new UriBuilder
            {
                Host = String.IsNullOrEmpty(binding.Host) ? "localhost" : binding.Host,
                Scheme = binding.Protocol,
                Port = binding.EndPoint.Port
            };

            if (builder.Port == 80)
            {
                builder.Port = -1;
            }

            return builder.ToString();
        }

        private int CreateSite(IISServer.ServerManager iis, string applicationName, string siteName, string siteRoot)
        {
            var pool = EnsureAppPool(iis, applicationName);
            int sitePort = GetRandomPort(iis);
            var site = iis.Sites.Add(siteName, siteRoot, sitePort);
            site.ApplicationDefaults.ApplicationPoolName = pool.Name;

            return sitePort;
        }

        private void DeleteSite(IISServer.ServerManager iis, string siteName)
        {
            var site = iis.Sites[siteName];
            if (site != null)
            {
                site.StopAndWait();
                iis.Sites.Remove(site);
            }
        }

        private static string GetSiteName(string applicationName)
        {
            return "signalr_" + applicationName;
        }

        private static string GetAppPool(string applicationName)
        {
            return applicationName;
        }

        private void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}