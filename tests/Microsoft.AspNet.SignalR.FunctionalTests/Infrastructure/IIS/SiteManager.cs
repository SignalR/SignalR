using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
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
        private static int? _existingIISExpressProcessId;

        private static readonly string IISExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                                     "IIS Express",
                                                                     "iisexpress.exe");

        private const string TestSiteName = "signalr-test-site";
        private const int TestSitePort = 1337;

        public SiteManager(string path)
        {
            _path = Path.GetFullPath(path);
            _appHostConfigPath = Path.GetFullPath(Path.Combine(_path, "bin", "config", "applicationHost.config"));
            _iisHomePath = Path.GetFullPath(Path.Combine(_appHostConfigPath, "..", ".."));
            _serverManager = new ServerManager(_appHostConfigPath);
        }

        public string GetSiteUrl()
        {
            Site site = _serverManager.Sites[TestSiteName];

            if (site == null)
            {
                site = _serverManager.Sites.Add(TestSiteName, "http", "*:" + TestSitePort + ":localhost", _path);

                _serverManager.CommitChanges();
            }

            EnsureIISExpressProcess();

            return String.Format("http://localhost:{0}", TestSitePort);
        }

        private void EnsureIISExpressProcess()
        {
            if (AlreadyRunningIISExpress())
            {
                return;
            }

            Process oldProcess = Interlocked.CompareExchange(ref _iisExpressProcess, CreateIISExpressProcess(), null);
            if (oldProcess == null)
            {
                _iisExpressProcess.Start();
                return;
            }
        }

        private bool AlreadyRunningIISExpress()
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
            iisExpressProcess.StartInfo = new ProcessStartInfo(IISExpressPath, "/config:\"" + _appHostConfigPath + "\" /site:" + TestSiteName);
            iisExpressProcess.StartInfo.EnvironmentVariables["IIS_USER_HOME"] = _iisHomePath;
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