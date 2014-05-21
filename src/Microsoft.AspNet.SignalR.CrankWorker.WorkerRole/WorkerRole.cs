using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Win32;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.IO;

namespace ClientWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private const string TEST_MANAGER_HOST = "http://perf-demo-mgr.perf-demo.net";

        private const string TCPIP_PARAMETERS = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\Tcpip\Parameters";
        private const string MAX_USER_PORT_VALUE_NAME = "MaxUserPort";
        private const int MAX_USER_PORT_VALUE = 65534;

        public override void Run()
        {
            var path = Path.Combine(Environment.GetEnvironmentVariable("RoleRoot") + @"\", @"approot\Crank\crank.exe");
            new Runner().Run(TEST_MANAGER_HOST, path);
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            var maxUserPort = (int?)Registry.GetValue(TCPIP_PARAMETERS, MAX_USER_PORT_VALUE_NAME, null);

            if (!maxUserPort.HasValue || maxUserPort.Value != MAX_USER_PORT_VALUE)
            {
                Registry.SetValue(TCPIP_PARAMETERS, MAX_USER_PORT_VALUE_NAME, MAX_USER_PORT_VALUE, RegistryValueKind.DWord);

                ProcessStartInfo restartInfo = new ProcessStartInfo();
                restartInfo.FileName = "shutdown.exe";
                restartInfo.Arguments = "-r";
                Process.Start(restartInfo);
                while (true) ;
            }

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
