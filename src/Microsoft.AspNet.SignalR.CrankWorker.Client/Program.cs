using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ClientWorkerRole;

namespace StandAloneWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"Crank\crank.exe";

            if (args.Length > 0)
            {
                var host = args[0];
                new Runner().Run(host, path);
            }
        }
    }
}
