using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using IISServer = Microsoft.Web.Administration;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public class SiteManager
    {
        private static Random portNumberGenRnd = new Random((int)DateTime.Now.Ticks);

        private readonly string _path;

        public SiteManager(string path)
        {
            _path = Path.GetFullPath(path);
        }

        public string CreateSite(string applicationName)
        {
            var iis = new IISServer.ServerManager();

            try
            {
                Directory.CreateDirectory(_path);

                int sitePort = CreateSite(iis, applicationName, _path);

                string url = String.Format("http://localhost:{0}/", sitePort);

                // Commit the changes to iis
                iis.CommitChanges();

                // Get teh site after committing changes
                var site = iis.Sites[applicationName];

                // Wait until the site is in the started state
                site.WaitForState(IISServer.ObjectState.Started);

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
            var appPool = iis.ApplicationPools[applicationName];

            // Make sure the acls are gone
            RemoveAcls(applicationName);

            if (appPool == null)
            {
                // If there's no app pool then do nothing
                return;
            }

            DeleteSite(iis, applicationName);

            iis.CommitChanges();

            // Remove the app pool and commit changes
            iis.ApplicationPools.Remove(iis.ApplicationPools[applicationName]);
            iis.CommitChanges();
        }

        private void RemoveAcls(string appPoolName)
        {
            // Setup Acls for this user
            var icacls = new Executable(@"C:\Windows\System32\icacls.exe", Directory.GetCurrentDirectory());

            try
            {
                // Give full control to the app folder (we can make it minimal later)
                icacls.Execute(@"""{0}"" /remove ""IIS AppPool\{1}""", _path, appPoolName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SetupAcls(string appPoolName)
        {
            // Setup Acls for this user
            var icacls = new Executable(@"C:\Windows\System32\icacls.exe", Directory.GetCurrentDirectory());

            try
            {
                // Give full control to the app folder (we can make it minimal later)
                icacls.Execute(@"""{0}"" /grant:r ""IIS AppPool\{1}:(OI)(CI)(F)"" /C /Q /T", _path, appPoolName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static int GetRandomPort(IISServer.ServerManager iis)
        {
            int randomPort = portNumberGenRnd.Next(1025, 65535);
            while (!IsAvailable(randomPort, iis))
            {
                randomPort = portNumberGenRnd.Next(1025, 65535);
            }

            return randomPort;
        }

        private static bool IsAvailable(int port, IISServer.ServerManager iis)
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

        private int CreateSite(IISServer.ServerManager iis, string applicationName, string siteRoot)
        {
            var appPool = EnsureAppPool(iis, applicationName);
            int sitePort = GetRandomPort(iis);
            var site = iis.Sites.Add(applicationName, siteRoot, sitePort);
            site.ApplicationDefaults.ApplicationPoolName = appPool.Name;

            return sitePort;
        }

        private static void DeleteSite(IISServer.ServerManager iis, string applicationName)
        {
            var site = iis.Sites[applicationName];
            if (site != null)
            {
                site.StopAndWait();
                iis.Sites.Remove(site);
            }
        }

        private IISServer.ApplicationPool EnsureAppPool(IISServer.ServerManager iis, string applicationName)
        {
            var appPool = iis.ApplicationPools[applicationName];

            if (appPool == null)
            {
                iis.ApplicationPools.Add(applicationName);
                iis.CommitChanges();

                appPool = iis.ApplicationPools[applicationName];
                appPool.ManagedPipelineMode = IISServer.ManagedPipelineMode.Integrated;
                appPool.ManagedRuntimeVersion = "v4.0";
                appPool.AutoStart = true;
                appPool.ProcessModel.LoadUserProfile = false;
                appPool.WaitForState(IISServer.ObjectState.Started);

                SetupAcls(applicationName);
            }

            return appPool;
        }
    }
}