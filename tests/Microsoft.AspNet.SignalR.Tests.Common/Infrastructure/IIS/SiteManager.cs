using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Threading;
using Microsoft.Web.Administration;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure.IIS
{
    public class SiteManager
    {
        private readonly string _path;
        private readonly string _appHostConfigPath;
        private readonly ServerManager _serverManager;

        private Process _iisExpressProcess;
        private Process _debuggerProcess;

        private static readonly string IISExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                                     "IIS Express",
                                                                     "iisexpress.exe");

        private const string TestSiteName = "signalr-test-site";
        private const int TestSitePort = 1337;
        private static string TestSiteUrl = String.Format("http://localhost:{0}", TestSitePort);
        private static string PingUrl = TestSiteUrl + "/ping";
        private static string GCUrl = TestSiteUrl + "/gc";

        public SiteManager(string path)
        {
            _path = Path.GetFullPath(path);
            _appHostConfigPath = Path.GetFullPath(Path.Combine(_path, "bin", "config", "applicationHost.config"));
            _serverManager = new ServerManager(_appHostConfigPath);
        }

        public string GetSiteUrl(IDictionary<string, string> extraData)
        {
            Site site = _serverManager.Sites[TestSiteName];

            if (site == null)
            {
                site = _serverManager.Sites.Add(TestSiteName, "http", "*:" + TestSitePort + ":localhost", _path);
                site.TraceFailedRequestsLogging.Enabled = true;

                _serverManager.CommitChanges();
            }

            EnsureNewIISExpressProcess();

            extraData["pid"] = _iisExpressProcess.Id.ToString();

            Attach();

            PingServerUrl();

            return TestSiteUrl;
        }

        private void Attach()
        {
            var windbgPath = ConfigurationManager.AppSettings["windbgPath"];

            if (String.IsNullOrEmpty(windbgPath))
            {
                return;
            }

            _debuggerProcess = Process.Start(windbgPath, "-g -p " + _iisExpressProcess.Id);
        }

        private void PingServerUrl()
        {
            string pingUrl = PingUrl + "?pid=" + _iisExpressProcess.Id;
            MakeHttpRequest(pingUrl);
        }

        public void StopSite()
        {
            if (_iisExpressProcess == null)
            {
                return;
            }

            string gcUrl = GCUrl + "?pid=" + _iisExpressProcess.Id;
            MakeHttpRequest(gcUrl);
            KillRunningIIsExpress();
        }

        private void MakeHttpRequest(string url, int delay = 250, int retryAttempts = 5)
        {
            int attempt = retryAttempts;

            while (true)
            {
                var request = HttpWebRequest.Create(url);
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

        private bool KillRunningIIsExpress()
        {
            try
            {
                KillProcess(_debuggerProcess);
                return KillProcess(_iisExpressProcess);
            }
            finally
            {
                _iisExpressProcess = null;
            }
        }

        private void EnsureNewIISExpressProcess()
        {
            if (_iisExpressProcess != null)
            {
                KillRunningIIsExpress();
            }
            else
            {
                int iisExpressPid;
                if (TryGetRunningIIsExpress(out iisExpressPid))
                {
                    KillProcess(iisExpressPid);
                }
            }

            EnsureIISExpressCompressionDirectory();
            Process iisExpressProcess = CreateIISExpressProcess();
            iisExpressProcess.Start();

            Trace.TraceInformation("Created new iis express instance. PID {0}", iisExpressProcess.Id);

            _iisExpressProcess = iisExpressProcess;
        }

        private void EnsureIISExpressCompressionDirectory()
        {
            var tempDirectory = Environment.GetEnvironmentVariable("TEMP");

            // TODO: Read this from the applicationHost.config
            var compressionDirectoryPath = Path.Combine(tempDirectory, "iisexpress", "IIS Temporary Compressed Files");
            Directory.CreateDirectory(compressionDirectoryPath);
        }

        private bool TryGetRunningIIsExpress(out int iisExpressId)
        {
            iisExpressId = -1;

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
                                iisExpressId = process.Id;
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
            iisExpressProcess.Exited += OnIISExpressExit;

            return iisExpressProcess;
        }

        private void OnIISExpressExit(object sender, EventArgs e)
        {
            Trace.TraceInformation("IISExpress exited");
        }

        private static bool KillProcess(int pid)
        {
            return KillProcess(GetProcess(pid));
        }

        private static bool KillProcess(Process process)
        {
            if (process == null)
            {
                return false;
            }

            int id = process.Id;

            bool killed = false;
            Exception exception = null;
            string name = String.Empty;

            try
            {
                name = process.ProcessName;

                process.Kill();

                process.WaitForExit();

                killed = GetProcess(process.Id) == null;
            }
            catch (Win32Exception ex)
            {
                killed = false;
                exception = ex;
            }
            catch (InvalidOperationException ex)
            {
                killed = false;
                exception = ex;
            }

            if (killed)
            {
                Trace.TraceInformation("Killed {0} PID {1}.", name, id);
            }
            else
            {
                Trace.TraceError("Failed to kill {0} PID {1}. {2}", name, id, exception);
            }

            return killed;
        }

        private static Process GetProcess(int pid)
        {
            try
            {
                return Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                // The process specified by the processId parameter is not running. The identifier might be expired.
                return null;
            }
            catch (InvalidOperationException)
            {
                // The process ended
                return null;
            }
        }
    }
}