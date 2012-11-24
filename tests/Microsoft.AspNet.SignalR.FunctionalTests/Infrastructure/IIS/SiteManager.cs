using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Threading;
using Microsoft.Web.Administration;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public class SiteManager
    {
        private readonly string _path;
        private readonly string _appHostConfigPath;
        private readonly ServerManager _serverManager;

        private static Process _iisExpressProcess;
        private static int? _existingIISExpressProcessId;

        private static readonly string IISExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                                     "IIS Express",
                                                                     "iisexpress.exe");

        private const string TestSiteName = "signalr-test-site";
        private const int TestSitePort = 1337;
        private static string TestSiteUrl = String.Format("http://localhost:{0}", TestSitePort);
        private static string PingUrl = TestSiteUrl + "/ping";

        public SiteManager(string path)
        {
            _path = Path.GetFullPath(path);
            _appHostConfigPath = Path.GetFullPath(Path.Combine(_path, "bin", "config", "applicationHost.config"));
            _serverManager = new ServerManager(_appHostConfigPath);
        }

        public string GetSiteUrl()
        {
            Site site = _serverManager.Sites[TestSiteName];

            if (site == null)
            {
                if (TryGetRunningIIsExpress())
                {
                    // Kill existing IIS Express process mapping to this application
                    KillProcess();
                }

                site = _serverManager.Sites.Add(TestSiteName, "http", "*:" + TestSitePort + ":localhost", _path);
                site.TraceFailedRequestsLogging.Enabled = true;

                _serverManager.CommitChanges();
            }

            EnsureIISExpressProcess();

            PingServerUrl();

            return TestSiteUrl;
        }

        private void PingServerUrl()
        {
            const int retryAttempts = 5;
            const int delay = 250;

            int attempt = retryAttempts;

            while (true)
            {
                var request = HttpWebRequest.Create(PingUrl);
                try
                {
                    var response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        break;
                    }
                }
                catch
                {
                    if (attempt == 0)
                    {
                        throw;
                    }
                }

                attempt--;
                Thread.Sleep(delay);
            }
        }

        public void StopSite()
        {
            KillProcess();
        }

        private void KillProcess()
        {
            Process process = _iisExpressProcess;
            if (process == null)
            {
                if (_existingIISExpressProcessId == null)
                {
                    return;
                }

                try
                {
                    process = Process.GetProcessById(_existingIISExpressProcessId.Value);
                }
                catch (ArgumentException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }

            if (process != null)
            {
                process.Kill();

                // Sleep a little 
                Thread.Sleep(500);
            }
        }

        private void EnsureIISExpressProcess()
        {
            if (TryGetRunningIIsExpress())
            {
                return;
            }

            Process oldProcess = Interlocked.CompareExchange(ref _iisExpressProcess, CreateIISExpressProcess(), null);
            if (oldProcess == null)
            {
                _iisExpressProcess.Start();

                // Give it a little time to start up the webserver
                Thread.Sleep(250);
                return;
            }
        }

        private bool TryGetRunningIIsExpress()
        {
            // If we have a cached IISExpress id then just use it
            if (_existingIISExpressProcessId != null)
            {
                try
                {
                    var process = Process.GetProcessById(_existingIISExpressProcessId.Value);

                    // Make sure it's iis express (Can process ids be reused?)
                    if (process.ProcessName.Equals("iisexpress"))
                    {
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    // The process specified by the processId parameter is not running. The identifier might be expired.
                }
                catch (InvalidOperationException)
                {
                    // The process ended
                }

                _existingIISExpressProcessId = null;
            }

            foreach (Process process in Process.GetProcessesByName("iisexpress"))
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                    {
                        foreach (ManagementObject processObj in searcher.Get())
                        {
                            string commandLine = (string)processObj["CommandLine"];
                            if (!String.IsNullOrEmpty(commandLine) &&
                                (commandLine.Contains(_appHostConfigPath) ||
                                 commandLine.Contains("/site:" + TestSiteName)))
                            {
                                _existingIISExpressProcessId = process.Id;
                                return true;
                            }
                        }
                    }
                }
                catch (Win32Exception ex)
                {
                    if ((uint)ex.ErrorCode != 0x80004005)
                    {
                        throw;
                    }
                }
            }

            return false;
        }

        private Process CreateIISExpressProcess()
        {
            if (!File.Exists(IISExpressPath))
            {
                throw new InvalidOperationException("Unable to locate IIS Express on the machine");
            }

            var iisExpressProcess = new Process();
            iisExpressProcess.StartInfo = new ProcessStartInfo(IISExpressPath, "/config:\"" + _appHostConfigPath + "\" /site:" + TestSiteName + " /systray:false");
            iisExpressProcess.StartInfo.CreateNoWindow = true;
            iisExpressProcess.StartInfo.UseShellExecute = false;
            iisExpressProcess.EnableRaisingEvents = true;
            iisExpressProcess.Exited += OnIIsExpressQuit;

            return iisExpressProcess;
        }

        private void OnIIsExpressQuit(object sender, EventArgs e)
        {
            Interlocked.Exchange(ref _iisExpressProcess, null);
        }
    }
}