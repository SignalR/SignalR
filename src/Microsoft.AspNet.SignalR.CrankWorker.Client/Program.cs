using Microsoft.AspNet.SignalR.CrankWorker.WorkerRole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.CrankWorker.Client
{
    class Program
    {
        private const string TEST_MANAGER_HOST = "http://localhost:38683/";

        static void Main(string[] args)
        {
            var path = @"crank.exe";

            var host = (args.Length > 0) ? args[0] : TEST_MANAGER_HOST;

            new Runner().Run(host, path);
        }
    }
}
